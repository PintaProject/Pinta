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
using Cairo;
using Gtk;
using Pinta.Core;
using Mono.Unix;

using Pinta.Tools.Brushes;

namespace Pinta.Tools
{
	public class PaintBrushTool : BaseBrushTool
	{
		#region Properties
		public override string Name { get { return Catalog.GetString ("Paintbrush"); } }
		public override string Icon { get { return "Tools.Paintbrush.png"; } }
		public override string StatusBarText { get { return Catalog.GetString ("Left click to draw with primary color, right click to draw with secondary color."); } }

		public override Gdk.Cursor DefaultCursor {
			get {
				int iconOffsetX, iconOffsetY;
				var icon = CreateIconWithShape ("Cursor.Paintbrush.png",
				                                CursorShape.Ellipse, BrushWidth, 8, 24,
				                                out iconOffsetX, out iconOffsetY);
                return new Gdk.Cursor (Gdk.Display.Default, icon, iconOffsetX, iconOffsetY);
			}
		}
		public override bool CursorChangesOnZoom { get { return true; } }

		public override Gdk.Key ShortcutKey { get { return Gdk.Key.B; } }
		public override int Priority { get { return 25; } }
		#endregion

		private BasePaintBrush default_brush;
		private BasePaintBrush active_brush;
		private ToolBarLabel brush_label;
		private ToolBarComboBox brush_combo_box;
		private Color stroke_color;
        private Point last_point;

		protected override void OnActivated ()
		{
			base.OnActivated ();

			PintaCore.PaintBrushes.BrushAdded += HandleBrushAddedOrRemoved;
			PintaCore.PaintBrushes.BrushRemoved += HandleBrushAddedOrRemoved;
		}

		protected override void OnDeactivated (BaseTool newTool)
		{
			base.OnDeactivated (newTool);

			PintaCore.PaintBrushes.BrushAdded -= HandleBrushAddedOrRemoved;
			PintaCore.PaintBrushes.BrushRemoved -= HandleBrushAddedOrRemoved;
		}

		protected override void OnBuildToolBar (Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			// Change the cursor when the BrushWidth is changed.
			brush_width.ComboBox.Changed += (sender, e) => SetCursor (DefaultCursor);

			tb.AppendItem (new Gtk.SeparatorToolItem ());

			if (brush_label == null)
				brush_label = new ToolBarLabel (string.Format (" {0}:  ", Catalog.GetString ("Type")));

			if (brush_combo_box == null) {
				brush_combo_box = new ToolBarComboBox (100, 0, false);
				brush_combo_box.ComboBox.Changed += (o, e) => {
					Gtk.TreeIter iter;
					if (brush_combo_box.ComboBox.GetActiveIter (out iter)) {
						active_brush = (BasePaintBrush)brush_combo_box.Model.GetValue (iter, 1);
					} else {
						active_brush = default_brush;
					}
				};

				RebuildBrushComboBox ();
			}

			tb.AppendItem (brush_label);
			tb.AppendItem (brush_combo_box);
		}

		/// <summary>
		/// Rebuild the list of brushes when a brush is added or removed.
		/// </summary>
		private void HandleBrushAddedOrRemoved (object sender, BrushEventArgs e)
		{
			RebuildBrushComboBox ();
		}

		/// <summary>
		/// Rebuild the list of brushes.
		/// </summary>
		private void RebuildBrushComboBox ()
		{
			brush_combo_box.Model.Clear ();
			default_brush = null;

			foreach (var brush in PintaCore.PaintBrushes) {
				if (default_brush == null)
					default_brush = (BasePaintBrush)brush;
				brush_combo_box.Model.AppendValues (brush.Name, brush);
			}

			brush_combo_box.ComboBox.Active = 0;
		}

		#region Mouse Handlers
		protected override void OnMouseDown (DrawingArea canvas, ButtonPressEventArgs args, PointD point)
		{
			base.OnMouseDown (canvas, args, point);
			active_brush.DoMouseDown ();
		}

		protected override void OnMouseUp (DrawingArea canvas, ButtonReleaseEventArgs args, PointD point)
		{
			base.OnMouseUp (canvas, args, point);
			active_brush.DoMouseUp ();
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			if (mouse_button == 1) {
				stroke_color = PintaCore.Palette.PrimaryColor;
			} else if (mouse_button == 3) {
				stroke_color = PintaCore.Palette.SecondaryColor;
			} else {
				last_point = point_empty;
				return;
			}

			// TODO: also multiply by pressure
			stroke_color = new Color (stroke_color.R, stroke_color.G, stroke_color.B,
				stroke_color.A * active_brush.StrokeAlphaMultiplier);

			int x = (int)point.X;
			int y = (int)point.Y;

			if (last_point.Equals (point_empty))
				last_point = new Point (x, y);

			if (doc.Workspace.PointInCanvas (point))
				surface_modified = true;

			var surf = doc.CurrentUserLayer.Surface;
			var invalidate_rect = Gdk.Rectangle.Zero;
			var brush_width = BrushWidth;

			using (var g = new Context (surf)) {
				g.AppendPath (doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;
				g.LineWidth = brush_width;
				g.LineJoin = LineJoin.Round;
				g.LineCap = BrushWidth == 1 ? LineCap.Butt : LineCap.Round;
				g.SetSourceColor (stroke_color);

                invalidate_rect = active_brush.DoMouseMove (g, stroke_color, surf,
				                                            x, y, last_point.X, last_point.Y);
			}

			// If we draw partially offscreen, Cairo gives us a bogus
			// dirty rectangle, so redraw everything.
			if (doc.Workspace.IsPartiallyOffscreen (invalidate_rect)) {
				doc.Workspace.Invalidate ();
			} else {
				doc.Workspace.Invalidate (doc.ClampToImageSize (invalidate_rect));
			}

			last_point = new Point (x, y);
		}
		#endregion
	}
}
