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
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Tools
{
	public class FreeformShapeTool : BaseBrushTool
	{
		private Point last_point = point_empty;

		protected ToolBarLabel fill_label;
		protected ToolBarDropDownButton fill_button;
		protected Gtk.SeparatorToolItem fill_sep;

		private Path path;
		private Color fill_color;
		private Color outline_color;

		private DashPatternBox dashPBox = new DashPatternBox();

		private string dashPattern = "-";

		public FreeformShapeTool ()
		{
		}

		#region Properties
		public override string Name { get { return Catalog.GetString ("Freeform Shape"); } }
		public override string Icon { get { return "Tools.FreeformShape.png"; } }
		public override string StatusBarText { get { return Catalog.GetString ("Left click to draw with primary color, right click to draw with secondary color."); } }
        public override Gdk.Cursor DefaultCursor { get { return new Gdk.Cursor (Gdk.Display.Default, PintaCore.Resources.GetIcon ("Cursor.FreeformShape.png"), 9, 18); } }
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.O; } }
		public override int Priority { get { return 47; } }
		#endregion

		#region ToolBar
		protected override void OnBuildToolBar (Toolbar tb)
		{
			base.OnBuildToolBar(tb);


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


			Gtk.ComboBox dpbBox = dashPBox.SetupToolbar(tb);

			if (dpbBox != null)
			{
				dpbBox.Changed += (o, e) =>
				{
					dashPattern = dpbBox.ActiveText;
				};
			}
		}
		#endregion

		#region Mouse Handlers
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			surface_modified = false;
			undo_surface = doc.CurrentUserLayer.Surface.Clone ();
			path = null;

			doc.ToolLayer.Clear ();
			doc.ToolLayer.Hidden = false;
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

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

			int x = (int)point.X;
			int y = (int)point.Y;

			if (last_point.Equals (point_empty)) {
				last_point = new Point (x, y);
				return;
			}

			if (doc.Workspace.PointInCanvas (point))
				surface_modified = true;

			doc.ToolLayer.Clear ();
			ImageSurface surf = doc.ToolLayer.Surface;

			using (Context g = new Context (surf)) {
				doc.Selection.Clip(g);

				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				g.SetDash(DashPatternBox.GenerateDashArray(dashPattern, BrushWidth), 0.0);

				if (path != null) {
					g.AppendPath (path);
					(path as IDisposable).Dispose ();
				} else {
					g.MoveTo (x, y);
				}
					
				g.LineTo (x, y);

				path = g.CopyPath ();
				
				g.ClosePath ();
				g.LineWidth = BrushWidth;
				g.FillRule = FillRule.EvenOdd;

				if (FillShape && StrokeShape) {
					g.SetSourceColor (fill_color);
					g.FillPreserve ();
					g.SetSourceColor (outline_color);
					g.Stroke ();
				} else if (FillShape) {
					g.SetSourceColor (outline_color);
					g.FillPreserve();
					g.SetSourceColor (outline_color);
					g.Stroke();
				} else {
					g.SetSourceColor (outline_color);
					g.Stroke ();
				}
			}

			doc.Workspace.Invalidate ();

			last_point = new Point (x, y);
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
            doc.ToolLayer.Clear ();
			doc.ToolLayer.Hidden = true;

			ImageSurface surf = doc.CurrentUserLayer.Surface;
			using (Context g = new Context (surf)) {
				g.AppendPath (doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				g.SetDash(DashPatternBox.GenerateDashArray(dashPattern, BrushWidth), 0.0);

				if (path != null) {
					g.AppendPath (path);
					(path as IDisposable).Dispose ();
					path = null;
				}

				g.ClosePath ();
				g.LineWidth = BrushWidth;
				g.FillRule = FillRule.EvenOdd;

				if (FillShape && StrokeShape) {
					g.SetSourceColor (fill_color);
					g.FillPreserve ();
					g.SetSourceColor (outline_color);
					g.Stroke ();
				} else if (FillShape) {
					g.SetSourceColor (outline_color);
					g.FillPreserve();
					g.SetSourceColor (outline_color);
					g.Stroke();
				} else {
					g.SetSourceColor (outline_color);
					g.Stroke ();
				}
			}

			if (surface_modified)
				PintaCore.History.PushNewItem (new SimpleHistoryItem (Icon, Name, undo_surface, doc.CurrentUserLayerIndex));
			else if (undo_surface != null)
				(undo_surface as IDisposable).Dispose ();

			surface_modified = false;

			doc.Workspace.Invalidate ();
		}
		#endregion

		#region Private Methods
		protected bool StrokeShape { get { return (int)fill_button.SelectedItem.Tag % 2 == 0; } }
		protected bool FillShape { get { return (int)fill_button.SelectedItem.Tag >= 1; } }
		#endregion
	}
}
