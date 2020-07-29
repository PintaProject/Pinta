// 
// BaseEditEngine.cs
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
    //The EditEngine was created for tools that wish to utilize any of the control point, line/curve, hover point (reacting to the mouse),
    //and etc. code that was originally used in the LineCurveTool for editability. If a class wishes to use it, it should create and instantiate
    //a protected instance of the EditEngine inside the class and then utilize it in a similar fashion to any of the editable tools.
    public abstract class BaseEditEngine
    {
		static BaseEditEngine ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Tools.Line.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Tools.Line.png")));
			fact.Add ("Tools.Rectangle.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Tools.Rectangle.png")));
			fact.Add ("Tools.Ellipse.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Tools.Ellipse.png")));
			fact.Add ("Tools.RoundedRectangle.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Tools.RoundedRectangle.png")));
			fact.AddDefault ();
		}

		public enum ShapeTypes
		{
			OpenLineCurveSeries,
			ClosedLineCurveSeries,
			Ellipse,
			RoundedLineSeries
		}

		public static Dictionary<ShapeTypes, ShapeTool> CorrespondingTools = new Dictionary<ShapeTypes, ShapeTool>();

        protected abstract string ShapeName { get; }

        protected readonly ShapeTool owner;

        protected bool is_drawing = false;

		protected Rectangle? last_dirty = null;
		protected Rectangle? last_hover = null;

		protected double last_control_pt_size = 0d;

        protected PointD shape_origin;
        protected PointD current_point;

		public static Color OutlineColor
		{
			get { return PintaCore.Palette.PrimaryColor; }
			set { PintaCore.Palette.PrimaryColor = value; }
		}

		public static Color FillColor
		{
			get { return PintaCore.Palette.SecondaryColor; }
			set { PintaCore.Palette.SecondaryColor = value; }
		}

        protected ToolBarComboBox brush_width;
        protected ToolBarLabel brush_width_label;
        protected ToolBarButton brush_width_minus;
        protected ToolBarButton brush_width_plus;
        protected ToolBarLabel fill_label;
        protected ToolBarDropDownButton fill_button;
        protected Gtk.SeparatorToolItem fill_sep;

		protected ToolBarLabel shape_type_label;
		protected ToolBarDropDownButton shape_type_button;
		protected Gtk.SeparatorToolItem shape_type_sep;

        protected DashPatternBox dash_pattern_box = new DashPatternBox();
		private string prev_dash_pattern = "-";

		private bool prev_antialiasing = true;

        public int BrushWidth
        {
            get
            {
				if (brush_width != null)
				{
					int width;

					if (Int32.TryParse(brush_width.ComboBox.ActiveText, out width))
					{
						if (width > 0)
						{
							(brush_width.ComboBox as Gtk.ComboBoxEntry).Entry.Text = width.ToString();

							return width;
						}
					}

					(brush_width.ComboBox as Gtk.ComboBoxEntry).Entry.Text = BaseTool.DEFAULT_BRUSH_WIDTH.ToString();
				}

                return BaseTool.DEFAULT_BRUSH_WIDTH;
            }
            
			set
			{
				if (brush_width != null)
				{
					(brush_width.ComboBox as Gtk.ComboBoxEntry).Entry.Text = value.ToString();
				}
			}
        }

		private int prev_brush_width = BaseTool.DEFAULT_BRUSH_WIDTH;

        private bool StrokeShape { get { return (int)fill_button.SelectedItem.Tag % 2 == 0; } }
        private bool FillShape { get { return (int)fill_button.SelectedItem.Tag >= 1; } }

		private ShapeTypes ShapeType
		{
			get
			{
				return (ShapeTypes)(int)shape_type_button.SelectedItem.Tag;
			}
		}

        protected static readonly Color hover_color =
            new Color(ToolControl.FillColor.R, ToolControl.FillColor.G, ToolControl.FillColor.B, ToolControl.FillColor.A * 2d / 3d);

		public const double ShapeClickStartingRange = 10d;
		public const double ShapeClickThicknessFactor = 1d;
		public const double DefaultEndPointTension = 0d;
		public const double DefaultMidPointTension = 1d / 3d;

		public int SelectedPointIndex, SelectedShapeIndex;
		protected int prev_selected_shape_index;

        /// <summary>
        /// The selected ControlPoint.
        /// </summary>
		public ControlPoint SelectedPoint
        {
            get
            {
                ShapeEngine selEngine = SelectedShapeEngine;

                if (selEngine != null && selEngine.ControlPoints.Count > SelectedPointIndex)
                {
                    return selEngine.ControlPoints[SelectedPointIndex];
                }
                else
                {
                    return null;
                }
            }

            set
            {
                ShapeEngine selEngine = SelectedShapeEngine;

                if (selEngine != null && selEngine.ControlPoints.Count > SelectedPointIndex)
                {
                    selEngine.ControlPoints[SelectedPointIndex] = value;
                }
            }
        }

		/// <summary>
		/// The active shape's ShapeEngine. A point does not have to be selected here, only a shape. This can be null.
		/// </summary>
		public ShapeEngine ActiveShapeEngine
		{
			get
			{
				if (SelectedShapeIndex > -1 && SEngines.Count > SelectedShapeIndex)
				{
					return SEngines[SelectedShapeIndex];
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>
		/// The selected shape's ShapeEngine. This requires that a point in the shape be selected and should be used in most cases. This can be null.
		/// </summary>
		public ShapeEngine SelectedShapeEngine
		{
			get
			{
				if (SelectedPointIndex > -1)
				{
					return ActiveShapeEngine;
				}
				else
				{
					return null;
				}
			}
		}

		protected PointD hover_point = new PointD(-1d, -1d);
		protected int hovered_pt_as_control_pt = -1;

		protected bool changing_tension = false;
		protected PointD last_mouse_pos = new PointD(0d, 0d);

        //Helps to keep track of the first modification on a shape after the mouse is clicked, to prevent unnecessary history items.
		protected bool clicked_without_modifying = false;

        //Stores the editable shape data.
		public static ShapeEngineCollection SEngines = new ShapeEngineCollection();

        #region ToolbarEventHandlers

        protected virtual void BrushMinusButtonClickedEvent(object o, EventArgs args)
        {
            if (BrushWidth > 1)
                BrushWidth--;

			//No need to store previous settings or redraw, as this is done in the Changed event handler.
        }

        protected virtual void BrushPlusButtonClickedEvent(object o, EventArgs args)
        {
            BrushWidth++;

			//No need to store previous settings or redraw, as this is done in the Changed event handler.
        }

		protected void Palette_PrimaryColorChanged(object sender, EventArgs e)
		{
			ShapeEngine activeEngine = ActiveShapeEngine;

			if (activeEngine != null)
			{
				activeEngine.OutlineColor = OutlineColor.Clone();

				DrawActiveShape(false, false, true, false, false);
			}
		}

		protected void Palette_SecondaryColorChanged(object sender, EventArgs e)
		{
			ShapeEngine activeEngine = ActiveShapeEngine;

			if (activeEngine != null)
			{
				activeEngine.FillColor = FillColor.Clone();

				DrawActiveShape(false, false, true, false, false);
			}
		}

        #endregion ToolbarEventHandlers


        public BaseEditEngine(ShapeTool passedOwner)
        {
            owner = passedOwner;

			owner.IsEditableShapeTool = true;

			ResetShapes();
        }

        public virtual void HandleBuildToolBar(Gtk.Toolbar tb)
        {
            if (brush_width_label == null)
                brush_width_label = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Brush width")));

            tb.AppendItem(brush_width_label);

            if (brush_width_minus == null)
            {
                brush_width_minus = new ToolBarButton("Toolbar.MinusButton.png", "", Catalog.GetString("Decrease brush size"));
                brush_width_minus.Clicked += BrushMinusButtonClickedEvent;
            }

            tb.AppendItem(brush_width_minus);

			if (brush_width == null)
			{
				brush_width = new ToolBarComboBox(65, 1, true, "1", "2", "3", "4", "5", "6", "7", "8", "9",
					"10", "11", "12", "13", "14", "15", "20", "25", "30", "35", "40", "45", "50", "55");

				brush_width.ComboBox.Changed += (o, e) =>
				{
					ShapeEngine selEngine = SelectedShapeEngine;

					if (selEngine != null)
					{
						selEngine.BrushWidth = BrushWidth;
                        StorePreviousSettings ();
                        DrawActiveShape (false, false, true, false, false);
					}
				};
			}

            tb.AppendItem(brush_width);

            if (brush_width_plus == null)
            {
                brush_width_plus = new ToolBarButton("Toolbar.PlusButton.png", "", Catalog.GetString("Increase brush size"));
                brush_width_plus.Clicked += BrushPlusButtonClickedEvent;
            }

            tb.AppendItem(brush_width_plus);

            if (fill_sep == null)
                fill_sep = new Gtk.SeparatorToolItem();

            tb.AppendItem(fill_sep);

            if (fill_label == null)
                fill_label = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Fill Style")));

            tb.AppendItem(fill_label);

            if (fill_button == null)
            {
                fill_button = new ToolBarDropDownButton();

                fill_button.AddItem(Catalog.GetString("Outline Shape"), "ShapeTool.Outline.png", 0);
                fill_button.AddItem(Catalog.GetString("Fill Shape"), "ShapeTool.Fill.png", 1);
                fill_button.AddItem(Catalog.GetString("Fill and Outline Shape"), "ShapeTool.OutlineFill.png", 2);
            }

            tb.AppendItem(fill_button);

			if (shape_type_sep == null)
				shape_type_sep = new Gtk.SeparatorToolItem();

			tb.AppendItem(shape_type_sep);

			if (shape_type_label == null)
				shape_type_label = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Shape Type")));

			tb.AppendItem(shape_type_label);

			if (shape_type_button == null)
			{
				shape_type_button = new ToolBarDropDownButton();

				shape_type_button.AddItem(Catalog.GetString("Open Line/Curve Series"), "Tools.Line.png", 0);
				shape_type_button.AddItem(Catalog.GetString("Closed Line/Curve Series"), "Tools.Rectangle.png", 1);
				shape_type_button.AddItem(Catalog.GetString("Ellipse"), "Tools.Ellipse.png", 2);
				shape_type_button.AddItem(Catalog.GetString("Rounded Line Series"), "Tools.RoundedRectangle.png", 3);

				shape_type_button.SelectedItemChanged += (o, e) =>
				{
					ShapeTypes newShapeType = ShapeType;
					ShapeEngine selEngine = SelectedShapeEngine;

					if (selEngine != null)
					{
						//Verify that the tool needs to be switched.
						if (GetCorrespondingTool(newShapeType) != this.owner)
						{
							//Create a new ShapesModifyHistoryItem so that the changing of the shape type can be undone.
							PintaCore.Workspace.ActiveDocument.History.PushNewItem(new ShapesModifyHistoryItem(
								this, owner.Icon, Catalog.GetString("Changed Shape Type")));

							//Clone the old shape; it should be automatically garbage-collected. newShapeType already has the updated value.
							selEngine = selEngine.Convert(newShapeType, SelectedShapeIndex);

							int previousSSI = SelectedShapeIndex;

							ActivateCorrespondingTool(selEngine.ShapeType, true);

							SelectedShapeIndex = previousSSI;

							//Draw the updated shape with organized points generation (for mouse detection). 
							DrawActiveShape(true, false, true, false, true);
						}
					}
				};
			}

			shape_type_button.SelectedItem = shape_type_button.Items[(int)owner.ShapeType];

			tb.AppendItem(shape_type_button);


            Gtk.ComboBox dpbBox = dash_pattern_box.SetupToolbar(tb);

            if (dpbBox != null)
            {
                dpbBox.Changed += (o, e) =>
                {
					ShapeEngine selEngine = SelectedShapeEngine;

					if (selEngine != null)
					{
						selEngine.DashPattern = dpbBox.ActiveText;
                        StorePreviousSettings ();
                        DrawActiveShape (false, false, true, false, false);
					}
                };
            }
        }

        public virtual void HandleActivated()
        {
			RecallPreviousSettings();

            PintaCore.Palette.PrimaryColorChanged += new EventHandler(Palette_PrimaryColorChanged);
            PintaCore.Palette.SecondaryColorChanged += new EventHandler(Palette_SecondaryColorChanged);
        }

		public virtual void HandleDeactivated(BaseTool newTool)
		{
			SelectedPointIndex = -1;
			SelectedShapeIndex = -1;

			StorePreviousSettings();

			//Determine if the tool being switched to will be another editable tool.
			if (PintaCore.Workspace.HasOpenDocuments && !newTool.IsEditableShapeTool)
			{
				//The tool being switched to is not editable. Finalize every editable shape not yet finalized.
				FinalizeAllShapes();
			}

            PintaCore.Palette.PrimaryColorChanged -= Palette_PrimaryColorChanged;
            PintaCore.Palette.SecondaryColorChanged -= Palette_SecondaryColorChanged;
        }

		public virtual void HandleAfterSave()
		{
			//When saving, everything will be finalized, which is good; however, afterwards, the user will expect
			//everything to remain editable. Currently, a finalization history item will always be added.
			PintaCore.Actions.Edit.Undo.Activate();

			//Redraw all of the editable shapes in case saving caused some extra/unexpected behavior.
			DrawAllShapes();
		}

        public virtual void HandleCommit()
        {
            //Finalize every editable shape not yet finalized.
			FinalizeAllShapes();
        }

		public virtual bool HandleBeforeUndo()
		{
			return false;
		}

		public virtual bool HandleBeforeRedo()
		{
			return false;
		}

        public virtual void HandleAfterUndo()
        {
            ShapeEngine activeEngine = ActiveShapeEngine;

            if (activeEngine != null)
            {
				UpdateToolbarSettings(activeEngine);
            }

            //Draw the current state.
			DrawActiveShape(true, false, true, false, false);
        }

        public virtual void HandleAfterRedo()
        {
            ShapeEngine activeEngine = ActiveShapeEngine;

            if (activeEngine != null)
            {
				UpdateToolbarSettings(activeEngine);
            }

            //Draw the current state.
			DrawActiveShape(true, false, true, false, false);
        }

        public virtual bool HandleKeyDown(Gtk.DrawingArea canvas, Gtk.KeyPressEventArgs args)
        {
			Gdk.Key keyPressed = args.Event.Key;

			if (keyPressed == Gdk.Key.Delete)
            {
                if (SelectedPointIndex > -1)
                {
                    List<ControlPoint> controlPoints = SelectedShapeEngine.ControlPoints;

					//Either delete a ControlPoint or an entire shape (if there's only 1 ControlPoint left).
                    if (controlPoints.Count > 1)
                    {
						//Create a new ShapesModifyHistoryItem so that the deletion of a control point can be undone.
						PintaCore.Workspace.ActiveDocument.History.PushNewItem(
							new ShapesModifyHistoryItem(this, owner.Icon, ShapeName + " " + Catalog.GetString("Point Deleted")));

						//Delete the selected point from the shape.
						controlPoints.RemoveAt(SelectedPointIndex);

						//Set the newly selected point to be the median-most point on the shape, order-wise.
                        if (SelectedPointIndex > controlPoints.Count / 2)
                        {
                            --SelectedPointIndex;
                        }
                    }
                    else
                    {
						Document doc = PintaCore.Workspace.ActiveDocument;

						//Create a new ShapesHistoryItem so that the deletion of a shape can be undone.
						doc.History.PushNewItem(
							new ShapesHistoryItem(this, owner.Icon, ShapeName + " " + Catalog.GetString("Deleted"),
								doc.CurrentUserLayer.Surface.Clone(), doc.CurrentUserLayer, SelectedPointIndex, SelectedShapeIndex, false));


						//Since the shape itself will be deleted, remove its ReEditableLayer from the drawing loop.

						ReEditableLayer removeMe = SEngines.ElementAt(SelectedShapeIndex).DrawingLayer;

						if (removeMe.InTheLoop)
						{
							SEngines.ElementAt(SelectedShapeIndex).DrawingLayer.TryRemoveLayer();
						}


						//Delete the selected shape.
						SEngines.RemoveAt(SelectedShapeIndex);

						//Redraw the workspace.
						doc.Workspace.Invalidate();

                        SelectedPointIndex = -1;
						SelectedShapeIndex = -1;
                    }

                    hover_point = new PointD(-1d, -1d);

					DrawActiveShape(true, false, true, false, false);
                }

                args.RetVal = true;
            }
            else if (keyPressed == Gdk.Key.Return)
            {
                //Finalize every editable shape not yet finalized.
				FinalizeAllShapes();

                args.RetVal = true;
            }
            else if (keyPressed == Gdk.Key.space)
            {
                ControlPoint selPoint = SelectedPoint;

                if (selPoint != null)
                {
                    //This can be assumed not to be null since selPoint was not null.
                    ShapeEngine selEngine = SelectedShapeEngine;

                    //Create a new ShapesModifyHistoryItem so that the adding of a control point can be undone.
                    PintaCore.Workspace.ActiveDocument.History.PushNewItem(
						new ShapesModifyHistoryItem(this, owner.Icon, ShapeName + " " + Catalog.GetString("Point Added")));


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
                            CalculateModifiedCurrentPoint();
                        }

                        //Space only: position of mouse (after any potential shift alignment).
                        newPointPos = new PointD(current_point.X, current_point.Y);
                    }

                    //Place the new point on the outside-most end, order-wise.
                    if ((double)SelectedPointIndex < (double)selEngine.ControlPoints.Count / 2d)
                    {
                        SelectedShapeEngine.ControlPoints.Insert(SelectedPointIndex,
                            new ControlPoint(new PointD(newPointPos.X, newPointPos.Y), DefaultMidPointTension));
                    }
                    else
                    {
                        SelectedShapeEngine.ControlPoints.Insert(SelectedPointIndex + 1,
                            new ControlPoint(new PointD(newPointPos.X, newPointPos.Y), DefaultMidPointTension));

                        ++SelectedPointIndex;
                    }

					DrawActiveShape(true, false, true, shiftKey, false);
                }

                args.RetVal = true;
            }
            else if (keyPressed == Gdk.Key.Up)
            {
                //Make sure a control point is selected.
                if (SelectedPointIndex > -1)
                {
                    //Move the selected control point.
                    SelectedPoint.Position.Y -= 1d;

					DrawActiveShape(true, false, true, false, false);
                }

                args.RetVal = true;
            }
            else if (keyPressed == Gdk.Key.Down)
            {
                //Make sure a control point is selected.
                if (SelectedPointIndex > -1)
                {
                    //Move the selected control point.
                    SelectedPoint.Position.Y += 1d;

					DrawActiveShape(true, false, true, false, false);
                }

                args.RetVal = true;
            }
            else if (keyPressed == Gdk.Key.Left)
            {
                //Make sure a control point is selected.
                if (SelectedPointIndex > -1)
                {
                    if ((args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
                    {
                        //Change the selected control point to be the previous one.
						
						--SelectedPointIndex;

                        if (SelectedPointIndex < 0)
                        {
							ShapeEngine activeEngine = ActiveShapeEngine;

							if (activeEngine != null)
							{
								SelectedPointIndex = activeEngine.ControlPoints.Count - 1;
							}
                        }
                    }
                    else
                    {
                        //Move the selected control point.
                        SelectedPoint.Position.X -= 1d;
                    }

					DrawActiveShape(true, false, true, false, false);
                }

                args.RetVal = true;
            }
            else if (keyPressed == Gdk.Key.Right)
            {
                //Make sure a control point is selected.
                if (SelectedPointIndex > -1)
                {
                    if ((args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
                    {
						//Change the selected control point to be the following one.

						ShapeEngine activeEngine = ActiveShapeEngine;

						if (activeEngine != null)
						{
							++SelectedPointIndex;

							if (SelectedPointIndex > activeEngine.ControlPoints.Count - 1)
							{
								SelectedPointIndex = 0;
							}
						}
                    }
                    else
                    {
                        //Move the selected control point.
                        SelectedPoint.Position.X += 1d;
                    }

					DrawActiveShape(true, false, true, false, false);
                }

                args.RetVal = true;
            }
            else
            {
                return false;
            }

            return true;
        }

        public virtual bool HandleKeyUp(Gtk.DrawingArea canvas, Gtk.KeyReleaseEventArgs args)
        {
			Gdk.Key keyReleased = args.Event.Key;

            if (keyReleased == Gdk.Key.Delete || keyReleased == Gdk.Key.Return || keyReleased == Gdk.Key.space
                || keyReleased == Gdk.Key.Up || keyReleased == Gdk.Key.Down
                || keyReleased == Gdk.Key.Left || keyReleased == Gdk.Key.Right)
            {
                args.RetVal = true;

                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual void HandleMouseDown(Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
        {
            //If we are already drawing, ignore any additional mouse down events.
			if (is_drawing)
			{
				return;
			}

			//Redraw the previously (and possibly currently) active shape without any control points in case another shape is made active.
			DrawActiveShape(false, false, false, false, false);
			
            Document doc = PintaCore.Workspace.ActiveDocument;

            shape_origin = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));
            current_point = shape_origin;

            bool shiftKey = (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;

            if (shiftKey)
            {
                CalculateModifiedCurrentPoint();
            }

            is_drawing = true;


            //Right clicking changes tension.
            if (args.Event.Button == 1)
            {
                changing_tension = false;
            }
            else
            {
                changing_tension = true;
            }


			bool ctrlKey = (args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask;


			int closestCPIndex, closestCPShapeIndex;
			ControlPoint closestControlPoint;
			double closestCPDistance;

			SEngines.FindClosestControlPoint(current_point,
				out closestCPShapeIndex, out closestCPIndex, out closestControlPoint, out closestCPDistance);

            int closestShapeIndex, closestPointIndex;
            PointD closestPoint;
            double closestDistance;

            OrganizedPointCollection.FindClosestPoint(SEngines, current_point,
                out closestShapeIndex, out closestPointIndex, out closestPoint, out closestDistance);

            bool clickedOnControlPoint = false;

			double currentClickRange = ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor;

			//Determine if the closest ControlPoint is within the expected click range.
			if (closestControlPoint != null && closestCPDistance < currentClickRange)
			{
				//User clicked directly on a ControlPoint on a shape.

				clicked_without_modifying = true;

				SelectedPointIndex = closestCPIndex;
				SelectedShapeIndex = closestCPShapeIndex;

				clickedOnControlPoint = true;
			}
			else if (closestDistance < currentClickRange) //Determine if the user clicked close enough to a shape.
			{
				//User clicked on a generated point on a shape.

				List<ControlPoint> controlPoints = SEngines[closestShapeIndex].ControlPoints;

				//Note: compare the currentPoint's distance here because it's the actual mouse position.
				if (controlPoints.Count > closestPointIndex && current_point.Distance(controlPoints[closestPointIndex].Position) < currentClickRange)
				{
					//User clicked on a control point (on the "previous order" side of the point).

					clicked_without_modifying = true;

					SelectedPointIndex = closestPointIndex;
					SelectedShapeIndex = closestShapeIndex;

					clickedOnControlPoint = true;
				}
				else if (closestPointIndex > 0)
				{
					if (current_point.Distance(controlPoints[closestPointIndex - 1].Position) < currentClickRange)
					{
						//User clicked on a control point (on the "following order" side of the point).

						clicked_without_modifying = true;

						SelectedPointIndex = closestPointIndex - 1;
						SelectedShapeIndex = closestShapeIndex;

						clickedOnControlPoint = true;
					}
					else if (controlPoints.Count > 0 && current_point.Distance(controlPoints[controlPoints.Count - 1].Position) < currentClickRange)
					{
						//User clicked on a control point (on the "following order" side of the point).

						clicked_without_modifying = true;

						SelectedPointIndex = closestPointIndex - 1;
						SelectedShapeIndex = closestShapeIndex;

						clickedOnControlPoint = true;
					}
				}

				//Check for clicking on a non-control point. Don't do anything here if right clicked.
				if (!changing_tension && !clickedOnControlPoint && closestShapeIndex > -1 && closestPointIndex > -1 && SEngines.Count > closestShapeIndex)
				{
					//User clicked on a non-control point on a shape.

					//Determine if the currently active tool matches the clicked on shape's corresponding tool, and if not, switch to it.
					if (ActivateCorrespondingTool(closestShapeIndex, true) != null)
					{
						//Pass on the event and its data to the newly activated tool.
						PintaCore.Tools.CurrentTool.DoMouseDown(canvas, args, point);

						//Don't do anything else here once the tool is switched and the event is passed on.
						return;
					}

					//The currently active tool matches the clicked on shape's corresponding tool.

					//Only create a new shape if the user isn't holding the control key down.
					if (!ctrlKey)
					{
						//Create a new ShapesModifyHistoryItem so that the adding of a control point can be undone.
						doc.History.PushNewItem(new ShapesModifyHistoryItem(this, owner.Icon, ShapeName + " " + Catalog.GetString("Point Added")));

						controlPoints.Insert(closestPointIndex,
							new ControlPoint(new PointD(current_point.X, current_point.Y), DefaultMidPointTension));
					}

					//These should be set after creating the history item.
					SelectedPointIndex = closestPointIndex;
					SelectedShapeIndex = closestShapeIndex;

					ShapeEngine activeEngine = ActiveShapeEngine;

					if (activeEngine != null)
					{
						UpdateToolbarSettings(activeEngine);
					}
				}
			}

			//Create a new shape if the user control + clicks on a shape or if the user simply clicks outside of any shapes.
			if (!changing_tension && (ctrlKey || (closestCPDistance >= currentClickRange && closestDistance >= currentClickRange)))
            {
				//Verify that the user clicked inside the image bounds or that the user is
				//holding the Ctrl key (to ignore the Image bounds and draw on the edge).
				if ((point.X == shape_origin.X && point.Y == shape_origin.Y) || ctrlKey)
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

					//Create a new ShapesHistoryItem so that the creation of a new shape can be undone.
					doc.History.PushNewItem(new ShapesHistoryItem(this, owner.Icon, ShapeName + " " + Catalog.GetString("Added"),
						doc.CurrentUserLayer.Surface.Clone(), doc.CurrentUserLayer, SelectedPointIndex, SelectedShapeIndex, false));

					//Create the shape, add its starting points, and add it to SEngines.
					SEngines.Add(CreateShape(ctrlKey, clickedOnControlPoint, prevSelPoint));

					//Select the new shape.
					SelectedShapeIndex = SEngines.Count - 1;

					ShapeEngine activeEngine = ActiveShapeEngine;

					if (activeEngine != null)
					{
						//Set the AntiAliasing.
						activeEngine.AntiAliasing = owner.UseAntialiasing;
					}

					StorePreviousSettings();
				}
            }
			else if (clickedOnControlPoint)
			{
				//Since the user is not creating a new shape or control point but rather modifying an existing control point, it should be determined
				//whether the currently active tool matches the clicked on shape's corresponding tool, and if not, switch to it.
				if (ActivateCorrespondingTool(SelectedShapeIndex, true) != null)
				{
					//Pass on the event and its data to the newly activated tool.
					PintaCore.Tools.CurrentTool.DoMouseDown(canvas, args, point);

					//Don't do anything else here once the tool is switched and the event is passed on.
					return;
				}

				//The currently active tool matches the clicked on shape's corresponding tool.

				ShapeEngine activeEngine = ActiveShapeEngine;

				if (activeEngine != null)
				{
					UpdateToolbarSettings(activeEngine);
				}
			}

            //Determine if the user right clicks outside of any shapes (neither on their control points nor on their generated points).
			if ((closestCPDistance >= currentClickRange && closestDistance >= currentClickRange) && changing_tension)
            {
                clicked_without_modifying = true;
            }

			DrawActiveShape(false, false, true, shiftKey, false);
        }

        public virtual void HandleMouseUp(Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
        {
            is_drawing = false;

            changing_tension = false;

			DrawActiveShape(true, false, true, args.Event.IsShiftPressed(), false);
        }

        public virtual void HandleMouseMove(object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
        {
            Document doc = PintaCore.Workspace.ActiveDocument;

            current_point = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));

            bool shiftKey = (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;

            if (shiftKey)
            {
                CalculateModifiedCurrentPoint();
            }

            if (!is_drawing)
            {
                //Redraw the active shape to show a (temporary) highlighted control point (over any shape) when applicable.
				DrawActiveShape(false, false, true, shiftKey, false);
            }
            else
            {
				ControlPoint selPoint = SelectedPoint;

                //Make sure a control point is selected.
				if (selPoint != null)
                {
                    if (clicked_without_modifying)
                    {
                        //Create a new ShapesModifyHistoryItem so that the modification of the shape can be undone.
                        doc.History.PushNewItem(
							new ShapesModifyHistoryItem(this, owner.Icon, ShapeName + " " + Catalog.GetString("Modified")));

                        clicked_without_modifying = false;
                    }

                    List<ControlPoint> controlPoints = SelectedShapeEngine.ControlPoints;

                    if (!changing_tension)
                    {
                        //Moving a control point.

                        //Make sure the control point was moved.
						if (current_point.X != selPoint.Position.X || current_point.Y != selPoint.Position.Y)
                        {
                            MovePoint(controlPoints);
                        }
                    }
                    else
                    {
                        //Changing a control point's tension.

                        //Unclamp the mouse position when changing tension.
                        current_point = new PointD(point.X, point.Y);

                        //Calculate the new tension based off of the movement of the mouse that's
                        //perpendicular to the previous and following control points.

                        PointD curPoint = selPoint.Position;
                        PointD prevPoint, nextPoint;

                        //Calculate the previous control point.
                        if (SelectedPointIndex > 0)
                        {
                            prevPoint = controlPoints[SelectedPointIndex - 1].Position;
                        }
                        else
                        {
                            //There is none.
                            prevPoint = curPoint;
                        }

                        //Calculate the following control point.
                        if (SelectedPointIndex < controlPoints.Count - 1)
                        {
                            nextPoint = controlPoints[SelectedPointIndex + 1].Position;
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
                            xChange = current_point.X - last_mouse_pos.X;
                        }
                        else
                        {
                            xChange = last_mouse_pos.X - current_point.X;
                        }

                        //Calculate the y change in the mouse position.
                        if (curPoint.Y <= midPoint.Y)
                        {
                            yChange = current_point.Y - last_mouse_pos.Y;
                        }
                        else
                        {
                            yChange = last_mouse_pos.Y - current_point.Y;
                        }

                        //Update the control point's tension.

                        //Note: the difference factors are to be inverted for x and y change because this is perpendicular motion.
                        controlPoints[SelectedPointIndex].Tension +=
                            Math.Round(Utility.Clamp((xChange * yDiff + yChange * xDiff) / totalDiff, -1d, 1d)) / 50d;

                        //Restrict the new tension to range from 0d to 1d.
                        controlPoints[SelectedPointIndex].Tension =
							Utility.Clamp(selPoint.Tension, 0d, 1d);
                    }

                    DrawActiveShape(false, false, true, shiftKey, false);
                }
            }

            last_mouse_pos = current_point;
        }


		/// <summary>
		/// Draw the currently active shape.
		/// </summary>
		/// <param name="calculateOrganizedPoints">Whether to calculate the spatially organized
		/// points for mouse detection after drawing the shape.</param>
		/// <param name="finalize">Whether to finalize the drawing.</param>
		/// <param name="drawHoverSelection">Whether to draw any hover point or selected point.</param>
		/// <param name="shiftKey">Whether the shift key is being pressed. This is for width/height constraining/equalizing.</param>
		/// <param name="preventSwitchBack">Whether to prevent switching back to the old tool if a tool change is necessary.</param>
		public void DrawActiveShape(bool calculateOrganizedPoints, bool finalize, bool drawHoverSelection, bool shiftKey, bool preventSwitchBack)
		{
			ShapeTool oldTool = BaseEditEngine.ActivateCorrespondingTool(SelectedShapeIndex, calculateOrganizedPoints);

			//First, determine if the currently active tool matches the shape's corresponding tool, and if not, switch to it.
			if (oldTool != null)
			{
				//The tool has switched, so call DrawActiveShape again but inside that tool.
				((ShapeTool)PintaCore.Tools.CurrentTool).EditEngine.DrawActiveShape(
					calculateOrganizedPoints, finalize, drawHoverSelection, shiftKey, preventSwitchBack);

				//Afterwards, switch back to the old tool, unless specified otherwise.
				if (!preventSwitchBack)
				{
					ActivateCorrespondingTool(oldTool.ShapeType, true);
				}

				return;
			}

			//The currently active tool should now match the shape's corresponding tool.

			BeforeDraw();

			ShapeEngine activeEngine = ActiveShapeEngine;

			if (activeEngine == null)
			{
				//No shape will be drawn; however, the hover point still needs to be drawn if drawHoverSelection is true.
				if (drawHoverSelection)
				{
					DrawTemporaryHoverPoint();
				}
			}
			else
			{
				//Clear any temporary drawing, because something new will be drawn.
				activeEngine.DrawingLayer.Layer.Clear();

				Rectangle dirty;

				//Determine if the drawing should be for finalizing the shape onto the image or drawing it temporarily.
				if (finalize)
				{
					dirty = DrawFinalized(activeEngine, true, shiftKey);
				}
				else
				{
					dirty = DrawUnfinalized(activeEngine, drawHoverSelection, shiftKey);
				}

				//Determine if the organized (spatially hashed) points should be generated. This is for mouse interaction detection after drawing.
				if (calculateOrganizedPoints)
				{
					OrganizePoints(activeEngine);
				}

				InvalidateAfterDraw(dirty);
			}
		}

		/// <summary>
		/// Do not call. Use DrawActiveShape.
		/// </summary>
		private void BeforeDraw()
		{
			//Clear the ToolLayer if it was used previously (e.g. for hover points when there was no active shape).
			PintaCore.Workspace.ActiveDocument.ToolLayer.Clear();

			//Invalidate the old hover point bounds, if any.
			if (last_hover != null)
			{
				PintaCore.Workspace.Invalidate(last_hover.Value.ToGdkRectangle());

				last_hover = null;
			}

			//Check to see if a new shape is selected.
			if (prev_selected_shape_index != SelectedShapeIndex)
			{
				//A new shape is selected, so clear the previous dirty Rectangle.
				last_dirty = null;

				prev_selected_shape_index = SelectedShapeIndex;
			}
		}

		/// <summary>
		/// Do not call. Use DrawActiveShape.
		/// </summary>
		private void DrawTemporaryHoverPoint()
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			//Since there is no active ShapeEngine, the ToolLayer's surface will be used to draw the hover point on.
			using (Context g = new Context(doc.ToolLayer.Surface))
			{
				g.AppendPath(doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip();

				CalculateHoverPoint();

				//Draw the hover point. Note: the hover point has its own invalidation.
				DrawHoverPoint(g);
			}

			doc.ToolLayer.Hidden = false;
		}

		/// <summary>
		/// Do not call. Use DrawActiveShape.
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="dirty"></param>
		/// <param name="shiftKey"></param>
		private Rectangle DrawFinalized(ShapeEngine engine, bool createHistoryItem, bool shiftKey)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			//Finalize the shape onto the CurrentUserLayer.

			ImageSurface undoSurface = null;

			if (createHistoryItem)
			{
				//We only need to create a history item if there was a previous shape.
				if (engine.ControlPoints.Count > 0)
				{
					undoSurface = doc.CurrentUserLayer.Surface.Clone();
				}
			}

			//Draw the finalized shape.
			Rectangle dirty = DrawShape(engine, doc.CurrentUserLayer, false, false);

			if (createHistoryItem)
			{
				//Make sure that the undo surface isn't null.
				if (undoSurface != null)
				{
					//Create a new ShapesHistoryItem so that the finalization of the shape can be undone.
					doc.History.PushNewItem(new ShapesHistoryItem(this, owner.Icon, ShapeName + " " + Catalog.GetString("Finalized"),
						undoSurface, doc.CurrentUserLayer, SelectedPointIndex, SelectedShapeIndex, false));
				}
			}

			return dirty;
		}

		/// <summary>
		/// Do not call. Use DrawActiveShape.
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="dirty"></param>
		/// <param name="drawHoverSelection"></param>
		/// <param name="shiftKey"></param>
		private Rectangle DrawUnfinalized(ShapeEngine engine, bool drawHoverSelection, bool shiftKey)
		{
			//Not finalizing the shape; drawing it on the temporary DrawingLayer.

			//Calculate the hover point unless told otherwise.
			if (drawHoverSelection)
			{
				CalculateHoverPoint();
			}
			else
			{
				//Do not draw the hover point. Instead, reset the hover point. NOTE: this is necessary even though the hover point
				//is reset later. It affects the DrawShape call.
				hover_point = new PointD(-1d, -1d);
				hovered_pt_as_control_pt = -1;
			}

			//Draw the shape onto the temporary DrawingLayer.
			Rectangle dirty = DrawShape(engine, engine.DrawingLayer.Layer, true, drawHoverSelection);

			//Reset the hover point after each drawing.
			hover_point = new PointD(-1d, -1d);
			hovered_pt_as_control_pt = -1;

			return dirty;
		}

		/// <summary>
		/// Calculate the hover point, if any. Result is stored in hover_point.
		/// </summary>
		private void CalculateHoverPoint()
		{
			if (SEngines.Count > 0)
			{
				hover_point = new PointD(-1d, -1d);

				int closestCPIndex, closestCPShapeIndex;
				ControlPoint closestControlPoint;
				double closestCPDistance;

				SEngines.FindClosestControlPoint(current_point,
					out closestCPShapeIndex, out closestCPIndex, out closestControlPoint, out closestCPDistance);

				int closestShapeIndex, closestPointIndex;
				PointD closestPoint;
				double closestDistance;

				OrganizedPointCollection.FindClosestPoint(SEngines, current_point,
					out closestShapeIndex, out closestPointIndex, out closestPoint, out closestDistance);

				double currentClickRange = ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor;

				List<ControlPoint> controlPoints = SEngines[closestShapeIndex].ControlPoints;

				//Determine if the closest ControlPoint is within the expected click range.
				if (closestControlPoint != null && closestCPDistance < currentClickRange)
				{
					//User clicked directly on a ControlPoint on a shape.

					hover_point.X = closestControlPoint.Position.X;
					hover_point.Y = closestControlPoint.Position.Y;
					hovered_pt_as_control_pt = closestCPIndex;
				}
				else if (closestDistance < currentClickRange) //Determine if the user is hovering the mouse close enough to a shape.
				{
					//User is hovering over a generated point on a shape.

					if (controlPoints.Count > closestPointIndex)
					{
						//Note: compare the currentPoint's distance here because it's the actual mouse position.
						if (current_point.Distance(controlPoints[closestPointIndex].Position) < currentClickRange)
						{
							//Mouse hovering over a control point (on the "previous order" side of the point).

							hover_point.X = controlPoints[closestPointIndex].Position.X;
							hover_point.Y = controlPoints[closestPointIndex].Position.Y;
							hovered_pt_as_control_pt = closestPointIndex;
						}
						else if (closestPointIndex > 0)
						{
							if (current_point.Distance(controlPoints[closestPointIndex - 1].Position) < currentClickRange)
							{
								//Mouse hovering over a control point (on the "following order" side of the point).

								hover_point.X = controlPoints[closestPointIndex - 1].Position.X;
								hover_point.Y = controlPoints[closestPointIndex - 1].Position.Y;
								hovered_pt_as_control_pt = closestPointIndex - 1;
							}
						}
						else if (controlPoints.Count > 0 && current_point.Distance(controlPoints[controlPoints.Count - 1].Position) < currentClickRange)
						{
							//Mouse hovering over a control point (on the "following order" side of the point).

							hovered_pt_as_control_pt = controlPoints.Count - 1;
							hover_point.X = controlPoints[hovered_pt_as_control_pt].Position.X;
							hover_point.Y = controlPoints[hovered_pt_as_control_pt].Position.Y;
						}
					}

					if (hover_point.X < 0d)
					{
						hover_point.X = closestPoint.X;
						hover_point.Y = closestPoint.Y;
					}
				}
			}
		}

		/// <summary>
		/// Do not call. Use DrawActiveShape.
		/// </summary>
		/// <param name="engine"></param>
		private void OrganizePoints(ShapeEngine engine)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			//Organize the generated points for quick mouse interaction detection.

			//First, clear the previously organized points, if any.
			engine.OrganizedPoints.ClearCollection();

			foreach (GeneratedPoint gp in engine.GeneratedPoints)
			{
				//For each generated point on the shape, calculate the spatial hashing for it and then store this information for later usage.
				engine.OrganizedPoints.StoreAndOrganizePoint(new OrganizedPoint(new PointD(gp.Position.X, gp.Position.Y), gp.ControlPointIndex));
			}
		}

		private void InvalidateAfterDraw(Rectangle dirty)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			//Inflate to accomodate for previously drawn control points, if any.
			int inflate = (int)(last_control_pt_size * 8d);
			dirty = dirty.Inflate(inflate, inflate);

			// Increase the size of the dirty rect to account for antialiasing.
			if (owner.UseAntialiasing)
			{
				dirty = dirty.Inflate(1, 1);
			}

			//Combine, clamp, and invalidate the dirty Rectangle.
			dirty = ((Rectangle?)dirty).UnionRectangles(last_dirty).Value;
			dirty = dirty.Clamp();
			doc.Workspace.Invalidate(dirty.ToGdkRectangle());

			last_dirty = dirty;
		}


		protected Rectangle DrawShape(ShapeEngine engine, Layer l, bool drawCP, bool drawHoverSelection)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			Rectangle? dirty = null;

			ShapeEngine activeEngine = ActiveShapeEngine;

			if (activeEngine != null)
			{
				using (Context g = new Context(l.Surface))
				{
					g.AppendPath(doc.Selection.SelectionPath);
					g.FillRule = FillRule.EvenOdd;
					g.Clip();

					g.Antialias = activeEngine.AntiAliasing ? Antialias.Subpixel : Antialias.None;

					g.SetDash(DashPatternBox.GenerateDashArray(activeEngine.DashPattern, activeEngine.BrushWidth), 0.0);

					g.LineWidth = activeEngine.BrushWidth;

					//Draw the shape.
					if (activeEngine.ControlPoints.Count > 0)
					{
						//Generate the points that make up the shape.
						activeEngine.GeneratePoints(activeEngine.BrushWidth);

                        PointD[] points = activeEngine.GetActualPoints ();

						//Expand the invalidation rectangle as necessary.

						if (FillShape)
						{
                            Color fill_color = StrokeShape ? activeEngine.FillColor : activeEngine.OutlineColor;
                            dirty = dirty.UnionRectangles (g.FillPolygonal (points, fill_color));
						}

						if (StrokeShape)
						{
							dirty = dirty.UnionRectangles(g.DrawPolygonal(points, activeEngine.OutlineColor));
						}
					}

					g.SetDash(new double[] { }, 0.0);

					//Draw anything extra (that not every shape has), like arrows.
					DrawExtras(ref dirty, g, engine);

					if (drawCP)
					{
						DrawControlPoints(g, drawHoverSelection);
					}
				}
			}


			return dirty ?? new Rectangle(0d, 0d, 0d, 0d);
		}

		protected void DrawControlPoints(Context g, bool drawHoverSelection)
		{
			ShapeEngine activeEngine = ActiveShapeEngine;

			if (activeEngine != null)
			{
				last_control_pt_size = Math.Min(activeEngine.BrushWidth + 1, 3);
			}
			else
			{
				last_control_pt_size = Math.Min(BrushWidth + 1, 3);
			}

			double controlPointOffset = (double)last_control_pt_size / 2d;

			if (activeEngine != null)
			{
				//Draw the control points for the active shape.

				if (drawHoverSelection)
				{
					ControlPoint selPoint = SelectedPoint;

					if (selPoint != null)
					{
						//Draw a ring around the selected point.
						g.FillStrokedEllipse(
							new Rectangle(
								selPoint.Position.X - controlPointOffset * 4d,
								selPoint.Position.Y - controlPointOffset * 4d,
								controlPointOffset * 8d, controlPointOffset * 8d),
							ToolControl.FillColor, ToolControl.StrokeColor, 1);
					}
				}

				List<ControlPoint> controlPoints = activeEngine.ControlPoints;

				//Determine if the shape has one or more points.
				if (controlPoints.Count > 0)
				{
					//Draw the control points for the shape.
					for (int i = 0; i < controlPoints.Count; ++i)
					{
						//Skip drawing the hovered control point.
						if (drawHoverSelection && hovered_pt_as_control_pt > -1 && hover_point.Distance(controlPoints[i].Position) < 1d)
						{
							continue;
						}

						//Draw each control point.
						g.FillStrokedEllipse(
							new Rectangle(
								controlPoints[i].Position.X - controlPointOffset,
								controlPoints[i].Position.Y - controlPointOffset,
								last_control_pt_size, last_control_pt_size),
							ToolControl.FillColor, ToolControl.StrokeColor, (int)last_control_pt_size);
					}
				}

				if (drawHoverSelection)
				{
					//Draw the hover point.
					DrawHoverPoint(g);
				}
			}
		}

		/// <summary>
		/// Draws the hover point, if any.
		/// </summary>
		/// <param name="g"></param>
		protected void DrawHoverPoint(Context g)
		{
			ShapeEngine activeEngine = ActiveShapeEngine;

			if (activeEngine != null)
			{
				last_control_pt_size = Math.Min(activeEngine.BrushWidth + 1, 5);
			}
			else
			{
				last_control_pt_size = Math.Min(BrushWidth + 1, 5);
			}

			double controlPointOffset = (double)last_control_pt_size / 2d;

			//Verify that the user isn't changing the tension of a control point and that there is a hover point to draw.
			if (!changing_tension && hover_point.X > -1d)
			{
				Rectangle hoverOuterEllipseRect = new Rectangle(
					hover_point.X - controlPointOffset * 3d, hover_point.Y - controlPointOffset * 3d,
					controlPointOffset * 6d, controlPointOffset * 6d);

				g.FillStrokedEllipse(hoverOuterEllipseRect, hover_color, hover_color, 1);

				g.FillStrokedEllipse(new Rectangle(
					hover_point.X - controlPointOffset, hover_point.Y - controlPointOffset,
					last_control_pt_size, last_control_pt_size), hover_color, hover_color, (int)last_control_pt_size);


				hoverOuterEllipseRect = hoverOuterEllipseRect.Inflate(1, 1);

				//Since the hover point can be outside of the active shape's bounds (hovering over a different shape), a special
				//invalidation call needs to be made for the hover point in order to ensure its visibility at all times.
				PintaCore.Workspace.Invalidate(hoverOuterEllipseRect.ToGdkRectangle());

				last_hover = hoverOuterEllipseRect;
				last_hover = last_hover.Value.Clamp();
			}
		}


		/// <summary>
		/// Go through every editable shape and draw it.
		/// </summary>
		public void DrawAllShapes()
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			//Store the SelectedShapeIndex value for later restoration.
			int previousToolSI = SelectedShapeIndex;

			//Draw all of the shapes.
			for (SelectedShapeIndex = 0; SelectedShapeIndex < SEngines.Count; ++SelectedShapeIndex)
			{
				//Only draw the selected point for the selected shape.
				DrawActiveShape(true, false, previousToolSI == SelectedShapeIndex, false, true);
			}

			//Restore the previous SelectedShapeIndex value.
			SelectedShapeIndex = previousToolSI;

			//Determine if the currently active tool matches the shape's corresponding tool, and if not, switch to it.
			BaseEditEngine.ActivateCorrespondingTool(SelectedShapeIndex, false);

			//The currently active tool should now match the shape's corresponding tool.
		}

		/// <summary>
		/// Go through every editable shape not yet finalized and finalize it.
		/// </summary>
		protected void FinalizeAllShapes()
		{
            if (SEngines.Count == 0)
                return;

			Document doc = PintaCore.Workspace.ActiveDocument;

			ImageSurface undoSurface = doc.CurrentUserLayer.Surface.Clone();
			
			int previousSelectedPointIndex = SelectedPointIndex;

			Rectangle? dirty = null;

			//Finalize all of the shapes.
			for (SelectedShapeIndex = 0; SelectedShapeIndex < SEngines.Count; ++SelectedShapeIndex)
			{
				//Get a reference to each shape's corresponding tool.
				ShapeTool correspondingTool = GetCorrespondingTool(SEngines[SelectedShapeIndex].ShapeType);

				if (correspondingTool != null)
				{
					//Finalize the now active shape using its corresponding tool's EditEngine.

					BaseEditEngine correspondingEngine = correspondingTool.EditEngine;

					correspondingEngine.SelectedShapeIndex = SelectedShapeIndex;

					correspondingEngine.BeforeDraw();

					//Clear any temporary drawing, because something new will be drawn.
					SEngines[SelectedShapeIndex].DrawingLayer.Layer.Clear();

					//Draw the current shape with the corresponding tool's EditEngine.
					dirty = dirty.UnionRectangles((Rectangle?)correspondingEngine.DrawFinalized(
						SEngines[SelectedShapeIndex], false, false));
				}
			}

			//Make sure that the undo surface isn't null.
			if (undoSurface != null)
			{
				//Create a new ShapesHistoryItem so that the finalization of the shapes can be undone.
				doc.History.PushNewItem(new ShapesHistoryItem(this, owner.Icon, Catalog.GetString("Finalized"),
					undoSurface, doc.CurrentUserLayer, previousSelectedPointIndex, prev_selected_shape_index, true));
			}

			if (dirty.HasValue)
			{
				InvalidateAfterDraw(dirty.Value);
			}

            // Ensure the ToolLayer gets hidden now that we're done with it
            doc.ToolLayer.Hidden = true;

			//Clear out all of the data.
			ResetShapes();
		}

		/// <summary>
		/// Constrain the current point to snap to fixed angles from the previous point, or to
        /// produce a square / circle when drawing those shape types.
		/// </summary>
		protected void CalculateModifiedCurrentPoint()
		{
			ShapeEngine selEngine = SelectedShapeEngine;

			//Don't bother calculating a modified point if there is no selected shape.
			if (selEngine != null)
			{
				if (ShapeType != ShapeTypes.OpenLineCurveSeries && selEngine.ControlPoints.Count == 4) {
					// Constrain to a square / circle.
					var origin = selEngine.ControlPoints [(SelectedPointIndex + 2) % 4].Position;

					var dx = current_point.X - origin.X;
					var dy = current_point.Y - origin.Y;
					var length = Math.Max (Math.Abs (dx), Math.Abs (dy));
					dx = length * Math.Sign (dx);
					dy = length * Math.Sign (dy);
					current_point = new PointD (origin.X + dx, origin.Y + dy);
                } else {
					// Calculate the modified position of currentPoint such that the angle between the adjacent point
					// (if any) and currentPoint is snapped to the closest angle out of a certain number of angles.
					ControlPoint adjacentPoint;

					if (SelectedPointIndex > 0) {
						//Previous point.
						adjacentPoint = selEngine.ControlPoints [SelectedPointIndex - 1];
					} else if (selEngine.ControlPoints.Count > 1) {
						//Previous point (looping around to the end) if there is more than 1 point.
						adjacentPoint = selEngine.ControlPoints [selEngine.ControlPoints.Count - 1];
					} else {
						//Don't bother calculating a modified point because there is no reference point to align it with (there is only 1 point).
						return;
					}

					PointD dir = new PointD (current_point.X - adjacentPoint.Position.X, current_point.Y - adjacentPoint.Position.Y);
					double theta = Math.Atan2 (dir.Y, dir.X);
					double len = Math.Sqrt (dir.X * dir.X + dir.Y * dir.Y);

					theta = Math.Round (12 * theta / Math.PI) * Math.PI / 12;
					current_point = new PointD ((adjacentPoint.Position.X + len * Math.Cos (theta)), (adjacentPoint.Position.Y + len * Math.Sin (theta)));
				}
			}
		}

		/// <summary>
		/// Resets the editable data.
		/// </summary>
		protected void ResetShapes()
		{
			SEngines = new ShapeEngineCollection();
			
			//The fields are modified instead of the properties here because a redraw call is undesired (for speed/efficiency).
			SelectedPointIndex = -1;
			SelectedShapeIndex = -1;

			is_drawing = false;

			last_dirty = null;
		}

		/// <summary>
		/// Activates the corresponding tool to the given shapeIndex value if the tool is not already active, and then returns the previous tool
		/// if a tool switch has occurred or null otherwise. If a switch did occur and this was called in e.g. an event handler, it should most
		/// likely pass the event data on to the newly activated tool (accessing it using PintaCore.Tools.CurrentTool) and then return.
		/// </summary>
		/// <param name="shapeIndex">The index of the shape in SEngines to find the corresponding tool to and switch to.</param>
		/// <param name="permanentSwitch">Whether the tool switch is permanent or just temporary (for drawing).</param>
		/// <returns>The *previous* tool if a tool switch has occurred or null otherwise.</returns>
		public static ShapeTool ActivateCorrespondingTool(int shapeIndex, bool permanentSwitch)
		{
			//First make sure that there is a validly selectable tool.
			if (shapeIndex > -1 && SEngines.Count > shapeIndex)
			{
				return ActivateCorrespondingTool(SEngines[shapeIndex].ShapeType, permanentSwitch);
			}

			//Let the caller know that the active tool has not been switched.
			return null;
		}

		/// <summary>
		/// Activates the corresponding tool to the given shapeType value if the tool is not already active, and then returns the previous tool
		/// if a tool switch has occurred or null otherwise. If a switch did occur and this was called in e.g. an event handler, it should most
		/// likely pass the event data on to the newly activated tool (accessing it using PintaCore.Tools.CurrentTool) and then return.
		/// </summary>
		/// <param name="shapeType">The index of the shape in SEngines to find the corresponding tool to and switch to.</param>
		/// <param name="permanentSwitch">Whether the tool switch is permanent or just temporary (for drawing).</param>
		/// <returns>The *previous* tool if a tool switch has occurred or null otherwise.</returns>
		public static ShapeTool ActivateCorrespondingTool(ShapeTypes shapeType, bool permanentSwitch)
		{
			ShapeTool correspondingTool = GetCorrespondingTool(shapeType);

			//Verify that the corresponding tool is valid and that it doesn't match the currently active tool.
			if (correspondingTool != null && PintaCore.Tools.CurrentTool != correspondingTool)
			{
				ShapeTool oldTool = PintaCore.Tools.CurrentTool as ShapeTool;

				//The active tool needs to be switched to the corresponding tool.
				PintaCore.Tools.SetCurrentTool(correspondingTool);

				ShapeTool newTool = (ShapeTool)PintaCore.Tools.CurrentTool;

				//What happens next depends on whether the old tool was an editable ShapeTool.
				if (oldTool != null && oldTool.IsEditableShapeTool)
				{
					if (permanentSwitch)
					{
						//Set the new tool's active shape and point to the old shape and point.
						newTool.EditEngine.SelectedPointIndex = oldTool.EditEngine.SelectedPointIndex;
						newTool.EditEngine.SelectedShapeIndex = oldTool.EditEngine.SelectedShapeIndex;

						//Make sure neither tool thinks it is drawing anything.
						newTool.EditEngine.is_drawing = false;
						oldTool.EditEngine.is_drawing = false;
					}

					ShapeEngine activeEngine = newTool.EditEngine.ActiveShapeEngine;

					if (activeEngine != null)
					{
						newTool.EditEngine.UpdateToolbarSettings(activeEngine);
					}
				}
				else
				{
					if (permanentSwitch)
					{
						//Make sure that the new tool doesn't think it is drawing anything.
						newTool.EditEngine.is_drawing = false;
					}
				}

				//Let the caller know that the active tool has been switched.
				return oldTool;
			}

			//Let the caller know that the active tool has not been switched.
			return null;
		}

		/// <summary>
		/// Gets the corresponding tool to the given shape type and then returns that tool.
		/// </summary>
		/// <param name="ShapeType">The shape type to find the corresponding tool to.</param>
		/// <returns>The corresponding tool to the given shape type.</returns>
		public static ShapeTool GetCorrespondingTool(ShapeTypes shapeType)
		{
			ShapeTool correspondingTool = null;
			
			//Get the corresponding BaseTool reference to the shape type.
			CorrespondingTools.TryGetValue(shapeType, out correspondingTool);

			return correspondingTool;
		}

		
		/// <summary>
		/// Copy the given shape's settings to the toolbar settings. Calls StorePreviousSettings.
		/// </summary>
		/// <param name="engine"></param>
		public virtual void UpdateToolbarSettings(ShapeEngine engine)
		{
			if (engine != null)
			{
				owner.UseAntialiasing = engine.AntiAliasing;

				//Update the DashPatternBox to represent the current shape's DashPattern.
				(dash_pattern_box.comboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = engine.DashPattern;

				OutlineColor = engine.OutlineColor.Clone();
				FillColor = engine.FillColor.Clone();

				BrushWidth = engine.BrushWidth;

				StorePreviousSettings();
			}
		}

		/// <summary>
		/// Copy the previous settings to the toolbar settings.
		/// </summary>
		protected virtual void RecallPreviousSettings()
		{
			if (dash_pattern_box.comboBox != null)
			{
				(dash_pattern_box.comboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = prev_dash_pattern;
			}

			owner.UseAntialiasing = prev_antialiasing;
			BrushWidth = prev_brush_width;
		}

		/// <summary>
		/// Copy the toolbar settings to the previous settings.
		/// </summary>
		protected virtual void StorePreviousSettings()
		{
			if (dash_pattern_box.comboBox != null)
			{
				prev_dash_pattern = (dash_pattern_box.comboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text;
			}

			prev_antialiasing = owner.UseAntialiasing;
			prev_brush_width = BrushWidth;
		}

		/// <summary>
		/// Creates a new shape, adds its starting points, and returns it.
		/// </summary>
		/// <param name="ctrlKey"></param>
		/// <param name="clickedOnControlPoint"></param>
		/// <param name="prevSelPoint"></param>
		protected abstract ShapeEngine CreateShape(bool ctrlKey, bool clickedOnControlPoint, PointD prevSelPoint);

        protected virtual void MovePoint(List<ControlPoint> controlPoints)
        {
			//Update the control point's position.
			controlPoints.ElementAt(SelectedPointIndex).Position = new PointD(current_point.X, current_point.Y);
        }

		protected virtual void DrawExtras(ref Rectangle? dirty, Context g, ShapeEngine engine)
        {
            
        }

		protected void AddLinePoints(bool ctrlKey, bool clickedOnControlPoint, ShapeEngine selEngine, PointD prevSelPoint)
		{
			PointD startingPoint;

			//Create the initial points of the shape. The second point will follow the mouse around until released.
			if (ctrlKey && clickedOnControlPoint)
			{
				startingPoint = prevSelPoint;

				clicked_without_modifying = false;
			}
			else
			{
				startingPoint = shape_origin;
			}


			selEngine.ControlPoints.Add(new ControlPoint(new PointD(startingPoint.X, startingPoint.Y), DefaultEndPointTension));
			selEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y + .01d), DefaultEndPointTension));


			SelectedPointIndex = 1;
			SelectedShapeIndex = SEngines.Count - 1;
		}

		protected void AddRectanglePoints(bool ctrlKey, bool clickedOnControlPoint, ShapeEngine selEngine, PointD prevSelPoint)
		{
			PointD startingPoint;

			//Create the initial points of the shape. The second point will follow the mouse around until released.
			if (ctrlKey && clickedOnControlPoint)
			{
				startingPoint = prevSelPoint;

				clicked_without_modifying = false;
			}
			else
			{
				startingPoint = shape_origin;
			}


			selEngine.ControlPoints.Add(new ControlPoint(new PointD(startingPoint.X, startingPoint.Y), 0.0));
			selEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X, startingPoint.Y + .01d), 0.0));
			selEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y + .01d), 0.0));
			selEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y), 0.0));


			SelectedPointIndex = 2;
			SelectedShapeIndex = SEngines.Count - 1;
		}

		protected void MoveRectangularPoint(List<ControlPoint> controlPoints)
		{
			ShapeEngine selEngine = SelectedShapeEngine;

			if (selEngine != null && selEngine.Closed && controlPoints.Count == 4)
			{
				//Figure out the indeces of the surrounding points. The lowest point index should be 0 and the highest 3.

				int previousPointIndex = SelectedPointIndex - 1;
				int nextPointIndex = SelectedPointIndex + 1;
				int oppositePointIndex = SelectedPointIndex + 2;

				if (previousPointIndex < 0)
				{
					previousPointIndex = controlPoints.Count - 1;
				}

				if (nextPointIndex >= controlPoints.Count)
				{
					nextPointIndex = 0;
					oppositePointIndex = 1;
				}
				else if (oppositePointIndex >= controlPoints.Count)
				{
					oppositePointIndex = 0;
				}


				ControlPoint previousPoint = controlPoints.ElementAt(previousPointIndex);
				ControlPoint oppositePoint = controlPoints.ElementAt(oppositePointIndex);
				ControlPoint nextPoint = controlPoints.ElementAt(nextPointIndex);


				//Now that we know the indexed order of the points, we can align everything properly.
				if (SelectedPointIndex == 2 || SelectedPointIndex == 0)
				{
					//Control point visual order (counter-clockwise order always goes selectedPoint, previousPoint, oppositePoint, nextPoint,
					//where moving point == selectedPoint):
					//
					//static (opposite) point		horizontally aligned point
					//vertically aligned point		moving point
					//OR
					//moving point					vertically aligned point
					//horizontally aligned point	static (opposite) point


					//Update the previous control point's position.
					previousPoint.Position = new PointD(previousPoint.Position.X, current_point.Y);

					//Update the next control point's position.
					nextPoint.Position = new PointD(current_point.X, nextPoint.Position.Y);


					//Even though it's supposed to be static, just in case the points get out of order
					//(they do sometimes), update the opposite control point's position.
					oppositePoint.Position = new PointD(previousPoint.Position.X, nextPoint.Position.Y);
				}
				else
				{
					//Control point visual order (counter-clockwise order always goes selectedPoint, previousPoint, oppositePoint, nextPoint,
					//where moving point == selectedPoint):
					//
					//horizontally aligned point	static (opposite) point
					//moving point					vertically aligned point
					//OR
					//vertically aligned point		moving point
					//static (opposite) point		horizontally aligned point


					//Update the previous control point's position.
					previousPoint.Position = new PointD(current_point.X, previousPoint.Position.Y);

					//Update the next control point's position.
					nextPoint.Position = new PointD(nextPoint.Position.X, current_point.Y);


					//Even though it's supposed to be static, just in case the points get out of order
					//(they do sometimes), update the opposite control point's position.
					oppositePoint.Position = new PointD(nextPoint.Position.X, previousPoint.Position.Y);
				}
			}
		}
    }
}
