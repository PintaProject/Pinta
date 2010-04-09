// 
// FreeformShapeTool.cs
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

namespace Pinta.Core
{
	public class FreeformShapeTool : BaseBrushTool
	{

		private Point last_point = point_empty;

		protected ToolBarImage fill_outline_image;
		protected ToolBarComboBox fill_outline;
		protected ToolBarLabel spacer_label;
		protected ToolBarLabel fill_outline_label;

		private Path path;
		private Color fill_color;
		private Color outline_color;

		public FreeformShapeTool ()
		{
		}

		#region Properties
		public override string Name { get { return "Freeform Shape"; } }
		public override string Icon { get { return "Tools.FreeformShape.png"; } }
		public override string StatusBarText { get { return "Left click to draw with primary color, right click to draw with secondary color"; } }
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.O; } }
		#endregion

		#region ToolBar
		protected override void OnBuildToolBar (Toolbar tb)
		{
			base.OnBuildToolBar(tb);

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
		#endregion

		#region Mouse Handlers
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			surface_modified = false;
			undo_surface = PintaCore.Layers.CurrentLayer.Surface.Clone ();
			path = null;

			PintaCore.Layers.ToolLayer.Clear ();
			PintaCore.Layers.ToolLayer.Hidden = false;
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if ((args.Event.State & Gdk.ModifierType.Button1Mask) == Gdk.ModifierType.Button1Mask) {
				outline_color = PintaCore.Palette.PrimaryColor;
				fill_color = PintaCore.Palette.SecondaryColor;
			} else if ((args.Event.State & Gdk.ModifierType.Button3Mask) == Gdk.ModifierType.Button3Mask) {
				outline_color = PintaCore.Palette.SecondaryColor;
				fill_color = PintaCore.Palette.PrimaryColor;
			} else {
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

			if (PintaCore.Workspace.PointInCanvas (point))
				surface_modified = true;

			PintaCore.Layers.ToolLayer.Clear ();
			ImageSurface surf = PintaCore.Layers.ToolLayer.Surface;

			using (Context g = new Context (surf)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.Antialias = Antialias.Subpixel;

				if (path != null) {
					g.AppendPath (path);
					(path as IDisposable).Dispose ();
				}
					
				g.LineTo (x, y);

				path = g.CopyPath ();
				
				g.ClosePath ();
				g.LineWidth = BrushWidth;
				g.LineJoin = LineJoin.Round;
				g.LineCap = LineCap.Round;
				g.FillRule = FillRule.EvenOdd;

				if (FillShape && StrokeShape) {
					g.Color = fill_color;
					g.FillPreserve ();
					g.Color = outline_color;
					g.Stroke ();
				} else if (FillShape) {
					g.Color = outline_color;
					g.Fill ();
				} else {
					g.Color = outline_color;
					g.Stroke ();
				}
			}

			PintaCore.Workspace.Invalidate ();

			last_point = new Point (x, y);
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			PintaCore.Layers.ToolLayer.Hidden = true;

			if (surface_modified)
				PintaCore.History.PushNewItem (new SimpleHistoryItem (Icon, Name, undo_surface, PintaCore.Layers.CurrentLayerIndex));
			else if (undo_surface != null)
				(undo_surface as IDisposable).Dispose ();

			surface_modified = false;
			ImageSurface surf = PintaCore.Layers.CurrentLayer.Surface;

			using (Context g = new Context (surf)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.Antialias = Antialias.Subpixel;

				if (path != null) {
					g.AppendPath (path);
					(path as IDisposable).Dispose ();
					path = null;
				}

				g.ClosePath ();
				g.LineWidth = BrushWidth;
				g.LineJoin = LineJoin.Round;
				g.LineCap = LineCap.Round;
				g.FillRule = FillRule.EvenOdd;

				if (FillShape && StrokeShape) {
					g.Color = fill_color;
					g.FillPreserve ();
					g.Color = outline_color;
					g.Stroke ();
				} else if (FillShape) {
					g.Color = outline_color;
					g.Fill ();
				} else {
					g.Color = outline_color;
					g.Stroke ();
				}
			}

			PintaCore.Workspace.Invalidate ();
		}
		#endregion

		#region Private Methods
		private bool StrokeShape { get { return fill_outline.ComboBox.Active % 2 == 0; } }
		private bool FillShape { get { return fill_outline.ComboBox.Active >= 1; } }
		#endregion
	}
}
