//
// EraserTool.cs
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
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class EraserTool : BaseBrushTool
{
	private enum EraserType
	{
		Normal = 0,
		Smooth = 1,
	}

	private PointI? last_point = null;
	private EraserType eraser_type = EraserType.Normal;

	private const int LUT_Resolution = 256;
	private readonly Lazy<byte[,]> lazy_lut_factor = new (CreateLookupTable);

	public EraserTool (IServiceProvider services) : base (services) { }

	public override string Name => Translations.GetString ("Eraser");
	public override string Icon => Pinta.Resources.Icons.ToolEraser;
	public override string StatusBarText => Translations.GetString ("Left click to erase to transparent, right click to erase to secondary color. ");
	public override bool CursorChangesOnZoom => true;
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_E);
	public override int Priority => 27;

	public override Gdk.Cursor DefaultCursor {
		get {
			var icon = GdkExtensions.CreateIconWithShape (
				"Cursor.Eraser.png",
				CursorShape.Ellipse,
				BrushWidth,
				8,
				22,
				out var iconOffsetX,
				out var iconOffsetY);

			return Gdk.Cursor.NewFromTexture (icon, iconOffsetX, iconOffsetY, null);
		}
	}

	protected override void OnBuildToolBar (Box tb)
	{
		base.OnBuildToolBar (tb);

		tb.Append (TypeLabel);
		tb.Append (TypeComboBox);
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		var new_point = e.Point;
		var new_pointd = e.PointDouble;

		if (mouse_button == MouseButton.None) {
			last_point = null;
			return;
		}

		if (!last_point.HasValue)
			last_point = new_point;

		if (document.Workspace.PointInCanvas (new_pointd))
			surface_modified = true;

		using Context g = document.CreateClippedContext ();
		var last_pointd = (PointD) last_point.Value;
		switch (eraser_type) {
			case EraserType.Normal:
				EraseNormal (g, last_pointd, new_pointd);
				break;
			case EraserType.Smooth:
				EraseSmooth (document.Layers.CurrentUserLayer.Surface, g, last_pointd, new_pointd);
				break;
		}

		int dirtyPadding = BrushWidth + 2;
		RectangleI dirty = RectangleI.FromPoints (last_point.Value, new_point).Inflated (dirtyPadding, dirtyPadding);

		if (document.Workspace.IsPartiallyOffscreen (dirty))
			document.Workspace.Invalidate ();
		else
			document.Workspace.Invalidate (document.ClampToImageSize (dirty));

		last_point = new_point;
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		if (type_combobox is not null)
			settings.PutSetting (SettingNames.ERASER_ERASE_TYPE, type_combobox.ComboBox.Active);
	}

	private static byte[,] CreateLookupTable ()
	{
		var arrayDimensions = LUT_Resolution + 1;
		var result = new byte[arrayDimensions, arrayDimensions];
		for (var dy = 0; dy < arrayDimensions; dy++) {
			for (var dx = 0; dx < arrayDimensions; dx++) {
				var d = Math.Sqrt (dx * dx + dy * dy) / LUT_Resolution;
				result[dy, dx] =
					d > 1.0
					? (byte) 255
					: (byte) (255.0 - Math.Cos (Math.Sqrt (d) * Math.PI / 2.0) * 255.0);
			}
		}
		return result;
	}

	private static ImageSurface CopySurfacePart (ImageSurface surf, RectangleI dest_rect)
	{
		var tmp_surface = CairoExtensions.CreateImageSurface (Format.Argb32, dest_rect.Width, dest_rect.Height);

		using Context g = new (tmp_surface) { Operator = Operator.Source };

		g.SetSourceSurface (surf, -dest_rect.Left, -dest_rect.Top);
		g.Rectangle (new RectangleD (0, 0, dest_rect.Width, dest_rect.Height));
		g.Fill ();

		//Flush to make sure all drawing operations are finished
		tmp_surface.Flush ();

		return tmp_surface;
	}

	private static void PasteSurfacePart (Context g, ImageSurface tmp_surface, RectangleI dest_rect)
	{
		g.Operator = Operator.Source;
		g.SetSourceSurface (tmp_surface, dest_rect.Left, dest_rect.Top);
		g.Rectangle (new RectangleD (dest_rect.Left, dest_rect.Top, dest_rect.Width, dest_rect.Height));
		g.Fill ();
	}

	private void EraseNormal (Context g, PointD start, PointD end)
	{
		g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

		// Adding 0.5 forces cairo into the correct square:
		// See https://bugs.launchpad.net/bugs/672232
		g.MoveTo (start.X + 0.5, start.Y + 0.5);
		g.LineTo (end.X + 0.5, end.Y + 0.5);

		// Right-click is erase to background color, left-click is transparent
		if (mouse_button == MouseButton.Right) {
			g.Operator = Operator.Source;
			g.SetSourceColor (Palette.SecondaryColor);
		} else
			g.Operator = Operator.Clear;

		g.LineWidth = BrushWidth;
		g.LineJoin = LineJoin.Round;
		g.LineCap = LineCap.Round;

		g.Stroke ();
	}

	private void EraseSmooth (ImageSurface surf, Context g, PointD start, PointD end)
	{
		var rad = (int) (BrushWidth / 2.0) + 1;

		// Premultiply with alpha value
		var bk_col_a = (byte) (Palette.SecondaryColor.A * 255.0);
		var bk_col_r = (byte) (Palette.SecondaryColor.R * bk_col_a);
		var bk_col_g = (byte) (Palette.SecondaryColor.G * bk_col_a);
		var bk_col_b = (byte) (Palette.SecondaryColor.B * bk_col_a);
		var num_steps = (int) start.Distance (end) / rad + 1;

		// Initialize lookup table when first used (to prevent slower startup of the application)
		var lut_factor = lazy_lut_factor.Value;

		for (var step = 0; step < num_steps; step++) {
			var pt = Utility.Lerp (start, end, (float) step / num_steps);
			int x = (int) pt.X, y = (int) pt.Y;

			var surface_rect = new RectangleI (0, 0, surf.Width, surf.Height);
			var brush_rect = new RectangleI (x - rad, y - rad, 2 * rad, 2 * rad);
			var dest_rect = RectangleI.Intersect (surface_rect, brush_rect);

			if (dest_rect.Width <= 0 || dest_rect.Height <= 0)
				continue;

			// Allow Clipping through a temporary surface
			var tmp_surface = CopySurfacePart (surf, dest_rect);
			var tmp_data = tmp_surface.GetPixelData ();

			for (var iy = dest_rect.Top; iy < dest_rect.Bottom; iy++) {

				var srcRow = tmp_data[(tmp_surface.Width * (iy - dest_rect.Top))..];
				var dy = Math.Abs ((iy - y) * LUT_Resolution / rad);

				for (var ix = dest_rect.Left; ix < dest_rect.Right; ix++) {

					var dx = Math.Abs ((ix - x) * LUT_Resolution / rad);

					var force = lut_factor[dy, dx];

					// Note: premultiplied alpha is used!
					int idx = ix - dest_rect.Left;

					ColorBgra original = srcRow[idx];

					srcRow[idx] = mouse_button switch {
						MouseButton.Right => ColorBgra.FromBgra (
							b: (byte) ((original.B * force + bk_col_b * (255 - force)) / 255),
							g: (byte) ((original.G * force + bk_col_g * (255 - force)) / 255),
							r: (byte) ((original.R * force + bk_col_r * (255 - force)) / 255),
							a: (byte) ((original.A * force + bk_col_a * (255 - force)) / 255)),
						_ => ColorBgra.FromBgra (
							b: (byte) (original.B * force / 255),
							g: (byte) (original.G * force / 255),
							r: (byte) (original.R * force / 255),
							a: (byte) (original.A * force / 255)),
					};
				}
			}

			// Draw the final result on the surface
			PasteSurfacePart (g, tmp_surface, dest_rect);
		}
	}

	private Label? type_label;
	private ToolBarComboBox? type_combobox;

	private Label TypeLabel => type_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Type")));
	private ToolBarComboBox TypeComboBox {
		get {
			if (type_combobox is null) {
				type_combobox = new ToolBarComboBox (100, 0, false, Translations.GetString ("Normal"), Translations.GetString ("Smooth"));

				type_combobox.ComboBox.OnChanged += (o, e) => {
					eraser_type = (EraserType) type_combobox.ComboBox.Active;
				};

				type_combobox.ComboBox.Active = Settings.GetSetting (SettingNames.ERASER_ERASE_TYPE, 0);
			}

			return type_combobox;
		}
	}
}
