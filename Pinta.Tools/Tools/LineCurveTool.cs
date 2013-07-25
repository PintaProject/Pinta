// 
// LineCurveTool.cs
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
using System.Collections.Generic;
using System.Linq;

namespace Pinta.Tools
{
	public class LineCurveTool : ShapeTool
	{
		private static readonly Color hoverColor =
			new Color(ToolControl.FillColor.R / 2d, ToolControl.FillColor.G / 2d, ToolControl.FillColor.B / 2d, ToolControl.FillColor.A / 3d);

		private const double CurveClickRange = 12d;
		private const double DefaultEndPointTension = 0d;
		private const double DefaultMidPointTension = 1d / 3d;

		//Must be an integer.
		public const int SectionSize = 15;

		//For optimization.
		public const double SectionSizeDouble = SectionSize;

		//Don't change this; it's automatically calculated.
		public static readonly int BorderingSectionRange = (int)Math.Ceiling(CurveClickRange / SectionSizeDouble);


		public static int selectedPointIndex = -1;
		public static int selectedPointCurveIndex = 0;

		/// <summary>
		/// The selected ControlPoint.
		/// </summary>
		private ControlPoint SelectedPoint
		{
			get
			{
				CurveEngine selEngine = SelectedCurveEngine;

				if (selEngine != null)
				{
					return selEngine.ControlPoints[selectedPointIndex];
				}
				else
				{
					return null;
				}
			}

			set
			{
				CurveEngine selEngine = SelectedCurveEngine;

				if (selEngine != null)
				{
					selEngine.ControlPoints[selectedPointIndex] = value;
				}
			}
		}

		/// <summary>
		/// The selected curve's CurveEngine.
		/// </summary>
		private CurveEngine SelectedCurveEngine
		{
			get
			{
				if (selectedPointIndex > -1 && cEngines.CEL.Count > selectedPointCurveIndex)
				{
					return cEngines.CEL[selectedPointCurveIndex];
				}
				else
				{
					return null;
				}
			}
		}

		private PointD hoverPoint = new PointD(-1d, -1d);
		private int hoveredPointAsControlPoint = -1;

		private bool changingTension = false;
		private PointD lastMousePosition = new PointD(0d, 0d);


		//Helps to keep track of the first modification on a curve after the mouse is clicked, to prevent unnecessary history items.
		private bool clickedWithoutModifying = false;


		//Stores the editable curve data.
		public static CurveEngineCollection cEngines = new CurveEngineCollection();


		public override string Name
		{
			get { return Catalog.GetString("Line/Curve"); }
		}
		public override string Icon {
			get { return "Tools.Line.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString ("Left click to draw a line with primary color." +
					"\nLeft click on a line to add curve control points." +
					"\nLeft click on a control point and drag to move it." +
					"\nRight click on a control point and drag to change tension." +
					"\nHold Shift to snap to angles." +
					"\nUse arrow keys to move selected control point." +
					"\nPress Ctrl + left/right arrows to navigate through (select) control points by order." +
					"\nPress Delete to delete selected control point." +
					"\nPress Space to create a new point on the outermost side of the selected control point at the mouse position." +
					"\nHold Ctrl while pressing Space to create the point at the exact same position." +
					"\nHold Ctrl while left clicking on a control point to create a new line at the exact same position." +
					"\nPress Enter to finalize the curve.");
			}
		}
		public override Gdk.Cursor DefaultCursor {
			get { return new Gdk.Cursor (PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon ("Cursor.Line.png"), 9, 18); }
		}
		protected override bool ShowStrokeComboBox {
			get { return false; }
		}
		public override int Priority {
			get { return 39; }
		}


		private Gtk.SeparatorToolItem arrowSep;
		private Gtk.CheckButton showArrowOneBox, showArrowTwoBox;
		private bool showOtherArrowOptions;

		private ToolBarComboBox arrowSize;
		private ToolBarLabel arrowSizeLabel;
		private ToolBarButton arrowSizeMinus, arrowSizePlus;

		private ToolBarComboBox arrowAngleOffset;
		private ToolBarLabel arrowAngleOffsetLabel;
		private ToolBarButton arrowAngleOffsetMinus, arrowAngleOffsetPlus;

		private ToolBarComboBox arrowLengthOffset;
		private ToolBarLabel arrowLengthOffsetLabel;
		private ToolBarButton arrowLengthOffsetMinus, arrowLengthOffsetPlus;


