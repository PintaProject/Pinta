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

namespace Pinta.Tools
{
	public class PaintBrushTool : BaseBrushTool
	{
		private readonly IPaintBrushService brushes;

		private BasePaintBrush default_brush;
		private BasePaintBrush active_brush;
		private Color stroke_color;
		private Point last_point;

		private const string BRUSH_SETTING = "paint-brush-brush";

		public PaintBrushTool (IServiceManager services) : base (services)
		{
			brushes = services.GetService<IPaintBrushService> ();

			if (!brushes.Any ())
				throw new InvalidOperationException ("There are no registered paint brushes.");

			default_brush = brushes.First ();
			active_brush = default_brush;

			brushes.BrushAdded += (_, _) => RebuildBrushComboBox ();
			brushes.BrushRemoved += (_, _) => RebuildBrushComboBox ();
		}

		public override string Name => Translations.GetString ("Paintbrush");
		public override string Icon => Pinta.Resources.Icons.ToolPaintBrush;
		public override string StatusBarText => Translations.GetString ("Left click to draw with primary color, right click to draw with secondary color.");
		public override bool CursorChangesOnZoom => true;
		public override Gdk.Key ShortcutKey => Gdk.Key.B;
		public override int Priority => 21;

		public override Gdk.Cursor DefaultCursor {
			get {
				var icon = GdkExtensions.CreateIconWithShape ("Cursor.Paintbrush.png",
								CursorShape.Ellipse, BrushWidth, 8, 24,
								out var iconOffsetX, out var iconOffsetY);

				return new Gdk.Cursor (Gdk.Display.Default, icon, iconOffsetX, iconOffsetY);
			}
		}

		protected override void OnBuildToolBar (Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			tb.AppendItem (Separator);

			tb.AppendItem (BrushLabel);
			tb.AppendItem (BrushComboBox);
		}

		protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
		{
			base.OnMouseDown (document, e);

			active_brush.DoMouseDown ();
		}

		protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
		{
			if (mouse_button == MouseButton.Left) {
				stroke_color = Palette.PrimaryColor;
			} else if (mouse_button == MouseButton.Right) {
				stroke_color = Palette.SecondaryColor;
			} else {
				last_point = point_empty;
				return;
			}

			// TODO: also multiply by pressure
			stroke_color = new Color (stroke_color.R, stroke_color.G, stroke_color.B,
				stroke_color.A * active_brush.StrokeAlphaMultiplier);

			var x = e.Point.X;
			var y = e.Point.Y;

			if (last_point.Equals (point_empty))
				last_point = e.Point;

			if (document.Workspace.PointInCanvas (e.PointDouble))
				surface_modified = true;

			var surf = document.Layers.CurrentUserLayer.Surface;
			var invalidate_rect = Gdk.Rectangle.Zero;
			var brush_width = BrushWidth;

			using (var g = document.CreateClippedContext ()) {
				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;
				g.LineWidth = brush_width;
				g.LineJoin = LineJoin.Round;
				g.LineCap = BrushWidth == 1 ? LineCap.Butt : LineCap.Round;
				g.SetSourceColor (stroke_color);

				invalidate_rect = active_brush.DoMouseMove (g, stroke_color, surf, x, y, last_point.X, last_point.Y);
			}

			// If we draw partially offscreen, Cairo gives us a bogus
			// dirty rectangle, so redraw everything.
			if (document.Workspace.IsPartiallyOffscreen (invalidate_rect))
				document.Workspace.Invalidate ();
			else
				document.Workspace.Invalidate (document.ClampToImageSize (invalidate_rect));

			last_point = e.Point;
		}

		protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
		{
			base.OnMouseUp (document, e);

			active_brush.DoMouseUp ();
		}

		protected override void OnSaveSettings (ISettingsService settings)
		{
			base.OnSaveSettings (settings);

			if (brush_combo_box is not null)
				settings.PutSetting (BRUSH_SETTING, brush_combo_box.ComboBox.Active);
		}

		private ToolBarLabel? brush_label;
		private ToolBarComboBox? brush_combo_box;
		private SeparatorToolItem? separator;

		private SeparatorToolItem Separator => separator ??= new SeparatorToolItem ();
		private ToolBarLabel BrushLabel => brush_label ??= new ToolBarLabel (string.Format (" {0}:  ", Translations.GetString ("Type")));

		private ToolBarComboBox BrushComboBox {
			get {
				if (brush_combo_box is null) {
					brush_combo_box = new ToolBarComboBox (100, 0, false);
					brush_combo_box.ComboBox.Changed += (o, e) => {
						var brush_name = brush_combo_box.ComboBox.ActiveText;
						active_brush = brushes.SingleOrDefault (brush => brush.Name == brush_name) ?? default_brush;
					};

					RebuildBrushComboBox ();

					var brush = Settings.GetSetting (BRUSH_SETTING, 0);

					if (brush < brush_combo_box.ComboBox.GetItemCount ())
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
			if (!brushes.Any ())
				throw new InvalidOperationException ("There are no registered paint brushes.");

			default_brush = brushes.First ();

			BrushComboBox.ComboBox.RemoveAll ();

			foreach (var brush in brushes)
				BrushComboBox.ComboBox.AppendText (brush.Name);

			BrushComboBox.ComboBox.Active = 0;
		}
	}
}
