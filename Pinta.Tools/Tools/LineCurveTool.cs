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

		public const int SectionSize = 15;

		//To avoid numerous conversions. Don't change this.
		public const double SectionSizeDouble = SectionSize;

		//Don't change this; it's automatically calculated!
		public static int BorderingSectionRange = (int)Math.Ceiling(CurveClickRange / SectionSize);

		private int selectedPointIndex = 0;
		private int selectedPointCurveIndex = 0;

		private PointD hoverPoint = new PointD(-1d, -1d);
		private int hoveredPointAsControlPoint = -1;

		//This is used to temporarily store the UserLayer's and TextLayer's previous ImageSurface states.
		private Cairo.ImageSurface curves_undo_surface;
		private Cairo.ImageSurface user_undo_surface;
		private CurveEngine undo_engine;
		
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
							cEngine.GeneratedPointsCollection[n] = CurveEngine.GenerateCardinalSplinePolynomialCurvePoints(n).ToArray();

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
							cEngine.GeneratedPointsCollection[n] = CurveEngine.GenerateCardinalSplinePolynomialCurvePoints(n).ToArray();

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
					int closestCurveIndex, closestPointIndex;
					PointD closestPoint;
					double closestDistance;

					OrganizedPointCollection.findClosestPoint(current_point, out closestCurveIndex, out closestPointIndex, out closestPoint, out closestDistance);

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
				for (int n = 0; n < cEngine.OrganizedPointsCollection.Count; ++n)
				{
					cEngine.OrganizedPointsCollection[n].ClearCollection();

					int pointIndex = 0;

					foreach (PointD p in cEngine.GeneratedPointsCollection[n])
					{
						cEngine.OrganizedPointsCollection[0].StoreAndOrganizePoint(new OrganizedPoint(new PointD(p.X, p.Y), pointIndex));

						//Keep track of the point's order in relation to the control points.
						if (cEngine.GivenPointsCollection[n].Count > pointIndex
							&& p.X == cEngine.GivenPointsCollection[n][pointIndex].Position.X
							&& p.Y == cEngine.GivenPointsCollection[n][pointIndex].Position.Y)
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



			int closestCurveIndex, closestPointIndex;
			PointD closestPoint;
			double closestDistance;

			OrganizedPointCollection.findClosestPoint(current_point,
				out closestCurveIndex, out closestPointIndex, out closestPoint, out closestDistance);



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
	}
}