		protected override void BuildToolBar(Gtk.Toolbar tb)
		{
			base.BuildToolBar(tb);


			#region Show Arrows

			//Arrow separator.

			if (arrowSep == null)
			{
				arrowSep = new Gtk.SeparatorToolItem();

				showOtherArrowOptions = false;
			}

			tb.AppendItem(arrowSep);


			//Show arrow 1.

			showArrowOneBox = new Gtk.CheckButton(Catalog.GetString("Arrow 1"));

			showArrowOneBox.Toggled += (o, e) =>
			{
				//Determine whether to change the visibility of Arrow options in the toolbar based on the updated Arrow showing/hiding.
				if (!showArrowOneBox.Active && !showArrowTwoBox.Active)
				{
					if (showOtherArrowOptions)
					{
						tb.Remove(arrowSizeLabel);
						tb.Remove(arrowSizeMinus);
						tb.Remove(arrowSize);
						tb.Remove(arrowSizePlus);
						tb.Remove(arrowAngleOffsetLabel);
						tb.Remove(arrowAngleOffsetMinus);
						tb.Remove(arrowAngleOffset);
						tb.Remove(arrowAngleOffsetPlus);
						tb.Remove(arrowLengthOffsetLabel);
						tb.Remove(arrowLengthOffsetMinus);
						tb.Remove(arrowLengthOffset);
						tb.Remove(arrowLengthOffsetPlus);

						showOtherArrowOptions = false;
					}
				}
				else
				{
					if (!showOtherArrowOptions)
					{
						tb.Add(arrowSizeLabel);
						tb.Add(arrowSizeMinus);
						tb.Add(arrowSize);
						tb.Add(arrowSizePlus);
						tb.Add(arrowAngleOffsetLabel);
						tb.Add(arrowAngleOffsetMinus);
						tb.Add(arrowAngleOffset);
						tb.Add(arrowAngleOffsetPlus);
						tb.Add(arrowLengthOffsetLabel);
						tb.Add(arrowLengthOffsetMinus);
						tb.Add(arrowLengthOffset);
						tb.Add(arrowLengthOffsetPlus);

						showOtherArrowOptions = true;
					}
				}

				CurveEngine selEngine = SelectedCurveEngine;

				if (selEngine != null)
				{
					selEngine.Arrow1.Show = showArrowOneBox.Active;

					drawCurves(false, false, false);
				}
			};

			tb.AddWidgetItem(showArrowOneBox);


			//Show arrow 2.

			showArrowTwoBox = new Gtk.CheckButton(Catalog.GetString("Arrow 2"));

			showArrowTwoBox.Toggled += (o, e) =>
			{
				//Determine whether to change the visibility of Arrow options in the toolbar based on the updated Arrow showing/hiding.
				if (!showArrowOneBox.Active && !showArrowTwoBox.Active)
				{
					if (showOtherArrowOptions)
					{
						tb.Remove(arrowSizeLabel);
						tb.Remove(arrowSizeMinus);
						tb.Remove(arrowSize);
						tb.Remove(arrowSizePlus);
						tb.Remove(arrowAngleOffsetLabel);
						tb.Remove(arrowAngleOffsetMinus);
						tb.Remove(arrowAngleOffset);
						tb.Remove(arrowAngleOffsetPlus);
						tb.Remove(arrowLengthOffsetLabel);
						tb.Remove(arrowLengthOffsetMinus);
						tb.Remove(arrowLengthOffset);
						tb.Remove(arrowLengthOffsetPlus);

						showOtherArrowOptions = false;
					}
				}
				else
				{
					if (!showOtherArrowOptions)
					{
						tb.Add(arrowSizeLabel);
						tb.Add(arrowSizeMinus);
						tb.Add(arrowSize);
						tb.Add(arrowSizePlus);
						tb.Add(arrowAngleOffsetLabel);
						tb.Add(arrowAngleOffsetMinus);
						tb.Add(arrowAngleOffset);
						tb.Add(arrowAngleOffsetPlus);
						tb.Add(arrowLengthOffsetLabel);
						tb.Add(arrowLengthOffsetMinus);
						tb.Add(arrowLengthOffset);
						tb.Add(arrowLengthOffsetPlus);

						showOtherArrowOptions = true;
					}
				}

				CurveEngine selEngine = SelectedCurveEngine;

				if (selEngine != null)
				{
					selEngine.Arrow2.Show = showArrowTwoBox.Active;

					drawCurves(false, false, false);
				}
			};

			tb.AddWidgetItem(showArrowTwoBox);

			#endregion Show Arrows


			#region Arrow Size

			if (arrowSizeLabel == null)
			{
				arrowSizeLabel = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Size")));
			}

			if (arrowSizeMinus == null)
			{
				arrowSizeMinus = new ToolBarButton("Toolbar.MinusButton.png", "", Catalog.GetString("Decrease arrow size"));
				arrowSizeMinus.Clicked += new EventHandler(arrowSizeMinus_Clicked);
			}

			if (arrowSize == null)
			{
				arrowSize = new ToolBarComboBox(65, 7, true,
					"3", "4", "5", "6", "7", "8", "9", "10", "12", "15", "18",
					"20", "25", "30", "40", "50", "60", "70", "80", "90", "100");

				arrowSize.ComboBox.Changed += (o, e) =>
				{
					if (arrowSize.ComboBox.ActiveText.Length < 1)
					{
						//Ignore the change until the user enters something.
						return;
					}
					else
					{
						double newSize = 10d;

						if (arrowSize.ComboBox.ActiveText == "-")
						{
							//The user is trying to enter a negative value: change it to 1.
							newSize = 1d;
						}
						else
						{
							if (Double.TryParse(arrowSize.ComboBox.ActiveText, out newSize))
							{
								if (newSize < 1d)
								{
									//Less than 1: change it to 1.
									newSize = 1d;
								}
								else if (newSize > 100d)
								{
									//Greater than 100: change it to 100.
									newSize = 100d;
								}
							}
							else
							{
								//Not a number: wait until the user enters something.
								return;
							}
						}

						(arrowSize.ComboBox as Gtk.ComboBoxEntry).Entry.Text = newSize.ToString();

						CurveEngine selEngine = SelectedCurveEngine;

						if (selEngine != null)
						{
							selEngine.Arrow1.ArrowSize = newSize;
							selEngine.Arrow2.ArrowSize = newSize;

							drawCurves(false, false, false);
						}
					}
				};
			}

			if (arrowSizePlus == null)
			{
				arrowSizePlus = new ToolBarButton("Toolbar.PlusButton.png", "", Catalog.GetString("Increase arrow size"));
				arrowSizePlus.Clicked += new EventHandler(arrowSizePlus_Clicked);
			}

			#endregion Arrow Size


			#region Angle Offset

			if (arrowAngleOffsetLabel == null)
			{
				arrowAngleOffsetLabel = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Angle")));
			}

			if (arrowAngleOffsetMinus == null)
			{
				arrowAngleOffsetMinus = new ToolBarButton("Toolbar.MinusButton.png", "", Catalog.GetString("Decrease angle offset"));
				arrowAngleOffsetMinus.Clicked += new EventHandler(arrowAngleOffsetMinus_Clicked);
			}

			if (arrowAngleOffset == null)
			{
				arrowAngleOffset = new ToolBarComboBox(65, 9, true,
					"-30", "-25", "-20", "-15", "-10", "-5", "0", "5", "10", "15", "20", "25", "30");

				arrowAngleOffset.ComboBox.Changed += (o, e) =>
				{
					if (arrowAngleOffset.ComboBox.ActiveText.Length < 1)
					{
						//Ignore the change until the user enters something.
						return;
					}
					else if (arrowAngleOffset.ComboBox.ActiveText == "-")
					{
						//The user is trying to enter a negative value: ignore the change until the user enters more.
						return;
					}
					else
					{
						double newAngle = 15d;

						if (Double.TryParse(arrowAngleOffset.ComboBox.ActiveText, out newAngle))
						{
							if (newAngle < -89d)
							{
								//Less than -89: change it to -89.
								newAngle = -89d;
							}
							else if (newAngle > 89d)
							{
								//Greater than 89: change it to 89.
								newAngle = 89d;
							}
						}
						else
						{
							//Not a number: wait until the user enters something.
							return;
						}

						(arrowAngleOffset.ComboBox as Gtk.ComboBoxEntry).Entry.Text = newAngle.ToString();

						CurveEngine selEngine = SelectedCurveEngine;

						if (selEngine != null)
						{
							selEngine.Arrow1.AngleOffset = newAngle;
							selEngine.Arrow2.AngleOffset = newAngle;

							drawCurves(false, false, false);
						}
					}
				};
			}

			if (arrowAngleOffsetPlus == null)
			{
				arrowAngleOffsetPlus = new ToolBarButton("Toolbar.PlusButton.png", "", Catalog.GetString("Increase angle offset"));
				arrowAngleOffsetPlus.Clicked += new EventHandler(arrowAngleOffsetPlus_Clicked);
			}

			#endregion Angle Offset


			#region Length Offset

			if (arrowLengthOffsetLabel == null)
			{
				arrowLengthOffsetLabel = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Length")));
			}

			if (arrowLengthOffsetMinus == null)
			{
				arrowLengthOffsetMinus = new ToolBarButton("Toolbar.MinusButton.png", "", Catalog.GetString("Decrease length offset"));
				arrowLengthOffsetMinus.Clicked += new EventHandler(arrowLengthOffsetMinus_Clicked);
			}

			if (arrowLengthOffset == null)
			{
				arrowLengthOffset = new ToolBarComboBox(65, 8, true,
					"-30", "-25", "-20", "-15", "-10", "-5", "0", "5", "10", "15", "20", "25", "30");

				arrowLengthOffset.ComboBox.Changed += (o, e) =>
				{
					if (arrowLengthOffset.ComboBox.ActiveText.Length < 1)
					{
						//Ignore the change until the user enters something.
						return;
					}
					else if (arrowLengthOffset.ComboBox.ActiveText == "-")
					{
						//The user is trying to enter a negative value: ignore the change until the user enters more.
						return;
					}
					else
					{
						double newLength = 10d;

						if (Double.TryParse(arrowLengthOffset.ComboBox.ActiveText, out newLength))
						{
							if (newLength < -100d)
							{
								//Less than -100: change it to -100.
								newLength = -100d;
							}
							else if (newLength > 100d)
							{
								//Greater than 100: change it to 100.
								newLength = 100d;
							}
						}
						else
						{
							//Not a number: wait until the user enters something.
							return;
						}

						(arrowLengthOffset.ComboBox as Gtk.ComboBoxEntry).Entry.Text = newLength.ToString();

						CurveEngine selEngine = SelectedCurveEngine;

						if (selEngine != null)
						{
							selEngine.Arrow1.LengthOffset = newLength;
							selEngine.Arrow2.LengthOffset = newLength;

							drawCurves(false, false, false);
						}
					}
				};
			}

			if (arrowLengthOffsetPlus == null)
			{
				arrowLengthOffsetPlus = new ToolBarButton("Toolbar.PlusButton.png", "", Catalog.GetString("Increase length offset"));
				arrowLengthOffsetPlus.Clicked += new EventHandler(arrowLengthOffsetPlus_Clicked);
			}

			#endregion Length Offset

			if (showOtherArrowOptions)
			{
				tb.Add(arrowSizeLabel);
				tb.Add(arrowSizeMinus);
				tb.Add(arrowSize);
				tb.Add(arrowSizePlus);
				tb.Add(arrowAngleOffsetLabel);
				tb.Add(arrowAngleOffsetMinus);
				tb.Add(arrowAngleOffset);
				tb.Add(arrowAngleOffsetPlus);
				tb.Add(arrowLengthOffsetLabel);
				tb.Add(arrowLengthOffsetMinus);
				tb.Add(arrowLengthOffset);
				tb.Add(arrowLengthOffsetPlus);
			}
		}

