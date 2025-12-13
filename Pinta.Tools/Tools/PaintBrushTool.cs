// 
// PaintBrushTool.cs
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
using System.Linq;
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class PaintBrushTool : BaseBrushTool
{
	private readonly IPaintBrushService brushes;

	private BasePaintBrush? default_brush;
	private BasePaintBrush? active_brush;
	private PointI? last_point = PointI.Zero;
	private uint? open_repeating_draw_id;
	private Box brush_specific_options_box;

	public PaintBrushTool (IServiceProvider services) : base (services)
	{
		brushes = services.GetService<IPaintBrushService> ();

		default_brush = brushes.FirstOrDefault ();
		active_brush = default_brush;

		brushes.BrushAdded += (_, _) => RebuildBrushComboBox ();
		brushes.BrushRemoved += (_, _) => RebuildBrushComboBox ();

		brush_specific_options_box = Box.New (Orientation.Horizontal, 10);
	}

	public override string Name => Translations.GetString ("Paintbrush");
	public override string Icon => Pinta.Resources.Icons.ToolPaintBrush;
	public override string StatusBarText => Translations.GetString ("Left click to draw with primary color, right click to draw with secondary color.");
	public override bool CursorChangesOnZoom => true;
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_B);
	public override int Priority => 21;

	public override Gdk.Cursor DefaultCursor {
		get {
			var icon = GdkExtensions.CreateIconWithShape ("Cursor.Paintbrush.png",
							CursorShape.Ellipse, BrushWidth, 8, 24,
							out var iconOffsetX, out var iconOffsetY);

			return Gdk.Cursor.NewFromTexture (icon, iconOffsetX, iconOffsetY, null);
		}
	}

	protected override void OnBuildToolBar (Box tb)
	{
		base.OnBuildToolBar (tb);

		tb.Append (Separator);

		tb.Append (BrushLabel);
		tb.Append (BrushComboBox);

		RebuildBrushSpecificOptions ();
		tb.Append (Separator);
		brush_specific_options_box.MarginStart = 10;
		tb.Append (brush_specific_options_box);
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		document.Layers.ToolLayer.Clear ();
		document.Layers.ToolLayer.Hidden = false;

		base.OnMouseDown (document, e);

		active_brush?.DoMouseDown ();
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (active_brush is null)
			return;

		if (mouse_button is not (MouseButton.Left or MouseButton.Right)) {
			last_point = null;
			return;
		}

		// TODO: also multiply color by pressure
		Color strokeColor = mouse_button switch {
			MouseButton.Right => new (
				Palette.SecondaryColor.R,
				Palette.SecondaryColor.G,
				Palette.SecondaryColor.B,
				Palette.SecondaryColor.A * active_brush.StrokeAlphaMultiplier
			),
			MouseButton.Left or _ => new (
				Palette.PrimaryColor.R,
				Palette.PrimaryColor.G,
				Palette.PrimaryColor.B,
				Palette.PrimaryColor.A * active_brush.StrokeAlphaMultiplier
			)
		};

		if (!last_point.HasValue)
			last_point = e.Point;

		if (document.Workspace.PointInCanvas (e.PointDouble))
			surface_modified = true;

		var surf = document.Layers.ToolLayer.Surface;
		using Context g = document.CreateClippedToolContext ();

		g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;
		g.LineWidth = BrushWidth;
		g.LineJoin = LineJoin.Round;
		g.LineCap = LineCap.Round;
		g.SetSourceColor (strokeColor);

		BrushStrokeArgs strokeArgs = new (strokeColor, e.Point, last_point.Value);

		CancelRepeatingDraw ();
		var invalidate_rect = active_brush.DoMouseMove (g, surf, strokeArgs);

		// If we draw partially offscreen, Cairo gives us a bogus
		// dirty rectangle, so redraw everything.
		if (document.Workspace.IsPartiallyOffscreen (invalidate_rect))
			document.Workspace.Invalidate ();
		else
			document.Workspace.Invalidate (document.ClampToImageSize (invalidate_rect));

		if (active_brush.MillisecondsBeforeReapply != 0) {
			open_repeating_draw_id = GLib.Functions.TimeoutAdd (GLib.Constants.PRIORITY_DEFAULT, active_brush.MillisecondsBeforeReapply, () => {
				OnMouseMove (document, e);
				return true;
			});
		}
		last_point = e.Point;
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		CancelRepeatingDraw ();
		using Context g = new (document.Layers.CurrentUserLayer.Surface);

		document.Layers.ToolLayer.Draw (g);

		document.Layers.ToolLayer.Hidden = true;

		base.OnMouseUp (document, e);

		active_brush?.DoMouseUp ();
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		if (brush_combo_box is not null)
			settings.PutSetting (SettingNames.PAINT_BRUSH_BRUSH, brush_combo_box.ComboBox.Active);
	}

	private Label? brush_label;
	private ToolBarComboBox? brush_combo_box;
	private Gtk.Separator? separator;

	private Gtk.Separator Separator => separator ??= GtkExtensions.CreateToolBarSeparator ();
	private Label BrushLabel => brush_label ??= Label.New (string.Format (" {0}:  ", Translations.GetString ("Type")));

	private ToolBarComboBox BrushComboBox {
		get {
			if (brush_combo_box is null) {
				brush_combo_box = new ToolBarComboBox (100, 0, false);
				brush_combo_box.ComboBox.OnChanged += (o, e) => {
					var brush_name = brush_combo_box.ComboBox.GetActiveText ();
					active_brush = brushes.SingleOrDefault (brush => brush.Name == brush_name) ?? default_brush;
				};

				RebuildBrushComboBox ();

				var brush = Settings.GetSetting (SettingNames.PAINT_BRUSH_BRUSH, 0);

				if (brush < brush_combo_box.ComboBox.Model.IterNChildren (null))
					brush_combo_box.ComboBox.Active = brush;
			}

			return brush_combo_box;
		}
	}

	/// <summary>
	/// Rebuild the list of brushes.
	/// </summary>
	private void RebuildBrushComboBox ()
	{
		default_brush = brushes.FirstOrDefault ();

		BrushComboBox.ComboBox.RemoveAll ();

		foreach (var brush in brushes)
			BrushComboBox.ComboBox.AppendText (brush.Name);

		BrushComboBox.ComboBox.Active = 0;
		BrushComboBox.ComboBox.OnChanged += (cbx, ev) => RebuildBrushSpecificOptions ();
	}

	private void CancelRepeatingDraw ()
	{
		if (open_repeating_draw_id != null) {
			GLib.Functions.SourceRemove (open_repeating_draw_id.Value);
			open_repeating_draw_id = null;
		}
	}

	private void RebuildBrushSpecificOptions ()
	{
		brush_specific_options_box.RemoveAll ();
		if (active_brush != null) {
			foreach (var option in active_brush.options) {
				brush_specific_options_box.Append (option.GetWidget ());
				brush_specific_options_box.Append (Separator);
			}
		}
	}
}
