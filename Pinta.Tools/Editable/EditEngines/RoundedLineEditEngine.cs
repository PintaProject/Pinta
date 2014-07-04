// 
// RoundedLineEditEngine.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2014 Andrew Davis, GSoC 2014
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Unix;

namespace Pinta.Tools
{
    public class RoundedLineEditEngine: BaseEditEngine
    {
		public const double DefaultRadius = 20d;

		protected ToolBarComboBox radius;
		protected ToolBarLabel radius_label;
		protected ToolBarButton radius_minus;
		protected ToolBarButton radius_plus;
		protected Gtk.SeparatorToolItem radius_sep;

		public double Radius
		{
			get
			{
				double rad;

				if (Double.TryParse(radius.ComboBox.ActiveText, out rad))
				{
					if (rad >= 0)
					{
						(radius.ComboBox as Gtk.ComboBoxEntry).Entry.Text = rad.ToString();

						return rad;
					}
					else
					{
						(radius.ComboBox as Gtk.ComboBoxEntry).Entry.Text = BrushWidth.ToString();

						return BrushWidth;
					}
				}
				else
				{
					(radius.ComboBox as Gtk.ComboBoxEntry).Entry.Text = BrushWidth.ToString();

					return BrushWidth;
				}
			}

			set
			{
				(radius.ComboBox as Gtk.ComboBoxEntry).Entry.Text = value.ToString();

				RoundedLineEngine selEngine = (RoundedLineEngine)SelectedShapeEngine;

				if (selEngine != null)
				{
					selEngine.Radius = Radius;

					DrawShapes(false, false, true, false);
				}
			}
		}


		public RoundedLineEditEngine(BaseTool passedOwner): base(passedOwner)
        {

        }

		protected override void createShape(bool ctrlKey, bool clickedOnControlPoint, ShapeEngine activeEngine, PointD prevSelPoint)
		{
			addRectanglePoints(ctrlKey, clickedOnControlPoint, activeEngine, prevSelPoint);


			//Set the new shape's DashPattern option to be the same as the previous shape's.
			activeEngine.DashPattern = dashPBox.comboBox.ComboBox.ActiveText;


			base.createShape(ctrlKey, clickedOnControlPoint, activeEngine, prevSelPoint);
		}

		protected override void movePoint(List<ControlPoint> controlPoints)
		{
			moveRectangularPoint(controlPoints);


			base.movePoint(controlPoints);
		}

		protected override void addShape()
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			SEngines.Add(new RoundedLineEngine(doc.CurrentUserLayer, Radius, owner.UseAntialiasing));

			base.addShape();
		}


		public override void HandleBuildToolBar(Gtk.Toolbar tb)
		{
			base.HandleBuildToolBar(tb);


			if (radius_sep == null)
				radius_sep = new Gtk.SeparatorToolItem();

			tb.AppendItem(radius_sep);

			if (radius_label == null)
				radius_label = new ToolBarLabel(string.Format("  {0}: ", Catalog.GetString("Radius")));

			tb.AppendItem(radius_label);

			if (radius_minus == null)
			{
				radius_minus = new ToolBarButton("Toolbar.MinusButton.png", "", Catalog.GetString("Decrease shape's corner radius"));
				radius_minus.Clicked += RadiusMinusButtonClickedEvent;
			}

			tb.AppendItem(radius_minus);

			if (radius == null)
			{
				radius = new ToolBarComboBox(65, 16, true, "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
				"10", "11", "12", "13", "14", "15", "20", "25", "30", "40",
				"50", "60", "70", "80");

				radius.ComboBox.Changed += (o, e) =>
				{
					//Go through the Get/Set routine.
					Radius = Radius;
				};
			}

			tb.AppendItem(radius);

			if (radius_plus == null)
			{
				radius_plus = new ToolBarButton("Toolbar.PlusButton.png", "", Catalog.GetString("Increase shape's corner radius"));
				radius_plus.Clicked += RadiusPlusButtonClickedEvent;
			}

			tb.AppendItem(radius_plus);
		}

		private void RadiusMinusButtonClickedEvent(object o, EventArgs args)
		{
			if (Math.Truncate(Radius) > 0)
			{
				Radius = Math.Truncate(Radius) - 1;
			}
		}

		private void RadiusPlusButtonClickedEvent(object o, EventArgs args)
		{
			Radius = Math.Truncate(Radius) + 1;
		}
    }
}
