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

using Gdk;
using Pinta.Core;

namespace Pinta.Tools
{
	public class CloneStampTool : BaseBrushTool
	{
		private bool painting;
		private PointI? origin = null;
		private PointI? offset = null;
		private PointI? last_point = null;

		public CloneStampTool (IServiceManager services) : base (services)
		{
		}

		public override string Name => Translations.GetString ("Clone Stamp");
		public override string Icon => Pinta.Resources.Icons.ToolCloneStamp;
		// Translators: {0} is 'Ctrl', or a platform-specific key such as 'Command' on macOS.
		public override string StatusBarText => Translations.GetString ("{0} + left click to set origin, left click to paint.", GtkExtensions.CtrlLabel ());
		public override bool CursorChangesOnZoom => true;
		public override Key ShortcutKey => Key.L;
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
			} else {
				origin = e.Point;
			}
		}

		protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
		{
			if (!painting || !offset.HasValue)
				return;

			var x = e.Point.X;
			var y = e.Point.Y;

			if (!last_point.HasValue) {
				last_point = e.Point;
				return;
			}

			var g = document.CreateClippedToolContext ();
			g.Antialias = UseAntialiasing ? Cairo.Antialias.Subpixel : Cairo.Antialias.None;

			g.MoveTo (last_point.Value.X, last_point.Value.Y);
			g.LineTo (x, y);

			g.SetSourceSurface (document.Layers.CurrentUserLayer.Surface, offset.Value.X, offset.Value.Y);
			g.LineWidth = BrushWidth;
			g.LineCap = Cairo.LineCap.Round;

			g.Stroke ();

			var dirty_rect = CairoExtensions.GetRectangleFromPoints (last_point.Value, e.Point, BrushWidth + 2);

			last_point = e.Point;
			surface_modified = true;
			document.Workspace.Invalidate (dirty_rect);
		}

		protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
		{
			painting = false;

			var g = new Cairo.Context (document.Layers.CurrentUserLayer.Surface);
			g.SetSourceSurface (document.Layers.ToolLayer.Surface, 0, 0);
			g.Paint ();

			base.OnMouseUp (document, e);

			offset = null;
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
}
