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
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

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
		RoundedLineSeries,
	}

	public static Dictionary<ShapeTypes, ShapeTool> CorrespondingTools { get; } = new ();

	protected abstract string ShapeName { get; }

	protected readonly ShapeTool owner;

	protected bool is_drawing = false;

	protected RectangleD? last_dirty = null;

	protected PointD shape_origin;
	protected PointD current_point;

	public static Color OutlineColor {
		get => PintaCore.Palette.PrimaryColor;
		set => PintaCore.Palette.PrimaryColor = value;
	}

	public static Color FillColor {
		get => PintaCore.Palette.SecondaryColor;
		set => PintaCore.Palette.SecondaryColor = value;
	}

	// NRT - Created by HandleBuildToolBar
	protected Gtk.SpinButton brush_width = null!;
	protected Gtk.Label brush_width_label = null!;
	protected Gtk.Label fill_label = null!;
	protected ToolBarDropDownButton fill_button = null!;
	protected Gtk.Separator fill_sep = null!;

	protected Gtk.Label shape_type_label = null!;
	protected ToolBarDropDownButton shape_type_button = null!;
	protected Gtk.Separator shape_type_sep = null!;

	protected DashPatternBox dash_pattern_box = new ();
	private string prev_dash_pattern = "-";

	private bool prev_antialiasing = true;

	public int BrushWidth {
		get => brush_width?.GetValueAsInt () ?? BaseTool.DEFAULT_BRUSH_WIDTH;
		set {
			if (brush_width is not null)
				brush_width.Value = value;
		}
	}

	private int prev_brush_width = BaseTool.DEFAULT_BRUSH_WIDTH;

	private bool StrokeShape {
		get {
			if (fill_button.SelectedItem?.Tag is int value)
				return value % 2 == 0;

			return true;
		}
	}

	private bool FillShape {
		get {
			if (fill_button.SelectedItem?.Tag is int value)
				return value >= 1;

			return false;
		}
	}

	private ShapeTypes ShapeType {
		get {
			if (shape_type_button.SelectedItem?.Tag is int value)
				return (ShapeTypes) value;

			return 0;
		}
	}

	public const double ShapeClickStartingRange = 10d;
	public const double DefaultEndPointTension = 0d;
	public const double DefaultMidPointTension = 1d / 3d;

	public int SelectedPointIndex;
	public int SelectedShapeIndex;

	protected int prev_selected_shape_index;

	/// <summary>
	/// The selected ControlPoint.
	/// </summary>
	public ControlPoint? SelectedPoint {
		get {
			ShapeEngine? selEngine = SelectedShapeEngine;

			if (selEngine != null && selEngine.ControlPoints.Count > SelectedPointIndex)
				return selEngine.ControlPoints[SelectedPointIndex];
			else
				return null;
		}
	}

	/// <summary>
	/// The active shape's ShapeEngine. A point does not have to be selected here, only a shape. This can be null.
	/// </summary>
	public ShapeEngine? ActiveShapeEngine {
		get {
			if (SelectedShapeIndex > -1 && SEngines.Count > SelectedShapeIndex)
				return SEngines[SelectedShapeIndex];
			else
				return null;
		}
	}

	/// <summary>
	/// The selected shape's ShapeEngine. This requires that a point in the shape be selected and should be used in most cases. This can be null.
	/// </summary>
	public ShapeEngine? SelectedShapeEngine => (SelectedPointIndex > -1) ? ActiveShapeEngine : null;

	/// <summary>
	/// Display the handles for all active shape engines' control points, along with the hover position
	/// </summary>
	public IEnumerable<IToolHandle> Handles =>
		SEngines.SelectMany (engine => engine.ControlPointHandles).Append (hover_handle);
	private readonly MoveHandle hover_handle = new ();

	private readonly Gdk.Cursor grab_cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.Grab);

	protected bool changing_tension = false;
	protected PointD last_mouse_pos = new (0d, 0d);

	//Helps to keep track of the first modification on a shape after the mouse is clicked, to prevent unnecessary history items.
	protected bool clicked_without_modifying = false;

	//Stores the editable shape data.
	public static ShapeEngineCollection SEngines = new ();

	#region ToolbarEventHandlers

	protected virtual void BrushMinusButtonClickedEvent (object? o, EventArgs args)
	{
		BrushWidth--;

		//No need to store previous settings or redraw, as this is done in the Changed event handler.
	}

	protected virtual void BrushPlusButtonClickedEvent (object? o, EventArgs args)
	{
		BrushWidth++;

		//No need to store previous settings or redraw, as this is done in the Changed event handler.
	}

	protected void Palette_PrimaryColorChanged (object? sender, EventArgs e)
	{
		ShapeEngine? activeEngine = ActiveShapeEngine;
		if (activeEngine == null) return;
		activeEngine.OutlineColor = OutlineColor;
		DrawActiveShape (false, false, true, false, false);
	}

	protected void Palette_SecondaryColorChanged (object? sender, EventArgs e)
	{
		ShapeEngine? activeEngine = ActiveShapeEngine;
		if (activeEngine == null) return;
		activeEngine.FillColor = FillColor;
		DrawActiveShape (false, false, true, false, false);
	}

	private void OnFillStyleChanged (object? sender, EventArgs e)
	{
		DrawActiveShape (false, false, true, false, false);
	}

	#endregion ToolbarEventHandlers

	private readonly IToolService tools;
	private readonly ActionManager actions;
	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;

	public BaseEditEngine (
		IServiceProvider services,
		ShapeTool passedOwner)
	{
		tools = services.GetService<IToolService> ();
		actions = services.GetService<ActionManager> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();

		owner = passedOwner;

		ResetShapes ();
	}

	private static string BRUSH_WIDTH_SETTING (string prefix)
		=> $"{prefix}-brush-width";

	private static string FILL_TYPE_SETTING (string prefix)
		=> $"{prefix}-fill-style";

	private static string SHAPE_TYPE_SETTING (string prefix)
		=> $"{prefix}-shape-type";

	private static string DASH_PATTERN_SETTING (string prefix)
		=> $"{prefix}-dash-pattern";

	public virtual void OnSaveSettings (ISettingsService settings, string toolPrefix)
	{
		if (brush_width is not null)
			settings.PutSetting (BRUSH_WIDTH_SETTING (toolPrefix), (int) brush_width.Value);

		if (fill_button is not null)
			settings.PutSetting (FILL_TYPE_SETTING (toolPrefix), fill_button.SelectedIndex);

		if (shape_type_button is not null)
			settings.PutSetting (SHAPE_TYPE_SETTING (toolPrefix), shape_type_button.SelectedIndex);

		if (dash_pattern_box?.ComboBox is not null)
			settings.PutSetting (DASH_PATTERN_SETTING (toolPrefix), dash_pattern_box.ComboBox.ComboBox.GetActiveText ()!);
	}

	public virtual void HandleBuildToolBar (Gtk.Box tb, ISettingsService settings, string toolPrefix)
	{
		if (brush_width_label == null) {
			string brushWidthText = Translations.GetString ("Brush width");
			brush_width_label = Gtk.Label.New ($" {brushWidthText}: ");
		}

		tb.Append (brush_width_label);

		if (brush_width == null) {

			brush_width = GtkExtensions.CreateToolBarSpinButton (1, 1e5, 1, settings.GetSetting (BRUSH_WIDTH_SETTING (toolPrefix), BaseTool.DEFAULT_BRUSH_WIDTH));
			brush_width.TooltipText = Translations.GetString ("Change brush width. Shortcut keys: [ ]");

			brush_width.OnValueChanged += (o, e) => {

				ShapeEngine? selEngine = SelectedShapeEngine;
				if (selEngine == null) return;
				selEngine.BrushWidth = BrushWidth;
				StorePreviousSettings ();
				DrawActiveShape (false, false, true, false, false);
			};
		}

		tb.Append (brush_width);

		fill_sep ??= GtkExtensions.CreateToolBarSeparator ();

		tb.Append (fill_sep);

		if (fill_label == null) {
			string fillStyleText = Translations.GetString ("Fill Style");
			fill_label = Gtk.Label.New ($" {fillStyleText}: ");
		}

		tb.Append (fill_label);

		if (fill_button == null) {
			fill_button = new ToolBarDropDownButton ();

			fill_button.AddItem (Translations.GetString ("Outline Shape"), Resources.Icons.FillStyleOutline, 0);
			fill_button.AddItem (Translations.GetString ("Fill Shape"), Resources.Icons.FillStyleFill, 1);
			fill_button.AddItem (Translations.GetString ("Fill and Outline Shape"), Resources.Icons.FillStyleOutlineFill, 2);

			fill_button.SelectedIndex = settings.GetSetting (FILL_TYPE_SETTING (toolPrefix), 0);
			fill_button.SelectedItemChanged += OnFillStyleChanged;
		}

		tb.Append (fill_button);

		shape_type_sep ??= GtkExtensions.CreateToolBarSeparator ();

		tb.Append (shape_type_sep);

		if (shape_type_label == null) {
			string shapeTypeText = Translations.GetString ("Shape Type");
			shape_type_label = Gtk.Label.New ($" {shapeTypeText}: ");
		}

		tb.Append (shape_type_label);

		if (shape_type_button == null) {
			shape_type_button = new ToolBarDropDownButton ();

			shape_type_button.AddItem (Translations.GetString ("Open Line/Curve Series"), Resources.Icons.ToolLine, 0);
			shape_type_button.AddItem (Translations.GetString ("Closed Line/Curve Series"), Resources.Icons.ToolRectangle, 1);
			shape_type_button.AddItem (Translations.GetString ("Ellipse"), Resources.Icons.ToolEllipse, 2);
			shape_type_button.AddItem (Translations.GetString ("Rounded Line Series"), Resources.Icons.ToolRectangleRounded, 3);

			shape_type_button.SelectedIndex = settings.GetSetting (SHAPE_TYPE_SETTING (toolPrefix), 0);

			shape_type_button.SelectedItemChanged += (o, e) => {
				ShapeTypes newShapeType = ShapeType;
				ShapeEngine? selEngine = SelectedShapeEngine;

				//Verify that the tool needs to be switched.
				if (GetCorrespondingTool (newShapeType) == owner)
					return;

				if (selEngine == null) {
					ActivateCorrespondingTool (newShapeType, true);
					return;
				}

				//if shape is selected it will be converted to new shape and shape type will be changed, otherwise only shape type will be changed.

				//Create a new ShapesModifyHistoryItem so that the changing of the shape type can be undone.
				workspace.ActiveDocument.History.PushNewItem (new ShapesModifyHistoryItem (
					this, owner.Icon, Translations.GetString ("Changed Shape Type")));

				//Clone the old shape; it should be automatically garbage-collected. newShapeType already has the updated value.
				selEngine = selEngine.Convert (newShapeType, SelectedShapeIndex);

				int previousSSI = SelectedShapeIndex;
				ActivateCorrespondingTool (selEngine.ShapeType, true);
				SelectedShapeIndex = previousSSI;
				//Draw the updated shape with organized points generation (for mouse detection).
				DrawActiveShape (true, false, true, false, true);
			};
		}

		shape_type_button.SelectedItem = shape_type_button.Items[(int) owner.ShapeType];

		tb.Append (shape_type_button);


		Gtk.ComboBoxText? dpbBox = dash_pattern_box.SetupToolbar (tb);

		if (dpbBox == null)
			return;

		dpbBox.GetEntry ().SetText (settings.GetSetting (DASH_PATTERN_SETTING (toolPrefix), "-"));

		dpbBox.OnChanged += (o, e) => {

			ShapeEngine? selEngine = SelectedShapeEngine;
			if (selEngine == null) return;
			selEngine.DashPattern = dpbBox.GetActiveText ()!;
			StorePreviousSettings ();
			DrawActiveShape (false, false, true, false, false);
		};
	}

	public virtual void HandleActivated ()
	{
		RecallPreviousSettings ();

		palette.PrimaryColorChanged += Palette_PrimaryColorChanged;
		palette.SecondaryColorChanged += Palette_SecondaryColorChanged;
	}

	public virtual void HandleDeactivated (BaseTool? newTool)
	{
		SelectedPointIndex = -1;
		SelectedShapeIndex = -1;

		StorePreviousSettings ();

		//Determine if the tool being switched to will be another editable tool.
		if (workspace.HasOpenDocuments && !(newTool?.IsEditableShapeTool == true)) {
			//The tool being switched to is not editable. Finalize every editable shape not yet finalized.
			FinalizeAllShapes ();
		}

		palette.PrimaryColorChanged -= Palette_PrimaryColorChanged;
		palette.SecondaryColorChanged -= Palette_SecondaryColorChanged;
	}

	public virtual void HandleAfterSave ()
	{
		//When saving, everything will be finalized, which is good; however, afterwards, the user will expect
		//everything to remain editable. Currently, a finalization history item will always be added.
		actions.Edit.Undo.Activate ();

		//Redraw all of the editable shapes in case saving caused some extra/unexpected behavior.
		DrawAllShapes ();
	}

	public virtual void HandleCommit ()
	{
		//Finalize every editable shape not yet finalized.
		FinalizeAllShapes ();
	}

	public virtual bool HandleBeforeUndo ()
		=> false;

	public virtual bool HandleBeforeRedo ()
		=> false;

	public virtual void HandleAfterUndo ()
	{
		ShapeEngine? activeEngine = ActiveShapeEngine;

		if (activeEngine != null)
			UpdateToolbarSettings (activeEngine);

		DrawActiveShape (true, false, true, false, false); // Draw the current state.
	}

	public virtual void HandleAfterRedo ()
	{
		ShapeEngine? activeEngine = ActiveShapeEngine;

		if (activeEngine != null)
			UpdateToolbarSettings (activeEngine);

		DrawActiveShape (true, false, true, false, false); // Draw the current state.
	}

	public virtual bool HandleKeyDown (Document document, ToolKeyEventArgs e)
	{
		Gdk.Key keyPressed = e.Key;
		switch (keyPressed.Value) {
			case Gdk.Constants.KEY_Delete:
				HandleDelete ();
				return true;
			case Gdk.Constants.KEY_Return:
			case Gdk.Constants.KEY_KP_Enter:
				FinalizeAllShapes ();
				return true;
			case Gdk.Constants.KEY_space:
				HandleSpace (e);
				return true;
			case Gdk.Constants.KEY_Up:
				HandleUp ();
				return true;
			case Gdk.Constants.KEY_Down:
				HandleDown ();
				return true;
			case Gdk.Constants.KEY_Left:
				HandleLeft (e);
				return true;
			case Gdk.Constants.KEY_Right:
				HandleRight (e);
				return true;
			case Gdk.Constants.KEY_bracketleft:
				BrushWidth--;
				return true;
			case Gdk.Constants.KEY_bracketright:
				BrushWidth++;
				return true;
			default:
				if (keyPressed.IsControlKey ()) {
					// Redraw since the Ctrl key affects the hover cursor, etc
					DrawActiveShape (false, false, true, e.IsShiftPressed, false, true);
					return true;
				} else {
					return false;
				}
		}
	}

	private void HandleRight (ToolKeyEventArgs e)
	{
		//Make sure a control point is selected.

		if (SelectedPointIndex < 0)
			return;

		if (e.IsControlPressed) {
			//Change the selected control point to be the following one.

			ShapeEngine? activeEngine = ActiveShapeEngine;

			if (activeEngine != null) {
				++SelectedPointIndex;

				if (SelectedPointIndex > activeEngine.ControlPoints.Count - 1)
					SelectedPointIndex = 0;

			}
		} else {
			//Move the selected control point.
			PointD originalPosition = SelectedPoint!.Position; // NRT - Checked by SelectedPointIndex
			SelectedPoint.Position = originalPosition with { X = originalPosition.X + 1d };
		}

		DrawActiveShape (true, false, true, false, false);
	}

	private void HandleLeft (ToolKeyEventArgs e)
	{
		//Make sure a control point is selected.

		if (SelectedPointIndex < 0)
			return;

		if (e.IsControlPressed) {
			//Change the selected control point to be the previous one.

			--SelectedPointIndex;

			if (SelectedPointIndex < 0) {
				ShapeEngine? activeEngine = ActiveShapeEngine;

				if (activeEngine != null)
					SelectedPointIndex = activeEngine.ControlPoints.Count - 1;

			}
		} else {
			//Move the selected control point.
			PointD originalPosition = SelectedPoint!.Position; // NRT - Checked by SelectedPointIndex
			SelectedPoint.Position = originalPosition with { X = originalPosition.X - 1d };
		}

		DrawActiveShape (true, false, true, false, false);
	}

	private void HandleDown ()
	{
		//Make sure a control point is selected.

		if (SelectedPointIndex < 0)
			return;

		//Move the selected control point.
		PointD originalPosition = SelectedPoint!.Position; // NRT - Checked by SelectedPointIndex
		SelectedPoint.Position = originalPosition with { Y = originalPosition.Y + 1d };

		DrawActiveShape (true, false, true, false, false);
	}

	private void HandleUp ()
	{
		//Make sure a control point is selected.

		if (SelectedPointIndex < 0)
			return;

		//Move the selected control point.
		var originalPosition = SelectedPoint!.Position; // NRT - Checked by SelectedPointIndex
		SelectedPoint.Position = originalPosition with { Y = originalPosition.Y - 1d };

		DrawActiveShape (true, false, true, false, false);
	}

	private void HandleSpace (ToolKeyEventArgs e)
	{
		ControlPoint? selPoint = SelectedPoint;

		if (selPoint == null)
			return;

		//This can be assumed not to be null since selPoint was not null.
		ShapeEngine selEngine = SelectedShapeEngine!; // NRT - ^^

		//Create a new ShapesModifyHistoryItem so that the adding of a control point can be undone.
		workspace.ActiveDocument.History.PushNewItem (
			new ShapesModifyHistoryItem (
				this,
				owner.Icon,
				ShapeName + " " + Translations.GetString ("Point Added")
			)
		);

		bool shiftKey = e.IsShiftPressed;
		bool ctrlKey = e.IsControlPressed;

		PointD newPointPos;

		if (ctrlKey) {
			//Ctrl + space combo: same position as currently selected point.
			newPointPos = new PointD (selPoint.Position.X, selPoint.Position.Y);
		} else {
			shape_origin = new PointD (selPoint.Position.X, selPoint.Position.Y);

			if (shiftKey) {
				CalculateModifiedCurrentPoint ();
			}

			//Space only: position of mouse (after any potential shift alignment).
			newPointPos = new PointD (current_point.X, current_point.Y);
		}

		//Place the new point on the outside-most end, order-wise.
		if (SelectedPointIndex < selEngine.ControlPoints.Count / 2d) {
			selEngine.ControlPoints.Insert (SelectedPointIndex,
			    new ControlPoint (new PointD (newPointPos.X, newPointPos.Y), DefaultMidPointTension));
		} else {
			selEngine.ControlPoints.Insert (SelectedPointIndex + 1,
			    new ControlPoint (new PointD (newPointPos.X, newPointPos.Y), DefaultMidPointTension));

			++SelectedPointIndex;
		}

		DrawActiveShape (true, false, true, shiftKey, false, e.IsControlPressed);
	}

	private void HandleDelete ()
	{
		if (SelectedPointIndex < 0)
			return;

		List<ControlPoint> controlPoints = SelectedShapeEngine!.ControlPoints; // NRT - Code assumes this is not-null

		//Either delete a ControlPoint or an entire shape (if there's only 1 ControlPoint left).
		if (controlPoints.Count > 1) {
			//Create a new ShapesModifyHistoryItem so that the deletion of a control point can be undone.
			workspace.ActiveDocument.History.PushNewItem (
				new ShapesModifyHistoryItem (
					this,
					owner.Icon,
					ShapeName + " " + Translations.GetString ("Point Deleted")
				)
			);

			//Delete the selected point from the shape.
			controlPoints.RemoveAt (SelectedPointIndex);

			//Set the newly selected point to be the median-most point on the shape, order-wise.
			if (SelectedPointIndex > controlPoints.Count / 2)
				--SelectedPointIndex;

		} else {
			Document doc = workspace.ActiveDocument;

			//Create a new ShapesHistoryItem so that the deletion of a shape can be undone.
			doc.History.PushNewItem (
				new ShapesHistoryItem (
					this,
					owner.Icon,
					ShapeName + " " + Translations.GetString ("Deleted"),
					doc.Layers.CurrentUserLayer.Surface.Clone (),
					doc.Layers.CurrentUserLayer,
					SelectedPointIndex,
					SelectedShapeIndex,
					false
				)
			);


			//Since the shape itself will be deleted, remove its ReEditableLayer from the drawing loop.

			ReEditableLayer removeMe = SEngines.ElementAt (SelectedShapeIndex).DrawingLayer;

			if (removeMe.InTheLoop)
				SEngines.ElementAt (SelectedShapeIndex).DrawingLayer.TryRemoveLayer ();

			//Delete the selected shape.
			SEngines.RemoveAt (SelectedShapeIndex);

			//Redraw the workspace.
			doc.Workspace.Invalidate ();

			SelectedPointIndex = -1;
			SelectedShapeIndex = -1;
		}

		DrawActiveShape (true, false, true, false, false);
	}

	public virtual bool HandleKeyUp (Document document, ToolKeyEventArgs e)
	{
		Gdk.Key keyReleased = e.Key;

		if (keyReleased.IsControlKey ())
			DrawActiveShape (false, false, true, e.IsShiftPressed, false, false);

		switch (keyReleased.Value) {
			case Gdk.Constants.KEY_Delete:
			case Gdk.Constants.KEY_Return:
			case Gdk.Constants.KEY_KP_Enter:
			case Gdk.Constants.KEY_space:
			case Gdk.Constants.KEY_Up:
			case Gdk.Constants.KEY_Down:
			case Gdk.Constants.KEY_Left:
			case Gdk.Constants.KEY_Right:
				return true;
			default:
				return false;
		}
	}

	public virtual void HandleMouseDown (Document document, ToolMouseEventArgs e)
	{
		PointD unclamped_point = e.PointDouble;

		//If we are already drawing, ignore any additional mouse down events.
		if (is_drawing) return;

		//Redraw the previously (and possibly currently) active shape without any control points in case another shape is made active.
		DrawActiveShape (false, false, false, false, false);

		Document doc = workspace.ActiveDocument;

		shape_origin = doc.ClampToImageSize (unclamped_point);
		current_point = shape_origin;

		bool shiftKey = e.IsShiftPressed;

		if (shiftKey)
			CalculateModifiedCurrentPoint ();

		is_drawing = true;

		//Right clicking changes tension.
		changing_tension = e.MouseButton != MouseButton.Left;

		bool ctrlKey = e.IsControlPressed;

		SEngines.FindClosestControlPoint (
			unclamped_point,
			out int closestCPShapeIndex,
			out int closestCPIndex,
			out var closestControlPoint,
			out _);

		OrganizedPointCollection.FindClosestPoint (
			SEngines,
			unclamped_point,
			out int closestShapeIndex,
			out int closestPointIndex,
			out var closestPoint,
			out _);

		bool clicked_control_point = false;
		bool clicked_generated_point = false;

		PointD current_window_point = workspace.CanvasPointToView (unclamped_point);
		MoveHandle test_handle = new ();

		// Check if the user is directly clicking on a control point.
		if (closestControlPoint != null) {
			test_handle.CanvasPosition = closestControlPoint.Position;
			clicked_control_point = test_handle.ContainsPoint (current_window_point);
			if (clicked_control_point) {
				SelectedPointIndex = closestCPIndex;
				SelectedShapeIndex = closestCPShapeIndex;
			}
		}

		// Otherwise, the user might have clicked on a generated point.
		if (!clicked_control_point && closestPoint.HasValue) {
			test_handle.CanvasPosition = closestPoint.Value;
			clicked_generated_point = test_handle.ContainsPoint (current_window_point);
		}

		clicked_without_modifying = clicked_control_point;

		if (!changing_tension && clicked_generated_point) {
			//Determine if the currently active tool matches the clicked on shape's corresponding tool, and if not, switch to it.
			if (ActivateCorrespondingTool (closestShapeIndex, true) != null) {
				//Pass on the event and its data to the newly activated tool.
				tools.DoMouseDown (document, e);

				//Don't do anything else here once the tool is switched and the event is passed on.
				return;
			}

			//The currently active tool matches the clicked on shape's corresponding tool.

			//Only create a new shape if the user isn't holding the control key down.
			if (!ctrlKey) {
				//Create a new ShapesModifyHistoryItem so that the adding of a control point can be undone.
				doc.History.PushNewItem (new ShapesModifyHistoryItem (this, owner.Icon, ShapeName + " " + Translations.GetString ("Point Added")));

				SEngines[closestShapeIndex].ControlPoints.Insert (closestPointIndex,
					new ControlPoint (new PointD (current_point.X, current_point.Y), DefaultMidPointTension));
			}

			//These should be set after creating the history item.
			SelectedPointIndex = closestPointIndex;
			SelectedShapeIndex = closestShapeIndex;

			ShapeEngine? activeEngine = ActiveShapeEngine;

			if (activeEngine != null)
				UpdateToolbarSettings (activeEngine);
		}

		//Create a new shape if the user control + clicks on a shape or if the user simply clicks outside of any shapes.
		if (!changing_tension && (ctrlKey || (!clicked_control_point && !clicked_generated_point))) {
			PointD prevSelPoint;

			//First, store the position of the currently selected point.
			if (SelectedPoint != null && ctrlKey) {
				prevSelPoint = new PointD (SelectedPoint.Position.X, SelectedPoint.Position.Y);
			} else {
				//This doesn't matter, other than the fact that it gets set to a value in order for the code to build.
				prevSelPoint = new PointD (0d, 0d);
			}

			//Create a new ShapesHistoryItem so that the creation of a new shape can be undone.
			doc.History.PushNewItem (new ShapesHistoryItem (this, owner.Icon, ShapeName + " " + Translations.GetString ("Added"),
				doc.Layers.CurrentUserLayer.Surface.Clone (), doc.Layers.CurrentUserLayer, SelectedPointIndex, SelectedShapeIndex, false));

			//Create the shape, add its starting points, and add it to SEngines.
			SEngines.Add (CreateShape (ctrlKey, clicked_control_point, prevSelPoint));

			//Select the new shape.
			SelectedShapeIndex = SEngines.Count - 1;

			ShapeEngine? activeEngine = ActiveShapeEngine;

			if (activeEngine != null) {
				//Set the AntiAliasing.
				activeEngine.AntiAliasing = owner.UseAntialiasing;
			}

			StorePreviousSettings ();
		} else if (clicked_control_point) {
			//Since the user is not creating a new shape or control point but rather modifying an existing control point, it should be determined
			//whether the currently active tool matches the clicked on shape's corresponding tool, and if not, switch to it.
			if (ActivateCorrespondingTool (SelectedShapeIndex, true) != null) {
				//Pass on the event and its data to the newly activated tool.
				tools.DoMouseDown (document, e);

				//Don't do anything else here once the tool is switched and the event is passed on.
				return;
			}

			//The currently active tool matches the clicked on shape's corresponding tool.

			ShapeEngine? activeEngine = ActiveShapeEngine;

			if (activeEngine != null)
				UpdateToolbarSettings (activeEngine);
		}

		//Determine if the user right clicks outside of any shapes (neither on their control points nor on their generated points).
		if ((!clicked_control_point && !clicked_generated_point) && changing_tension)
			clicked_without_modifying = true;

		DrawActiveShape (false, false, true, shiftKey, false, e.IsControlPressed);
	}

	public virtual void HandleMouseUp (Document document, ToolMouseEventArgs e)
	{
		is_drawing = false;

		changing_tension = false;

		DrawActiveShape (true, false, true, e.IsShiftPressed, false, e.IsControlPressed);
	}

	public virtual void HandleMouseMove (Document document, ToolMouseEventArgs e)
	{
		current_point = e.PointDouble;
		bool shiftKey = e.IsShiftPressed;

		if (!is_drawing) {
			//Redraw the active shape to show a (temporary) highlighted control point (over any shape) when applicable.
			DrawActiveShape (false, false, true, shiftKey, false, e.IsControlPressed);
			last_mouse_pos = current_point;
			return;
		}

		Document doc = document;

		current_point = document.ClampToImageSize (current_point);

		if (shiftKey)
			CalculateModifiedCurrentPoint ();

		ControlPoint? selPoint = SelectedPoint;

		//Make sure a control point is selected.
		if (selPoint == null) {
			last_mouse_pos = current_point;
			return;
		}

		if (clicked_without_modifying) {
			//Create a new ShapesModifyHistoryItem so that the modification of the shape can be undone.
			doc.History.PushNewItem (
							new ShapesModifyHistoryItem (this, owner.Icon, ShapeName + " " + Translations.GetString ("Modified")));

			clicked_without_modifying = false;
		}

		List<ControlPoint> controlPoints = SelectedShapeEngine!.ControlPoints; // NRT - Code assumes this is not-null

		if (!changing_tension) {
			//Moving a control point.

			//Make sure the control point was moved.
			if (current_point.X != selPoint.Position.X || current_point.Y != selPoint.Position.Y)
				MovePoint (controlPoints);

			DrawActiveShape (false, false, true, shiftKey, false, e.IsControlPressed);
			last_mouse_pos = current_point;
			return;
		}

		//Changing a control point's tension.

		//Unclamp the mouse position when changing tension.
		current_point = e.PointDouble;

		//Calculate the new tension based off of the movement of the mouse that's
		//perpendicular to the previous and following control points.

		PointD curPoint = selPoint.Position;
		PointD prevPoint, nextPoint;

		//Calculate the previous control point.
		if (SelectedPointIndex > 0) {
			prevPoint = controlPoints[SelectedPointIndex - 1].Position;
		} else {
			//There is none.
			prevPoint = curPoint;
		}

		//Calculate the following control point.
		if (SelectedPointIndex < controlPoints.Count - 1) {
			nextPoint = controlPoints[SelectedPointIndex + 1].Position;
		} else {
			//There is none.
			nextPoint = curPoint;
		}

		//The x and y differences are used as factors for the x and y change in the mouse position.
		double xDiff = prevPoint.X - nextPoint.X;
		double yDiff = prevPoint.Y - nextPoint.Y;
		double totalDiff = xDiff + yDiff;

		//Calculate the midpoint in between the previous and following points.
		PointD midPoint = new PointD ((prevPoint.X + nextPoint.X) / 2d, (prevPoint.Y + nextPoint.Y) / 2d);

		//Calculate the x change in the mouse position.
		double xChange =
			(curPoint.X <= midPoint.X)
			? current_point.X - last_mouse_pos.X
			: last_mouse_pos.X - current_point.X;

		//Calculate the y change in the mouse position.
		double yChange =
			(curPoint.Y <= midPoint.Y)
			? current_point.Y - last_mouse_pos.Y
			: last_mouse_pos.Y - current_point.Y;

		//Update the control point's tension.

		//Note: the difference factors are to be inverted for x and y change because this is perpendicular motion.
		controlPoints[SelectedPointIndex].Tension +=
			Math.Round (Math.Clamp ((xChange * yDiff + yChange * xDiff) / totalDiff, -1d, 1d)) / 50d;

		//Restrict the new tension to range from 0d to 1d.
		controlPoints[SelectedPointIndex].Tension = Math.Clamp (selPoint.Tension, 0d, 1d);

		DrawActiveShape (false, false, true, shiftKey, false, e.IsControlPressed);


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
	public void DrawActiveShape (bool calculateOrganizedPoints, bool finalize, bool drawHoverSelection, bool shiftKey, bool preventSwitchBack, bool ctrl_key = false)
	{
		ShapeTool? oldTool = BaseEditEngine.ActivateCorrespondingTool (SelectedShapeIndex, calculateOrganizedPoints);

		//First, determine if the currently active tool matches the shape's corresponding tool, and if not, switch to it.
		if (oldTool != null) {
			//The tool has switched, so call DrawActiveShape again but inside that tool.
			if (tools.CurrentTool is ShapeTool tool)
				tool.EditEngine.DrawActiveShape (
				calculateOrganizedPoints, finalize, drawHoverSelection, shiftKey, preventSwitchBack);

			//Afterwards, switch back to the old tool, unless specified otherwise.
			if (!preventSwitchBack) {
				ActivateCorrespondingTool (oldTool.ShapeType, true);
			}

			return;
		}

		//The currently active tool should now match the shape's corresponding tool.

		BeforeDraw ();

		ShapeEngine? activeEngine = ActiveShapeEngine;

		if (activeEngine == null) {
			//No shape will be drawn; however, the hover point still needs to be drawn if drawHoverSelection is true.
			UpdateHoverHandle (drawHoverSelection, ctrl_key);
			return;
		}

		//Clear any temporary drawing, because something new will be drawn.
		activeEngine.DrawingLayer.Layer.Clear ();

		RectangleD dirty;

		//Determine if the drawing should be for finalizing the shape onto the image or drawing it temporarily.
		if (finalize)
			dirty = DrawFinalized (activeEngine, true, shiftKey);
		else
			dirty = DrawUnfinalized (activeEngine, drawHoverSelection, shiftKey, ctrl_key);

		//Determine if the organized (spatially hashed) points should be generated. This is for mouse interaction detection after drawing.
		if (calculateOrganizedPoints)
			OrganizePoints (activeEngine);

		InvalidateAfterDraw (dirty);
	}

	/// <summary>
	/// Do not call. Use DrawActiveShape.
	/// </summary>
	private void BeforeDraw ()
	{
		//Check to see if a new shape is selected.
		if (prev_selected_shape_index == SelectedShapeIndex)
			return;

		//A new shape is selected, so clear the previous dirty Rectangle.
		last_dirty = null;

		prev_selected_shape_index = SelectedShapeIndex;
	}

	/// <summary>
	/// Do not call. Use DrawActiveShape.
	/// </summary>
	/// <param name="engine"></param>
	/// <param name="dirty"></param>
	/// <param name="shiftKey"></param>
	private RectangleD DrawFinalized (ShapeEngine engine, bool createHistoryItem, bool shiftKey)
	{
		Document doc = workspace.ActiveDocument;

		//Finalize the shape onto the CurrentUserLayer.

		ImageSurface? undoSurface = null;

		if (createHistoryItem && engine.ControlPoints.Count > 0) //We only need to create a history item if there was a previous shape.
			undoSurface = doc.Layers.CurrentUserLayer.Surface.Clone ();

		//Draw the finalized shape.
		RectangleD dirty = DrawShape (engine, doc.Layers.CurrentUserLayer, false, false, false);

		if (createHistoryItem && undoSurface != null) {

			//Create a new ShapesHistoryItem so that the finalization of the shape can be undone.

			doc.History.PushNewItem (
				new ShapesHistoryItem (
					this,
					owner.Icon,
					ShapeName + " " + Translations.GetString ("Finalized"),
					undoSurface,
					doc.Layers.CurrentUserLayer,
					SelectedPointIndex,
					SelectedShapeIndex,
					false
				)
			);
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
	private RectangleD DrawUnfinalized (ShapeEngine engine, bool drawHoverSelection, bool shiftKey, bool ctrl_key)
	{
		//Draw the shape onto the temporary DrawingLayer.
		return DrawShape (engine, engine.DrawingLayer.Layer, true, drawHoverSelection, ctrl_key);
	}

	/// <summary>
	/// Do not call. Use DrawActiveShape.
	/// </summary>
	/// <param name="engine"></param>
	private static void OrganizePoints (ShapeEngine engine)
	{
		//Organize the generated points for quick mouse interaction detection.

		//First, clear the previously organized points, if any.
		engine.OrganizedPoints.ClearCollection ();

		foreach (GeneratedPoint gp in engine.GeneratedPoints) {
			//For each generated point on the shape, calculate the spatial hashing for it and then store this information for later usage.
			engine.OrganizedPoints.StoreAndOrganizePoint (new OrganizedPoint (new PointD (gp.Position.X, gp.Position.Y), gp.ControlPointIndex));
		}
	}

	private void InvalidateAfterDraw (RectangleD dirty)
	{
		Document doc = workspace.ActiveDocument;

		// Increase the size of the dirty rect to account for antialiasing.
		if (owner.UseAntialiasing)
			dirty = dirty.Inflated (1, 1);

		//Combine, clamp, and invalidate the dirty Rectangle.
		if (((RectangleD?) dirty).UnionRectangles (last_dirty) is RectangleD r)
			dirty = r;

		dirty = dirty.Clamp ();
		doc.Workspace.Invalidate (dirty.ToInt ());

		last_dirty = dirty;
	}


	protected RectangleD DrawShape (ShapeEngine engine, Layer l, bool drawCP, bool drawHoverSelection, bool ctrl_key)
	{
		ShapeEngine? activeEngine = ActiveShapeEngine;

		if (activeEngine == null)
			return RectangleD.Zero;

		Document doc = workspace.ActiveDocument;

		using Context g = new (l.Surface);
		g.AppendPath (doc.Selection.SelectionPath);
		g.FillRule = FillRule.EvenOdd;
		g.Clip ();

		g.Antialias = activeEngine.AntiAliasing ? Antialias.Subpixel : Antialias.None;

		bool isDashedLine = g.SetDashFromString (activeEngine.DashPattern, activeEngine.BrushWidth, LineCap.Square);

		g.LineWidth = activeEngine.BrushWidth;

		RectangleD? dirty = null;

		//Draw the shape.
		if (activeEngine.ControlPoints.Count > 0) {
			//Generate the points that make up the shape.
			activeEngine.GeneratePoints (activeEngine.BrushWidth);

			var points = activeEngine.GetActualPoints ();

			//Expand the invalidation rectangle as necessary.

			if (FillShape) {
				Color fill_color = StrokeShape ? activeEngine.FillColor : activeEngine.OutlineColor;
				dirty = dirty.UnionRectangles (g.FillPolygonal (points.AsSpan (), fill_color));
			}

			if (StrokeShape) {

				// dashpatterns cannot work with butt, so if we are using a dashpattern we default to square.
				LineCap lineCap =
					isDashedLine
					? LineCap.Square
					: activeEngine.LineCap;

				dirty = dirty.UnionRectangles (g.DrawPolygonal (points.AsSpan (), activeEngine.OutlineColor, lineCap));
			}
		}

		g.SetDash (Array.Empty<double> (), 0.0);

		//Draw anything extra (that not every shape has), like arrows.
		DrawExtras (ref dirty, g, engine);
		DrawControlPoints (g, activeEngine, drawCP, drawHoverSelection, ctrl_key);

		return dirty ?? RectangleD.Zero;
	}

	private void DrawControlPoints (Context g, ShapeEngine shape, bool draw_controls, bool draw_selection, bool ctrl_key)
	{
		RectangleI dirty = MoveHandle.UnionInvalidateRects (shape.ControlPointHandles);
		shape.ControlPointHandles.Clear ();

		if (!draw_controls) {
			workspace.InvalidateWindowRect (dirty);
			return;
		}

		UpdateHoverHandle (draw_selection, ctrl_key);

		foreach (ControlPoint point in shape.ControlPoints) {

			//Skip drawing the control point if it is being hovered over.
			if (draw_selection && hover_handle.Active && hover_handle.CanvasPosition.Distance (point.Position) < 1d)
				continue;

			shape.ControlPointHandles.Add (
				new MoveHandle {
					Active = true,
					CanvasPosition = point.Position,
					Selected = (point == SelectedPoint) && draw_selection
				}
			);
		}

		dirty = dirty.Union (MoveHandle.UnionInvalidateRects (shape.ControlPointHandles));

		workspace.InvalidateWindowRect (dirty);
	}

	/// <summary>
	/// Update the hover handle's position and redraw it.
	/// </summary>
	protected void UpdateHoverHandle (bool draw_selection, bool ctrl_key)
	{
		RectangleI dirty =
			hover_handle.Active
			? hover_handle.InvalidateRect
			: RectangleI.Zero;

		// Don't show the hover handle while the user is changing a control point's tension.
		hover_handle.Active = hover_handle.Selected = false;

		if (!changing_tension && draw_selection) {

			var current_window_point = workspace.CanvasPointToView (current_point);

			SEngines.FindClosestControlPoint (
				current_point,
				out _,
				out _,
				out var closestControlPoint,
				out _);

			// Check if the user is directly hovering over a control point.
			if (closestControlPoint != null) {
				hover_handle.CanvasPosition = closestControlPoint.Position;
				hover_handle.Active = hover_handle.Selected = hover_handle.ContainsPoint (current_window_point);
			}

			// Otherwise, the user may be hovering over a generated point.
			if (!hover_handle.Active) {

				OrganizedPointCollection.FindClosestPoint (
					SEngines,
					current_point,
					out _,
					out _,
					out var closestPoint,
					out _);

				if (closestPoint.HasValue) {
					hover_handle.CanvasPosition = closestPoint.Value;
					hover_handle.Active = hover_handle.ContainsPoint (current_window_point);
				}
			}

			if (hover_handle.Active)
				dirty = dirty.Union (hover_handle.InvalidateRect);
		}

		// Update the tool's cursor if we are hovering over a control point / generated point,
		// and Ctrl is not pressed (since Ctrl+click starts a new shape).
		// Otherwise, the normal cursor is shown to indicate that a shape can be drawn.
		var tool = tools.CurrentTool!;

		if (hover_handle.Active && !is_drawing && !ctrl_key)
			tool.SetCursor (grab_cursor);
		else
			tool.SetCursor (tool.DefaultCursor);

		workspace.InvalidateWindowRect (dirty);
	}

	/// <summary>
	/// Go through every editable shape and draw it.
	/// </summary>
	public void DrawAllShapes ()
	{
		//Store the SelectedShapeIndex value for later restoration.
		int previousToolSI = SelectedShapeIndex;

		//Draw all of the shapes.
		for (SelectedShapeIndex = 0; SelectedShapeIndex < SEngines.Count; ++SelectedShapeIndex) {
			//Only draw the selected point for the selected shape.
			DrawActiveShape (true, false, previousToolSI == SelectedShapeIndex, false, true);
		}

		//Restore the previous SelectedShapeIndex value.
		SelectedShapeIndex = previousToolSI;

		//Determine if the currently active tool matches the shape's corresponding tool, and if not, switch to it.
		BaseEditEngine.ActivateCorrespondingTool (SelectedShapeIndex, false);

		//The currently active tool should now match the shape's corresponding tool.
	}

	/// <summary>
	/// Go through every editable shape not yet finalized and finalize it.
	/// </summary>
	protected void FinalizeAllShapes ()
	{
		//Finalize every editable shape not yet finalized.

		if (SEngines.Count == 0)
			return;

		Document doc = workspace.ActiveDocument;

		ImageSurface undoSurface = doc.Layers.CurrentUserLayer.Surface.Clone ();

		int previousSelectedPointIndex = SelectedPointIndex;

		RectangleD? dirty = null;

		//Finalize all of the shapes.
		for (SelectedShapeIndex = 0; SelectedShapeIndex < SEngines.Count; ++SelectedShapeIndex) {
			//Get a reference to each shape's corresponding tool.
			ShapeTool? correspondingTool = GetCorrespondingTool (SEngines[SelectedShapeIndex].ShapeType);

			if (correspondingTool == null)
				continue;

			//Finalize the now active shape using its corresponding tool's EditEngine.

			BaseEditEngine correspondingEngine = correspondingTool.EditEngine;

			correspondingEngine.SelectedShapeIndex = SelectedShapeIndex;

			correspondingEngine.BeforeDraw ();

			//Clear any temporary drawing, because something new will be drawn.
			SEngines[SelectedShapeIndex].DrawingLayer.Layer.Clear ();

			//Draw the current shape with the corresponding tool's EditEngine.
			dirty = dirty.UnionRectangles ((RectangleD?) correspondingEngine.DrawFinalized (
				SEngines[SelectedShapeIndex], false, false));
		}

		//Make sure that the undo surface isn't null.
		if (undoSurface != null) {
			//Create a new ShapesHistoryItem so that the finalization of the shapes can be undone.
			doc.History.PushNewItem (new ShapesHistoryItem (this, owner.Icon, Translations.GetString ("Finalized"),
				undoSurface, doc.Layers.CurrentUserLayer, previousSelectedPointIndex, prev_selected_shape_index, true));
		}

		if (dirty.HasValue) {
			InvalidateAfterDraw (dirty.Value);
		}

		//Clear out all of the data.
		ResetShapes ();
	}

	/// <summary>
	/// Constrain the current point to snap to fixed angles from the previous point, or to
	/// produce a square / circle when drawing those shape types.
	/// </summary>
	protected void CalculateModifiedCurrentPoint ()
	{
		ShapeEngine? selEngine = SelectedShapeEngine;

		//Don't bother calculating a modified point if there is no selected shape.
		if (selEngine == null)
			return;

		if (ShapeType != ShapeTypes.OpenLineCurveSeries && selEngine.ControlPoints.Count == 4) {

			// Constrain to a square / circle.

			PointD origin = selEngine.ControlPoints[(SelectedPointIndex + 2) % 4].Position;

			PointD d = current_point - origin;

			var length = Math.Max (Math.Abs (d.X), Math.Abs (d.Y));

			PointD offset = new (
				X: length * Math.Sign (d.X),
				Y: length * Math.Sign (d.Y));

			current_point = origin + offset;

		} else {
			// Calculate the modified position of currentPoint such that the angle between the adjacent point
			// (if any) and currentPoint is snapped to the closest angle out of a certain number of angles.
			ControlPoint adjacentPoint;

			if (SelectedPointIndex > 0) {
				//Previous point.
				adjacentPoint = selEngine.ControlPoints[SelectedPointIndex - 1];
			} else if (selEngine.ControlPoints.Count > 1) {
				//Previous point (looping around to the end) if there is more than 1 point.
				adjacentPoint = selEngine.ControlPoints[^1];
			} else {
				//Don't bother calculating a modified point because there is no reference point to align it with (there is only 1 point).
				return;
			}

			PointD dir = new (
				X: current_point.X - adjacentPoint.Position.X,
				Y: current_point.Y - adjacentPoint.Position.Y);

			RadiansAngle baseTheta = new (Math.Atan2 (dir.Y, dir.X));

			double len = Math.Sqrt (dir.X * dir.X + dir.Y * dir.Y);

			RadiansAngle theta = new (Math.Round (12 * baseTheta.Radians / Math.PI) * Math.PI / 12);

			current_point = new PointD (
				X: adjacentPoint.Position.X + len * Math.Cos (theta.Radians),
				Y: adjacentPoint.Position.Y + len * Math.Sin (theta.Radians));
		}
	}

	/// <summary>
	/// Resets the editable data.
	/// </summary>
	protected void ResetShapes ()
	{
		SEngines = new ShapeEngineCollection ();

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
	public static ShapeTool? ActivateCorrespondingTool (int shapeIndex, bool permanentSwitch)
	{
		//First make sure that there is a validly selectable tool.
		if (shapeIndex > -1 && SEngines.Count > shapeIndex)
			return ActivateCorrespondingTool (SEngines[shapeIndex].ShapeType, permanentSwitch);

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
	public static ShapeTool? ActivateCorrespondingTool (ShapeTypes shapeType, bool permanentSwitch)
	{
		ShapeTool? correspondingTool = GetCorrespondingTool (shapeType);

		//Verify that the corresponding tool is valid and that it doesn't match the currently active tool.
		if (correspondingTool == null || PintaCore.Tools.CurrentTool == correspondingTool) {
			//Let the caller know that the active tool has not been switched.
			return null;
		}

		ShapeTool? oldTool = PintaCore.Tools.CurrentTool as ShapeTool;

		int oldToolSPI = -1;
		int oldToolSSI = -1;
		//SetCurrentTool sets oldTool's SelectedPointIndex and SelectedShapeIndex to -1 so their value has to be saved before this happens.
		if (oldTool != null && oldTool.IsEditableShapeTool && permanentSwitch) {
			oldToolSPI = oldTool.EditEngine.SelectedPointIndex;
			oldToolSSI = oldTool.EditEngine.SelectedShapeIndex;
		}

		//The active tool needs to be switched to the corresponding tool.
		PintaCore.Tools.SetCurrentTool (correspondingTool);
		var newTool = (ShapeTool?) PintaCore.Tools.CurrentTool;

		// This shouldn't be possible, but we need a null check.
		if (newTool is null)
			return null;

		//What happens next depends on whether the old tool was an editable ShapeTool.
		if (oldTool != null && oldTool.IsEditableShapeTool) {

			if (permanentSwitch) {
				//Set the new tool's active shape and point to the old shape and point.
				newTool.EditEngine.SelectedPointIndex = oldToolSPI;
				newTool.EditEngine.SelectedShapeIndex = oldToolSSI;

				//Make sure neither tool thinks it is drawing anything.
				newTool.EditEngine.is_drawing = false;
				oldTool.EditEngine.is_drawing = false;
			}

			ShapeEngine? activeEngine = newTool.EditEngine.ActiveShapeEngine;

			if (activeEngine != null)
				newTool.EditEngine.UpdateToolbarSettings (activeEngine);

		} else {
			if (permanentSwitch) {
				//Make sure that the new tool doesn't think it is drawing anything.
				newTool.EditEngine.is_drawing = false;
			}
		}

		//Let the caller know that the active tool has been switched.
		return oldTool;
	}

	/// <summary>
	/// Gets the corresponding tool to the given shape type and then returns that tool.
	/// </summary>
	/// <param name="ShapeType">The shape type to find the corresponding tool to.</param>
	/// <returns>The corresponding tool to the given shape type.</returns>
	public static ShapeTool? GetCorrespondingTool (ShapeTypes shapeType)
	{

		//Get the corresponding BaseTool reference to the shape type.
		CorrespondingTools.TryGetValue (shapeType, out var correspondingTool);

		return correspondingTool;
	}


	/// <summary>
	/// Copy the given shape's settings to the toolbar settings. Calls StorePreviousSettings.
	/// </summary>
	/// <param name="engine"></param>
	public virtual void UpdateToolbarSettings (ShapeEngine engine)
	{
		owner.UseAntialiasing = engine.AntiAliasing;

		//Update the DashPatternBox to represent the current shape's DashPattern.
		dash_pattern_box.ComboBox!.ComboBox.GetEntry ().SetText (engine.DashPattern); // NRT - Code assumes this is not-null

		OutlineColor = engine.OutlineColor;
		FillColor = engine.FillColor;

		BrushWidth = engine.BrushWidth;

		StorePreviousSettings ();
	}

	/// <summary>
	/// Copy the previous settings to the toolbar settings.
	/// </summary>
	protected virtual void RecallPreviousSettings ()
	{
		dash_pattern_box.ComboBox?.ComboBox.GetEntry ().SetText (prev_dash_pattern);

		owner.UseAntialiasing = prev_antialiasing;
		BrushWidth = prev_brush_width;
	}

	/// <summary>
	/// Copy the toolbar settings to the previous settings.
	/// </summary>
	protected virtual void StorePreviousSettings ()
	{
		if (dash_pattern_box.ComboBox != null)
			prev_dash_pattern = dash_pattern_box.ComboBox.ComboBox.GetEntry ().GetText ();

		prev_antialiasing = owner.UseAntialiasing;
		prev_brush_width = BrushWidth;
	}

	/// <summary>
	/// Creates a new shape, adds its starting points, and returns it.
	/// </summary>
	/// <param name="ctrlKey"></param>
	/// <param name="clickedOnControlPoint"></param>
	/// <param name="prevSelPoint"></param>
	protected abstract ShapeEngine CreateShape (bool ctrlKey, bool clickedOnControlPoint, PointD prevSelPoint);

	protected virtual void MovePoint (List<ControlPoint> controlPoints)
	{
		//Update the control point's position.
		controlPoints.ElementAt (SelectedPointIndex).Position = new PointD (current_point.X, current_point.Y);
	}

	protected virtual void DrawExtras (ref RectangleD? dirty, Context g, ShapeEngine engine)
	{

	}

	protected void AddLinePoints (bool ctrlKey, bool clickedOnControlPoint, ShapeEngine selEngine, PointD prevSelPoint)
	{
		PointD startingPoint;

		//Create the initial points of the shape. The second point will follow the mouse around until released.
		if (ctrlKey && clickedOnControlPoint) {
			startingPoint = prevSelPoint;

			clicked_without_modifying = false;
		} else {
			startingPoint = shape_origin;
		}


		selEngine.ControlPoints.Add (new ControlPoint (new PointD (startingPoint.X, startingPoint.Y), DefaultEndPointTension));
		selEngine.ControlPoints.Add (
			new ControlPoint (new PointD (startingPoint.X + .01d, startingPoint.Y + .01d), DefaultEndPointTension));


		SelectedPointIndex = 1;
		SelectedShapeIndex = SEngines.Count - 1;
	}

	protected void AddRectanglePoints (bool ctrlKey, bool clickedOnControlPoint, ShapeEngine selEngine, PointD prevSelPoint)
	{
		PointD startingPoint;

		//Create the initial points of the shape. The second point will follow the mouse around until released.
		if (ctrlKey && clickedOnControlPoint) {
			startingPoint = prevSelPoint;

			clicked_without_modifying = false;
		} else {
			startingPoint = shape_origin;
		}


		selEngine.ControlPoints.Add (new ControlPoint (new PointD (startingPoint.X, startingPoint.Y), 0.0));
		selEngine.ControlPoints.Add (
			new ControlPoint (new PointD (startingPoint.X, startingPoint.Y + .01d), 0.0));
		selEngine.ControlPoints.Add (
			new ControlPoint (new PointD (startingPoint.X + .01d, startingPoint.Y + .01d), 0.0));
		selEngine.ControlPoints.Add (
			new ControlPoint (new PointD (startingPoint.X + .01d, startingPoint.Y), 0.0));


		SelectedPointIndex = 2;
		SelectedShapeIndex = SEngines.Count - 1;
	}

	protected void MoveRectangularPoint (List<ControlPoint> controlPoints)
	{
		ShapeEngine? selEngine = SelectedShapeEngine;

		if (selEngine == null || !selEngine.Closed || controlPoints.Count != 4)
			return;

		//Figure out the indices of the surrounding points. The lowest point index should be 0 and the highest 3.

		int previousPointIndex = SelectedPointIndex - 1;
		int nextPointIndex = SelectedPointIndex + 1;
		int oppositePointIndex = SelectedPointIndex + 2;

		if (previousPointIndex < 0)
			previousPointIndex = controlPoints.Count - 1;

		if (nextPointIndex >= controlPoints.Count) {
			nextPointIndex = 0;
			oppositePointIndex = 1;
		} else if (oppositePointIndex >= controlPoints.Count) {
			oppositePointIndex = 0;
		}


		ControlPoint previousPoint = controlPoints.ElementAt (previousPointIndex);
		ControlPoint oppositePoint = controlPoints.ElementAt (oppositePointIndex);
		ControlPoint nextPoint = controlPoints.ElementAt (nextPointIndex);


		//Now that we know the indexed order of the points, we can align everything properly.
		if (SelectedPointIndex == 2 || SelectedPointIndex == 0) {
			//Control point visual order (counter-clockwise order always goes selectedPoint, previousPoint, oppositePoint, nextPoint,
			//where moving point == selectedPoint):
			//
			//static (opposite) point		horizontally aligned point
			//vertically aligned point		moving point
			//OR
			//moving point					vertically aligned point
			//horizontally aligned point	static (opposite) point


			//Update the previous control point's position.
			previousPoint.Position = new PointD (previousPoint.Position.X, current_point.Y);

			//Update the next control point's position.
			nextPoint.Position = new PointD (current_point.X, nextPoint.Position.Y);


			//Even though it's supposed to be static, just in case the points get out of order
			//(they do sometimes), update the opposite control point's position.
			oppositePoint.Position = new PointD (previousPoint.Position.X, nextPoint.Position.Y);
		} else {
			//Control point visual order (counter-clockwise order always goes selectedPoint, previousPoint, oppositePoint, nextPoint,
			//where moving point == selectedPoint):
			//
			//horizontally aligned point	static (opposite) point
			//moving point					vertically aligned point
			//OR
			//vertically aligned point		moving point
			//static (opposite) point		horizontally aligned point


			//Update the previous control point's position.
			previousPoint.Position = new PointD (current_point.X, previousPoint.Position.Y);

			//Update the next control point's position.
			nextPoint.Position = new PointD (nextPoint.Position.X, current_point.Y);


			//Even though it's supposed to be static, just in case the points get out of order
			//(they do sometimes), update the opposite control point's position.
			oppositePoint.Position = new PointD (nextPoint.Position.X, previousPoint.Position.Y);
		}
	}
}
