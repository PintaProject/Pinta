// 
// SelectShapeTool.cs
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
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Tools
{
	public abstract class SelectShapeTool : BaseTool
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
		protected ToolBarLabel fill_label;
		protected ToolBarDropDownButton fill_button;
		protected Gtk.SeparatorToolItem fill_sep;
		protected Rectangle last_dirty;
		protected ImageSurface undo_surface;
		protected bool surface_modified;

		public SelectShapeTool()
		{
		}

		static SelectShapeTool ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("ShapeTool.Outline.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("ShapeTool.Outline.png")));
			fact.Add ("ShapeTool.Fill.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("ShapeTool.Fill.png")));
			fact.Add ("ShapeTool.OutlineFill.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("ShapeTool.OutlineFill.png")));
			fact.AddDefault ();
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
		protected override bool ShowAntialiasingButton { get { return true; } }
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
				brush_width_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("Brush width")));
			
			tb.AppendItem (brush_width_label);
			
			if (brush_width_minus == null) {
				brush_width_minus = new ToolBarButton ("Toolbar.MinusButton.png", "", Catalog.GetString ("Decrease brush size"));
				brush_width_minus.Clicked += MinusButtonClickedEvent;
			}
			
			tb.AppendItem (brush_width_minus);
			
			if (brush_width == null)
				brush_width = new ToolBarComboBox (65, 1, true, "1", "2", "3", "4", "5", "6", "7", "8", "9",
				"10", "11", "12", "13", "14", "15", "20", "25", "30", "35",
				"40", "45", "50", "55");
			
			tb.AppendItem (brush_width);
			
			if (brush_width_plus == null) {
				brush_width_plus = new ToolBarButton ("Toolbar.PlusButton.png", "", Catalog.GetString ("Increase brush size"));
				brush_width_plus.Clicked += PlusButtonClickedEvent;
			}
			
			tb.AppendItem (brush_width_plus);
			
			if (ShowStrokeComboBox) {
				if (fill_sep == null)
					fill_sep = new Gtk.SeparatorToolItem ();

				tb.AppendItem (fill_sep);

				if (fill_label == null)
					fill_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("Fill Style")));

				tb.AppendItem (fill_label);

				if (fill_button == null) {
					fill_button = new ToolBarDropDownButton ();

					fill_button.AddItem (Catalog.GetString ("Outline Shape"), "ShapeTool.Outline.png", 0);
					fill_button.AddItem (Catalog.GetString ("Fill Shape"), "ShapeTool.Fill.png", 1);
					fill_button.AddItem (Catalog.GetString ("Fill and Outline Shape"), "ShapeTool.OutlineFill.png", 2);
				}

				tb.AppendItem (fill_button);
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
			// If we are already drawing, ignore any additional mouse down events
			if (is_drawing)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

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

			doc.ToolLayer.Clear ();
			doc.ToolLayer.Hidden = false;

			surface_modified = false;
			undo_surface = doc.CurrentUserLayer.Surface.Clone ();
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			double x = point.X;
			double y = point.Y;

			current_point = point;
			doc.ToolLayer.Hidden = true;

			DrawShape (Utility.PointsToRectangle (shape_origin, new PointD (x, y), args.Event.IsShiftPressed ()), doc.CurrentUserLayer);
			
			doc.Workspace.Invalidate (last_dirty.ToGdkRectangle ());
			
			is_drawing = false;

			if (surface_modified)
				doc.History.PushNewItem (CreateHistoryItem ());
			else if (undo_surface != null)
				(undo_surface as IDisposable).Dispose ();

			surface_modified = false;
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (!is_drawing)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			current_point = point;
			double x = point.X;
			double y = point.Y;

			doc.ToolLayer.Clear ();

			Rectangle dirty = DrawShape (Utility.PointsToRectangle (shape_origin, new PointD (x, y), args.Event.State.IsShiftPressed()), doc.ToolLayer);

			// Increase the size of the dirty rect to account for antialiasing.
			if (UseAntialiasing) {
				dirty = dirty.Inflate (1, 1);
			}

			dirty = dirty.Clamp ();

			doc.Workspace.Invalidate (last_dirty.ToGdkRectangle ());
			doc.Workspace.Invalidate (dirty.ToGdkRectangle ());
			
			last_dirty = dirty;

			if (doc.Workspace.PointInCanvas (point))
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
			return new SimpleHistoryItem (Icon, Name, undo_surface, PintaCore.Workspace.ActiveDocument.CurrentUserLayerIndex);
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

		protected bool StrokeShape { get { return (int)fill_button.SelectedItem.Tag % 2 == 0; } }
		protected bool FillShape { get { return (int)fill_button.SelectedItem.Tag >= 1; } }
		protected virtual bool ShowStrokeComboBox { get { return true; } }
		#endregion
	}
}