		/// <summary>
		/// Set the Arrow options for the current curve to their respective values in the toolbar.
		/// </summary>
		private void setArrowOptions()
		{
			CurveEngine selEngine = SelectedCurveEngine;

			if (selEngine != null)
			{
				selEngine.Arrow1.Show = showArrowOneBox.Active;
				selEngine.Arrow2.Show = showArrowTwoBox.Active;

				showOtherArrowOptions = showArrowOneBox.Active && showArrowTwoBox.Active;

				if (showOtherArrowOptions)
				{
					Double.TryParse((arrowSize.ComboBox as Gtk.ComboBoxEntry).Entry.Text, out selEngine.Arrow1.ArrowSize);
					Double.TryParse((arrowAngleOffset.ComboBox as Gtk.ComboBoxEntry).Entry.Text, out selEngine.Arrow1.AngleOffset);
					Double.TryParse((arrowLengthOffset.ComboBox as Gtk.ComboBoxEntry).Entry.Text, out selEngine.Arrow1.LengthOffset);

					selEngine.Arrow1.ArrowSize = Utility.Clamp(selEngine.Arrow1.ArrowSize, 1d, 100d);
					selEngine.Arrow2.ArrowSize = selEngine.Arrow1.ArrowSize;
					selEngine.Arrow1.AngleOffset = Utility.Clamp(selEngine.Arrow1.AngleOffset, -89d, 89d);
					selEngine.Arrow2.AngleOffset = selEngine.Arrow1.AngleOffset;
					selEngine.Arrow1.LengthOffset = Utility.Clamp(selEngine.Arrow1.LengthOffset, -100d, 100d);
					selEngine.Arrow2.LengthOffset = selEngine.Arrow1.LengthOffset;
				}
			}
		}

