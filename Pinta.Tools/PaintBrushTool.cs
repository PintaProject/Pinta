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
	[System.ComponentModel.Composition.Export (typeof (BaseTool))]
	public class PaintBrushTool : BaseBrushTool
	{
		#region Properties
		public override string Name { get { return Catalog.GetString ("Paintbrush"); } }
		public override string Icon { get { return "Tools.Paintbrush.png"; } }
		public override string StatusBarText { get { return Catalog.GetString ("Left click to draw with primary color, right click to draw with secondary color."); } }
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.B; } }
		public override int Priority { get { return 25; } }

		public Color StrokeColor { get; private set; }
		public Color FillColor { get; private set; }
		public Point LastPoint { get; private set; }
		public Cairo.Context Drawable { get; private set; }
		#endregion

		private PaintBrush default_brush;
		private PaintBrush active_brush;
		private ToolBarLabel brush_label;
		private ToolBarComboBox brush_combo_box;

		protected override void OnBuildToolBar (Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			if (brush_label == null)
				brush_label = new ToolBarLabel (string.Format (" {0}:  ", Catalog.GetString ("Type")));

			if (brush_combo_box == null) {
				brush_combo_box = new ToolBarComboBox (100, 0, false);
				brush_combo_box.ComboBox.Changed += (o, e) => {
					Gtk.TreeIter iter;
					if (brush_combo_box.ComboBox.GetActiveIter (out iter)) {
						active_brush = (PaintBrush)brush_combo_box.Model.GetValue (iter, 1);
					} else {
						active_brush = default_brush;
					}
				};
				foreach (var brush in PintaCore.PaintBrushes) {
					if (default_brush == null)
						default_brush = (PaintBrush)brush;
					brush_combo_box.Model.AppendValues (brush.Name, brush);
				}
				brush_combo_box.ComboBox.Active = 0;
			}

			tb.AppendItem (brush_label);
			tb.AppendItem (brush_combo_box);
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
			if (mouse_button == 1) {
				StrokeColor = PintaCore.Palette.PrimaryColor;
				FillColor = PintaCore.Palette.SecondaryColor;
			} else if (mouse_button == 3) {
				StrokeColor = PintaCore.Palette.SecondaryColor;
				FillColor = PintaCore.Palette.PrimaryColor;
			} else {
				LastPoint = point_empty;
				return;
			}

			// TODO: also multiply by pressure
			StrokeColor = new Color (StrokeColor.R, StrokeColor.G, StrokeColor.B,
				StrokeColor.A * active_brush.StrokeAlphaMultiplier);

			int x = (int)point.X;
			int y = (int)point.Y;
			
			if (LastPoint.Equals (point_empty))
				LastPoint = new Point (x, y);
			
			if (PintaCore.Workspace.PointInCanvas (point))
				surface_modified = true;

			var surf = PintaCore.Layers.CurrentLayer.Surface;
			var invalidate_rect = Gdk.Rectangle.Zero;
			var brush_width = BrushWidth;

			using (Drawable = new Context (surf)) {
				Drawable.AppendPath (PintaCore.Workspace.ActiveDocument.SelectionPath);
				Drawable.FillRule = FillRule.EvenOdd;
				Drawable.Clip ();

				Drawable.Antialias = Antialias.Subpixel;
				Drawable.LineWidth = brush_width;
				Drawable.LineJoin = LineJoin.Round;
				Drawable.LineCap = BrushWidth == 1 ? LineCap.Butt : LineCap.Round;
				Drawable.Color = StrokeColor;

				Drawable.Translate (brush_width / 2.0, brush_width / 2.0);

				active_brush.Tool = this;
				invalidate_rect = active_brush.DoMouseMove (x, y, LastPoint.X, LastPoint.Y);
				active_brush.Tool = null;
			}

			Drawable = null;

			if (invalidate_rect.IsEmpty) {
				PintaCore.Workspace.Invalidate ();
			} else {
				PintaCore.Workspace.Invalidate (invalidate_rect);
			}

			LastPoint = new Point (x, y);
		}
		#endregion
	}
}
