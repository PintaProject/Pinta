//
// CloneStampTool.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2010 Jonathan Pobst
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gdk;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class CloneStampTool : BaseBrushTool
{
	private bool painting;
	private PointI? origin = null;
	private PointI? offset = null;
	private PointI? last_point = null;

	private readonly SystemManager system_manager;
	public CloneStampTool (IServiceProvider services) : base (services)
	{
		system_manager = services.GetService<SystemManager> ();
	}

	public override string Name => Translations.GetString ("Clone Stamp");
	public override string Icon => Pinta.Resources.Icons.ToolCloneStamp;
	// Translators: {0} is 'Ctrl', or a platform-specific key such as 'Command' on macOS.
	public override string StatusBarText => Translations.GetString ("{0} + left click to set origin, left click to paint.", system_manager.CtrlLabel ());
	public override bool CursorChangesOnZoom => true;
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_L);
	public override int Priority => 47;
	protected override bool ShowAntialiasingButton => true;

	public override Cursor DefaultCursor {
		get {
			var icon = GdkExtensions.CreateIconWithShape ("Cursor.CloneStamp.png",
							CursorShape.Ellipse, BrushWidth, 16, 26,
							out var iconOffsetX, out var iconOffsetY);
			return Gdk.Cursor.NewFromTexture (icon, iconOffsetX, iconOffsetY, null);
		}
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		// We only do stuff with the left mouse button
		if (e.MouseButton != MouseButton.Left)
			return;

		// Ctrl click is set origin, regular click is begin drawing
		if (!e.IsControlPressed) {
			if (!origin.HasValue)
				return;

			painting = true;

			if (!offset.HasValue)
				offset = new (e.Point.X - origin.Value.X, e.Point.Y - origin.Value.Y);

			document.Layers.ToolLayer.Clear ();
			document.Layers.ToolLayer.Hidden = false;

			surface_modified = false;
			undo_surface = document.Layers.CurrentUserLayer.Surface.Clone ();

			// But we also want to stamp this location.
			last_point = e.Point;
			OnMouseMove(document, e);

		} else {
			origin = e.Point;
			offset = null;
		}
	}

	protected override void OnMouseMove(Document document, ToolMouseEventArgs e)
	{
		if (!painting || !offset.HasValue)
			return;

		var x = e.Point.X;
		var y = e.Point.Y;

		if (!last_point.HasValue)
		{
			last_point = e.Point;
			return;
		}

		int radius = BrushWidth / 2;
		float hardness = 0.0f;
		float softnessFactor = 4.0f;

		var toolSurface = document.Layers.ToolLayer.Surface;
		var sourceSurface = document.Layers.CurrentUserLayer.Surface;

		unsafe
		{
			Span<byte> toolData = toolSurface.GetData();
			Span<byte> srcData = sourceSurface.GetData();
			int toolStride = toolSurface.Stride;
			int srcStride = sourceSurface.Stride;

			fixed (byte* toolBase = toolData)
				fixed (byte* srcBase = srcData)
				{
					for (int dy = -radius; dy <= radius; dy++)
					{
						int py = y + dy;
						if (py < 0 || py >= toolSurface.Height) continue;

						for (int dx = -radius; dx <= radius; dx++)
						{
							int px = x + dx;
							if (px < 0 || px >= toolSurface.Width) continue;

							int distSq = dx * dx + dy * dy;
							if (distSq > radius * radius) continue;

							float t = MathF.Sqrt(distSq) / radius;
							t = Math.Clamp(t, 0f, 1f);

							float alphaMask = MathF.Exp(-softnessFactor * t * t);
							alphaMask = MathF.Pow(alphaMask, 1f - hardness);
							alphaMask = Math.Clamp(alphaMask, 0f, 1f);

							int srcX = px - offset.Value.X;
							int srcY = py - offset.Value.Y;
							if (srcX < 0 || srcX >= sourceSurface.Width ||
								srcY < 0 || srcY >= sourceSurface.Height)
								continue;

							byte* dst = toolBase + py * toolStride + px * 4;
							byte* src = srcBase + srcY * srcStride + srcX * 4;

							// Correct premultiplied blending (replace, not add)
							float maskAlpha = (src[3] / 255f) * alphaMask; // 0..1
							float inv = 1f - maskAlpha;

							dst[0] = (byte)(dst[0] * inv + src[0] * maskAlpha);
							dst[1] = (byte)(dst[1] * inv + src[1] * maskAlpha);
							dst[2] = (byte)(dst[2] * inv + src[2] * maskAlpha);
							dst[3] = (byte)(dst[3] * inv + src[3] * maskAlpha);
						}
					}

				toolSurface.MarkDirty();
			}
		}

		last_point = e.Point;
		surface_modified = true;

		int dirtyPadding = BrushWidth + 2;
		RectangleI dirtyRect = RectangleI.FromPoints(last_point.Value, e.Point)
			.Inflated(dirtyPadding, dirtyPadding);

		document.Workspace.Invalidate(dirtyRect);
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		painting = false;

		using Cairo.Context g = new (document.Layers.CurrentUserLayer.Surface);
		g.SetSourceSurface (document.Layers.ToolLayer.Surface, 0, 0);
		g.Paint ();

		base.OnMouseUp (document, e);

		// Note: the offset persists until the clone source is reselected.
		last_point = null;

		document.Layers.ToolLayer.Clear ();
		document.Layers.ToolLayer.Hidden = true;
		document.Workspace.Invalidate ();
	}

	protected override bool OnKeyDown (Document document, ToolKeyEventArgs e)
	{
		// Note that this WON'T work if user presses control key and THEN selects the tool!
		if (e.Key.IsControlKey ()) {
			SetCursor (Gdk.Cursor.NewFromTexture (Resources.GetIcon ("Cursor.CloneStampSetSource.png"), 16, 26, null));
		}

		return false;
	}

	protected override bool OnKeyUp (Document document, ToolKeyEventArgs e)
	{
		if (e.Key.IsControlKey ())
			SetCursor (DefaultCursor);

		return false;
	}

	protected override void OnDeactivated (Document? document, BaseTool? newTool)
	{
		origin = null;
	}
}