		/// <summary>
		/// Set the Arrow options in the toolbar to their respective values for the current curve.
		/// </summary>
		private void setToolbarArrowOptions()
		{
			CurveEngine selEngine = SelectedCurveEngine;

			if (selEngine != null)
			{
				showArrowOneBox.Active = selEngine.Arrow1.Show;
				showArrowTwoBox.Active = selEngine.Arrow2.Show;

				if (showOtherArrowOptions)
				{
					(arrowSize.ComboBox as Gtk.ComboBoxEntry).Entry.Text = selEngine.Arrow1.ArrowSize.ToString();
					(arrowAngleOffset.ComboBox as Gtk.ComboBoxEntry).Entry.Text = selEngine.Arrow1.AngleOffset.ToString();
					(arrowLengthOffset.ComboBox as Gtk.ComboBoxEntry).Entry.Text = selEngine.Arrow1.LengthOffset.ToString();
				}
			}
		}


		#region ToolbarEventHandlers

		void arrowSizeMinus_Clicked(object sender, EventArgs e)
		{
			double newSize = 10d;

			if (Double.TryParse(arrowSize.ComboBox.ActiveText, out newSize))
			{
				--newSize;

				if (newSize < 1d)
				{
					newSize = 1d;
				}
			}
			else
			{
				newSize = 10d;
			}

			(arrowSize.ComboBox as Gtk.ComboBoxEntry).Entry.Text = newSize.ToString();
		}

		void arrowSizePlus_Clicked(object sender, EventArgs e)
		{
			double newSize = 10d;

			if (Double.TryParse(arrowSize.ComboBox.ActiveText, out newSize))
			{
				++newSize;

				if (newSize > 100d)
				{
					newSize = 100d;
				}
			}
			else
			{
				newSize = 10d;
			}

			(arrowSize.ComboBox as Gtk.ComboBoxEntry).Entry.Text = newSize.ToString();
		}

		void arrowAngleOffsetMinus_Clicked(object sender, EventArgs e)
		{
			double newAngle = 0d;

			if (Double.TryParse(arrowAngleOffset.ComboBox.ActiveText, out newAngle))
			{
				--newAngle;

				if (newAngle < -89d)
				{
					newAngle = -89d;
				}
			}
			else
			{
				newAngle = 0d;
			}

			(arrowAngleOffset.ComboBox as Gtk.ComboBoxEntry).Entry.Text = newAngle.ToString();
		}

		void arrowAngleOffsetPlus_Clicked(object sender, EventArgs e)
		{
			double newAngle = 0d;

			if (Double.TryParse(arrowAngleOffset.ComboBox.ActiveText, out newAngle))
			{
				++newAngle;

				if (newAngle > 89d)
				{
					newAngle = 89d;
				}
			}
			else
			{
				newAngle = 0d;
			}

			(arrowAngleOffset.ComboBox as Gtk.ComboBoxEntry).Entry.Text = newAngle.ToString();
		}

		void arrowLengthOffsetMinus_Clicked(object sender, EventArgs e)
		{
			double newLength = 10d;

			if (Double.TryParse(arrowLengthOffset.ComboBox.ActiveText, out newLength))
			{
				--newLength;

				if (newLength < -100d)
				{
					newLength = -100d;
				}
			}
			else
			{
				newLength = 10d;
			}

			(arrowLengthOffset.ComboBox as Gtk.ComboBoxEntry).Entry.Text = newLength.ToString();
		}

		void arrowLengthOffsetPlus_Clicked(object sender, EventArgs e)
		{
			double newLength = 10d;

			if (Double.TryParse(arrowLengthOffset.ComboBox.ActiveText, out newLength))
			{
				++newLength;

				if (newLength > 100d)
				{
					newLength = 100d;
				}
			}
			else
			{
				newLength = 10d;
			}

			(arrowLengthOffset.ComboBox as Gtk.ComboBoxEntry).Entry.Text = newLength.ToString();
		}

		#endregion ToolbarEventHandlers


		protected override Rectangle DrawShape (Rectangle rect, Layer l, bool drawControlPoints)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			Rectangle? dirty = null;

