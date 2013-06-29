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
		private static double curveClickRange = 8d;
		private static double defaultTension = 1d / 3d;

		private int selectedPointIndex = 0;
		private int selectedPointCurveIndex = 0;

		//This is used to temporarily store the UserLayer's and TextLayer's previous ImageSurface states.
		private Cairo.ImageSurface curves_undo_surface;
		private Cairo.ImageSurface user_undo_surface;
		private CurveEngine undo_engine;

		// The selection from when editing started. This ensures that text doesn't suddenly disappear/appear
		// if the selection changes before the text is finalized.
		private DocumentSelection selection; //***IMPLEMENT THIS LATER?***

		private CurveEngine CurrentCurveEngine
		{
			get
			{
				return PintaCore.Workspace.HasOpenDocuments ?
					PintaCore.Workspace.ActiveDocument.CurrentUserLayer.cEngine : null;
			}

			set
			{
				PintaCore.Workspace.ActiveDocument.CurrentUserLayer.cEngine = value;
			}
		}

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

			using (Context g = new Context(l.Surface))
			{
				List<List<ControlPoint>> gPC = CurrentCurveEngine.givenPointsCollection;



				g.AppendPath(doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip();

				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				g.LineWidth = BrushWidth;



				Color cpColor = new Color(0d, .06d, .6d);

				//Used to find the minimal invalidation rectangle.
				double dX = -1d;
				double dY = -1d;
				double dX2 = -1d;
				double dY2 = -1d;

				for (int n = 0; n < gPC.Count; ++n)
				{
					if (gPC[n].Count > 0)
					{
						CurrentCurveEngine.generatedCurvePointsCollection[n] = GenerateCardinalSplinePolynomialCurvePoints(n).ToArray();

						Rectangle tempDirty = g.DrawPolygonal(CurrentCurveEngine.generatedCurvePointsCollection[n], outline_color);

						//Expand the invalidation rectangle as necessary.
						if (dX2 == -1d)
						{
							dX = tempDirty.X;
							dY = tempDirty.Y;
							dX2 = tempDirty.X + tempDirty.Width;
							dY2 = tempDirty.Y + tempDirty.Height;
						}
						else
						{
							if (tempDirty.X < dX)
							{
								dX = tempDirty.X;
							}

							if (tempDirty.Y < dY)
							{
								dY = tempDirty.Y;
							}

							if (tempDirty.X + tempDirty.Width > dX2)
							{
								dX2 = tempDirty.X + tempDirty.Width;
							}

							if (tempDirty.Y + tempDirty.Height > dY2)
							{
								dY2 = tempDirty.Y + tempDirty.Height;
							}
						}



						//Draw the control points.
						for (int i = 0; i < gPC[n].Count; ++i)
						{
							g.DrawEllipse(new Rectangle(gPC[n][i].Position.X - 1d, gPC[n][i].Position.Y - 1d, 2d, 2d), cpColor, 2);
						}
					}
				}

				if (dY2 == -1d)
				{
					dX = 0d;
					dY = 0d;
					dX2 = 0d;
					dY2 = 0d;
				}

				dirty = new Rectangle(dX, dY, dX2 - dX, dY2 - dY);
				dirty.Inflate(8, 8);



				//NOTE: Control point graphics need to replicate the coloring of selection points.
				if (selectedPointIndex > -1)
				{
					//Draw a ring around the selected point.
					g.DrawEllipse(
						new Rectangle(
							gPC[selectedPointCurveIndex][selectedPointIndex].Position.X - 4d,
							gPC[selectedPointCurveIndex][selectedPointIndex].Position.Y - 4d, 8d, 8d),
						cpColor, 1);
				}
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

		protected void drawCurves(bool finalize, bool shiftKey)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			doc.CurrentUserLayer.CurvesLayer.Layer.Clear();

			Rectangle dirty = DrawShape(
				Utility.PointsToRectangle(shape_origin, new PointD(current_point.X, current_point.Y), shiftKey),
				doc.CurrentUserLayer.CurvesLayer.Layer,
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



			if (finalize)
			{
				is_drawing = false;

				if (surface_modified)
				{
					//Make sure that neither undo surface is null.
					if (curves_undo_surface != null && user_undo_surface != null)
					{
						//Create a new CurvesHistoryItem so that the updated drawing of curves can be undone.
						doc.History.PushNewItem(new CurvesHistoryItem(Icon, Name,
							curves_undo_surface.Clone(), user_undo_surface.Clone(),
							undo_engine.Clone(), doc.CurrentUserLayer));
					}
				}

				surface_modified = false;
			}
		}

		/*protected void drawCurveOnToolLayer(bool shiftKey)
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
		}*/

		protected override void OnKeyDown(Gtk.DrawingArea canvas, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete)
			{
				if (selectedPointIndex > -1)
				{
					List<List<ControlPoint>> gPC = CurrentCurveEngine.givenPointsCollection;


					undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.Clone();

					//Delete the selected point from the curve.
					gPC[selectedPointCurveIndex].RemoveAt(selectedPointIndex);

					//Set the newly selected point to be the median-most point on the curve, if possible. Otherwise, set it to noPointSelected.
					if (gPC[selectedPointCurveIndex].Count > 0)
					{
						if (selectedPointIndex != gPC[selectedPointCurveIndex].Count / 2)
						{
							if (selectedPointIndex > gPC[selectedPointCurveIndex].Count / 2)
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

					drawCurves(true, false);
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



			//Store the previous state of the current UserLayer's and CurvesLayer's ImageSurfaces.
			user_undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.Clone();
			curves_undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.CurvesLayer.Layer.Surface.Clone();

			//Store the previous state of the Curve Engine.
			undo_engine = CurrentCurveEngine.Clone();



			List<List<ControlPoint>> gPC = CurrentCurveEngine.givenPointsCollection;


			PointD closestPoint = new PointD(double.MaxValue, double.MaxValue);
			double closestDistance = double.MaxValue;
			double currentDistance = double.MaxValue;
			int closestPointNumber = -1;
			int closestPointCurveNumber = 0;
			int currentPointNumber = 0;

			for (int n = 0; n < gPC.Count; ++n)
			{
				if (gPC[n].Count > 0)
				{
					//Reset this for each curve.
					currentPointNumber = 0;

					//Find the closest point on the curve to the mouse position.
					foreach (PointD p in CurrentCurveEngine.generatedCurvePointsCollection[n])
					{
						if (p.X == gPC[n][currentPointNumber].Position.X && p.Y == gPC[n][currentPointNumber].Position.Y)
						{
							++currentPointNumber;
						}

						currentDistance = p.Distance(current_point);

						if (currentDistance < closestDistance)
						{
							closestPoint = p;
							closestDistance = currentDistance;

							closestPointNumber = currentPointNumber;
							closestPointCurveNumber = n;
						}
					}
				}
			}



			bool clickedOnControlPoint = false;

			if (closestDistance < curveClickRange)
			{
				//User clicked on a generated point on a line/curve.

				if (closestPointNumber > 0)
				{
					//Note: compare the current_point's distance here (instead of the closestPoint) because it's the actual mouse position.
					if (gPC[closestPointCurveNumber].Count > closestPointNumber &&
						current_point.Distance(gPC[closestPointCurveNumber][closestPointNumber].Position) < curveClickRange)
					{
						//User clicked on a control point (on the "previous order" side of the point).

						selectedPointIndex = closestPointNumber;
						selectedPointCurveIndex = closestPointCurveNumber;

						clickedOnControlPoint = true;
					}
					else if (current_point.Distance(gPC[closestPointCurveNumber][closestPointNumber - 1].Position) < curveClickRange)
					{
						//User clicked on a control point (on the "following order" side of the point).

						selectedPointIndex = closestPointNumber - 1;
						selectedPointCurveIndex = closestPointCurveNumber;

						clickedOnControlPoint = true;
					}
				}

				if (!clickedOnControlPoint)
				{
					//User clicked on a non-control point on a line/curve.

					gPC[closestPointCurveNumber].Insert(closestPointNumber,
						new ControlPoint(new PointD(current_point.X, current_point.Y), defaultTension));

					selectedPointIndex = closestPointNumber;
					selectedPointCurveIndex = closestPointCurveNumber;
				}
			}
			else
			{
				//User clicked outside of any lines/curves.

				//Make sure there's actually a curve.
				if (gPC.Count > 0)
				{
					//Make sure that neither undo surface is null.
					if (curves_undo_surface != null && user_undo_surface != null)
					{
						//Create a new CurvesHistoryItem so that the current curve can be finalized.
						doc.History.PushNewItem(new CurvesHistoryItem(Icon, Name,
							curves_undo_surface.Clone(), user_undo_surface.Clone(),
							undo_engine.Clone(), doc.CurrentUserLayer));
					}
				}

				if (gPC[0].Count > 0)
				{
					//Create a new curve.
					gPC.Insert(0, new List<ControlPoint>());
					CurrentCurveEngine.generatedCurvePointsCollection.Insert(0, new PointD[0]);
				}
				
				//Add the first two points of the line. The second point will follow the mouse around until released.
				gPC[0].Add(new ControlPoint(new PointD(shape_origin.X, shape_origin.Y), defaultTension));
				gPC[0].Add(new ControlPoint(new PointD(shape_origin.X + .01d, shape_origin.Y + .01d), defaultTension));

				selectedPointIndex = 1;
				selectedPointCurveIndex = 0;
			}



			if (!clickedOnControlPoint)
			{
				surface_modified = true;

				drawCurves(false, (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask);
			}
		}

		protected override void OnMouseUp(Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			doc.ToolLayer.Hidden = true;

			current_point = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));

			drawCurves(true, args.Event.IsShiftPressed());
		}

		protected override void OnMouseMove(object o, Gtk.MotionNotifyEventArgs args, PointD point)
		{
			if (!is_drawing)
				return;


			List<List<ControlPoint>> gPC = CurrentCurveEngine.givenPointsCollection;


			//Make sure the point was moved.
			if (point.X != gPC[selectedPointCurveIndex][selectedPointIndex].Position.X
				|| point.Y != gPC[selectedPointCurveIndex][selectedPointIndex].Position.Y)
			{
				Document doc = PintaCore.Workspace.ActiveDocument;

				current_point = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));


				gPC[selectedPointCurveIndex].RemoveAt(selectedPointIndex);
				gPC[selectedPointCurveIndex].Insert(selectedPointIndex, new ControlPoint(new PointD(current_point.X, current_point.Y), defaultTension));

				surface_modified = true;



				drawCurves(false, (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask);
			}
		}

		/// <summary>
		/// Generate each point in a cardinal spline polynomial curve that passes through the given control points.
		/// </summary>
		/// <param name="curveNum">The number of the curve to generate the points for.</param>
		/// <returns></returns>
		protected List<PointD> GenerateCardinalSplinePolynomialCurvePoints(int curveNum)
		{
			List<List<ControlPoint>> gPC = CurrentCurveEngine.givenPointsCollection;


			List<PointD> generatedPoints = new List<PointD>();

			//Note: it's important that there be many generated points even if there are only 2 given points and it's just a line.
			//This is because the generated points are used in the check that determines if the mouse clicks on the line/curve.
			if (gPC[curveNum].Count < 2)
			{
				foreach (ControlPoint cP in gPC[curveNum])
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
				gPC[curveNum][1].Tension * (gPC[curveNum][1].Position.X - gPC[curveNum][0].Position.X),
				gPC[curveNum][1].Tension * (gPC[curveNum][1].Position.Y - gPC[curveNum][0].Position.Y)));

			//Calculate all of the middle tangents.
			for (int i = 1; i < gPC[curveNum].Count - 1; ++i)
			{
				bezierTangents.Add(new PointD(
					gPC[curveNum][i + 1].Tension * (gPC[curveNum][i + 1].Position.X - gPC[curveNum][i - 1].Position.X),
					gPC[curveNum][i + 1].Tension * (gPC[curveNum][i + 1].Position.Y - gPC[curveNum][i - 1].Position.Y)));
			}

			//Calculate the last tangent.
			bezierTangents.Add(new PointD(
				gPC[curveNum][gPC[curveNum].Count - 1].Tension * (gPC[curveNum][gPC[curveNum].Count - 1].Position.X - gPC[curveNum][gPC[curveNum].Count - 2].Position.X),
				gPC[curveNum][gPC[curveNum].Count - 1].Tension * (gPC[curveNum][gPC[curveNum].Count - 1].Position.Y - gPC[curveNum][gPC[curveNum].Count - 2].Position.Y)));



			//For optimization.
			int iMinusOne;

			//Generate the resulting curve's points with consecutive cubic Bezier curves that
			//use the given points as end points and the calculated tangents as control points.
			for (int i = 1; i < gPC[curveNum].Count; ++i)
			{
				iMinusOne = i - 1;

				GenerateCubicBezierCurvePoints(
					generatedPoints,
					gPC[curveNum][iMinusOne].Position,
					new PointD(
						gPC[curveNum][iMinusOne].Position.X + bezierTangents[iMinusOne].X,
						gPC[curveNum][iMinusOne].Position.Y + bezierTangents[iMinusOne].Y),
					new PointD(
						gPC[curveNum][i].Position.X - bezierTangents[i].X,
						gPC[curveNum][i].Position.Y - bezierTangents[i].Y),
					gPC[curveNum][i].Position);
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
