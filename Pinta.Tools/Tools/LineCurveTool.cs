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


		private int selectedPointIndex = -1;
		private int selectedPointCurveIndex = 0;

		/// <summary>
		/// The selected ControlPoint.
		/// </summary>
		private ControlPoint SelectedPoint
		{
			get
			{
				return SelectedCurveEngine.ControlPoints[selectedPointIndex];
			}

			set
			{
				SelectedCurveEngine.ControlPoints[selectedPointIndex] = value;
			}
		}

		/// <summary>
		/// The selected curve's CurveEngine.
		/// </summary>
		private CurveEngine SelectedCurveEngine
		{
			get
			{
				if (cEngines.CEL.Count > selectedPointCurveIndex)
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


		//This is used to temporarily store the UserLayer's previous ImageSurface state.
		private ImageSurface user_undo_surface;

		//Helps to keep track of the first modification on a curve after the mouse is clicked, to prevent unnecessary history items.
		private bool clickedWithoutModifying = false;


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

		private Gtk.SeparatorToolItem arrowSep;
		private ToolBarLabel showArrowOneLabel, showArrowTwoLabel;
		private ToolBarComboBox showArrowOneBox, showArrowTwoBox;

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

			if (showArrowOneBox == null)
			{
				showArrowOneBox = new ToolBarComboBox(65, 1, true, "Show", "Hide");
				showArrowOneBox.ComboBox.Changed += new EventHandler(ComboBox_ArrowOneShowChanged);
			}

			tb.AppendItem(showArrowOneBox);


			if (showArrowTwoLabel == null)
			{
				showArrowTwoLabel = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Arrow 2")));
			}

			tb.AppendItem(showArrowTwoLabel);

			if (showArrowTwoBox == null)
			{
				showArrowTwoBox = new ToolBarComboBox(65, 1, true, "Show", "Hide");
				showArrowTwoBox.ComboBox.Changed += new EventHandler(ComboBox_ArrowTwoShowChanged);
			}

			tb.AppendItem(showArrowTwoBox);
		}

		/// <summary>
		/// Reset the Arrow options in the toolbar to their default values.
		/// </summary>
		private void resetToolbarArrowOptions()
		{
			//TODO: Additionally hide Arrow options!

			showArrowOneBox.ComboBox.Active = 1;
			showArrowTwoBox.ComboBox.Active = 1;
		}

		/// <summary>
		/// Set the Arrow options in the toolbar to their respective values for the current curve.
		/// </summary>
		private void setToolbarArrowOptions()
		{
			CurveEngine selEngine = SelectedCurveEngine;

			if (selEngine != null)
			{
				showArrowOneBox.ComboBox.Active = selEngine.Arrow1.Show ? 0 : 1;
				showArrowTwoBox.ComboBox.Active = selEngine.Arrow2.Show ? 0 : 1;
			}
			else
			{
				//TODO: Hide Arrow options when no curve is selected!
			}
		}

		private void ComboBox_ArrowOneShowChanged(object sender, EventArgs e)
		{
			CurveEngine selEngine = SelectedCurveEngine;

			if (selEngine != null)
			{
				//Create a new CurveModifyHistoryItem so that the option change can be undone.
				PintaCore.Workspace.ActiveDocument.History.PushNewItem(
					new CurveModifyHistoryItem(Icon, Name, cEngines.PartialClone()));

				selEngine.Arrow1.Show = (showArrowOneBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text == "Show";
			}
		}

		private void ComboBox_ArrowTwoShowChanged(object sender, EventArgs e)
		{
			CurveEngine selEngine = SelectedCurveEngine;

			if (selEngine != null)
			{
				//Create a new CurveModifyHistoryItem so that the option change can be undone.
				PintaCore.Workspace.ActiveDocument.History.PushNewItem(
					new CurveModifyHistoryItem(Icon, Name, cEngines.PartialClone()));

				selEngine.Arrow2.Show = (showArrowTwoBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text == "Show";
			}
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
								cEngines.CEL[n].Arrow1.Draw(g, dirty, outline_color, genPoints[0], genPoints[1]);
							}
						}

						if (cEngines.CEL[n].Arrow2.Show)
						{
							if (genPoints.Length > 1)
							{
								cEngines.CEL[n].Arrow1.Draw(g, dirty, outline_color,
									genPoints[genPoints.Length - 1], genPoints[genPoints.Length - 2]);
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
				//Make sure that the undo surface isn't null and that there are actually points.
				if (user_undo_surface != null && cEngines.CEL[0].ControlPoints.Count > 0)
				{
					//Create a new CurvesHistoryItem so that the finalization of the curves can be undone.
					doc.History.PushNewItem(
						new CurvesHistoryItem(Icon, Name, user_undo_surface.Clone(), cEngines.PartialClone(), doc.CurrentUserLayer));
				}

				is_drawing = false;
				surface_modified = false;


				selectedPointIndex = -1;

				dirty = DrawShape(
					Utility.PointsToRectangle(shape_origin, new PointD(current_point.X, current_point.Y), shiftKey),
					doc.CurrentUserLayer, false);
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
		/// Finalize the curve onto the UserLayer and clear out any old curve data.
		/// </summary>
		private void finalizeCurve()
		{
			PintaCore.Workspace.ActiveDocument.ToolLayer.Hidden = true;

			drawCurves(false, true, false);

			cEngines = new CurveEngineCollection();
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
					//Create a new CurveModifyHistoryItem so that the deletion of a control point can be undone.
					PintaCore.Workspace.ActiveDocument.History.PushNewItem(
						new CurveModifyHistoryItem(Icon, Name, cEngines.PartialClone()));


					List<ControlPoint> controlPoints = SelectedCurveEngine.ControlPoints;


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

		public override void AfterUndo()
		{
			surface_modified = true;
			selectedPointIndex = -1;

			//Draw the current state.
			drawCurves(true, false, false);

			setToolbarArrowOptions();

			base.AfterUndo();
		}

		public override void AfterRedo()
		{
			surface_modified = true;
			selectedPointIndex = -1;

			//Draw the current state.
			drawCurves(true, false, false);

			setToolbarArrowOptions();

			base.AfterRedo();
		}
		
		protected override void OnKeyUp(Gtk.DrawingArea canvas, Gtk.KeyReleaseEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete || args.Event.Key == Gdk.Key.Up || args.Event.Key == Gdk.Key.Down
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

			lastMousePosition = point;

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

				List<ControlPoint> controlPoints = cEngines.CEL[closestCurveIndex].ControlPoints;

				bool clickedOnControlPoint = false;

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
							new CurveModifyHistoryItem(Icon, Name, cEngines.PartialClone()));

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

					//Keep track of the current UserLayer's ImageSurface.
					user_undo_surface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.Clone();

					//Finalize the previous curve (if needed).
					drawCurves(false, true, false);

					//Create a new CurvesHistoryItem so that the creation of a new curve can be undone.
					doc.History.PushNewItem(
						new CurvesHistoryItem(Icon, Name, user_undo_surface.Clone(), cEngines.PartialClone(), doc.CurrentUserLayer));


					is_drawing = true;

					//Clear out all of the old data.
					cEngines = new CurveEngineCollection();

					//Reset the toolbar arrow options.
					resetToolbarArrowOptions();



					//Add the first two points of the line. The second point will follow the mouse around until released.
					cEngines.CEL[0].ControlPoints.Add(new ControlPoint(new PointD(shape_origin.X, shape_origin.Y), DefaultEndPointTension));
					cEngines.CEL[0].ControlPoints.Add(new ControlPoint(new PointD(shape_origin.X + .01d, shape_origin.Y + .01d), DefaultEndPointTension));

					selectedPointIndex = 1;
					selectedPointCurveIndex = 0;
				}
				else
				{
					clickedWithoutModifying = true;
				}
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
					if (clickedWithoutModifying)
					{
						//Create a new CurveModifyHistoryItem so that the modification of the curve can be undone.
						doc.History.PushNewItem(
							new CurveModifyHistoryItem(Icon, Name, cEngines.PartialClone()));

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