			using (Context g = new Context(l.Surface))
			{
				g.AppendPath(doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip();

				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				g.SetDash(new double[] { 5.0, 20.0, 1.0, 30.0 }, 0.0);

				g.LineWidth = BrushWidth;

				//Draw the curves.
				for (int n = 0; n < cEngines.CEL.Count; ++n)
				{
					List<ControlPoint> controlPoints = cEngines.CEL[n].ControlPoints;

					if (controlPoints.Count > 0)
					{
						//Generate the points that make up the curve.
						cEngines.CEL[n].GenerateCardinalSplinePolynomialCurvePoints(n);

						//Expand the invalidation rectangle as necessary.
						dirty = dirty.UnionRectangles(g.DrawPolygonal(cEngines.CEL[n].GeneratedPoints, outline_color));
					}
				}

				g.SetDash(new double[] {}, 0.0);

				//Draw the arrows for all of the curves.
				for (int n = 0; n < cEngines.CEL.Count; ++n)
				{
					PointD[] genPoints = cEngines.CEL[n].GeneratedPoints;

					//For each curve currently being drawn/edited by the user.
					for (int i = 0; i < cEngines.CEL[n].ControlPoints.Count; ++i)
					{
						if (cEngines.CEL[n].Arrow1.Show)
						{
							if (genPoints.Length > 1)
							{
								dirty = dirty.UnionRectangles(cEngines.CEL[n].Arrow1.Draw(g, outline_color, genPoints[0], genPoints[1]));
							}
						}

						if (cEngines.CEL[n].Arrow2.Show)
						{
							if (genPoints.Length > 1)
							{
								dirty = dirty.UnionRectangles(cEngines.CEL[n].Arrow1.Draw(g, outline_color,
									genPoints[genPoints.Length - 1], genPoints[genPoints.Length - 2]));
							}
						}
					}
				}

				if (drawControlPoints)
				{
					//Draw the control points for all of the curves.

					int controlPointSize = BrushWidth + 1;
					double controlPointOffset = (double)controlPointSize / 2d;

					if (selectedPointIndex > -1)
					{
						//Draw a ring around the selected point.
						g.FillStrokedEllipse(
							new Rectangle(
								SelectedCurveEngine.ControlPoints[selectedPointIndex].Position.X - controlPointOffset * 4d,
								SelectedCurveEngine.ControlPoints[selectedPointIndex].Position.Y - controlPointOffset * 4d,
								controlPointOffset * 8d, controlPointOffset * 8d),
							ToolControl.FillColor, ToolControl.StrokeColor, 1);
					}

					//For each curve currently being drawn/edited by the user.
					for (int n = 0; n < cEngines.CEL.Count; ++n)
					{
						List<ControlPoint> controlPoints = cEngines.CEL[n].ControlPoints;

						//If the curve has one or more points.
						if (controlPoints.Count > 0)
						{
							//Draw the control points for the curve.
							for (int i = 0; i < controlPoints.Count; ++i)
							{
								//Skip drawing the hovered control point.
								if (hoveredPointAsControlPoint > -1 && hoverPoint.Distance(controlPoints[i].Position) < 1d)
								{
									continue;
								}

								// Draw the control point.
								g.FillStrokedEllipse(
									new Rectangle(
										controlPoints[i].Position.X - controlPointOffset,
										controlPoints[i].Position.Y - controlPointOffset,
										controlPointSize, controlPointSize),
									ToolControl.FillColor, ToolControl.StrokeColor, controlPointSize);
							}
						}
					}
					
					//Draw the hover point.
					if (!changingTension && hoverPoint.X > -1d)
					{
						g.FillStrokedEllipse(new Rectangle(
							hoverPoint.X - controlPointOffset * 4d, hoverPoint.Y - controlPointOffset * 4d,
							controlPointOffset * 8d, controlPointOffset * 8d), hoverColor, hoverColor, 1);
						g.FillStrokedEllipse(new Rectangle(
							hoverPoint.X - controlPointOffset, hoverPoint.Y - controlPointOffset,
							controlPointSize, controlPointSize), hoverColor, hoverColor, controlPointSize);
					}

					if (dirty != null)
					{
						//Inflate to accomodate for control points.
						dirty = dirty.Value.Inflate(controlPointSize * 8, controlPointSize * 8);
					}
				}
			}


			return dirty ?? new Rectangle(0d, 0d, 0d, 0d);
		}

		/// <summary>
		/// Draw all of the lines/curves that are currently being drawn/edited by the user.
		/// </summary>
		/// <param name="calculateOrganizedPoints">Whether or not to calculate the spatially organized
		/// points for mouse detection after drawing the curve.</param>
		/// <param name="finalize">Whether or not to finalize the drawing.</param>
		/// <param name="shiftKey">Whether or not the shift key is being pressed.</param>
		public void drawCurves(bool calculateOrganizedPoints, bool finalize, bool shiftKey)
		{
			if (!surface_modified)
			{
				return;
			}

			Document doc = PintaCore.Workspace.ActiveDocument;

			Rectangle dirty;

			if (finalize)
			{
				doc.ToolLayer.Clear();

				ImageSurface undoSurface = doc.CurrentUserLayer.Surface.Clone();

				is_drawing = false;
				surface_modified = false;

				selectedPointIndex = -1;

				dirty = DrawShape(
					Utility.PointsToRectangle(shape_origin, new PointD(current_point.X, current_point.Y), shiftKey),
					doc.CurrentUserLayer, false);

				//Make sure that the undo surface isn't null and that there are actually points.
				if (undoSurface != null && cEngines.CEL[0].ControlPoints.Count > 0)
				{
					//Create a new CurvesHistoryItem so that the finalization of the curves can be undone.
					doc.History.PushNewItem(
						new CurvesHistoryItem(Icon, Catalog.GetString("Line/Curve Finalized"),
							undoSurface, cEngines.PartialClone(), doc.CurrentUserLayer));
				}

				//Clear out all of the old data.
				cEngines = new CurveEngineCollection();
			}
			else
			{
				//Only calculate the hover point when there isn't a request to organize the generated points by spatial hashing.
				if (!calculateOrganizedPoints)
				{
					//Calculate the hover point, if any.

					int closestCurveIndex, closestPointIndex;
					PointD closestPoint;
					double closestDistance;

					OrganizedPointCollection.findClosestPoint(current_point, out closestCurveIndex, out closestPointIndex, out closestPoint, out closestDistance);

					List<ControlPoint> controlPoints = cEngines.CEL[closestCurveIndex].ControlPoints;

					//Determine if the user is hovering the mouse close enough to a line,
					//curve, or point that's currently being drawn/edited by the user.
					if (closestDistance < CurveClickRange)
					{
						//User is hovering over a generated point on a line/curve.

						if (controlPoints.Count > closestPointIndex)
						{
							//Note: compare the current_point's distance here because it's the actual mouse position.
							if (current_point.Distance(controlPoints[closestPointIndex].Position) < CurveClickRange)
							{
								//Mouse hovering over a control point (on the "previous order" side of the point).

								hoverPoint.X = controlPoints[closestPointIndex].Position.X;
								hoverPoint.Y = controlPoints[closestPointIndex].Position.Y;
								hoveredPointAsControlPoint = closestPointIndex;
							}
							else if (current_point.Distance(controlPoints[closestPointIndex - 1].Position) < CurveClickRange)
							{
								//Mouse hovering over a control point (on the "following order" side of the point).

								hoverPoint.X = controlPoints[closestPointIndex - 1].Position.X;
								hoverPoint.Y = controlPoints[closestPointIndex - 1].Position.Y;
								hoveredPointAsControlPoint = closestPointIndex - 1;
							}
						}

						if (hoverPoint.X < 0d)
						{
							hoverPoint.X = closestPoint.X;
							hoverPoint.Y = closestPoint.Y;
						}
					}
				}



				doc.ToolLayer.Clear();

				dirty = DrawShape(
					Utility.PointsToRectangle(shape_origin, new PointD(current_point.X, current_point.Y), shiftKey),
					doc.ToolLayer, true);



				//Reset the hover point after each drawing.
				hoverPoint = new PointD(-1d, -1d);
				hoveredPointAsControlPoint = -1;
			}

			if (calculateOrganizedPoints)
			{
				//Organize the generated points for quick mouse interaction detection.

				//First, clear the previously organized points, if any.
				for (int n = 0; n < cEngines.CEL.Count; ++n)
				{
					cEngines.CEL[n].OrganizedPoints.ClearCollection();

					int pointIndex = 0;

					foreach (PointD p in cEngines.CEL[n].GeneratedPoints)
					{
						cEngines.CEL[n].OrganizedPoints.StoreAndOrganizePoint(new OrganizedPoint(new PointD(p.X, p.Y), pointIndex));

						//Keep track of the point's order in relation to the control points.
						if (cEngines.CEL[n].ControlPoints.Count > pointIndex
							&& p.X == cEngines.CEL[n].ControlPoints[pointIndex].Position.X
							&& p.Y == cEngines.CEL[n].ControlPoints[pointIndex].Position.Y)
						{
							++pointIndex;
						}
					}
				}
			}

			// Increase the size of the dirty rect to account for antialiasing.
			if (UseAntialiasing)
			{
				dirty = dirty.Inflate(1, 1);
			}

			dirty = ((Rectangle?)dirty).UnionRectangles(last_dirty).Value;
			dirty = dirty.Clamp();
			doc.Workspace.Invalidate(dirty.ToGdkRectangle());
			last_dirty = dirty;
		}

