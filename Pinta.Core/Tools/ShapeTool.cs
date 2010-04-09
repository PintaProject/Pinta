// 
// ShapeTool.cs
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

namespace Pinta.Core
{
	public abstract class ShapeTool : BaseTool
	{
		protected bool is_drawing = false;
		protected PointD shape_origin;
		protected PointD current_point;
		protected Color outline_color;
		protected Color fill_color;

		protected ToolBarComboBox brush_width;
		protected ToolBarLabel brush_width_label;
		protected ToolBarButton brush_width_minus;
		protected ToolBarButton brush_width_plus;
		protected ToolBarImage fill_outline_image;
		protected ToolBarComboBox fill_outline;
		protected ToolBarLabel spacer_label;
		protected ToolBarLabel fill_outline_label;

		protected Rectangle last_dirty;
		protected ImageSurface undo_surface;
		protected bool surface_modified;

		public ShapeTool ()
		{
		}

		#region Properties
		protected int BrushWidth {
			get {
				int width;
				if (Int32.TryParse (brush_width.ComboBox.ActiveText, out width)) {
					if (width > 0) {
						(brush_width.ComboBox as Gtk.ComboBoxEntry).Entry.Text = width.ToString ();
						return width;
					}
				}
				(brush_width.ComboBox as Gtk.ComboBoxEntry).Entry.Text = DEFAULT_BRUSH_WIDTH.ToString ();
				return DEFAULT_BRUSH_WIDTH;
			}
			set { (brush_width.ComboBox as Gtk.ComboBoxEntry).Entry.Text = value.ToString (); }
		}
		
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.O; } }
		#endregion
		
		#region ToolBar
		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);
			
			BuildToolBar (tb);
		}

		// Do this in a separate method so SelectTool can override it as 
		// a no-op, but still get the BaseShape.OnBuildToolBar logic.
		protected virtual void BuildToolBar (Gtk.Toolbar tb)
		{
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
			
			if (ShowStrokeComboBox) {
				if (spacer_label == null)
					spacer_label = new ToolBarLabel ("  ");

				tb.AppendItem (spacer_label);
				
				if (fill_outline_image == null)
					fill_outline_image = new ToolBarImage ("ShapeTool.OutlineFill.png");

				tb.AppendItem (fill_outline_image);
				
				if (fill_outline_label == null)
					fill_outline_label = new ToolBarLabel (" : ");

				tb.AppendItem (fill_outline_label);

				if (fill_outline == null)
					fill_outline = new ToolBarComboBox (150, 0, false, "Outline Shape", "Fill Shape", "Fill and Outline Shape");
					
				tb.AppendItem (fill_outline);
			}
		}
		
		protected virtual void MinusButtonClickedEvent (object o, EventArgs args)
		{
			if (BrushWidth > 1)
				BrushWidth--;
		}

		protected virtual void PlusButtonClickedEvent (object o, EventArgs args)
		{
			BrushWidth++;
		}
		#endregion

		#region Mouse Handlers
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			shape_origin = point;
			current_point = point;
			
			is_drawing = true;
			
			if (args.Event.Button == 1) {
				outline_color = PintaCore.Palette.PrimaryColor;
				fill_color = PintaCore.Palette.SecondaryColor;
			} else {
				outline_color = PintaCore.Palette.SecondaryColor;
				fill_color = PintaCore.Palette.PrimaryColor;
			}
			
			PintaCore.Layers.ToolLayer.Clear ();
			PintaCore.Layers.ToolLayer.Hidden = false;

			surface_modified = false;
			undo_surface = PintaCore.Layers.CurrentLayer.Surface.Clone ();
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			double x = point.X;
			double y = point.Y;

			current_point = point;
			PintaCore.Layers.ToolLayer.Hidden = true;
			
			DrawShape (PointsToRectangle (shape_origin, new PointD (x, y), (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask), PintaCore.Layers.CurrentLayer);
			
			Gdk.Rectangle r = GetRectangleFromPoints (shape_origin, new PointD (x, y));
			PintaCore.Workspace.Invalidate (last_dirty.ToGdkRectangle ());
			
			is_drawing = false;

			if (surface_modified)
				PintaCore.History.PushNewItem (CreateHistoryItem ());
			else if (undo_surface != null)
				(undo_surface as IDisposable).Dispose ();

			surface_modified = false;
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (!is_drawing)
				return;

			current_point = point;
			double x = point.X;
			double y = point.Y;
			
			PintaCore.Layers.ToolLayer.Clear ();
			
			Rectangle dirty = DrawShape (PointsToRectangle (shape_origin, new PointD (x, y), (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask), PintaCore.Layers.ToolLayer);
			dirty = dirty.Clamp ();
			
			PintaCore.Workspace.Invalidate (last_dirty.ToGdkRectangle ());
			PintaCore.Workspace.Invalidate (dirty.ToGdkRectangle ());
			
			last_dirty = dirty;

			if (PintaCore.Workspace.PointInCanvas (point))
				surface_modified = true;
		}
		#endregion

		#region Virtual Methods
		protected virtual Rectangle DrawShape (Rectangle r, Layer l)
		{
			return r;
		}
		
		protected virtual BaseHistoryItem CreateHistoryItem ()
		{
			return new SimpleHistoryItem (Icon, Name, undo_surface, PintaCore.Layers.CurrentLayerIndex);
		}
		#endregion

		#region Protected Methods
		protected Gdk.Rectangle GetRectangleFromPoints (PointD a, PointD b)
		{
			int x = (int)Math.Min (a.X, b.X) - BrushWidth - 2;
			int y = (int)Math.Min (a.Y, b.Y) - BrushWidth - 2;
			int w = (int)Math.Max (a.X, b.X) - x + (BrushWidth * 2) + 4;
			int h = (int)Math.Max (a.Y, b.Y) - y + (BrushWidth * 2) + 4;
			
			return new Gdk.Rectangle (x, y, w, h);
		}

		protected Rectangle PointsToRectangle (PointD p1, PointD p2, bool constrain)
		{
			// We want to create a rectangle that always has positive width/height
			double x, y, w, h;
			
			if (p1.Y <= p2.Y) {
				y = p1.Y;
				h = p2.Y - y;
			} else {
				y = p2.Y;
				h = p1.Y - y;
			}
			
			if (p1.X <= p2.X) {
				x = p1.X;
				
				if (constrain)
					w = h;
				else
					w = p2.X - x;
			} else {
				x = p2.X;
				
				if (constrain) {
					w = h;
					x = p1.X - w;
				} else
					w = p1.X - x;
			}

			return new Rectangle (x, y, w, h);
		}
		
		protected bool StrokeShape { get { return fill_outline.ComboBox.Active % 2 == 0; } }
		protected bool FillShape { get { return fill_outline.ComboBox.Active >= 1; } }
		protected virtual bool ShowStrokeComboBox { get { return true; } }
		#endregion
	}
}
