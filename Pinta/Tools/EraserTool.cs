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

namespace Pinta
{
	public class EraserTool : BaseTool
	{
		private static Point point_empty = new Point (-500, -500);
		
		private Point last_point = point_empty;
				
		private ToolBarComboBox brush_width;
		private ToolBarLabel brush_width_label;
		private ToolBarButton brush_width_minus;
		private ToolBarButton brush_width_plus;
		
		public EraserTool ()
		{
		}

		#region Properties
		public override string Name { get { return "Eraser"; } }
		public override string Icon { get { return "Tools.Eraser.png"; } }
		public override string StatusBarText { get { return "Click and drag to erase a portion of the image"; } }
		public override bool Enabled { get { return true; } }
		
		private int BrushWidth { 
			get { return int.Parse (brush_width.ComboBox.ActiveText); }
			set { (brush_width.ComboBox as Gtk.ComboBoxEntry).Entry.Text = value.ToString (); }
		}
		#endregion

		#region ToolBar
		protected override void OnBuildToolBar (Toolbar tb)
		{
			base.OnBuildToolBar (tb);
			
			if (brush_width_label == null)
				brush_width_label = new ToolBarLabel (" Brush width: ");
			
			tb.AppendItem (brush_width_label);
	
			if (brush_width_minus == null) {
				brush_width_minus = new ToolBarButton ("Toolbar.MinusButton.png", "", "Decrease brush size");
				brush_width_minus.Clicked += MinusButtonClickedEvent;
			}
			
			tb.AppendItem (brush_width_minus);
		
			if (brush_width == null)
				brush_width = new ToolBarComboBox (50, 1, true, "1", "2", "3", "4", "5", "6", "7", "8", "9",
				"10", "11", "12", "13", "14", "15", "20", "25", "30", "35",
				"40", "45", "50", "55");
			
			tb.AppendItem (brush_width);
			
			if (brush_width_plus == null) {
				brush_width_plus = new ToolBarButton ("Toolbar.PlusButton.png", "", "Increase brush size");
				brush_width_plus.Clicked += PlusButtonClickedEvent;
			}
			
			tb.AppendItem (brush_width_plus);
		}
		
		private void MinusButtonClickedEvent (object o, EventArgs args)
		{
			if (BrushWidth > 1)
				BrushWidth--;
		}
		
		private void PlusButtonClickedEvent (object o, EventArgs args)
		{
			BrushWidth++;
		}
		#endregion

		#region Mouse Handlers
		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if ((args.Event.State & Gdk.ModifierType.Button1Mask) != Gdk.ModifierType.Button1Mask
				&& (args.Event.State & Gdk.ModifierType.Button3Mask) != Gdk.ModifierType.Button3Mask
			) {
				last_point = point_empty;
				return;
			}

			DrawingArea drawingarea1 = (DrawingArea)o;
			
			int x = (int)point.X;
			int y = (int)point.Y;
			
			if (last_point.Equals (point_empty)) {
				last_point = new Point (x, y);
				return;
			}

			ImageSurface surf = PintaCore.Layers.CurrentLayer.Surface;
			
			using (Context g = new Context (surf)) {
				g.Antialias = Antialias.Subpixel;
				
				g.MoveTo (last_point.X, last_point.Y);
				g.LineTo (x, y);
				
				g.Operator = Operator.Clear;
				g.LineWidth = int.Parse (brush_width.ComboBox.ActiveText);
				g.LineJoin = LineJoin.Round;
				g.LineCap = LineCap.Round;
				
				g.Stroke ();
			}
			
			Gdk.Rectangle r = GetRectangleFromPoints (last_point, new Point (x, y));

			PintaCore.Workspace.InvalidateRect (r, true);
			
			last_point = new Point (x, y);
		}
		#endregion

		#region Private Methods
		private Gdk.Rectangle GetRectangleFromPoints (Point a, Point b)
		{
			int x = Math.Min (a.X, b.X) - BrushWidth - 2;
			int y = Math.Min (a.Y, b.Y) - BrushWidth - 2;
			int w = Math.Max (a.X, b.X) - x + (BrushWidth * 2) + 4;
			int h = Math.Max (a.Y, b.Y) - y + (BrushWidth * 2) + 4;
			
			return new Gdk.Rectangle (x, y, w, h);
		}
		#endregion
	}
}
