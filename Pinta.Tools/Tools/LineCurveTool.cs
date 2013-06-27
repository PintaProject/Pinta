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
		private List<PointD> givenPoints = new List<PointD>();
		private PointD[] generatedCurvePoints = new PointD[0];
		private static PointD noPointSelected = new PointD();
		private PointD selectedPoint = noPointSelected;
		private int selectedPointIndex = 0;

		public override string Name {
			get { return Catalog.GetString ("Line"); }
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

		protected override Rectangle DrawShape (Rectangle rect, Layer l)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			Rectangle dirty;

			if (givenPoints.Count > 0)
			{
				using (Context g = new Context(l.Surface))
				{
					g.AppendPath(doc.Selection.SelectionPath);
					g.FillRule = FillRule.EvenOdd;
					g.Clip();

					g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;



					//Clear the Canvas so it doesn't get cluttered with the other curves drawn.
					g.FillRectangle(new Rectangle(0d, 0d, doc.ImageSize.Width - 1d, doc.ImageSize.Height - 1d), new Color(1d, 1d, 1d));

					generatedCurvePoints = GenerateCardinalSplinePolynomialCurvePoints(givenPoints).ToArray();

					g.LineWidth = BrushWidth;
					dirty = g.DrawPolygonal(generatedCurvePoints, outline_color);
					dirty.Inflate(10, 10);


					Color cpColor = new Color(0d, .06d, .6d);

					//Draw the control points.
					for (int i = 0; i < givenPoints.Count; ++i)
					{
						g.DrawEllipse(new Rectangle(givenPoints[i].X - 1d, givenPoints[i].Y - 1d, 2d, 2d), cpColor, 2);
					}

					if (!selectedPoint.Equals(noPointSelected))
					{
						//Draw a ring around the selected point.
						g.DrawEllipse(new Rectangle(selectedPoint.X - 4d, selectedPoint.Y - 4d, 8d, 8d), cpColor, 1);
					}


					//Draw a line from the starting mouse position to the ending mouse position.
					//g.DrawLine(givenPoints[0], current_point, new Color(.01d, .75d, 0d), 1);
				}
			}
			else
			{
				dirty = last_dirty;
			}
			
			return dirty;
		}

		/// <summary>
		/// Forces the line to snap to angles.
		/// </summary>
		protected override Rectangle DrawShape (Rectangle r, Layer l, bool shiftkey_pressed)
		{
			if (shiftkey_pressed) {
				PointD dir = new PointD(current_point.X - shape_origin.X, current_point.Y - shape_origin.Y);
				double theta = Math.Atan2(dir.Y, dir.X);
				double len = Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);

				theta = Math.Round(12 * theta / Math.PI) * Math.PI / 12;
				current_point = new PointD((shape_origin.X + len * Math.Cos(theta)), (shape_origin.Y + len * Math.Sin(theta)));
			}
			return DrawShape (r, l);
		}

		protected void drawCurveOnToolLayer(bool shiftKey)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			doc.ToolLayer.Clear();

			Rectangle dirty = DrawShape(
				Utility.PointsToRectangle(shape_origin, new PointD(current_point.X, current_point.Y), shiftKey),
				doc.ToolLayer,
				shiftKey);

			// Increase the size of the dirty rect to account for antialiasing.
			if (UseAntialiasing)
			{
				dirty = dirty.Inflate(1, 1);
			}

			dirty = dirty.Clamp();

			doc.Workspace.Invalidate(last_dirty.ToGdkRectangle());
			doc.Workspace.Invalidate(dirty.ToGdkRectangle());

			last_dirty = dirty;
		}

		protected void drawCurveOnUserLayer(bool shiftKey)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			DrawShape(Utility.PointsToRectangle(shape_origin, new PointD(current_point.X, current_point.Y), shiftKey), doc.CurrentUserLayer, shiftKey);

			doc.Workspace.Invalidate(last_dirty.ToGdkRectangle());

			is_drawing = false;

			if (surface_modified)
				doc.History.PushNewItem(CreateHistoryItem());
			else if (undo_surface != null)
				(undo_surface as IDisposable).Dispose();

			surface_modified = false;
		}

		protected override void OnKeyDown(Gtk.DrawingArea canvas, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				if (!selectedPoint.Equals(noPointSelected))
				{
					undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.Clone();

					//Delete the selected point from the curve.
					givenPoints.Remove(selectedPoint);

					//Set the newly selected point to be the median-most point on the curve, if possible. Otherwise, set it to noPointSelected.
					if (givenPoints.Count > 0)
					{
						if (selectedPointIndex != givenPoints.Count / 2)
						{
							if (selectedPointIndex > givenPoints.Count / 2)
							{
								--selectedPointIndex;
							}
							else
							{
								++selectedPointIndex;
							}
						}

						selectedPoint = givenPoints[selectedPointIndex];
					}
					else
					{
						selectedPoint = noPointSelected;
					}

					surface_modified = true;

					drawCurveOnUserLayer(false);
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

		protected override void OnMouseDown(Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, PointD point)
		{
			// If we are already drawing, ignore any additional mouse down events
			if (is_drawing)
				return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			shape_origin = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));
			current_point = shape_origin;

			is_drawing = true;

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

			doc.ToolLayer.Hidden = false;

			surface_modified = false;
			undo_surface = doc.CurrentUserLayer.Surface.Clone();



			PointD closestPoint = new PointD(double.MaxValue, double.MaxValue);
			double closestDistance = double.MaxValue;
			double currentDistance = double.MaxValue;
			int closestPointNumber = -1;
			int currentPointNumber = 0;

			//Find the closest point on the curve to the mouse position.
			//*NOTE:* later on, this will need to be changed to check ALL lines/curves on the current layer!!!
			//Keep track of which line/curve is clicked on as well!!!
			foreach (PointD p in generatedCurvePoints)
			{
				if (p.X == givenPoints[currentPointNumber].X && p.Y == givenPoints[currentPointNumber].Y)
				{
					++currentPointNumber;
				}

				currentDistance = p.Distance(current_point);

				if (currentDistance < closestDistance)
				{
					closestPoint = p;
					closestDistance = currentDistance;

					closestPointNumber = currentPointNumber;
				}
			}



			bool clickedOnControlPoint = false;

			if (closestDistance < 8d)
			{
				//User clicked on a generated point on a line/curve.

				if (givenPoints.Count - 1 < closestPointNumber)
				{
					--closestPointNumber;
				}

				if (closestPointNumber > 0)
				{
					if (closestPoint.Distance(givenPoints[closestPointNumber]) < 8d)
					{
						//User clicked on a control point (on the "previous order" side of the point).

						selectedPoint = givenPoints[closestPointNumber];
						selectedPointIndex = closestPointNumber;

						clickedOnControlPoint = true;
					}
					else if (closestPoint.Distance(givenPoints[closestPointNumber - 1]) < 8d)
					{
						//User clicked on a control point (on the "following order" side of the point).

						selectedPoint = givenPoints[closestPointNumber - 1];
						selectedPointIndex = closestPointNumber - 1;

						clickedOnControlPoint = true;
					}
				}

				if (!clickedOnControlPoint)
				{
					//User clicked on a non-control point on a line/curve.

					givenPoints.Insert(closestPointNumber, new PointD(current_point.X, current_point.Y));
					selectedPointIndex = closestPointNumber;
					selectedPoint = givenPoints[selectedPointIndex];
				}
			}
			else
			{
				//User clicked outside of any lines/curves.

				//*"Finalize" ("store") the curve here*?
				givenPoints.Clear();
				
				//Add the first two points of the line. The second point will follow the mouse around until released.
				givenPoints.Add(new PointD(shape_origin.X, shape_origin.Y));
				givenPoints.Add(new PointD(shape_origin.X + .01d, shape_origin.Y + .01d));

				selectedPoint = givenPoints[1];
				selectedPointIndex = 1;
			}



			if (!clickedOnControlPoint)
			{
				surface_modified = true;

				drawCurveOnToolLayer((args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask);
			}
		}

		protected override void OnMouseUp(Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			doc.ToolLayer.Hidden = true;

			current_point = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));

			drawCurveOnUserLayer(args.Event.IsShiftPressed());
		}

		protected override void OnMouseMove(object o, Gtk.MotionNotifyEventArgs args, PointD point)
		{
			if (!is_drawing)
				return;

			//Make sure the point was moved.
			if (point.X != selectedPoint.X || point.Y != selectedPoint.Y)
			{
				Document doc = PintaCore.Workspace.ActiveDocument;

				current_point = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));



				givenPoints.Remove(selectedPoint);
				givenPoints.Insert(selectedPointIndex, new PointD(current_point.X, current_point.Y));
				selectedPoint = givenPoints[selectedPointIndex];

				surface_modified = true;



				drawCurveOnToolLayer((args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask);
			}
		}

		/// <summary>
		/// Generate each point in a cardinal spline polynomial curve that passes through the given control points.
		/// </summary>
		/// <param name="givenPoints">The given points in the curve (in order) to base the rest of the curve off of.</param>
		/// <returns></returns>
		protected List<PointD> GenerateCardinalSplinePolynomialCurvePoints(List<PointD> givenPoints)
		{
			//Note: it's important that there be many generated points even if there are only 2 given points and it's just a line.
			//This is because the generated points are used in the check that determines if the mouse clicks on the line/curve.
			if (givenPoints.Count < 2)
			{
				return givenPoints;
			}


			List<PointD> generatedPoints = new List<PointD>();

			//Generate tangents for each of the smaller cubic Bezier curves that make up each segment of the resulting curve.

			//Stores all of the tangent values.
			List<PointD> bezierTangents = new List<PointD>();

			double tension = 1d / 3d; //Change the second number here to modify the curves' tensions. This number should be between 0d and 1d.

			//Calculate the first tangent.
			bezierTangents.Add(new PointD(tension * (givenPoints[1].X - givenPoints[0].X), tension * (givenPoints[1].Y - givenPoints[0].Y)));

			//Calculate all of the middle tangents.
			for (int i = 1; i < givenPoints.Count - 1; ++i)
			{
				bezierTangents.Add(new PointD(tension * (givenPoints[i + 1].X - givenPoints[i - 1].X), tension * (givenPoints[i + 1].Y - givenPoints[i - 1].Y)));
			}

			//Calculate the last tangent.
			bezierTangents.Add(new PointD(
				tension * (givenPoints[givenPoints.Count - 1].X - givenPoints[givenPoints.Count - 2].X),
				tension * (givenPoints[givenPoints.Count - 1].Y - givenPoints[givenPoints.Count - 2].Y)));



			//For optimization.
			int iMinusOne;

			//Generate the resulting curve's points with consecutive cubic Bezier curves that
			//use the given points as end points and the calculated tangents as control points.
			for (int i = 1; i < givenPoints.Count; ++i)
			{
				iMinusOne = i - 1;

				GenerateCubicBezierCurvePoints(
					generatedPoints,
					givenPoints[iMinusOne],
					new PointD(
						givenPoints[iMinusOne].X + bezierTangents[iMinusOne].X,
						givenPoints[iMinusOne].Y + bezierTangents[iMinusOne].Y),
					new PointD(
						givenPoints[i].X - bezierTangents[i].X,
						givenPoints[i].Y - bezierTangents[i].Y),
					givenPoints[i]);
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
		protected void GenerateCubicBezierCurvePoints(List<PointD> resultList, PointD p0, PointD p1, PointD p2, PointD p3)
		{
			//Note: this must be low enough for mouse clicks to be properly considered on/off the line/curve at any given point.
			double tInterval = .01d;

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