		/// <summary>
		/// Calculate the modified position of current_point such that the angle between shape_origin
		/// and current_point is snapped to the closest angle out of a certain number of angles.
		/// </summary>
		private void calculateModifiedCurrentPoint()
		{
			PointD dir = new PointD(current_point.X - shape_origin.X, current_point.Y - shape_origin.Y);
			double theta = Math.Atan2(dir.Y, dir.X);
			double len = Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);

			theta = Math.Round(12 * theta / Math.PI) * Math.PI / 12;
			current_point = new PointD((shape_origin.X + len * Math.Cos(theta)), (shape_origin.Y + len * Math.Sin(theta)));
		}

		protected override void OnActivated()
		{
			PintaCore.Workspace.ActiveDocument.ToolLayer.Clear();
			PintaCore.Workspace.ActiveDocument.ToolLayer.Hidden = false;

			drawCurves(false, false, false);

			base.OnActivated();
		}

		protected override void OnDeactivated()
		{
			PintaCore.Workspace.ActiveDocument.ToolLayer.Hidden = true;

			//Finalize the previous curve (if needed).
			drawCurves(false, true, false);

			base.OnDeactivated();
		}

		protected override void OnCommit()
		{
			PintaCore.Workspace.ActiveDocument.ToolLayer.Hidden = true;

			//Finalize the previous curve (if needed).
			drawCurves(false, true, false);

			base.OnCommit();
		}

		protected override void OnKeyDown(Gtk.DrawingArea canvas, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				if (selectedPointIndex > -1)
				{
					//Create a new CurveModifyHistoryItem so that the deletion of a control point can be undone.
					PintaCore.Workspace.ActiveDocument.History.PushNewItem(
						new CurveModifyHistoryItem(Icon, Catalog.GetString("Line/Curve Point Deleted"), cEngines.PartialClone()));


					List<ControlPoint> controlPoints = SelectedCurveEngine.ControlPoints;


					undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.Clone();

					//Delete the selected point from the curve.
					controlPoints.RemoveAt(selectedPointIndex);

					//Set the newly selected point to be the median-most point on the curve, order-wise.
					if (controlPoints.Count > 0)
					{
						if (selectedPointIndex > controlPoints.Count / 2)
						{
							--selectedPointIndex;
						}
					}
					else
					{
						selectedPointIndex = -1;
					}

					surface_modified = true;

					hoverPoint = new PointD(-1d, -1d);

					drawCurves(true, false, false);
				}

				args.RetVal = true;
			}
			else if (args.Event.Key == Gdk.Key.Return)
			{
				//Finalize the previous curve (if needed).
				drawCurves(false, true, false);

				args.RetVal = true;
			}
			else if (args.Event.Key == Gdk.Key.space)
			{
				ControlPoint selPoint = SelectedPoint;

				if (selPoint != null)
				{
					//This can be assumed not to be null since selPoint was not null.
					CurveEngine selEngine = SelectedCurveEngine;

					//Create a new CurveModifyHistoryItem so that the adding of a control point can be undone.
					PintaCore.Workspace.ActiveDocument.History.PushNewItem(
						new CurveModifyHistoryItem(Icon, Catalog.GetString("Line/Curve Point Added"), cEngines.PartialClone()));


					bool shiftKey = (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;
					bool ctrlKey = (args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask;

					PointD newPointPos;

					if (ctrlKey)
					{
						//Ctrl + space combo: same position as currently selected point.
						newPointPos = new PointD(selPoint.Position.X, selPoint.Position.Y);
					}
					else
					{
						shape_origin = new PointD(selPoint.Position.X, selPoint.Position.Y);

						if (shiftKey)
						{
							calculateModifiedCurrentPoint();
						}

						//Space only: position of mouse (after any potential shift alignment).
						newPointPos = new PointD(current_point.X, current_point.Y);
					}

					//Place the new point on the outside-most end, order-wise.
					if ((double)selectedPointIndex < (double)selEngine.ControlPoints.Count / 2d)
					{
						SelectedCurveEngine.ControlPoints.Insert(selectedPointIndex,
							new ControlPoint(new PointD(newPointPos.X, newPointPos.Y), DefaultMidPointTension));
					}
					else
					{
						SelectedCurveEngine.ControlPoints.Insert(selectedPointIndex + 1,
							new ControlPoint(new PointD(newPointPos.X, newPointPos.Y), DefaultMidPointTension));

						++selectedPointIndex;
					}

					drawCurves(true, false, shiftKey);
				}
			}
			else if (args.Event.Key == Gdk.Key.Up)
			{
				//Make sure a control point is selected.
				if (selectedPointIndex > -1)
				{
					//Move the selected control point.
					SelectedPoint.Position.Y -= 1d;

					drawCurves(true, false, false);
				}

				args.RetVal = true;
			}
			else if (args.Event.Key == Gdk.Key.Down)
			{
				//Make sure a control point is selected.
				if (selectedPointIndex > -1)
				{
					//Move the selected control point.
					SelectedPoint.Position.Y += 1d;

					drawCurves(true, false, false);
				}

				args.RetVal = true;
			}
			else if (args.Event.Key == Gdk.Key.Left)
			{
				//Make sure a control point is selected.
				if (selectedPointIndex > -1)
				{
					if ((args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
					{
						//Change the selected control point to be the previous one, if applicable.
						if (selectedPointIndex > 0)
						{
							--selectedPointIndex;
						}
					}
					else
					{
						//Move the selected control point.
						SelectedPoint.Position.X -= 1d;
					}

					drawCurves(true, false, false);
				}

				args.RetVal = true;
			}
			else if (args.Event.Key == Gdk.Key.Right)
			{
				//Make sure a control point is selected.
				if (selectedPointIndex > -1)
				{
					if ((args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
					{
						//Change the selected control point to be the following one, if applicable.
						if (selectedPointIndex < SelectedCurveEngine.ControlPoints.Count - 1)
						{
							++selectedPointIndex;
						}
					}
					else
					{
						//Move the selected control point.
						SelectedPoint.Position.X += 1d;
					}

					drawCurves(true, false, false);
				}

				args.RetVal = true;
			}
			else
			{
				base.OnKeyDown(canvas, args);
			}
		}
		
		protected override void OnKeyUp(Gtk.DrawingArea canvas, Gtk.KeyReleaseEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete || args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.space
				|| args.Event.Key == Gdk.Key.Up || args.Event.Key == Gdk.Key.Down
				|| args.Event.Key == Gdk.Key.Left || args.Event.Key == Gdk.Key.Right)
			{
				args.RetVal = true;
			}
			else
			{
				base.OnKeyUp(canvas, args);
			}
		}

		public override void AfterUndo()
		{
			surface_modified = true;

			//Draw the current state.
			drawCurves(true, false, false);

			setToolbarArrowOptions();

			base.AfterUndo();
		}

		public override void AfterRedo()
		{
			surface_modified = true;

			//Draw the current state.
			drawCurves(true, false, false);

			setToolbarArrowOptions();

			base.AfterRedo();
		}

		protected override void OnMouseDown(Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, PointD point)
		{
			// If we are already drawing, ignore any additional mouse down events
			if (is_drawing)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			lastMousePosition = point;

			shape_origin = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));
			current_point = shape_origin;

			bool shiftKey = (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;

			if (shiftKey)
			{
				calculateModifiedCurrentPoint();
			}

			is_drawing = true;
			surface_modified = true;
			doc.ToolLayer.Hidden = false;

			outline_color = PintaCore.Palette.PrimaryColor;
			fill_color = PintaCore.Palette.SecondaryColor;


			//Right clicking changes tension.
			if (args.Event.Button == 1)
			{
				changingTension = false;
			}
			else
			{
				changingTension = true;
			}


			int closestCurveIndex, closestPointIndex;
			PointD closestPoint;
			double closestDistance;

			OrganizedPointCollection.findClosestPoint(current_point,
				out closestCurveIndex, out closestPointIndex, out closestPoint, out closestDistance);

			bool clickedOnControlPoint = false;

			//Determine if the user clicked close enough to a line, curve, or point that's currently being drawn/edited by the user.
			if (closestDistance < CurveClickRange)
			{
				//User clicked on a generated point on a line/curve.

				List<ControlPoint> controlPoints = cEngines.CEL[closestCurveIndex].ControlPoints;

				//Note: compare the current_point's distance here because it's the actual mouse position.
				if (controlPoints.Count > closestPointIndex &&
					current_point.Distance(controlPoints[closestPointIndex].Position) < CurveClickRange)
				{
					//User clicked on a control point (on the "previous order" side of the point).

					clickedWithoutModifying = true;

					selectedPointIndex = closestPointIndex;
					selectedPointCurveIndex = closestCurveIndex;

					clickedOnControlPoint = true;
				}
				else if (current_point.Distance(controlPoints[closestPointIndex - 1].Position) < CurveClickRange)
				{
					//User clicked on a control point (on the "following order" side of the point).

					clickedWithoutModifying = true;

					selectedPointIndex = closestPointIndex - 1;
					selectedPointCurveIndex = closestCurveIndex;

					clickedOnControlPoint = true;
				}

				//Don't change anything here if right clicked.
				if (!changingTension)
				{
					if (!clickedOnControlPoint)
					{
						//User clicked on a non-control point on a line/curve.

						//Create a new CurveModifyHistoryItem so that the adding of a control point can be undone.
						doc.History.PushNewItem(
							new CurveModifyHistoryItem(Icon, Catalog.GetString("Line/Curve Point Added"), cEngines.PartialClone()));

						controlPoints.Insert(closestPointIndex,
							new ControlPoint(new PointD(current_point.X, current_point.Y), DefaultMidPointTension));

						selectedPointIndex = closestPointIndex;
						selectedPointCurveIndex = closestCurveIndex;
					}
				}
			}

			bool ctrlKey = (args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask;

			//Create a new line/curve if the user simply clicks outside of any lines/curves or if the user control + clicks on an existing point.
			if (!changingTension && ((ctrlKey && clickedOnControlPoint) || closestDistance >= CurveClickRange))
			{
				PointD prevSelPoint;

				//First, store the position of the currently selected point.
				if (SelectedPoint != null && ctrlKey)
				{
					prevSelPoint = new PointD(SelectedPoint.Position.X, SelectedPoint.Position.Y);
				}
				else
				{
					//This doesn't matter, other than the fact that it gets set to a value in order for the code to build.
					prevSelPoint = new PointD(0d, 0d);
				}


				//Next, take care of the old curve's data.

				//Finalize the previous curve (if needed).
				drawCurves(false, true, false);

				//Create a new CurvesHistoryItem so that the creation of a new curve can be undone.
				doc.History.PushNewItem(
					new CurvesHistoryItem(Icon, Catalog.GetString("Line/Curve Added"), doc.CurrentUserLayer.Surface.Clone(), cEngines.PartialClone(), doc.CurrentUserLayer));

				is_drawing = true;


				//Then create the first two points of the line/curve. The second point will follow the mouse around until released.
				if (ctrlKey && clickedOnControlPoint)
				{
					cEngines.CEL[0].ControlPoints.Add(new ControlPoint(new PointD(prevSelPoint.X, prevSelPoint.Y), DefaultEndPointTension));
					cEngines.CEL[0].ControlPoints.Add(
						new ControlPoint(new PointD(prevSelPoint.X + .01d, prevSelPoint.Y + .01d), DefaultEndPointTension));

					clickedWithoutModifying = false;
				}
				else
				{
					cEngines.CEL[0].ControlPoints.Add(new ControlPoint(new PointD(shape_origin.X, shape_origin.Y), DefaultEndPointTension));
					cEngines.CEL[0].ControlPoints.Add(
						new ControlPoint(new PointD(shape_origin.X + .01d, shape_origin.Y + .01d), DefaultEndPointTension));
				}

				selectedPointIndex = 1;
				selectedPointCurveIndex = 0;

				setArrowOptions();
			}

			//If the user right clicks outside of any lines/curves.
			if (closestDistance >= CurveClickRange && changingTension)
			{
				clickedWithoutModifying = true;
			}

			surface_modified = true;

			drawCurves(false, false, shiftKey);
		}

		protected override void OnMouseUp(Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, PointD point)
		{
			is_drawing = false;

			changingTension = false;

			drawCurves(true, false, args.Event.IsShiftPressed());
		}

		protected override void OnMouseMove(object o, Gtk.MotionNotifyEventArgs args, PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			current_point = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));

			bool shiftKey = (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;

			if (shiftKey)
			{
				calculateModifiedCurrentPoint();
			}


			if (!is_drawing)
			{
				//Redraw everything to show a (temporary) highlighted control point when applicable.
				drawCurves(false, false, shiftKey);
			}
			else
			{
				//Make sure a control point is selected.
				if (selectedPointIndex > -1)
				{
					if (clickedWithoutModifying)
					{
						//Create a new CurveModifyHistoryItem so that the modification of the curve can be undone.
						doc.History.PushNewItem(
							new CurveModifyHistoryItem(Icon, Catalog.GetString("Line/Curve Modified"), cEngines.PartialClone()));

						clickedWithoutModifying = false;
					}

					List<ControlPoint> controlPoints = SelectedCurveEngine.ControlPoints;

					if (!changingTension)
					{
						//Moving a control point.

						//Make sure the control point was moved.
						if (current_point.X != controlPoints[selectedPointIndex].Position.X
							|| current_point.Y != controlPoints[selectedPointIndex].Position.Y)
						{
							//Keep the tension value consistent.
							double movingPointTension = controlPoints[selectedPointIndex].Tension;

							//Update the control point's position.
							controlPoints.RemoveAt(selectedPointIndex);
							controlPoints.Insert(selectedPointIndex,
								new ControlPoint(new PointD(current_point.X, current_point.Y),
									movingPointTension));
						}
					}
					else
					{
						//Changing a control point's tension.

						//Unclamp the mouse position when changing tension.
						current_point = new PointD(point.X, point.Y);

						//Calculate the new tension based off of the movement of the mouse that's
						//perpendicular to the previous and following control points.

						PointD curPoint = controlPoints[selectedPointIndex].Position;
						PointD prevPoint, nextPoint;

						//Calculate the previous control point.
						if (selectedPointIndex > 0)
						{
							prevPoint = controlPoints[selectedPointIndex - 1].Position;
						}
						else
						{
							//There is none.
							prevPoint = curPoint;
						}

						//Calculate the following control point.
						if (selectedPointIndex < controlPoints.Count - 1)
						{
							nextPoint = controlPoints[selectedPointIndex + 1].Position;
						}
						else
						{
							//There is none.
							nextPoint = curPoint;
						}

						//The x and y differences are used as factors for the x and y change in the mouse position.
						double xDiff = prevPoint.X - nextPoint.X;
						double yDiff = prevPoint.Y - nextPoint.Y;
						double totalDiff = xDiff + yDiff;

						//Calculate the midpoint in between the previous and following points.
						PointD midPoint = new PointD((prevPoint.X + nextPoint.X) / 2d, (prevPoint.Y + nextPoint.Y) / 2d);

						double xChange = 0d, yChange = 0d;

						//Calculate the x change in the mouse position.
						if (curPoint.X <= midPoint.X)
						{
							xChange = current_point.X - lastMousePosition.X;
						}
						else
						{
							xChange = lastMousePosition.X - current_point.X;
						}

						//Calculate the y change in the mouse position.
						if (curPoint.Y <= midPoint.Y)
						{
							yChange = current_point.Y - lastMousePosition.Y;
						}
						else
						{
							yChange = lastMousePosition.Y - current_point.Y;
						}

						//Update the control point's tension.
						//Note: the difference factors are to be inverted for x and y change because this is perpendicular motion.
						controlPoints[selectedPointIndex].Tension +=
							Math.Round(Utility.Clamp((xChange * yDiff + yChange * xDiff) / totalDiff, -1d, 1d)) / 50d;

						//Restrict the new tension to range from 0d to 1d.
						controlPoints[selectedPointIndex].Tension =
							Utility.Clamp(controlPoints[selectedPointIndex].Tension, 0d, 1d);
					}

					surface_modified = true;

					drawCurves(false, false, shiftKey);
				}
			}

			lastMousePosition = current_point;
		}
	}
}
