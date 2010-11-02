// 
// RoundedRectangleTool.cs
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
	//[System.ComponentModel.Composition.Export (typeof (BaseTool))]
	public class RoundedRectangleTool : ShapeTool
	{		
		protected ToolBarComboBox radius;
		protected ToolBarLabel radius_label;
		protected ToolBarButton radius_minus;
		protected ToolBarButton radius_plus;
		protected Gtk.SeparatorToolItem radius_sep;

		public override string Name {
			get { return Catalog.GetString ("Rounded Rectangle"); }
		}
		public override string Icon {
			get { return "Tools.RoundedRectangle.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString ("Click and drag to draw a rounded rectangle (right click for secondary color). Hold shift to constrain."); }
		}
		public override int Priority { get { return 43; } }

		public double Radius {
			get {
				double rad;
				if (Double.TryParse (radius.ComboBox.ActiveText, out rad))
					if (rad >= 0) {
						(radius.ComboBox as Gtk.ComboBoxEntry).Entry.Text = rad.ToString ();
						return rad;
					} else {
						(radius.ComboBox as Gtk.ComboBoxEntry).Entry.Text = BrushWidth.ToString ();
						return BrushWidth;
					}
				else {
					(radius.ComboBox as Gtk.ComboBoxEntry).Entry.Text = BrushWidth.ToString ();
					return BrushWidth;
				}
			}
			set { (radius.ComboBox as Gtk.ComboBoxEntry).Entry.Text = value.ToString (); }			
		}
		
		protected override void BuildToolBar (Gtk.Toolbar tb)
		{
			base.BuildToolBar (tb);
			
			if (radius_sep == null)
				radius_sep = new Gtk.SeparatorToolItem ();

			tb.AppendItem (radius_sep);

			if (radius_label == null)
				radius_label = new ToolBarLabel (string.Format ("  {0}: ", Catalog.GetString ("Radius")));
			
			tb.AppendItem (radius_label);
			
			if (radius_minus == null) {
				radius_minus = new ToolBarButton ("Toolbar.MinusButton.png", "", Catalog.GetString ("Decrease rectangle's corner radius"));
				radius_minus.Clicked += RadiusMinusButtonClickedEvent;
			}
			
			tb.AppendItem (radius_minus);
			
			if (radius == null)
				radius = new ToolBarComboBox (50, 2, true, "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
				"10", "11", "12", "13", "14", "15", "20", "25", "30", "35",
				"40", "45", "50", "55");
			
			tb.AppendItem (radius);
			
			if (radius_plus == null) {
				radius_plus = new ToolBarButton ("Toolbar.PlusButton.png", "", Catalog.GetString ("Increase rectangle's corner radius"));
				radius_plus.Clicked += RadiusPlusButtonClickedEvent;
			}
			
			tb.AppendItem (radius_plus);
		}

		protected override Rectangle DrawShape (Rectangle rect, Layer l)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			Rectangle dirty;
			
			using (Context g = new Context (l.Surface)) {
				g.AppendPath (doc.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				if (FillShape && StrokeShape)
					dirty = g.FillStrokedRoundedRectangle (rect, Radius, fill_color, outline_color, BrushWidth);
				else if (FillShape)
					dirty = g.FillRoundedRectangle (rect, Radius, outline_color);
				else
					dirty = g.DrawRoundedRectangle (rect, Radius, outline_color, BrushWidth);
			}
			
			return dirty;
		}
		
		private void RadiusMinusButtonClickedEvent (object o, EventArgs args)
		{
			if (Math.Truncate(Radius) > 0)
				Radius = Math.Truncate(Radius) - 1;
		}

		private void RadiusPlusButtonClickedEvent (object o, EventArgs args)
		{
			Radius = Math.Truncate(Radius) + 1;
		}
	}
}
