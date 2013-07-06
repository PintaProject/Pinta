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
		private const double CurveClickRange = 12d;
		private const double DefaultTension = 1d / 6d;
		private const double SectionSize = 15d;

		//Don't change this; it's automatically calculated!
		private static double borderingSectionRange = Math.Ceiling(CurveClickRange / SectionSize) * SectionSize;

		private int selectedPointIndex = 0;
		private int selectedPointCurveIndex = 0;

		private PointD hoverPoint = new PointD(-1d, -1d);
		private int hoveredPointAsControlPoint = -1;

		private PointD closestPoint;
		private double closestDistance;
		private int closestPointIndex = 0;
		private int closestCurveIndex = 0;

		//This is used to temporarily store the UserLayer's and TextLayer's previous ImageSurface states.
		private Cairo.ImageSurface curves_undo_surface;
		private Cairo.ImageSurface user_undo_surface;
		private CurveEngine undo_engine;

		// The selection from when editing started. This ensures that text doesn't suddenly disappear/appear
		// if the selection changes before the text is finalized.
		private DocumentSelection selection; //***IMPLEMENT THIS BUG FIX LATER***
		
		//Stores the editable curve data.
		public static CurveEngine cEngine = new CurveEngine();

		public override string Name {
			get { return Catalog.GetString ("Line/Curve"); }
		}
		public override string Icon {
			get { return "Tools.Line.png"; }
		}
		public override string StatusBarText {
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

		protected override Rectangle DrawShape (Rectangle rect, Layer l, bool drawControlPoints)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			Rectangle? dirty = null;

			using (Context g = new Context(l.Surface))
			{
				List<List<ControlPoint>> controlPoints = cEngine.GivenPointsCollection;



				g.AppendPath(doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip();

				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				g.LineWidth = BrushWidth;



				Color cpColor = new Color(0d, .06d, .6d);
				Color hpColor = new Color(.5d, .5d, .5d);

				if (drawControlPoints)
				{
					//For each curve currently being drawn/edited by the user.
					for (int n = 0; n < controlPoints.Count; ++n)
					{
						//If the curve has one or more points.
						if (controlPoints[n].Count > 0)
						{
							//Generate the points that make up the curve.
							cEngine.GeneratedPointsCollection[n] = generateCardinalSplinePolynomialCurvePoints(n).ToArray();

							//Expand the invalidation rectangle as necessary.
							dirty = dirty.UnionRectangles(g.DrawPolygonal(cEngine.GeneratedPointsCollection[n], outline_color));

							//Draw the control points.
							for (int i = 0; i < controlPoints[n].Count; ++i)
							{
								//Skip the hovered control point.
								if (hoveredPointAsControlPoint > -1 && hoverPoint.Distance(controlPoints[n][i].Position) < 1d)
								{
									continue;
								}

								//NOTE: Control point graphics need to replicate the coloring of selection points.
								g.DrawEllipse(new Rectangle(controlPoints[n][i].Position.X - 1d, controlPoints[n][i].Position.Y - 1d, 2d, 2d), cpColor, 2);
							}
						}
					}

					//NOTE: Control point graphics need to replicate the coloring of selection points.
					if (selectedPointIndex > -1)
					{
						//Draw a ring around the selected point.
						g.DrawEllipse(
							new Rectangle(
								controlPoints[selectedPointCurveIndex][selectedPointIndex].Position.X - 4d,
								controlPoints[selectedPointCurveIndex][selectedPointIndex].Position.Y - 4d,
								8d, 8d),
							cpColor, 1);
					}

					//NOTE: Control point graphics need to replicate the coloring of selection points.
					if (hoverPoint.X > -1d)
					{
						//NOTE: Control point graphics need to replicate the coloring of selection points.
						g.DrawEllipse(new Rectangle(hoverPoint.X - 1d, hoverPoint.Y - 1d, 2d, 2d), hpColor, 2);
						g.DrawEllipse(new Rectangle(hoverPoint.X - 4d, hoverPoint.Y - 4d, 8d, 8d), hpColor, 1);
					}

					if (dirty != null)
					{
						dirty = dirty.Value.Inflate(8, 8);
					}
				}
				else
				{
					for (int n = 0; n < controlPoints.Count; ++n)
					{
						if (controlPoints[n].Count > 0)
						{
							cEngine.GeneratedPointsCollection[n] = generateCardinalSplinePolynomialCurvePoints(n).ToArray();

							//Expand the invalidation rectangle as necessary.
							dirty = dirty.UnionRectangles(g.DrawPolygonal(cEngine.GeneratedPointsCollection[n], outline_color));
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
		protected void drawCurves(bool calculateOrganizedPoints, bool finalize, bool shiftKey)
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
					findClosestPoint();

					List<List<ControlPoint>> controlPoints = cEngine.GivenPointsCollection;

					//Determine if the user is hovering the mouse close enough to a line,
					//curve, or point that's currently being drawn/edited by the user.
					if (closestDistance < CurveClickRange)
					{
						//User is hovering over a generated point on a line/curve.

						//Note: compare the current_point's distance here because it's the actual mouse position.
						if (controlPoints[closestCurveIndex].Count > closestPointIndex &&
							current_point.Distance(controlPoints[closestCurveIndex][closestPointIndex].Position) < CurveClickRange)
						{
							//User clicked on a control point (on the "previous order" side of the point).

							hoverPoint.X = controlPoints[closestCurveIndex][closestPointIndex].Position.X;
							hoverPoint.Y = controlPoints[closestCurveIndex][closestPointIndex].Position.Y;
							hoveredPointAsControlPoint = closestPointIndex;
						}
						else if (current_point.Distance(controlPoints[closestCurveIndex][closestPointIndex - 1].Position) < CurveClickRange)
						{
							//User clicked on a control point (on the "following order" side of the point).

							hoverPoint.X = controlPoints[closestCurveIndex][closestPointIndex - 1].Position.X;
							hoverPoint.Y = controlPoints[closestCurveIndex][closestPointIndex - 1].Position.Y;
							hoveredPointAsControlPoint = closestPointIndex - 1;
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
				cEngine.OrganizedPointsCollection[0].Collection.Clear();

				double sX, sY;

				int pointIndex = 0;

				foreach (PointD p in cEngine.GeneratedPointsCollection[0])
				{
					sX = (p.X - p.X % SectionSize) / SectionSize;
					sY = (p.Y - p.Y % SectionSize) / SectionSize;

					//These must be created each time to ensure that they are fresh for each loop iteration.
					Dictionary<double, List<OrganizedPoint>> xSection;
					List<OrganizedPoint> ySection;

					//Ensure that the ySection for this particular point exists.
					if (!cEngine.OrganizedPointsCollection[0].Collection.TryGetValue(sX, out xSection))
					{
						//This particular X section does not exist yet; create it.
						xSection = new Dictionary<double, List<OrganizedPoint>>();
						cEngine.OrganizedPointsCollection[0].Collection.Add(sX, xSection);
					}

					//Ensure that the ySection (which is contained within the respective xSection) for this particular point exists.
					if (!xSection.TryGetValue(sY, out ySection))
					{
						//This particular Y section does not exist yet; create it.
						ySection = new List<OrganizedPoint>();
						xSection.Add(sY, ySection);
					}

					//Now that both the corresponding xSection and ySection for this particular point exist, add the point to the list.
					ySection.Add(new OrganizedPoint(new PointD(p.X, p.Y), pointIndex));

					if (cEngine.GivenPointsCollection[0].Count > pointIndex
						&& p.X == cEngine.GivenPointsCollection[0][pointIndex].Position.X
						&& p.Y == cEngine.GivenPointsCollection[0][pointIndex].Position.Y)
					{
						++pointIndex;
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
						undo_engine.Clone(), doc.CurrentUserLayer));
				}

				is_drawing = false;
				surface_modified = false;
			}
		}

		protected override void OnDeactivated()
		{
			PintaCore.Workspace.ActiveDocument.ToolLayer.Hidden = true;

			drawCurves(false, true, false);

			cEngine = new CurveEngine();

			base.OnDeactivated();
		}

		protected override void OnCommit()
		{
			PintaCore.Workspace.ActiveDocument.ToolLayer.Hidden = true;

			drawCurves(false, true, false);

			cEngine = new CurveEngine();

			base.OnCommit();
		}

		protected override void OnKeyDown(Gtk.DrawingArea canvas, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				if (selectedPointIndex > -1)
				{
					List<List<ControlPoint>> controlPoints = cEngine.GivenPointsCollection;


					undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.Clone();

					//Delete the selected point from the curve.
					controlPoints[selectedPointCurveIndex].RemoveAt(selectedPointIndex);

					//Set the newly selected point to be the median-most point on the curve, if possible. Otherwise, set it to noPointSelected.
					if (controlPoints[selectedPointCurveIndex].Count > 0)
					{
						if (selectedPointIndex != controlPoints[selectedPointCurveIndex].Count / 2)
						{
							if (selectedPointIndex > controlPoints[selectedPointCurveIndex].Count / 2)
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
			else
			{
				base.OnKeyUp(canvas, args);
			}
		}

		public override bool TryHandleUndo()
		{
			if (surface_modified)
			{
				selectedPointIndex = -1;
				surface_modified = false;

				//Draw the current state.
				drawCurves(true, false, false);
			}

			return base.TryHandleUndo();
		}

		public override bool TryHandleRedo()
		{
			selectedPointIndex = -1;
			surface_modified = false;

			//Draw the current state.
			drawCurves(true, false, false);

			return base.TryHandleRedo();
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

			if (args.Event.Button == 1)
			{
				outline_color = PintaCore.Palette.PrimaryColor;
				fill_color = PintaCore.Palette.SecondaryColor;
			}
			else
			{
				outline_color = PintaCore.Palette.SecondaryColor;
				fill_color = PintaCore.Palette.PrimaryColor;
			}



			findClosestPoint();



			List<List<ControlPoint>> controlPoints = cEngine.GivenPointsCollection;

			//Determine if the user clicked close enough to a line, curve, or point that's currently being drawn/edited by the user.
			if (closestDistance < CurveClickRange)
			{
				//User clicked on a generated point on a line/curve.

				bool clickedOnControlPoint = false;

				//Note: compare the current_point's distance here because it's the actual mouse position.
				if (controlPoints[closestCurveIndex].Count > closestPointIndex &&
					current_point.Distance(controlPoints[closestCurveIndex][closestPointIndex].Position) < CurveClickRange)
				{
					//User clicked on a control point (on the "previous order" side of the point).

					selectedPointIndex = closestPointIndex;
					selectedPointCurveIndex = closestCurveIndex;

					clickedOnControlPoint = true;
				}
				else if (current_point.Distance(controlPoints[closestCurveIndex][closestPointIndex - 1].Position) < CurveClickRange)
				{
					//User clicked on a control point (on the "following order" side of the point).

					selectedPointIndex = closestPointIndex - 1;
					selectedPointCurveIndex = closestCurveIndex;

					clickedOnControlPoint = true;
				}

				if (!clickedOnControlPoint)
				{
					//User clicked on a non-control point on a line/curve.

					controlPoints[closestCurveIndex].Insert(closestPointIndex,
						new ControlPoint(new PointD(current_point.X, current_point.Y), DefaultTension));

					selectedPointIndex = closestPointIndex;
					selectedPointCurveIndex = closestCurveIndex;
				}
			}
			else
			{
				//User clicked outside of any lines/curves.

				if (controlPoints[0].Count > 0)
				{
					//Create a new curve.

					//Finalize the previous curve.
					drawCurves(false, true, (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask);

					is_drawing = true;

					//Clear out all of the old data.
					controlPoints[0].Clear();
					cEngine.GeneratedPointsCollection[0] = new PointD[0];
				}


				//Store the previous state of the current UserLayer's and ToolLayer's ImageSurfaces.
				user_undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.Clone();
				curves_undo_surface = doc.ToolLayer.Surface.Clone();

				//Store the previous state of the Curve Engine.
				undo_engine = cEngine.Clone();


				//Create a new CurvesHistoryItem so that the updated drawing of curves can be undone.
				doc.History.PushNewItem(new CurvesHistoryItem(Icon, Name,
					curves_undo_surface.Clone(), user_undo_surface.Clone(),
					undo_engine.Clone(), doc.CurrentUserLayer));
				

				//Add the first two points of the line. The second point will follow the mouse around until released.
				controlPoints[0].Add(new ControlPoint(new PointD(shape_origin.X, shape_origin.Y), DefaultTension));
				controlPoints[0].Add(new ControlPoint(new PointD(shape_origin.X + .01d, shape_origin.Y + .01d), DefaultTension));

				selectedPointIndex = 1;
				selectedPointCurveIndex = 0;
			}



			drawCurves(false, false, shiftKey);
		}

		protected override void OnMouseUp(Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, PointD point)
		{
			is_drawing = false;

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
				drawCurves(false, false, shiftKey);

				return;
			}



			List<List<ControlPoint>> controlPoints = cEngine.GivenPointsCollection;



			//Make sure the point was moved.
			if (current_point.X != controlPoints[selectedPointCurveIndex][selectedPointIndex].Position.X
				|| current_point.Y != controlPoints[selectedPointCurveIndex][selectedPointIndex].Position.Y)
			{
				controlPoints[selectedPointCurveIndex].RemoveAt(selectedPointIndex);
				controlPoints[selectedPointCurveIndex].Insert(selectedPointIndex,
					new ControlPoint(new PointD(current_point.X, current_point.Y),
						DefaultTension));

				surface_modified = true;



				drawCurves(false, false, shiftKey);
			}
		}

		/// <summary>
		/// Efficiently calculate the closest point (to current_point) on the curve being edited.
		/// </summary>
		protected void findClosestPoint()
		{
			Dictionary<double, Dictionary<double, List<OrganizedPoint>>> oP = cEngine.OrganizedPointsCollection[0].Collection;

			double currentDistance = double.MaxValue;

			closestDistance = double.MaxValue;
			closestPointIndex = 0;
			closestCurveIndex = 0;

			//Calculate the current_point's corresponding *center* section.
			double sX = (current_point.X - current_point.X % SectionSize) / SectionSize;
			double sY = (current_point.Y - current_point.Y % SectionSize) / SectionSize;

			double xMin = sX - borderingSectionRange;
			double xMax = sX + borderingSectionRange;
			double yMin = sY - borderingSectionRange;
			double yMax = sY + borderingSectionRange;

			//Since the mouse and/or curve points can be close to the edge of a section,
			//the points in the surrounding sections must also be checked.
			for (double x = xMin; x <= xMax; ++x)
			{
				//This must be created each time to ensure that it is fresh for each loop iteration.
				Dictionary<double, List<OrganizedPoint>> xSection;

				//If the xSection doesn't exist, move on.
				if (oP.TryGetValue(x, out xSection))
				{
					//Since the mouse and/or curve points can be close to the edge of a section,
					//the points in the surrounding sections must also be checked.
					for (double y = yMin; y <= yMax; ++y)
					{
						List<OrganizedPoint> ySection;

						//If the ySection doesn't exist, move on.
						if (xSection.TryGetValue(y, out ySection))
						{
							foreach (OrganizedPoint p in ySection)
							{
								currentDistance = p.Position.Distance(current_point);

								if (currentDistance < closestDistance)
								{
									closestDistance = currentDistance;

									closestPointIndex = p.Index;

									closestPoint = p.Position;
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Calculate the modified position of current_point such that the angle between shape_origin
		/// and current_point is snapped to the closest angle out of a certain number of angles.
		/// </summary>
		protected void calculateModifiedCurrentPoint()
		{
			PointD dir = new PointD(current_point.X - shape_origin.X, current_point.Y - shape_origin.Y);
			double theta = Math.Atan2(dir.Y, dir.X);
			double len = Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);

			theta = Math.Round(12 * theta / Math.PI) * Math.PI / 12;
			current_point = new PointD((shape_origin.X + len * Math.Cos(theta)), (shape_origin.Y + len * Math.Sin(theta)));
		}

		/// <summary>
		/// Generate each point in a cardinal spline polynomial curve that passes through the given control points.
		/// </summary>
		/// <param name="curveNum">The number of the curve to generate the points for.</param>
		/// <returns></returns>
		protected List<PointD> generateCardinalSplinePolynomialCurvePoints(int curveNum)
		{
			List<List<ControlPoint>> controlPoints = cEngine.GivenPointsCollection;


			List<PointD> generatedPoints = new List<PointD>();

			//Note: it's important that there be many generated points even if there are only 2 given points and it's just a line.
			//This is because the generated points are used in the check that determines if the mouse clicks on the line/curve.
			if (controlPoints[curveNum].Count < 2)
			{
				foreach (ControlPoint cP in controlPoints[curveNum])
				{
					generatedPoints.Add(cP.Position);
				}

				return generatedPoints;
			}



			//Generate tangents for each of the smaller cubic Bezier curves that make up each segment of the resulting curve.

			//Stores all of the tangent values.
			List<PointD> bezierTangents = new List<PointD>();

			//Calculate the first tangent.
			bezierTangents.Add(new PointD(
				controlPoints[curveNum][1].Tension * (controlPoints[curveNum][1].Position.X - controlPoints[curveNum][0].Position.X),
				controlPoints[curveNum][1].Tension * (controlPoints[curveNum][1].Position.Y - controlPoints[curveNum][0].Position.Y)));

			//Calculate all of the middle tangents.
			for (int i = 1; i < controlPoints[curveNum].Count - 1; ++i)
			{
				bezierTangents.Add(new PointD(
					controlPoints[curveNum][i + 1].Tension * (controlPoints[curveNum][i + 1].Position.X - controlPoints[curveNum][i - 1].Position.X),
					controlPoints[curveNum][i + 1].Tension * (controlPoints[curveNum][i + 1].Position.Y - controlPoints[curveNum][i - 1].Position.Y)));
			}

			//Calculate the last tangent.
			bezierTangents.Add(new PointD(
				controlPoints[curveNum][controlPoints[curveNum].Count - 1].Tension * (controlPoints[curveNum][controlPoints[curveNum].Count - 1].Position.X - controlPoints[curveNum][controlPoints[curveNum].Count - 2].Position.X),
				controlPoints[curveNum][controlPoints[curveNum].Count - 1].Tension * (controlPoints[curveNum][controlPoints[curveNum].Count - 1].Position.Y - controlPoints[curveNum][controlPoints[curveNum].Count - 2].Position.Y)));



			//For optimization.
			int iMinusOne;

			//Generate the resulting curve's points with consecutive cubic Bezier curves that
			//use the given points as end points and the calculated tangents as control points.
			for (int i = 1; i < controlPoints[curveNum].Count; ++i)
			{
				iMinusOne = i - 1;

				generateCubicBezierCurvePoints(
					generatedPoints,
					controlPoints[curveNum][iMinusOne].Position,
					new PointD(
						controlPoints[curveNum][iMinusOne].Position.X + bezierTangents[iMinusOne].X,
						controlPoints[curveNum][iMinusOne].Position.Y + bezierTangents[iMinusOne].Y),
					new PointD(
						controlPoints[curveNum][i].Position.X - bezierTangents[i].X,
						controlPoints[curveNum][i].Position.Y - bezierTangents[i].Y),
					controlPoints[curveNum][i].Position);
			}


			return generatedPoints;
		}

		/// <summary>
		/// Generate each point in a cubic Bezier curve given the end points and control points.
		/// </summary>
		/// <param name="resultList">The resulting List of PointD's to add the generated points to.</param>
		/// <param name="p0">The first end point that the curve passes through.</param>
		/// <param name="p1">The first control point that the curve does not necessarily pass through.</param>
		/// <param name="p2">The second control point that the curve does not necessarily pass through.</param>
		/// <param name="p3">The second end point that the curve passes through.</param>
		/// <returns></returns>
		protected static void generateCubicBezierCurvePoints(List<PointD> resultList, PointD p0, PointD p1, PointD p2, PointD p3)
		{
			//Note: this must be low enough for mouse clicks to be properly considered on/off the line/curve at any given point.
			double tInterval = .025d;

			double oneMinusT;
			double oneMinusTSquared;
			double oneMinusTCubed;

			double tSquared;
			double tCubed;

			double oneMinusTSquaredTimesTTimesThree;
			double oneMinusTTimesTSquaredTimesThree;

			//t will go from 0d to 1d at the interval of tInterval.
			for (double t = 0d; t < 1d + tInterval; t += tInterval)
			{
				//There are 3 "layers" in a cubic Bezier curve's calculation. These "layers"
				//must be calculated for each intermediate Point (for each value of t from
				//tInterval to 1d). The Points in each "layer" store [the distance between
				//two consecutive Points from the previous "layer" multipled by the value
				//of t (which is between 0d-1d)] plus [the position of the first Point of
				//the two consecutive Points from the previous "layer"]. This must be
				//calculated for the X and Y of every consecutive Point in every layer
				//until the last Point possible is reached, which is the Point on the curve.

				//Note: the code below is an optimized version of the commented explanation above.

				oneMinusT = 1d - t;
				oneMinusTSquared = oneMinusT * oneMinusT;
				oneMinusTCubed = oneMinusTSquared * oneMinusT;

				tSquared = t * t;
				tCubed = tSquared * t;

				oneMinusTSquaredTimesTTimesThree = oneMinusTSquared * t * 3d;
				oneMinusTTimesTSquaredTimesThree = oneMinusT * tSquared * 3d;

				resultList.Add(new PointD(
					oneMinusTCubed * p0.X + oneMinusTSquaredTimesTTimesThree * p1.X + oneMinusTTimesTSquaredTimesThree * p2.X + tCubed * p3.X,
					oneMinusTCubed * p0.Y + oneMinusTSquaredTimesTTimesThree * p1.Y + oneMinusTTimesTSquaredTimesThree * p2.Y + tCubed * p3.Y));
			}
		}
	}
}
