// 
// EditEngine.cs
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
    //a private instance of the EditEngine inside the class and then utilize it in a similar fashion to any of the editable tools.
    public class BaseEditEngine
    {
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
        protected ImageSurface undoSurface;
        protected bool surfaceModified;


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

        public bool ShowStrokeComboBox = true;


        public static readonly Color HoverColor =
            new Color(ToolControl.FillColor.R / 2d, ToolControl.FillColor.G / 2d, ToolControl.FillColor.B / 2d, ToolControl.FillColor.A * 2d / 3d);

        public const double ShapeClickStartingRange = 10d;
        public const double ShapeClickThicknessFactor = 1d;
        public const double DefaultEndPointTension = 0d;
        public const double DefaultMidPointTension = 1d / 3d;


        public int SelectedPointIndex = -1;
        public int SelectedShapeIndex = 0;

        /// <summary>
        /// The selected ControlPoint.
        /// </summary>
        public ControlPoint SelectedPoint
        {
            get
            {
                ShapeEngine selEngine = SelectedShapeEngine;

                if (selEngine != null)
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

                if (selEngine != null)
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
                if (SEngines.Count > SelectedShapeIndex)
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
        public ShapeEngineCollection SEngines = new ShapeEngineCollection(false);



        #region ToolbarEventHandlers

        protected virtual void BrushMinusButtonClickedEvent(object o, EventArgs args)
        {
            if (BrushWidth > 1)
                BrushWidth--;

            DrawShapes(false, false, false);
        }

        protected virtual void BrushPlusButtonClickedEvent(object o, EventArgs args)
        {
            BrushWidth++;

            DrawShapes(false, false, false);
        }

        #endregion ToolbarEventHandlers



        public BaseEditEngine(BaseTool passedOwner)
        {
            owner = passedOwner;
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


            Gtk.ComboBox dpbBox = dashPBox.SetupToolbar(tb);

            if (dpbBox != null)
            {
                dpbBox.Changed += (o, e) =>
                {
                    ActiveShapeEngine.DashPattern = dpbBox.ActiveText;

                    //Update the shape.
                    DrawShapes(false, false, false);
                };
            }
        }

        public virtual void HandleActivated()
        {
            PintaCore.Workspace.ActiveDocument.ToolLayer.Clear();
            PintaCore.Workspace.ActiveDocument.ToolLayer.Hidden = false;

            DrawShapes(false, false, false);

            PintaCore.Palette.PrimaryColorChanged += new EventHandler(Palette_PrimaryColorChanged);
            PintaCore.Palette.SecondaryColorChanged += new EventHandler(Palette_SecondaryColorChanged);
        }

        public virtual void HandleDeactivated()
        {
            PintaCore.Workspace.ActiveDocument.ToolLayer.Hidden = true;

            //Finalize the previous shape (if needed).
            DrawShapes(false, true, false);

            PintaCore.Palette.PrimaryColorChanged -= Palette_PrimaryColorChanged;
            PintaCore.Palette.SecondaryColorChanged += Palette_SecondaryColorChanged;
        }

        public virtual void HandleCommit()
        {
            PintaCore.Workspace.ActiveDocument.ToolLayer.Hidden = true;

            //Finalize the previous shape (if needed).
            DrawShapes(false, true, false);
        }

        public virtual void HandleAfterUndo()
        {
            surfaceModified = true;


            ShapeEngine actEngine = ActiveShapeEngine;

            if (actEngine != null)
            {
                owner.UseAntialiasing = actEngine.AntiAliasing;

                //Update the DashPatternBox to represent the current shape's DashPattern.
                (dashPBox.comboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = actEngine.DashPattern;
            }


            //Draw the current state.
            DrawShapes(true, false, false);
        }

        public virtual void HandleAfterRedo()
        {
            surfaceModified = true;


            ShapeEngine actEngine = ActiveShapeEngine;

            if (actEngine != null)
            {
                owner.UseAntialiasing = actEngine.AntiAliasing;

                //Update the DashPatternBox to represent the current shape's DashPattern.
                (dashPBox.comboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = actEngine.DashPattern;
            }


            //Draw the current state.
            DrawShapes(true, false, false);
        }

        public virtual bool HandleKeyDown(Gtk.DrawingArea canvas, Gtk.KeyPressEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Delete)
            {
                if (SelectedPointIndex > -1)
                {
                    //Create a new ShapeModifyHistoryItem so that the deletion of a control point can be undone.
                    PintaCore.Workspace.ActiveDocument.History.PushNewItem(
                        new ShapeModifyHistoryItem(this, owner.Icon, Catalog.GetString("Shape Point Deleted")));


                    List<ControlPoint> controlPoints = SelectedShapeEngine.ControlPoints;


                    undoSurface = PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.Clone();

                    //Delete the selected point from the shape.
                    controlPoints.RemoveAt(SelectedPointIndex);

                    //Set the newly selected point to be the median-most point on the shape, order-wise.
                    if (controlPoints.Count > 0)
                    {
                        if (SelectedPointIndex > controlPoints.Count / 2)
                        {
                            --SelectedPointIndex;
                        }
                    }
                    else
                    {
                        SelectedPointIndex = -1;
                    }

                    surfaceModified = true;

                    HoverPoint = new PointD(-1d, -1d);

                    DrawShapes(true, false, false);
                }

                args.RetVal = true;
            }
            else if (args.Event.Key == Gdk.Key.Return)
            {
                //Finalize the previous shape (if needed).
                DrawShapes(false, true, false);

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
                        new ShapeModifyHistoryItem(this, owner.Icon, Catalog.GetString("Shape Point Added")));


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

                    DrawShapes(true, false, shiftKey);
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

                    DrawShapes(true, false, false);
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

                    DrawShapes(true, false, false);
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

                    DrawShapes(true, false, false);
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

                    DrawShapes(true, false, false);
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
            doc.ToolLayer.Hidden = false;

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

            int closestShapeIndex, closestPointIndex;
            PointD closestPoint;
            double closestDistance;

            OrganizedPointCollection.FindClosestPoint(SEngines, currentPoint,
                out closestShapeIndex, out closestPointIndex, out closestPoint, out closestDistance);

            bool clickedOnControlPoint = false;

            //Determine if the user clicked close enough to a line, shape, or point that's currently being drawn/edited by the user.
            if (closestDistance < ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor)
            {
                //User clicked on a generated point on a shape.

                List<ControlPoint> controlPoints = SEngines[closestShapeIndex].ControlPoints;

                //Note: compare the currentPoint's distance here because it's the actual mouse position.
                if (controlPoints.Count > closestPointIndex &&
                    currentPoint.Distance(controlPoints[closestPointIndex].Position) < ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor)
                {
                    //User clicked on a control point (on the "previous order" side of the point).

                    ClickedWithoutModifying = true;

                    SelectedPointIndex = closestPointIndex;
                    SelectedShapeIndex = closestShapeIndex;

                    clickedOnControlPoint = true;
                }
                else if (currentPoint.Distance(controlPoints[closestPointIndex - 1].Position) < ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor)
                {
                    //User clicked on a control point (on the "following order" side of the point).

                    ClickedWithoutModifying = true;

                    SelectedPointIndex = closestPointIndex - 1;
                    SelectedShapeIndex = closestShapeIndex;

                    clickedOnControlPoint = true;
                }

                //Don't change anything here if right clicked.
                if (!ChangingTension)
                {
                    if (!clickedOnControlPoint)
                    {
                        //User clicked on a non-control point on a shape.

                        //Create a new ShapeModifyHistoryItem so that the adding of a control point can be undone.
                        doc.History.PushNewItem(
                            new ShapeModifyHistoryItem(this, owner.Icon, Catalog.GetString("Shape Point Added")));

                        controlPoints.Insert(closestPointIndex,
                            new ControlPoint(new PointD(currentPoint.X, currentPoint.Y), DefaultMidPointTension));

                        SelectedPointIndex = closestPointIndex;
                        SelectedShapeIndex = closestShapeIndex;
                    }
                }
            }

            bool ctrlKey = (args.Event.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask;

            //Create a new shape if the user simply clicks outside of any shapes or if the user control + clicks on an existing point.
            if (!ChangingTension && ((ctrlKey && clickedOnControlPoint) || closestDistance >= ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor))
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



                //Next, take care of the old shape's data.

                //Finalize the previous shape (if needed).
                DrawShapes(false, true, false);

                ShapeEngine actEngine = ActiveShapeEngine;

                //Set the DashPattern for the finalized shape to be the same as the unfinalized shape's.
                actEngine.DashPattern = dashPBox.comboBox.ComboBox.ActiveText;

                //Verify that the user clicked inside the image bounds or that the user is
                //holding the Ctrl key (to ignore the Image bounds and draw on the edge).
                if ((point.X == shapeOrigin.X && point.Y == shapeOrigin.Y) || ctrlKey)
                {
                    //Create a new ShapeHistoryItem so that the creation of a new shape can be undone.
                    doc.History.PushNewItem(
                        new ShapeHistoryItem(this, owner.Icon, Catalog.GetString("Shape Added"),
                            doc.CurrentUserLayer.Surface.Clone(), doc.CurrentUserLayer, SelectedPointIndex, SelectedShapeIndex));

                    isDrawing = true;


                    CreateShape(ctrlKey, clickedOnControlPoint, actEngine, prevSelPoint);
                }
            }

            //If the user right clicks outside of any shapes.
            if (closestDistance >= ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor && ChangingTension)
            {
                ClickedWithoutModifying = true;
            }

            surfaceModified = true;

            DrawShapes(false, false, shiftKey);
        }

        public virtual void HandleMouseUp(Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
        {
            isDrawing = false;

            ChangingTension = false;

            DrawShapes(true, false, args.Event.IsShiftPressed());
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
                DrawShapes(false, false, shiftKey);
            }
            else
            {
                //Make sure a control point is selected.
                if (SelectedPointIndex > -1)
                {
                    if (ClickedWithoutModifying)
                    {
                        //Create a new ShapeModifyHistoryItem so that the modification of the shape can be undone.
                        doc.History.PushNewItem(
                            new ShapeModifyHistoryItem(this, owner.Icon, Catalog.GetString("Shape Modified")));

                        ClickedWithoutModifying = false;
                    }

                    List<ControlPoint> controlPoints = SelectedShapeEngine.ControlPoints;

                    if (!ChangingTension)
                    {
                        //Moving a control point.

                        //Make sure the control point was moved.
                        if (currentPoint.X != controlPoints[SelectedPointIndex].Position.X
                            || currentPoint.Y != controlPoints[SelectedPointIndex].Position.Y)
                        {
                            MovePoint(controlPoints);
                        }
                    }
                    else
                    {
                        //Changing a control point's tension.

                        //Unclamp the mouse position when changing tension.
                        currentPoint = new PointD(point.X, point.Y);

                        //Calculate the new tension based off of the movement of the mouse that's
                        //perpendicular to the previous and following control points.

                        PointD curPoint = controlPoints[SelectedPointIndex].Position;
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
                            Utility.Clamp(controlPoints[SelectedPointIndex].Tension, 0d, 1d);
                    }

                    surfaceModified = true;

                    DrawShapes(false, false, shiftKey);
                }
            }

            LastMousePosition = currentPoint;
        }

        public virtual void DrawControlPoints(Context g, Rectangle? dirty)
        {
            //Draw the control points for all of the shapes.

            int controlPointSize = Math.Min(BrushWidth + 1, 5);
            double controlPointOffset = (double)controlPointSize / 2d;

            if (SelectedPointIndex > -1)
            {
                //Draw a ring around the selected point.
                g.FillStrokedEllipse(
                    new Rectangle(
                        SelectedPoint.Position.X - controlPointOffset * 4d,
                        SelectedPoint.Position.Y - controlPointOffset * 4d,
                        controlPointOffset * 8d, controlPointOffset * 8d),
                    ToolControl.FillColor, ToolControl.StrokeColor, 1);
            }

            //For each shape currently being drawn/edited by the user.
            for (int n = 0; n < SEngines.Count; ++n)
            {
                List<ControlPoint> controlPoints = SEngines[n].ControlPoints;

                //If the shape has one or more points.
                if (controlPoints.Count > 0)
                {
                    //Draw the control points for the shape.
                    for (int i = 0; i < controlPoints.Count; ++i)
                    {
                        //Skip drawing the hovered control point.
                        if (HoveredPointAsControlPoint > -1 && HoverPoint.Distance(controlPoints[i].Position) < 1d)
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
            if (!ChangingTension && HoverPoint.X > -1d)
            {
                g.FillStrokedEllipse(new Rectangle(
                    HoverPoint.X - controlPointOffset * 4d, HoverPoint.Y - controlPointOffset * 4d,
                    controlPointOffset * 8d, controlPointOffset * 8d), HoverColor, HoverColor, 1);
                g.FillStrokedEllipse(new Rectangle(
                    HoverPoint.X - controlPointOffset, HoverPoint.Y - controlPointOffset,
                    controlPointSize, controlPointSize), HoverColor, HoverColor, controlPointSize);
            }

            if (dirty != null)
            {
                //Inflate to accomodate for control points.
                dirty = dirty.Value.Inflate(controlPointSize * 8, controlPointSize * 8);
            }
        }

        /// <summary>
        /// Draw all of the shapes that are currently being drawn/edited by the user.
        /// </summary>
        /// <param name="calculateOrganizedPoints">Whether or not to calculate the spatially organized
        /// points for mouse detection after drawing the shape.</param>
        /// <param name="finalize">Whether or not to finalize the drawing.</param>
        /// <param name="shiftKey">Whether or not the shift key is being pressed.</param>
        public void DrawShapes(bool calculateOrganizedPoints, bool finalize, bool shiftKey)
        {
            if (!surfaceModified)
            {
                return;
            }

            Document doc = PintaCore.Workspace.ActiveDocument;

            Rectangle dirty;

            if (finalize)
            {
                doc.ToolLayer.Clear();

                ImageSurface undoSurface = null;

                // We only need to create a history item if there was a previous shape.
                if (ActiveShapeEngine.ControlPoints.Count > 0)
                {
                    undoSurface = doc.CurrentUserLayer.Surface.Clone();
                }

                isDrawing = false;
                surfaceModified = false;

                int previousSelectedPointIndex = SelectedPointIndex;
                int previousSelectedShapeIndex = SelectedShapeIndex;

                SelectedPointIndex = -1;

                dirty = DrawShape(
                    Utility.PointsToRectangle(shapeOrigin, new PointD(currentPoint.X, currentPoint.Y), shiftKey),
                    doc.CurrentUserLayer, false);

                //Make sure that the undo surface isn't null and that there are actually points.
                if (undoSurface != null)
                {
                    //Create a new ShapesHistoryItem so that the finalization of the shapes can be undone.
                    doc.History.PushNewItem(new ShapeHistoryItem(this, owner.Icon, Catalog.GetString("Shape Finalized"),
                            undoSurface, doc.CurrentUserLayer, previousSelectedPointIndex, previousSelectedShapeIndex));
                }

                //Clear out all of the old data.
                SEngines = new ShapeEngineCollection(true);
            }
            else
            {
                //Only calculate the hover point when there isn't a request to organize the generated points by spatial hashing.
                if (!calculateOrganizedPoints)
                {
                    //Calculate the hover point, if any.

                    int closestShapeIndex, closestPointIndex;
                    PointD closestPoint;
                    double closestDistance;

                    OrganizedPointCollection.FindClosestPoint(SEngines, currentPoint,
                        out closestShapeIndex, out closestPointIndex, out closestPoint, out closestDistance);

                    List<ControlPoint> controlPoints = SEngines[closestShapeIndex].ControlPoints;

                    //Determine if the user is hovering the mouse close enough to a line,
                    //shape, or point that's currently being drawn/edited by the user.
                    if (closestDistance < ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor)
                    {
                        //User is hovering over a generated point on a shape.

                        if (controlPoints.Count > closestPointIndex)
                        {
                            //Note: compare the current_point's distance here because it's the actual mouse position.
                            if (currentPoint.Distance(controlPoints[closestPointIndex].Position)
                                < ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor)
                            {
                                //Mouse hovering over a control point (on the "previous order" side of the point).

                                HoverPoint.X = controlPoints[closestPointIndex].Position.X;
                                HoverPoint.Y = controlPoints[closestPointIndex].Position.Y;
                                HoveredPointAsControlPoint = closestPointIndex;
                            }
                            else if (currentPoint.Distance(controlPoints[closestPointIndex - 1].Position)
                                < ShapeClickStartingRange + BrushWidth * ShapeClickThicknessFactor)
                            {
                                //Mouse hovering over a control point (on the "following order" side of the point).

                                HoverPoint.X = controlPoints[closestPointIndex - 1].Position.X;
                                HoverPoint.Y = controlPoints[closestPointIndex - 1].Position.Y;
                                HoveredPointAsControlPoint = closestPointIndex - 1;
                            }
                        }

                        if (HoverPoint.X < 0d)
                        {
                            HoverPoint.X = closestPoint.X;
                            HoverPoint.Y = closestPoint.Y;
                        }
                    }
                }



                doc.ToolLayer.Clear();

                dirty = DrawShape(
                    Utility.PointsToRectangle(shapeOrigin, new PointD(currentPoint.X, currentPoint.Y), shiftKey),
                    doc.ToolLayer, true);



                //Reset the hover point after each drawing.
                HoverPoint = new PointD(-1d, -1d);
                HoveredPointAsControlPoint = -1;
            }

            if (calculateOrganizedPoints)
            {
                //Organize the generated points for quick mouse interaction detection.

                //First, clear the previously organized points, if any.
                for (int n = 0; n < SEngines.Count; ++n)
                {
                    SEngines[n].OrganizedPoints.ClearCollection();

                    int pointIndex = 0;

                    foreach (PointD p in SEngines[n].GeneratedPoints)
                    {
                        SEngines[n].OrganizedPoints.StoreAndOrganizePoint(new OrganizedPoint(new PointD(p.X, p.Y), pointIndex));

                        //Keep track of the point's order in relation to the control points.
                        if (SEngines[n].ControlPoints.Count > pointIndex
                            && p.X == SEngines[n].ControlPoints[pointIndex].Position.X
                            && p.Y == SEngines[n].ControlPoints[pointIndex].Position.Y)
                        {
                            ++pointIndex;
                        }
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


        /*protected virtual Rectangle DrawShape(Rectangle r, Layer l)
        {
            return r;
        }*/

        protected Rectangle DrawShape(Rectangle rect, Layer l, bool drawControlPoints)
        {
            Document doc = PintaCore.Workspace.ActiveDocument;

            Rectangle? dirty = null;

            using (Context g = new Context(l.Surface))
            {
                g.AppendPath(doc.Selection.SelectionPath);
                g.FillRule = FillRule.EvenOdd;
                g.Clip();

                ActiveShapeEngine.AntiAliasing = owner.UseAntialiasing;

                g.Antialias = owner.UseAntialiasing ? Antialias.Subpixel : Antialias.None;

                g.SetDash(DashPatternBox.GenerateDashArray(ActiveShapeEngine.DashPattern, BrushWidth), 0.0);

                g.LineWidth = BrushWidth;

                //Draw the shapes.
                for (int n = 0; n < SEngines.Count; ++n)
                {
                    List<ControlPoint> controlPoints = SEngines[n].ControlPoints;

                    if (controlPoints.Count > 0)
                    {
                        //Generate the points that make up the shape.
                        SEngines[n].GenerateCardinalSplinePolynomialCurvePoints();

                        //Expand the invalidation rectangle as necessary.
                        dirty = dirty.UnionRectangles(g.DrawPolygonal(SEngines[n].GeneratedPoints, outlineColor));
                    }
                }

                g.SetDash(new double[] { }, 0.0);


                DrawExtras(dirty, g);


                if (drawControlPoints)
                {
                    DrawControlPoints(g, dirty);
                }
            }


            return dirty ?? new Rectangle(0d, 0d, 0d, 0d);
        }

        protected virtual BaseHistoryItem CreateHistoryItem()
        {
            return new SimpleHistoryItem(owner.Icon, owner.Name, undoSurface, PintaCore.Workspace.ActiveDocument.CurrentUserLayerIndex);
        }

        protected Gdk.Rectangle GetRectangleFromPoints(PointD a, PointD b)
        {
            int x = (int)Math.Min(a.X, b.X) - BrushWidth - 2;
            int y = (int)Math.Min(a.Y, b.Y) - BrushWidth - 2;
            int w = (int)Math.Max(a.X, b.X) - x + (BrushWidth * 2) + 4;
            int h = (int)Math.Max(a.Y, b.Y) - y + (BrushWidth * 2) + 4;

            return new Gdk.Rectangle(x, y, w, h);
        }


        protected void Palette_PrimaryColorChanged(object sender, EventArgs e)
        {
            outlineColor = PintaCore.Palette.PrimaryColor;

            DrawShapes(false, false, false);
        }

        protected void Palette_SecondaryColorChanged(object sender, EventArgs e)
        {
            outlineColor = PintaCore.Palette.SecondaryColor;

            DrawShapes(false, false, false);
        }


        protected virtual void CreateShape(bool ctrlKey, bool clickedOnControlPoint, ShapeEngine actEngine, PointD prevSelPoint)
        {

        }

        protected virtual void MovePoint(List<ControlPoint> controlPoints)
        {
            
        }

        protected virtual void DrawExtras(Rectangle? dirty, Context g)
        {
            
        }
    }
}
