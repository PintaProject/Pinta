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
			new Color(ToolControl.FillColor.R / 2d, ToolControl.FillColor.G / 2d, ToolControl.FillColor.B / 2d, ToolControl.FillColor.A / 2d);

		private const double CurveClickRange = 12d;
		private const double DefaultEndPointTension = 0d;
		private const double DefaultMidPointTension = 1d / 3d;

		private const double radiansToDegrees = Math.PI / 180d;
		private const double invRadiansToDegrees = 180d / Math.PI;

		public const int SectionSize = 15;

		//For optimization.
		public const double SectionSizeDouble = SectionSize;

		//Don't change this; it's automatically calculated.
		public static readonly int BorderingSectionRange = (int)Math.Ceiling(CurveClickRange / SectionSize);


		private int selectedPointIndex = -1;
		private int selectedPointCurveIndex = 0;

		/// <summary>
		/// The selected ControlPoint.
		/// </summary>
		private ControlPoint selectedPoint
		{
			get
			{
				return cEngines.CEL[selectedPointCurveIndex].GivenPoints[selectedPointIndex];
			}

			set
			{
				cEngines.CEL[selectedPointCurveIndex].GivenPoints[selectedPointIndex] = value;
			}
		}

		private PointD hoverPoint = new PointD(-1d, -1d);
		private int hoveredPointAsControlPoint = -1;

		private bool changingTension = false;
		private PointD lastMousePos = new PointD(0d, 0d);


		//This is used to temporarily store the UserLayer's and TextLayer's previous ImageSurface states.
		private ImageSurface curves_undo_surface;
		private ImageSurface user_undo_surface;
		private CurveEngineCollection undo_engines;
		
		//Stores the editable curve data.
		public static CurveEngineCollection cEngines = new CurveEngineCollection();


		public override string Name {
			get { return Catalog.GetString ("Line/Curve"); }
		}
		public override string Icon {
			get { return "Tools.Line.png"; }
		}
		//TODO: "Left click to draw a line with primary color. Left click on line to add curve control points.
		//Right click to change tension. Hold Shift key to snap to angles." etc...
		public override string StatusBarText {
			//TODO: "Left click to draw a line with primary color. Left click on line to add curve control points.
			//Right click to change tension. Hold Shift key to snap to angles."
			get { return Catalog.GetString ("Left click to draw with primary color, right click for secondary color. Hold Shift key to snap to angles."); }
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

		private bool ShowArrowOne
		{
			get
			{
				return (showArrowOne.ComboBox as Gtk.ComboBoxEntry).Entry.Text == "Show";
			}
		}

		private bool ShowArrowTwo
		{
			get
			{
				return (showArrowTwo.ComboBox as Gtk.ComboBoxEntry).Entry.Text == "Show";
			}
		}

		private ToolBarLabel showArrowOneLabel, showArrowTwoLabel;
		private ToolBarComboBox showArrowOne, showArrowTwo;
		private Gtk.SeparatorToolItem arrowSep;

		protected override void BuildToolBar(Gtk.Toolbar tb)
		{
			base.BuildToolBar(tb);

			if (arrowSep == null)
			{
				arrowSep = new Gtk.SeparatorToolItem();
			}

			tb.AppendItem(arrowSep);


			if (showArrowOneLabel == null)
			{
				showArrowOneLabel = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Arrow 1")));
			}

			tb.AppendItem(showArrowOneLabel);

			if (showArrowOne == null)
			{
				showArrowOne = new ToolBarComboBox(65, 1, true, "Show", "Hide");
			}

			tb.AppendItem(showArrowOne);


			if (showArrowTwoLabel == null)
			{
				showArrowTwoLabel = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Arrow 2")));
			}

			tb.AppendItem(showArrowTwoLabel);

			if (showArrowTwo == null)
			{
				showArrowTwo = new ToolBarComboBox(65, 1, true, "Show", "Hide");
			}

			tb.AppendItem(showArrowTwo);
		}


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

				g.LineWidth = BrushWidth;

				int controlPointSize = BrushWidth;
				double controlPointOffset = (double)controlPointSize / 2d;



				//Draw the lines/curves.
				if (drawControlPoints)
				{
					//For each curve currently being drawn/edited by the user.
					for (int n = 0; n < cEngines.CEL.Count; ++n)
					{
						List<ControlPoint> controlPoints = cEngines.CEL[n].GivenPoints;

						//If the curve has one or more points.
						if (controlPoints.Count > 0)
						{
							//Generate the points that make up the curve.
							cEngines.CEL[n].GenerateCardinalSplinePolynomialCurvePoints(n);

							//Expand the invalidation rectangle as necessary.
							dirty = dirty.UnionRectangles(g.DrawPolygonal(cEngines.CEL[n].GeneratedPoints, outline_color));

							//Draw the control points.
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

					if (selectedPointIndex > -1)
					{
						//Draw a ring around the selected point.
						g.FillStrokedEllipse(
							new Rectangle(
								cEngines.CEL[selectedPointCurveIndex].GivenPoints[selectedPointIndex].Position.X - controlPointOffset * 3d,
								cEngines.CEL[selectedPointCurveIndex].GivenPoints[selectedPointIndex].Position.Y - controlPointOffset * 3d,
								controlPointOffset * 6d, controlPointOffset * 6d),
							ToolControl.FillColor, ToolControl.StrokeColor, 1);
					}
					
					//Draw the hover point.
					if (!changingTension && hoverPoint.X > -1d)
					{
						g.FillStrokedEllipse(new Rectangle(
							hoverPoint.X - controlPointOffset, hoverPoint.Y - controlPointOffset,
							controlPointSize, controlPointSize), hoverColor, hoverColor, controlPointSize);
						g.FillStrokedEllipse(new Rectangle(
							hoverPoint.X - controlPointOffset * 3d, hoverPoint.Y - controlPointOffset * 3d,
							controlPointOffset * 6d, controlPointOffset * 6d), hoverColor, hoverColor, 1);
					}

					if (dirty != null)
					{
						//Inflate to accomodate for control points.
						dirty = dirty.Value.Inflate(controlPointSize * 8, controlPointSize * 8);
					}
				}
				else
				{
					for (int n = 0; n < cEngines.CEL.Count; ++n)
					{
						List<ControlPoint> controlPoints = cEngines.CEL[n].GivenPoints;

						if (controlPoints.Count > 0)
						{
							//Generate the points that make up the curve.
							cEngines.CEL[n].GenerateCardinalSplinePolynomialCurvePoints(n);

							//Expand the invalidation rectangle as necessary.
							dirty = dirty.UnionRectangles(g.DrawPolygonal(cEngines.CEL[n].GeneratedPoints, outline_color));
						}
					}
				}


				//Draw the arrows.

				for (int n = 0; n < cEngines.CEL.Count; ++n)
				{
					List<ControlPoint> controlPoints = cEngines.CEL[n].GivenPoints;
					PointD[] genPoints = cEngines.CEL[n].GeneratedPoints;

					PointD endPoint, almostEndPoint;
					double endingAngle, angleOffset = 10d, arrowWidth = 10d, arrowLength = 30d;

					//For each curve currently being drawn/edited by the user.
					for (int i = 0; i < controlPoints.Count; ++i)
					{
						cEngines.CEL[n].showArrowOne = ShowArrowOne;
						cEngines.CEL[n].showArrowTwo = ShowArrowTwo;

						if (cEngines.CEL[n].showArrowOne)
						{
							endPoint = genPoints[0];
							almostEndPoint = genPoints[1];

							endingAngle = Math.Atan(Math.Abs(endPoint.Y - almostEndPoint.Y) / Math.Abs(endPoint.X - almostEndPoint.X)) * invRadiansToDegrees;

							if (endPoint.Y - almostEndPoint.Y > 0)
							{
								if (endPoint.X - almostEndPoint.X > 0)
								{
									endingAngle = 180d - endingAngle;
								}
							}
							else
							{
								if (endPoint.X - almostEndPoint.X > 0)
								{
									endingAngle += 180d;
								}
								else
								{
									endingAngle = 360d - endingAngle;
								}
							}

							PointD[] arrowPoints =
							{
								endPoint,
								new PointD(
									endPoint.X + Math.Cos((endingAngle + 270 + angleOffset) * radiansToDegrees) * arrowWidth,
									endPoint.Y + Math.Sin((endingAngle + 270 + angleOffset) * radiansToDegrees) * arrowWidth * -1d),
								new PointD(
									endPoint.X + Math.Cos((endingAngle + 180) * radiansToDegrees) * arrowLength,
									endPoint.Y + Math.Sin((endingAngle + 180) * radiansToDegrees) * arrowLength * -1d),
								new PointD(
									endPoint.X + Math.Cos((endingAngle + 90 - angleOffset) * radiansToDegrees) * arrowWidth,
									endPoint.Y + Math.Sin((endingAngle + 90 - angleOffset) * radiansToDegrees) * arrowWidth * -1d),
								endPoint
							};

							g.DrawPolygonal(arrowPoints, outline_color);

							//Calculate the minimum bounding rectangle for the arrowhead and union it with the existing invalidation rectangle.
							dirty = dirty.UnionRectangles(new Rectangle(
								Math.Min(Math.Min(arrowPoints[1].X, arrowPoints[2].X), arrowPoints[3].X),
								Math.Min(Math.Min(arrowPoints[1].Y, arrowPoints[2].Y), arrowPoints[3].Y),
								Math.Max(Math.Max(arrowPoints[1].X, arrowPoints[2].X), arrowPoints[3].X),
								Math.Max(Math.Max(arrowPoints[1].Y, arrowPoints[2].Y), arrowPoints[3].Y)));
						}

						if (cEngines.CEL[n].showArrowTwo)
						{
							endPoint = genPoints[genPoints.Length - 1];
							almostEndPoint = genPoints[genPoints.Length - 2];

							endingAngle = Math.Atan(Math.Abs(endPoint.Y - almostEndPoint.Y) / Math.Abs(endPoint.X - almostEndPoint.X)) * invRadiansToDegrees;

							if (endPoint.Y - almostEndPoint.Y > 0)
							{
								if (endPoint.X - almostEndPoint.X > 0)
								{
									endingAngle = 180d - endingAngle;
								}
							}
							else
							{
								if (endPoint.X - almostEndPoint.X > 0)
								{
									endingAngle += 180d;
								}
								else
								{
									endingAngle = 360d - endingAngle;
								}
							}

							PointD[] arrowPoints =
							{
								endPoint,
								new PointD(
									endPoint.X + Math.Cos((endingAngle + 270 + angleOffset) * radiansToDegrees) * arrowWidth,
									endPoint.Y + Math.Sin((endingAngle + 270 + angleOffset) * radiansToDegrees) * arrowWidth * -1d),
								new PointD(
									endPoint.X + Math.Cos((endingAngle + 180) * radiansToDegrees) * arrowLength,
									endPoint.Y + Math.Sin((endingAngle + 180) * radiansToDegrees) * arrowLength * -1d),
								new PointD(
									endPoint.X + Math.Cos((endingAngle + 90 - angleOffset) * radiansToDegrees) * arrowWidth,
									endPoint.Y + Math.Sin((endingAngle + 90 - angleOffset) * radiansToDegrees) * arrowWidth * -1d),
								endPoint
							};

							g.DrawPolygonal(arrowPoints, outline_color);

							//Calculate the minimum bounding rectangle for the arrowhead and union it with the existing invalidation rectangle.
							dirty = dirty.UnionRectangles(new Rectangle(
								Math.Min(Math.Min(arrowPoints[1].X, arrowPoints[2].X), arrowPoints[3].X),
								Math.Min(Math.Min(arrowPoints[1].Y, arrowPoints[2].Y), arrowPoints[3].Y),
								Math.Max(Math.Max(arrowPoints[1].X, arrowPoints[2].X), arrowPoints[3].X),
								Math.Max(Math.Max(arrowPoints[1].Y, arrowPoints[2].Y), arrowPoints[3].Y)));
						}
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
		private void drawCurves(bool calculateOrganizedPoints, bool finalize, bool shiftKey)
		{
			if (!surface_modified)
			{
				return;
			}

			Document doc = PintaCore.Workspace.ActiveDocument;

			Rectangle dirty;

			if (finalize)
			{
				selectedPointIndex = -1;

				dirty = DrawShape(
					Utility.PointsToRectangle(shape_origin, new PointD(current_point.X, current_point.Y), shiftKey),
					doc.CurrentUserLayer, false);
			}
			else
			{
				if (!calculateOrganizedPoints)
				{
					int closestCurveIndex, closestPointIndex;
					PointD closestPoint;
					double closestDistance;

					OrganizedPointCollection.findClosestPoint(current_point, out closestCurveIndex, out closestPointIndex, out closestPoint, out closestDistance);

					List<ControlPoint> controlPoints = cEngines.CEL[closestCurveIndex].GivenPoints;

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
								//User clicked on a control point (on the "previous order" side of the point).

								hoverPoint.X = controlPoints[closestPointIndex].Position.X;
								hoverPoint.Y = controlPoints[closestPointIndex].Position.Y;
								hoveredPointAsControlPoint = closestPointIndex;
							}
							else if (current_point.Distance(controlPoints[closestPointIndex - 1].Position) < CurveClickRange)
							{
								//User clicked on a control point (on the "following order" side of the point).

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
						if (cEngines.CEL[n].GivenPoints.Count > pointIndex
							&& p.X == cEngines.CEL[n].GivenPoints[pointIndex].Position.X
							&& p.Y == cEngines.CEL[n].GivenPoints[pointIndex].Position.Y)
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



			if (finalize)
			{
				//Make sure that neither undo surface is null.
				if (curves_undo_surface != null && user_undo_surface != null)
				{
					//Create a new CurvesHistoryItem so that the updated drawing of curves can be undone.
					doc.History.PushNewItem(new CurvesHistoryItem(Icon, Name,
						curves_undo_surface.Clone(), user_undo_surface.Clone(),
						cEngines.Clone(), doc.CurrentUserLayer));
				}

				is_drawing = false;
				surface_modified = false;
			}
		}

		/// <summary>
		/// Finalize the curve onto the UserLayer and clear out any old curve data.
		/// </summary>
		private void finalizeCurve()
		{
			PintaCore.Workspace.ActiveDocument.ToolLayer.Hidden = true;

			drawCurves(false, true, false);

			cEngines = new CurveEngineCollection();
		}

		protected override void OnDeactivated()
		{
			finalizeCurve();

			base.OnDeactivated();
		}

		protected override void OnCommit()
		{
			finalizeCurve();

			base.OnCommit();
		}

		protected override void OnKeyDown(Gtk.DrawingArea canvas, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				if (selectedPointIndex > -1)
				{
					List<ControlPoint> controlPoints = cEngines.CEL[selectedPointCurveIndex].GivenPoints;


					undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.Clone();

					//Delete the selected point from the curve.
					controlPoints.RemoveAt(selectedPointIndex);

					//Set the newly selected point to be the median-most point on the curve, if possible. Otherwise, set it to noPointSelected.
					if (controlPoints.Count > 0)
					{
						if (selectedPointIndex != controlPoints.Count / 2)
						{
							if (selectedPointIndex > controlPoints.Count / 2)
							{
								--selectedPointIndex;
							}
							else
							{
								++selectedPointIndex;
							}
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
				finalizeCurve();

				args.RetVal = true;
			}
			else if (args.Event.Key == Gdk.Key.Up)
			{
				//Make sure a control point is selected.
				if (selectedPointIndex > -1)
				{
					//Move the selected control point.
					selectedPoint.Position.Y -= 1d;

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
					selectedPoint.Position.Y += 1d;

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
						selectedPoint.Position.X -= 1d;
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
						if (selectedPointIndex < cEngines.CEL[selectedPointCurveIndex].GivenPoints.Count - 1)
						{
							++selectedPointIndex;
						}
					}
					else
					{
						//Move the selected control point.
						selectedPoint.Position.X += 1d;
					}

					drawCurves(true, false, false);
				}

				args.RetVal = true;
			}
			else if (args.Event.Key == Gdk.Key.z && (args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
			{
				finalizeCurve();

				PintaCore.Workspace.Invalidate();

				selectedPointIndex = -1;
				surface_modified = false;
			}
			else if (args.Event.Key == Gdk.Key.y && (args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
			{
				//Draw the current state.
				drawCurves(true, false, false);

				selectedPointIndex = -1;
			}
			else
			{
				base.OnKeyDown(canvas, args);
			}
		}
		
		protected override void OnKeyUp(Gtk.DrawingArea canvas, Gtk.KeyReleaseEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				args.RetVal = true;
			}
			else if (args.Event.Key == Gdk.Key.Up || args.Event.Key == Gdk.Key.Down
				|| args.Event.Key == Gdk.Key.Left || args.Event.Key == Gdk.Key.Right)
			{
				args.RetVal = true;
			}
			else
			{
				base.OnKeyUp(canvas, args);
			}
		}

		protected override void OnMouseDown(Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, PointD point)
		{
			// If we are already drawing, ignore any additional mouse down events
			if (is_drawing)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			bool shiftKey = (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;

			if (shiftKey)
			{
				calculateModifiedCurrentPoint();
			}

			shape_origin = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));
			current_point = shape_origin;

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



			
			//Determine if the user clicked close enough to a line, curve, or point that's currently being drawn/edited by the user.
			if (closestDistance < CurveClickRange)
			{
				//User clicked on a generated point on a line/curve.

				List<ControlPoint> controlPoints = cEngines.CEL[closestCurveIndex].GivenPoints;

				bool clickedOnControlPoint = false;

				//Note: compare the current_point's distance here because it's the actual mouse position.
				if (controlPoints.Count > closestPointIndex &&
					current_point.Distance(controlPoints[closestPointIndex].Position) < CurveClickRange)
				{
					//User clicked on a control point (on the "previous order" side of the point).

					selectedPointIndex = closestPointIndex;
					selectedPointCurveIndex = closestCurveIndex;

					clickedOnControlPoint = true;
				}
				else if (current_point.Distance(controlPoints[closestPointIndex - 1].Position) < CurveClickRange)
				{
					//User clicked on a control point (on the "following order" side of the point).

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

						controlPoints.Insert(closestPointIndex,
							new ControlPoint(new PointD(current_point.X, current_point.Y), DefaultMidPointTension));

						selectedPointIndex = closestPointIndex;
						selectedPointCurveIndex = closestCurveIndex;
					}
				}
			}
			else
			{
				//User clicked outside of any lines/curves.

				//Don't change anything here if right clicked.
				if (!changingTension)
				{
					//Create a new curve.

					//Finalize the previous curve.
					drawCurves(false, true, (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask);

					is_drawing = true;

					//Clear out all of the old data.
					cEngines = new CurveEngineCollection();


					//Store the previous state of the current UserLayer's and ToolLayer's ImageSurfaces.
					user_undo_surface = doc.CurrentUserLayer.Surface.Clone();
					curves_undo_surface = doc.ToolLayer.Surface.Clone();

					//Store the previous state of the Curve Engine.
					undo_engines = cEngines.Clone();


					//Add the first two points of the line. The second point will follow the mouse around until released.
					cEngines.CEL[0].GivenPoints.Add(new ControlPoint(new PointD(shape_origin.X, shape_origin.Y), DefaultEndPointTension));
					cEngines.CEL[0].GivenPoints.Add(new ControlPoint(new PointD(shape_origin.X + .01d, shape_origin.Y + .01d), DefaultEndPointTension));

					selectedPointIndex = 1;
					selectedPointCurveIndex = 0;
				}
			}



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
			bool shiftKey = (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;

			if (shiftKey)
			{
				calculateModifiedCurrentPoint();
			}

			Document doc = PintaCore.Workspace.ActiveDocument;

			current_point = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));


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
					List<ControlPoint> controlPoints = cEngines.CEL[selectedPointCurveIndex].GivenPoints;

					//Make sure the control point was moved.
					if (current_point.X != controlPoints[selectedPointIndex].Position.X
						|| current_point.Y != controlPoints[selectedPointIndex].Position.Y)
					{
						if (!changingTension)
						{
							//Keep the tension value consistent.
							double movingPointTension = controlPoints[selectedPointIndex].Tension;

							//Update the control point's position.
							controlPoints.RemoveAt(selectedPointIndex);
							controlPoints.Insert(selectedPointIndex,
								new ControlPoint(new PointD(current_point.X, current_point.Y),
									movingPointTension));

							surface_modified = true;
						}
						else
						{
							//Update the control point's tension.
							controlPoints[selectedPointIndex].Tension += (lastMousePos.Y - current_point.Y) / 200d;

							//Restrict the new tension to range from 0d to 1d.
							controlPoints[selectedPointIndex].Tension =
								Utility.Clamp(controlPoints[selectedPointIndex].Tension, 0d, 1d);

							surface_modified = true;
						}

						drawCurves(false, false, shiftKey);
					}
				}
			}

			lastMousePos = current_point;
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
	}
}
