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
		public enum ShapeTypes
		{
			OpenLineCurveSeries,
			ClosedLineCurveSeries,
			Ellipse,
			RoundedLineSeries
		}

        protected readonly BaseTool owner;

        protected bool isDrawing = false;

        protected PointD shapeOrigin;
        protected PointD currentPoint;
        protected Color outlineColor;
        protected Color fillColor;

        protected ToolBarComboBox brushWidth;
        protected ToolBarLabel brushWidthLabel;
        protected ToolBarButton brushWidthMinus;
        protected ToolBarButton brushWidthPlus;
        protected ToolBarLabel fillLabel;
        protected ToolBarDropDownButton fillButton;
        protected Gtk.SeparatorToolItem fillSep;
        protected Rectangle lastDirty;
        protected bool surfaceModified;

		protected ToolBarLabel shapeTypeLabel;
		protected ToolBarDropDownButton shapeTypeButton;
		protected Gtk.SeparatorToolItem shapeTypeSep;

        protected DashPatternBox dashPBox = new DashPatternBox();


        protected int BrushWidth
        {
            get
            {
                int width;
                if (Int32.TryParse(brushWidth.ComboBox.ActiveText, out width))
                {
                    if (width > 0)
                    {
                        (brushWidth.ComboBox as Gtk.ComboBoxEntry).Entry.Text = width.ToString();
                        return width;
                    }
                }
                (brushWidth.ComboBox as Gtk.ComboBoxEntry).Entry.Text = BaseTool.DEFAULT_BRUSH_WIDTH.ToString();
                return BaseTool.DEFAULT_BRUSH_WIDTH;
            }
            set { (brushWidth.ComboBox as Gtk.ComboBoxEntry).Entry.Text = value.ToString(); }
        }

        protected bool ShowAntialiasingButton { get { return true; } }
        protected bool StrokeShape { get { return (int)fillButton.SelectedItem.Tag % 2 == 0; } }
        protected bool FillShape { get { return (int)fillButton.SelectedItem.Tag >= 1; } }
		protected ShapeTypes ShapeType { get { return (ShapeTypes)shapeTypeButton.SelectedItem.Tag; } }

        public bool ShowStrokeComboBox = true;


        public static readonly Color HoverColor =
            new Color(ToolControl.FillColor.R / 2d, ToolControl.FillColor.G / 2d, ToolControl.FillColor.B / 2d, ToolControl.FillColor.A * 2d / 3d);

		public const double ShapeClickStartingRange = 10d;
		public const double ShapeClickThicknessFactor = 1d;
		public const double DefaultEndPointTension = 0d;
		public const double DefaultMidPointTension = 1d / 3d;


		private int sPI, sSI;

        public int SelectedPointIndex
		{
			get
			{
				return sPI;
			}

			set
			{
				//Clear the old selection.
				DrawShapes(false, false, false, false);

				sPI = value;
			}
		}

		public int SelectedShapeIndex
		{
			get
			{
				return sSI;
			}

			set
			{
				//Clear the old selection.
				DrawShapes(false, false, false, false);

				sSI = value;
			}
		}

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
        /// The active shape's ShapeEngine.
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
        /// The selected shape's ShapeEngine. This can be null.
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

		public PointD HoverPoint = new PointD(-1d, -1d);
		public int HoveredPointAsControlPoint = -1;

		public bool ChangingTension = false;
		public PointD LastMousePosition = new PointD(0d, 0d);


        //Helps to keep track of the first modification on a shape after the mouse is clicked, to prevent unnecessary history items.
		public bool ClickedWithoutModifying = false;


        //Stores the editable shape data.
		public static ShapeEngineCollection SEngines = new ShapeEngineCollection();


        #region ToolbarEventHandlers

        protected virtual void BrushMinusButtonClickedEvent(object o, EventArgs args)
        {
            if (BrushWidth > 1)
                BrushWidth--;

            DrawShapes(false, false, true, false);
        }

        protected virtual void BrushPlusButtonClickedEvent(object o, EventArgs args)
        {
            BrushWidth++;

            DrawShapes(false, false, true, false);
        }

        #endregion ToolbarEventHandlers


        public BaseEditEngine(BaseTool passedOwner)
        {
            owner = passedOwner;

			owner.Editable = true;

			resetShapes();
        }


        public virtual void HandleBuildToolBar(Gtk.Toolbar tb)
        {
            if (brushWidthLabel == null)
                brushWidthLabel = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Brush width")));

            tb.AppendItem(brushWidthLabel);

            if (brushWidthMinus == null)
            {
                brushWidthMinus = new ToolBarButton("Toolbar.MinusButton.png", "", Catalog.GetString("Decrease brush size"));
                brushWidthMinus.Clicked += BrushMinusButtonClickedEvent;
            }

            tb.AppendItem(brushWidthMinus);

            if (brushWidth == null)
                brushWidth = new ToolBarComboBox(65, 1, true, "1", "2", "3", "4", "5", "6", "7", "8", "9",
                "10", "11", "12", "13", "14", "15", "20", "25", "30", "35",
                "40", "45", "50", "55");

            tb.AppendItem(brushWidth);

            if (brushWidthPlus == null)
            {
                brushWidthPlus = new ToolBarButton("Toolbar.PlusButton.png", "", Catalog.GetString("Increase brush size"));
                brushWidthPlus.Clicked += BrushPlusButtonClickedEvent;
            }

            tb.AppendItem(brushWidthPlus);


            if (ShowStrokeComboBox)
            {
                if (fillSep == null)
                    fillSep = new Gtk.SeparatorToolItem();

                tb.AppendItem(fillSep);

                if (fillLabel == null)
                    fillLabel = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Fill Style")));

                tb.AppendItem(fillLabel);

                if (fillButton == null)
                {
                    fillButton = new ToolBarDropDownButton();

                    fillButton.AddItem(Catalog.GetString("Outline Shape"), "ShapeTool.Outline.png", 0);
                    fillButton.AddItem(Catalog.GetString("Fill Shape"), "ShapeTool.Fill.png", 1);
                    fillButton.AddItem(Catalog.GetString("Fill and Outline Shape"), "ShapeTool.OutlineFill.png", 2);
                }

                tb.AppendItem(fillButton);
            }


			if (shapeTypeSep == null)
				shapeTypeSep = new Gtk.SeparatorToolItem();

			tb.AppendItem(shapeTypeSep);

			if (shapeTypeLabel == null)
				shapeTypeLabel = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Shape Type")));

			tb.AppendItem(shapeTypeLabel);

			if (shapeTypeButton == null)
			{
				shapeTypeButton = new ToolBarDropDownButton();
				
				shapeTypeButton.AddItem(Catalog.GetString("Open Line/Curve Series"), "Tools.Line.png", 0);
				shapeTypeButton.AddItem(Catalog.GetString("Closed Line/Curve Series"), "Tools.Rectangle.png", 1);
				shapeTypeButton.AddItem(Catalog.GetString("Ellipse"), "Tools.Ellipse.png", 2);
				shapeTypeButton.AddItem(Catalog.GetString("Rounded Line Series"), "Tools.RoundedRectangle.png", 3);

				shapeTypeButton.SelectedItemChanged += (o, e) =>
				{
					ShapeEngine activeEngine = ActiveShapeEngine;

					if (activeEngine != null)
					{
						activeEngine = activeEngine.GenericClone(ShapeType);
					}

					//Draw the new shape with organized points generation (for mouse detection).
					DrawShapes(true, false, true, false);
				};
			}

			tb.AppendItem(shapeTypeButton);


            Gtk.ComboBox dpbBox = dashPBox.SetupToolbar(tb);

            if (dpbBox != null)
            {
                dpbBox.Changed += (o, e) =>
                {
					ShapeEngine activeEngine = ActiveShapeEngine;

					if (activeEngine != null)
					{
						activeEngine.DashPattern = dpbBox.ActiveText;
					}

                    //Update the shape.
                    DrawShapes(false, false, true, false);
                };
            }
        }

        public virtual void HandleActivated()
        {
            DrawShapes(false, false, true, false);

            PintaCore.Palette.PrimaryColorChanged += new EventHandler(Palette_PrimaryColorChanged);
            PintaCore.Palette.SecondaryColorChanged += new EventHandler(Palette_SecondaryColorChanged);
        }

		public virtual void HandleDeactivated(BaseTool newTool)
        {
			if (!newTool.Editable)
			{
				//Finalize the previous shape (if needed).
				DrawShapes(false, true, false, false);
			}
			else
			{
				//Redraw the previous shape without any hover or selection points.
				DrawShapes(false, false, false, false);

				//Then deselect (if needed).
				SelectedPointIndex = -1;
				SelectedShapeIndex = -1;
			}

            PintaCore.Palette.PrimaryColorChanged -= Palette_PrimaryColorChanged;
            PintaCore.Palette.SecondaryColorChanged += Palette_SecondaryColorChanged;
        }

        public virtual void HandleCommit()
        {
            //Finalize the previous shape (if needed).
			DrawShapes(false, true, false, false);
        }

        public virtual void HandleAfterUndo()
        {
            surfaceModified = true;


            ShapeEngine activeEngine = ActiveShapeEngine;

            if (activeEngine != null)
            {
                owner.UseAntialiasing = activeEngine.AntiAliasing;

                //Update the DashPatternBox to represent the current shape's DashPattern.
                (dashPBox.comboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = activeEngine.DashPattern;
            }

            //Draw the current state.
            DrawShapes(true, false, true, false);
        }

        public virtual void HandleAfterRedo()
        {
            surfaceModified = true;


            ShapeEngine activeEngine = ActiveShapeEngine;

            if (activeEngine != null)
            {
                owner.UseAntialiasing = activeEngine.AntiAliasing;

                //Update the DashPatternBox to represent the current shape's DashPattern.
                (dashPBox.comboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = activeEngine.DashPattern;
            }


            //Draw the current state.
            DrawShapes(true, false, true, false);
        }

        public virtual bool HandleKeyDown(Gtk.DrawingArea canvas, Gtk.KeyPressEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Delete)
            {
                if (SelectedPointIndex > -1)
                {
                    List<ControlPoint> controlPoints = SelectedShapeEngine.ControlPoints;

					//Either delete a ControlPoint or an entire shape (if there's only 1 ControlPoint left).
                    if (controlPoints.Count > 1)
                    {
						//Create a new ShapeModifyHistoryItem so that the deletion of a control point can be undone.
						PintaCore.Workspace.ActiveDocument.History.PushNewItem(
							new ShapesModifyHistoryItem(this, owner.Icon, Catalog.GetString("Shape Point Deleted")));

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

						//Create a new ShapeHistoryItem so that the deletion of a shape can be undone.
						doc.History.PushNewItem(
							new ShapesHistoryItem(this, owner.Icon, Catalog.GetString("Shape Deleted"),
								doc.CurrentUserLayer.Surface.Clone(), doc.CurrentUserLayer, SelectedPointIndex, SelectedShapeIndex));

						//Clear the drawing of the shape.
						SEngines[SelectedShapeIndex].DrawingLayer.Layer.Clear();

						//Delete the selected shape.
						SEngines.RemoveAt(SelectedShapeIndex);

						//Redraw the workspace.
						doc.Workspace.Invalidate();

                        SelectedPointIndex = -1;
						--SelectedShapeIndex;
                    }

                    surfaceModified = true;

                    HoverPoint = new PointD(-1d, -1d);

                    DrawShapes(true, false, true, false);
                }

                args.RetVal = true;
            }
            else if (args.Event.Key == Gdk.Key.Return)
            {
                //Finalize the previous shape (if needed).
				DrawShapes(false, true, false, false);

                args.RetVal = true;
            }
            else if (args.Event.Key == Gdk.Key.space)
            {
                ControlPoint selPoint = SelectedPoint;

                if (selPoint != null)
                {
                    //This can be assumed not to be null since selPoint was not null.
                    ShapeEngine selEngine = SelectedShapeEngine;

                    //Create a new ShapeModifyHistoryItem so that the adding of a control point can be undone.
                    PintaCore.Workspace.ActiveDocument.History.PushNewItem(
						new ShapesModifyHistoryItem(this, owner.Icon, Catalog.GetString("Shape Point Added")));


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
                        shapeOrigin = new PointD(selPoint.Position.X, selPoint.Position.Y);

                        if (shiftKey)
                        {
                            CalculateModifiedCurrentPoint();
                        }

                        //Space only: position of mouse (after any potential shift alignment).
                        newPointPos = new PointD(currentPoint.X, currentPoint.Y);
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

                    DrawShapes(true, false, true, shiftKey);
                }

                args.RetVal = true;
            }
            else if (args.Event.Key == Gdk.Key.Up)
            {
                //Make sure a control point is selected.
                if (SelectedPointIndex > -1)
                {
                    //Move the selected control point.
                    SelectedPoint.Position.Y -= 1d;

                    DrawShapes(true, false, true, false);
                }

                args.RetVal = true;
            }
            else if (args.Event.Key == Gdk.Key.Down)
            {
                //Make sure a control point is selected.
                if (SelectedPointIndex > -1)
                {
                    //Move the selected control point.
                    SelectedPoint.Position.Y += 1d;

                    DrawShapes(true, false, true, false);
                }

                args.RetVal = true;
            }
            else if (args.Event.Key == Gdk.Key.Left)
            {
                //Make sure a control point is selected.
                if (SelectedPointIndex > -1)
                {
                    if ((args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
                    {
                        //Change the selected control point to be the previous one, if applicable.
                        if (SelectedPointIndex > 0)
                        {
                            --SelectedPointIndex;
                        }
                    }
                    else
                    {
                        //Move the selected control point.
                        SelectedPoint.Position.X -= 1d;
                    }

                    DrawShapes(true, false, true, false);
                }

                args.RetVal = true;
            }
            else if (args.Event.Key == Gdk.Key.Right)
            {
                //Make sure a control point is selected.
                if (SelectedPointIndex > -1)
                {
                    if ((args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
                    {
                        //Change the selected control point to be the following one, if applicable.
                        if (SelectedPointIndex < SelectedShapeEngine.ControlPoints.Count - 1)
                        {
                            ++SelectedPointIndex;
                        }
                    }
                    else
                    {
                        //Move the selected control point.
                        SelectedPoint.Position.X += 1d;
                    }

                    DrawShapes(true, false, true, false);
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
            if (args.Event.Key == Gdk.Key.Delete || args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.space
                || args.Event.Key == Gdk.Key.Up || args.Event.Key == Gdk.Key.Down
                || args.Event.Key == Gdk.Key.Left || args.Event.Key == Gdk.Key.Right)
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
            // If we are already drawing, ignore any additional mouse down events
            if (isDrawing)
                return;
			
            Document doc = PintaCore.Workspace.ActiveDocument;

            shapeOrigin = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));
            currentPoint = shapeOrigin;

            bool shiftKey = (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;

            if (shiftKey)
            {
                CalculateModifiedCurrentPoint();
            }

            isDrawing = true;
            surfaceModified = true;

            outlineColor = PintaCore.Palette.PrimaryColor;
            fillColor = PintaCore.Palette.SecondaryColor;


            //Right clicking changes tension.
            if (args.Event.Button == 1)
            {
                ChangingTension = false;
            }
            else
            {
                ChangingTension = true;
            }


			int closestCPIndex, closestCPShapeIndex;
			ControlPoint closestControlPoint;
			double closestCPDistance;

			SEngines.FindClosestControlPoint(currentPoint,
				out closestCPShapeIndex, out closestCPIndex, out closestControlPoint, out closestCPDistance);

            int closestShapeIndex, closestPointIndex;
            PointD closestPoint;
            double closestDistance;

            OrganizedPointCollection.FindClosestPoint(SEngines, currentPoint,
                out closestShapeIndex, out closestPointIndex, out closestPoint, out closestDistance);

            bool clickedOnControlPoint = false;

			double currentClickRange = ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor;

			//Determine if the closest ControlPoint is within the expected click range.
			if (closestControlPoint != null && closestCPDistance < currentClickRange)
			{
				//User clicked directly on a ControlPoint on a shape.

				ClickedWithoutModifying = true;

				SelectedPointIndex = closestCPIndex;
				SelectedShapeIndex = closestCPShapeIndex;

				clickedOnControlPoint = true;
			}
			else if (closestDistance < currentClickRange) //Determine if the user clicked close enough to a shape.
			{
				//User clicked on a generated point on a shape.

				List<ControlPoint> controlPoints = SEngines[closestShapeIndex].ControlPoints;

				//Note: compare the currentPoint's distance here because it's the actual mouse position.
				if (controlPoints.Count > closestPointIndex && currentPoint.Distance(controlPoints[closestPointIndex].Position) < currentClickRange)
				{
					//User clicked on a control point (on the "previous order" side of the point).

					ClickedWithoutModifying = true;

					SelectedPointIndex = closestPointIndex;
					SelectedShapeIndex = closestShapeIndex;

					clickedOnControlPoint = true;
				}
				else if (closestPointIndex > 0)
				{
					if (currentPoint.Distance(controlPoints[closestPointIndex - 1].Position) < currentClickRange)
					{
						//User clicked on a control point (on the "following order" side of the point).

						ClickedWithoutModifying = true;

						SelectedPointIndex = closestPointIndex - 1;
						SelectedShapeIndex = closestShapeIndex;

						clickedOnControlPoint = true;
					}
					else if (controlPoints.Count > 0 && currentPoint.Distance(controlPoints[controlPoints.Count - 1].Position) < currentClickRange)
					{
						//User clicked on a control point (on the "following order" side of the point).

						ClickedWithoutModifying = true;

						SelectedPointIndex = closestPointIndex - 1;
						SelectedShapeIndex = closestShapeIndex;

						clickedOnControlPoint = true;
					}
				}

				//Check for clicking on a non-control point. Don't do anything here if right clicked.
				if (!ChangingTension)
				{
					if (!clickedOnControlPoint)
					{
						//User clicked on a non-control point on a shape.

						//Create a new ShapeModifyHistoryItem so that the adding of a control point can be undone.
						doc.History.PushNewItem(
							new ShapesModifyHistoryItem(this, owner.Icon, Catalog.GetString("Shape Point Added")));

						controlPoints.Insert(closestPointIndex,
							new ControlPoint(new PointD(currentPoint.X, currentPoint.Y), DefaultMidPointTension));

						SelectedPointIndex = closestPointIndex;
						SelectedShapeIndex = closestShapeIndex;
					}
				}
			}

            bool ctrlKey = (args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask;

			//Create a new shape if the user control + clicks on an existing point or if the user simply clicks outside of any shapes.
			if (!ChangingTension && ((ctrlKey && clickedOnControlPoint) || (closestCPDistance >= currentClickRange && closestDistance >= currentClickRange)))
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


				//Create a new ShapeHistoryItem so that the creation of a new shape can be undone.
				doc.History.PushNewItem(
					new ShapesHistoryItem(this, owner.Icon, Catalog.GetString("Shape Added"),
						doc.CurrentUserLayer.Surface.Clone(), doc.CurrentUserLayer, SelectedPointIndex, SelectedShapeIndex));

				addShape();


                ShapeEngine activeEngine = ActiveShapeEngine;

				if (activeEngine != null)
				{
					//Verify that the user clicked inside the image bounds or that the user is
					//holding the Ctrl key (to ignore the Image bounds and draw on the edge).
					if ((point.X == shapeOrigin.X && point.Y == shapeOrigin.Y) || ctrlKey)
					{
						isDrawing = true;

						createShape(ctrlKey, clickedOnControlPoint, activeEngine, prevSelPoint);
					}
				}
            }

            //If the user right clicks outside of any shapes.
			if ((closestCPDistance >= currentClickRange && closestDistance >= currentClickRange) && ChangingTension)
            {
                ClickedWithoutModifying = true;
            }

            surfaceModified = true;

            DrawShapes(false, false, true, shiftKey);
        }

        public virtual void HandleMouseUp(Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
        {
            isDrawing = false;

            ChangingTension = false;

            DrawShapes(true, false, true, args.Event.IsShiftPressed());
        }

        public virtual void HandleMouseMove(object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
        {
            Document doc = PintaCore.Workspace.ActiveDocument;

            currentPoint = new PointD(Utility.Clamp(point.X, 0, doc.ImageSize.Width - 1), Utility.Clamp(point.Y, 0, doc.ImageSize.Height - 1));

            bool shiftKey = (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;

            if (shiftKey)
            {
                CalculateModifiedCurrentPoint();
            }


            if (!isDrawing)
            {
                //Redraw everything to show a (temporary) highlighted control point when applicable.
                DrawShapes(false, false, true, shiftKey);
            }
            else
            {
				ControlPoint selPoint = SelectedPoint;

                //Make sure a control point is selected.
				if (selPoint != null)
                {
                    if (ClickedWithoutModifying)
                    {
                        //Create a new ShapeModifyHistoryItem so that the modification of the shape can be undone.
                        doc.History.PushNewItem(
							new ShapesModifyHistoryItem(this, owner.Icon, Catalog.GetString("Shape Modified")));

                        ClickedWithoutModifying = false;
                    }

                    List<ControlPoint> controlPoints = SelectedShapeEngine.ControlPoints;

                    if (!ChangingTension)
                    {
                        //Moving a control point.

                        //Make sure the control point was moved.
						if (currentPoint.X != selPoint.Position.X || currentPoint.Y != selPoint.Position.Y)
                        {
                            movePoint(controlPoints);
                        }
                    }
                    else
                    {
                        //Changing a control point's tension.

                        //Unclamp the mouse position when changing tension.
                        currentPoint = new PointD(point.X, point.Y);

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
                            xChange = currentPoint.X - LastMousePosition.X;
                        }
                        else
                        {
                            xChange = LastMousePosition.X - currentPoint.X;
                        }

                        //Calculate the y change in the mouse position.
                        if (curPoint.Y <= midPoint.Y)
                        {
                            yChange = currentPoint.Y - LastMousePosition.Y;
                        }
                        else
                        {
                            yChange = LastMousePosition.Y - currentPoint.Y;
                        }

                        //Update the control point's tension.

                        //Note: the difference factors are to be inverted for x and y change because this is perpendicular motion.
                        controlPoints[SelectedPointIndex].Tension +=
                            Math.Round(Utility.Clamp((xChange * yDiff + yChange * xDiff) / totalDiff, -1d, 1d)) / 50d;

                        //Restrict the new tension to range from 0d to 1d.
                        controlPoints[SelectedPointIndex].Tension =
							Utility.Clamp(selPoint.Tension, 0d, 1d);
                    }

                    surfaceModified = true;

                    DrawShapes(false, false, true, shiftKey);
                }
            }

            LastMousePosition = currentPoint;
        }


		protected void Palette_PrimaryColorChanged(object sender, EventArgs e)
		{
			outlineColor = PintaCore.Palette.PrimaryColor;

			DrawShapes(false, false, true, false);
		}

		protected void Palette_SecondaryColorChanged(object sender, EventArgs e)
		{
			fillColor = PintaCore.Palette.SecondaryColor;

			DrawShapes(false, false, true, false);
		}


		public virtual void DrawControlPoints(Context g, Rectangle? dirty, bool drawHoverSelection)
		{
			ShapeEngine activeEngine = ActiveShapeEngine;

			if (activeEngine != null)
			{
				//Draw the control points for the active shape.

				int controlPointSize = Math.Min(BrushWidth + 1, 5);
				double controlPointOffset = (double)controlPointSize / 2d;

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

				//If the shape has one or more points.
				if (controlPoints.Count > 0)
				{
					//Draw the control points for the shape.
					for (int i = 0; i < controlPoints.Count; ++i)
					{
						//Skip drawing the hovered control point.
						if (drawHoverSelection && HoveredPointAsControlPoint > -1 && HoverPoint.Distance(controlPoints[i].Position) < 1d)
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

				if (drawHoverSelection)
				{
					//Draw the hover point.
					if (!ChangingTension && HoverPoint.X > -1d)
					{
						g.FillStrokedEllipse(new Rectangle(
							HoverPoint.X - controlPointOffset * 4d, HoverPoint.Y - controlPointOffset * 4d,
							controlPointOffset * 8d, controlPointOffset * 8d), HoverColor, HoverColor, 1);
						g.FillStrokedEllipse(new Rectangle(
							HoverPoint.X - controlPointOffset, HoverPoint.Y - controlPointOffset,
							controlPointSize, controlPointSize), HoverColor, HoverColor, controlPointSize);
					}
				}

				if (dirty != null)
				{
					//Inflate to accomodate for control points.
					dirty = dirty.Value.Inflate(controlPointSize * 8, controlPointSize * 8);
				}
			}
		}

		/// <summary>
		/// Draw all of the shapes that are currently being drawn/edited by the user.
		/// </summary>
		/// <param name="calculateOrganizedPoints">Whether or not to calculate the spatially organized
		/// points for mouse detection after drawing the shape.</param>
		/// <param name="finalize">Whether or not to finalize the drawing.</param>
		/// <param name="drawHoverSelection">Whether or not to draw any hover point or selected point.</param>
		/// <param name="shiftKey">Whether or not the shift key is being pressed.</param>
		public void DrawShapes(bool calculateOrganizedPoints, bool finalize, bool drawHoverSelection, bool shiftKey)
		{
			if (!surfaceModified || SEngines.Count == 0)
			{
				return;
			}

			ShapeEngine activeEngine = ActiveShapeEngine;

			if (activeEngine == null)
			{
				return;
			}

			activeEngine.DrawingLayer.Layer.Clear();

			Document doc = PintaCore.Workspace.ActiveDocument;

			Rectangle dirty;

			if (finalize)
			{
				ImageSurface undoSurface = null;

				// We only need to create a history item if there was a previous shape.
				if (activeEngine != null)
				{
					if (activeEngine.ControlPoints.Count > 0)
					{
						undoSurface = doc.CurrentUserLayer.Surface.Clone();
					}
				}

				isDrawing = false;
				surfaceModified = false;

				int previousSelectedPointIndex = SelectedPointIndex;
				int previousSelectedShapeIndex = SelectedShapeIndex;

				SelectedPointIndex = -1;

				dirty = DrawShape(
					Utility.PointsToRectangle(shapeOrigin, new PointD(currentPoint.X, currentPoint.Y), shiftKey),
					doc.CurrentUserLayer, false, drawHoverSelection);

				//Make sure that the undo surface isn't null and that there are actually points.
				if (undoSurface != null)
				{
					//Create a new ShapesHistoryItem so that the finalization of the shapes can be undone.
					doc.History.PushNewItem(new ShapesHistoryItem(this, owner.Icon, Catalog.GetString("Shape Finalized"),
							undoSurface, doc.CurrentUserLayer, previousSelectedPointIndex, previousSelectedShapeIndex));
				}

				//Clear out all of the old data.
				resetShapes();
			}
			else
			{
				//Only calculate the hover point when there isn't a request to organize the generated points by spatial hashing.
				if (!calculateOrganizedPoints)
				{
					if (drawHoverSelection)
					{
						//Calculate the hover point, if any.

						int closestCPIndex, closestCPShapeIndex;
						ControlPoint closestControlPoint;
						double closestCPDistance;

						SEngines.FindClosestControlPoint(currentPoint,
							out closestCPShapeIndex, out closestCPIndex, out closestControlPoint, out closestCPDistance);

						int closestShapeIndex, closestPointIndex;
						PointD closestPoint;
						double closestDistance;

						OrganizedPointCollection.FindClosestPoint(SEngines, currentPoint,
							out closestShapeIndex, out closestPointIndex, out closestPoint, out closestDistance);

						bool clickedOnControlPoint = false;

						double currentClickRange = ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor;

						List<ControlPoint> controlPoints = SEngines[closestShapeIndex].ControlPoints;

						//Determine if the closest ControlPoint is within the expected click range.
						if (closestControlPoint != null && closestCPDistance < currentClickRange)
						{
							//User clicked directly on a ControlPoint on a shape.

							HoverPoint.X = closestControlPoint.Position.X;
							HoverPoint.Y = closestControlPoint.Position.Y;
							HoveredPointAsControlPoint = closestCPIndex;
						}
						else if (closestDistance < currentClickRange) //Determine if the user is hovering the mouse close enough to a shape.
						{
							//User is hovering over a generated point on a shape.

							if (controlPoints.Count > closestPointIndex)
							{
								//Note: compare the currentPoint's distance here because it's the actual mouse position.
								if (currentPoint.Distance(controlPoints[closestPointIndex].Position) < currentClickRange)
								{
									//Mouse hovering over a control point (on the "previous order" side of the point).

									HoverPoint.X = controlPoints[closestPointIndex].Position.X;
									HoverPoint.Y = controlPoints[closestPointIndex].Position.Y;
									HoveredPointAsControlPoint = closestPointIndex;
								}
								else if (closestPointIndex > 0)
								{
									if (currentPoint.Distance(controlPoints[closestPointIndex - 1].Position) < currentClickRange)
									{
										//Mouse hovering over a control point (on the "following order" side of the point).

										HoverPoint.X = controlPoints[closestPointIndex - 1].Position.X;
										HoverPoint.Y = controlPoints[closestPointIndex - 1].Position.Y;
										HoveredPointAsControlPoint = closestPointIndex - 1;
									}
								}
								else if (controlPoints.Count > 0 &&
									currentPoint.Distance(controlPoints[controlPoints.Count - 1].Position) < currentClickRange)
								{
									//Mouse hovering over a control point (on the "following order" side of the point).

									HoveredPointAsControlPoint = controlPoints.Count - 1;
									HoverPoint.X = controlPoints[HoveredPointAsControlPoint].Position.X;
									HoverPoint.Y = controlPoints[HoveredPointAsControlPoint].Position.Y;
								}
							}

							if (HoverPoint.X < 0d)
							{
								HoverPoint.X = closestPoint.X;
								HoverPoint.Y = closestPoint.Y;
							}
						}
					}
					else
					{
						//Reset the hover point. NOTE: this is necessary even though the hover point is reset later. It affects the DrawShape call.
						HoverPoint = new PointD(-1d, -1d);
						HoveredPointAsControlPoint = -1;
					}
				}

				dirty = DrawShape(
					Utility.PointsToRectangle(shapeOrigin, new PointD(currentPoint.X, currentPoint.Y), shiftKey),
					activeEngine.DrawingLayer.Layer, true, drawHoverSelection);

				//Reset the hover point after each drawing.
				HoverPoint = new PointD(-1d, -1d);
				HoveredPointAsControlPoint = -1;
			}

			if (calculateOrganizedPoints)
			{
				//Organize the generated points for quick mouse interaction detection.

				//First, clear the previously organized points, if any.
				activeEngine.OrganizedPoints.ClearCollection();

				int pointIndex = 0;

				foreach (PointD p in activeEngine.GeneratedPoints)
				{
					activeEngine.OrganizedPoints.StoreAndOrganizePoint(new OrganizedPoint(new PointD(p.X, p.Y), pointIndex));

					//Keep track of the point's order in relation to the control points.
					if (activeEngine.ControlPoints.Count > pointIndex
						&& p.X == activeEngine.ControlPoints[pointIndex].Position.X
						&& p.Y == activeEngine.ControlPoints[pointIndex].Position.Y)
					{
						++pointIndex;
					}
				}
			}

			// Increase the size of the dirty rect to account for antialiasing.
			if (owner.UseAntialiasing)
			{
				dirty = dirty.Inflate(1, 1);
			}

			dirty = ((Rectangle?)dirty).UnionRectangles(lastDirty).Value;
			dirty = dirty.Clamp();
			doc.Workspace.Invalidate(dirty.ToGdkRectangle());
			lastDirty = dirty;
		}

		protected Rectangle DrawShape(Rectangle rect, Layer l, bool drawControlPoints, bool drawHoverSelection)
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

					activeEngine.AntiAliasing = owner.UseAntialiasing;

					g.Antialias = owner.UseAntialiasing ? Antialias.Subpixel : Antialias.None;

					g.SetDash(DashPatternBox.GenerateDashArray(activeEngine.DashPattern, BrushWidth), 0.0);

					g.LineWidth = BrushWidth;

					//Draw the shape.
					if (activeEngine.ControlPoints.Count > 0)
					{
						//Generate the points that make up the shape.
						activeEngine.GeneratePoints();

						//Expand the invalidation rectangle as necessary.

						if (FillShape)
						{
							dirty = dirty.UnionRectangles(g.FillPolygonal(activeEngine.GeneratedPoints, fillColor));
						}

						if (StrokeShape)
						{
							dirty = dirty.UnionRectangles(g.DrawPolygonal(activeEngine.GeneratedPoints, outlineColor));
						}
					}

					g.SetDash(new double[] { }, 0.0);


					drawExtras(dirty, g);


					if (drawControlPoints)
					{
						DrawControlPoints(g, dirty, drawHoverSelection);
					}
				}
			}


			return dirty ?? new Rectangle(0d, 0d, 0d, 0d);
		}


		/// <summary>
		/// Calculate the modified position of current_point such that the angle between the adjacent point
		/// (if any) and current_point is snapped to the closest angle out of a certain number of angles.
		/// </summary>
		public void CalculateModifiedCurrentPoint()
		{
			ShapeEngine selEngine = SelectedShapeEngine;
			ControlPoint adjacentPoint;

			if (selEngine == null)
			{
				//Don't bother calculating a modified point because there is no selected shape.
				return;
			}
			else
			{
				if (SelectedPointIndex > 0)
				{
					adjacentPoint = selEngine.ControlPoints[SelectedPointIndex - 1];
				}
				else if (SelectedPointIndex + 1 < selEngine.ControlPoints.Count)
				{
					adjacentPoint = selEngine.ControlPoints[SelectedPointIndex + 1];
				}
				else
				{
					//Don't bother calculating a modified point because there is no reference point to align it with (there is only 1 point).
					return;
				}
			}

			PointD dir = new PointD(currentPoint.X - adjacentPoint.Position.X, currentPoint.Y - adjacentPoint.Position.Y);
			double theta = Math.Atan2(dir.Y, dir.X);
			double len = Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);

			theta = Math.Round(12 * theta / Math.PI) * Math.PI / 12;
			currentPoint = new PointD((adjacentPoint.Position.X + len * Math.Cos(theta)), (adjacentPoint.Position.Y + len * Math.Sin(theta)));
		}

		protected void resetShapes()
		{
			SEngines = new ShapeEngineCollection();
			
			SelectedPointIndex = -1;
			SelectedShapeIndex = 0;
		}


        protected Gdk.Rectangle getRectangleFromPoints(PointD a, PointD b)
        {
            int x = (int)Math.Min(a.X, b.X) - BrushWidth - 2;
            int y = (int)Math.Min(a.Y, b.Y) - BrushWidth - 2;
            int w = (int)Math.Max(a.X, b.X) - x + (BrushWidth * 2) + 4;
            int h = (int)Math.Max(a.Y, b.Y) - y + (BrushWidth * 2) + 4;

            return new Gdk.Rectangle(x, y, w, h);
        }


        protected virtual void createShape(bool ctrlKey, bool clickedOnControlPoint, ShapeEngine activeEngine, PointD prevSelPoint)
        {

        }

        protected virtual void movePoint(List<ControlPoint> controlPoints)
        {
			//Update the control point's position.
			controlPoints.ElementAt(SelectedPointIndex).Position = new PointD(currentPoint.X, currentPoint.Y);
        }

        protected virtual void drawExtras(Rectangle? dirty, Context g)
        {
            
        }

		protected virtual void addShape()
		{
			//Overrides should implement (with their own ShapeEngine child type):
			//Document doc = PintaCore.Workspace.ActiveDocument;
			//SEngines.Add(new ShapeEngine(doc.CurrentUserLayer, owner.UseAntialiasing));
			//base.AddShape();

			SelectedShapeIndex = SEngines.Count - 1;
		}


		protected void addLinePoints(bool ctrlKey, bool clickedOnControlPoint, ShapeEngine activeEngine, PointD prevSelPoint)
		{
			PointD startingPoint;

			//Create the initial points of the shape. The second point will follow the mouse around until released.
			if (ctrlKey && clickedOnControlPoint)
			{
				startingPoint = prevSelPoint;

				ClickedWithoutModifying = false;
			}
			else
			{
				startingPoint = shapeOrigin;
			}


			activeEngine.ControlPoints.Add(new ControlPoint(new PointD(startingPoint.X, startingPoint.Y), DefaultEndPointTension));
			activeEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y + .01d), DefaultEndPointTension));


			SelectedPointIndex = 1;
			SelectedShapeIndex = SEngines.Count - 1;
		}

		protected void addRectanglePoints(bool ctrlKey, bool clickedOnControlPoint, ShapeEngine activeEngine, PointD prevSelPoint)
		{
			PointD startingPoint;

			//Create the initial points of the shape. The second point will follow the mouse around until released.
			if (ctrlKey && clickedOnControlPoint)
			{
				startingPoint = prevSelPoint;

				ClickedWithoutModifying = false;
			}
			else
			{
				startingPoint = shapeOrigin;
			}


			activeEngine.ControlPoints.Add(new ControlPoint(new PointD(startingPoint.X, startingPoint.Y), 0.0));
			activeEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X, startingPoint.Y + .01d), 0.0));
			activeEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y + .01d), 0.0));
			activeEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y), 0.0));


			SelectedPointIndex = 2;
			SelectedShapeIndex = SEngines.Count - 1;
		}

		protected void moveRectangularPoint(List<ControlPoint> controlPoints)
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


				ControlPoint selectedPoint = controlPoints.ElementAt(SelectedPointIndex);
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
					previousPoint.Position = new PointD(previousPoint.Position.X, currentPoint.Y);

					//Update the next control point's position.
					nextPoint.Position = new PointD(currentPoint.X, nextPoint.Position.Y);


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
					previousPoint.Position = new PointD(currentPoint.X, previousPoint.Position.Y);

					//Update the next control point's position.
					nextPoint.Position = new PointD(nextPoint.Position.X, currentPoint.Y);


					//Even though it's supposed to be static, just in case the points get out of order
					//(they do sometimes), update the opposite control point's position.
					oppositePoint.Position = new PointD(nextPoint.Position.X, previousPoint.Position.Y);
				}
			}
		}
    }
}
