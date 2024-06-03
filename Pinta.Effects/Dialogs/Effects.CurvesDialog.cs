//
// CurvesDialog.cs
//  
// Author:
//      Krzysztof Marecki <marecki.krzysztof@gmail.com>
// 
// Copyright (c) 2010 Krzysztof Marecki
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
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;


public sealed class CurvesDialog : Gtk.Dialog
{
	private readonly Gtk.ComboBoxText combo_map;
	private readonly Gtk.Label label_point;
	private readonly Gtk.DrawingArea curves_drawing;
	private readonly Gtk.CheckButton check_red;
	private readonly Gtk.CheckButton check_green;
	private readonly Gtk.CheckButton check_blue;
	private readonly Gtk.Button button_reset;
	private readonly Gtk.Label label_tip;

	private sealed class ControlPointDrawingInfo
	{
		public Cairo.Color Color { get; set; }
		public bool IsActive { get; set; }
	}

	//drawing area width and height
	private const int Size = 256;
	//control point radius
	private const int Radius = 6;

	private int channels;
	//last added control point x;
	private int? last_cpx;
	private PointI last_mouse_pos = new (0, 0);
	// Keys of existing control points which cannot be overwritten by a new control point.
	private readonly HashSet<int> orig_cps = new ();

	//control points for luminosity transfer mode
	private SortedList<int, int>[] luminosity_cps = null!; // NRT - Set via code flow
	private SortedList<int, int>[] rgb_cps = null!;

	public SortedList<int, int>[] ControlPoints {
		get => (Mode == ColorTransferMode.Luminosity) ? luminosity_cps : rgb_cps;
		set {
			if (Mode == ColorTransferMode.Luminosity)
				luminosity_cps = value;
			else
				rgb_cps = value;
		}
	}

	public ColorTransferMode Mode =>
		(combo_map.Active == 0)
		? ColorTransferMode.Rgb
		: ColorTransferMode.Luminosity;

	public CurvesData EffectData { get; }

	public CurvesDialog (IChromeService chrome, CurvesData effectData)
	{
		const int spacing = 6;

		Gtk.ComboBoxText comboMap = CreateComboMap ();

		Gtk.Label labelPoint = CreateLabelPoint ();

		Gtk.CheckButton checkRed = CreateColorCheck (Translations.GetString ("Red  "));
		Gtk.CheckButton checkGreen = CreateColorCheck (Translations.GetString ("Green"));
		Gtk.CheckButton checkBlue = CreateColorCheck (Translations.GetString ("Blue "));

		Gtk.Button buttonReset = CreateResetButton ();

		Gtk.Label labelTip = Gtk.Label.New (Translations.GetString ("Tip: Right-click to remove control points."));

		Gtk.EventControllerMotion motionController = CreateCurvesMotionController ();

		Gtk.GestureClick clickController = CreateCurvesClickController ();

		Gtk.Box boxAbove = new () { Spacing = spacing };
		boxAbove.SetOrientation (Gtk.Orientation.Horizontal);
		boxAbove.Append (Gtk.Label.New (Translations.GetString ("Transfer Map")));
		boxAbove.Append (comboMap);
		boxAbove.Append (labelPoint);

		Gtk.Box boxBelow = new ();
		boxBelow.SetOrientation (Gtk.Orientation.Horizontal);
		boxBelow.Prepend (checkBlue);
		boxBelow.Prepend (checkGreen);
		boxBelow.Prepend (checkRed);
		boxBelow.Append (buttonReset);

		Gtk.DrawingArea curvesDrawing = new Gtk.DrawingArea {
			WidthRequest = 256,
			HeightRequest = 256,
			CanFocus = true,
		};
		curvesDrawing.SetAllMargins (8);
		curvesDrawing.SetDrawFunc ((area, context, width, height) => HandleDrawingDrawnEvent (context));
		curvesDrawing.AddController (motionController);
		curvesDrawing.AddController (clickController);

		Gtk.Box content_area = this.GetContentAreaBox ();
		content_area.SetAllMargins (12);
		content_area.Spacing = spacing;
		content_area.Append (boxAbove);
		content_area.Append (curvesDrawing);
		content_area.Append (boxBelow);
		content_area.Append (labelTip);

		// --- Gtk.Window initialization

		Title = Translations.GetString ("Curves");

		TransientFor = chrome.MainWindow;

		Modal = true;

		Resizable = false;

		// --- Gtk.Dialog initialization

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- References to keep

		curves_drawing = curvesDrawing;

		combo_map = comboMap;

		label_point = labelPoint;

		check_red = checkRed;
		check_green = checkGreen;
		check_blue = checkBlue;

		button_reset = buttonReset;

		label_tip = labelTip;

		// --- Initialization

		EffectData = effectData;

		ResetControlPoints ();
	}

	private Gtk.ComboBoxText CreateComboMap ()
	{
		Gtk.ComboBoxText result = new ();
		result.AppendText (Translations.GetString ("RGB"));
		result.AppendText (Translations.GetString ("Luminosity"));
		result.Active = 1;
		result.OnChanged += HandleComboMapChanged;
		return result;
	}

	private static Gtk.Label CreateLabelPoint ()
	{
		Gtk.Label result = Gtk.Label.New ("(256, 256)");
		result.Hexpand = true;
		result.Halign = Gtk.Align.End;
		return result;
	}

	private Gtk.Button CreateResetButton ()
	{
		Gtk.Button result = new () {
			WidthRequest = 81,
			HeightRequest = 30,
			Label = Translations.GetString ("Reset"),
			Halign = Gtk.Align.End,
			Hexpand = true,
		};
		result.OnClicked += HandleButtonResetClicked;
		return result;
	}

	private Gtk.CheckButton CreateColorCheck (string label)
	{
		Gtk.CheckButton result = new () { Label = label, Active = true };
		result.Hide ();
		result.OnToggled += HandleCheckToggled;
		return result;
	}

	private Gtk.EventControllerMotion CreateCurvesMotionController ()
	{
		Gtk.EventControllerMotion result = Gtk.EventControllerMotion.New ();
		result.OnMotion += HandleDrawingMotionNotifyEvent;
		result.OnLeave += (_, _) => InvalidateDrawing ();
		return result;
	}

	private Gtk.GestureClick CreateCurvesClickController ()
	{
		Gtk.GestureClick result = Gtk.GestureClick.New ();
		result.SetButton (0); // Handle all buttons
		result.OnPressed += HandleDrawingButtonPressEvent;
		return result;
	}

	private void UpdateLivePreview (string propertyName)
	{
		if (EffectData == null)
			return;

		EffectData.ControlPoints = ControlPoints;
		EffectData.Mode = Mode;
		EffectData.FirePropertyChanged (propertyName);
	}

	private void HandleCheckToggled (object? o, EventArgs args)
	{
		InvalidateDrawing ();
	}

	void HandleButtonResetClicked (object? sender, EventArgs e)
	{
		ResetControlPoints ();
		InvalidateDrawing ();
	}

	private void ResetControlPoints ()
	{
		channels = (Mode == ColorTransferMode.Luminosity) ? 1 : 3;
		ControlPoints = new SortedList<int, int>[channels];

		for (int i = 0; i < channels; i++) {
			ControlPoints[i] = new () {
				{ 0, 0 },
				{ Size - 1, Size - 1 }
			};
		}

		UpdateLivePreview (nameof (ControlPoints));
	}

	private void HandleComboMapChanged (object? sender, EventArgs e)
	{
		if (ControlPoints == null)
			ResetControlPoints ();
		else
			UpdateLivePreview (nameof (Mode));

		bool visible = (Mode == ColorTransferMode.Rgb);
		check_red.Visible = check_green.Visible = check_blue.Visible = visible;

		InvalidateDrawing ();
	}

	private void InvalidateDrawing ()
	{
		//to invalidate whole drawing area
		curves_drawing.QueueDraw ();
	}

	private IEnumerable<SortedList<int, int>> GetActiveControlPoints ()
	{
		if (Mode == ColorTransferMode.Luminosity) {
			yield return ControlPoints[0];
			yield break;
		}

		if (check_red.Active)
			yield return ControlPoints[0];

		if (check_green.Active)
			yield return ControlPoints[1];

		if (check_blue.Active)
			yield return ControlPoints[2];
	}

	private void AddControlPoint (int x, int y)
	{
		foreach (var controlPoints in GetActiveControlPoints ())
			controlPoints[x] = Size - 1 - y;

		last_cpx = x;

		UpdateLivePreview (nameof (ControlPoints));
	}

	private void HandleDrawingMotionNotifyEvent (
		Gtk.EventControllerMotion controller,
		Gtk.EventControllerMotion.MotionSignalArgs args)
	{
		int x = (int) args.X;
		int y = (int) args.Y;

		last_mouse_pos = new (x, y);

		if (x < 0 || x >= Size || y < 0 || y >= Size)
			return;

		if (controller.GetCurrentEventState () != Gdk.ModifierType.Button1Mask) {
			InvalidateDrawing ();
			return;
		}

		if (last_cpx is not null) {
			// The first and last control points cannot be removed, so also forbid dragging them away.
			if (last_cpx == 0)
				x = 0;
			else if (last_cpx == Size - 1)
				x = Size - 1;
			else {
				// Remove the old version of the control point being edited.
				foreach (var controlPoints in GetActiveControlPoints ()) {
					if (controlPoints.ContainsKey (last_cpx.Value))
						controlPoints.Remove (last_cpx.Value);
				}
			}
		}

		// Don't allow overwriting any of the original control points while dragging.
		if (!orig_cps.Contains (x))
			AddControlPoint (x, y);
		else
			last_cpx = null;

		InvalidateDrawing ();
	}

	/// <summary>
	/// If the provided coordinates are close to an existing control point, snap to the control point's coordinates.
	/// </summary>
	private static bool SnapToControlPointProximity (
		IEnumerable<SortedList<int, int>> activeControlPoints,
		ref PointI pos)
	{
		foreach (var controlPoints in activeControlPoints) {
			for (int i = 0; i < controlPoints.Count; i++) {

				int cpx = controlPoints.Keys[i];
				int cpy = Size - 1 - controlPoints.Values[i];

				if (!CheckControlPointProximity (cpx, cpy, pos))
					continue;

				pos = new PointI (X: cpx, Y: cpy);

				return true;
			}
		}

		return false;
	}

	private void HandleDrawingButtonPressEvent (
		Gtk.GestureClick controller,
		Gtk.GestureClick.PressedSignalArgs args)
	{
		PointI pos = new (
			X: (int) args.X,
			Y: (int) args.Y);

		if (controller.GetCurrentMouseButton () == MouseButton.Left) {

			orig_cps.Clear ();
			foreach (var controlPoints in GetActiveControlPoints ()) {
				orig_cps.UnionWith (controlPoints.Keys);
			}

			if (SnapToControlPointProximity (GetActiveControlPoints (), ref pos))
				orig_cps.Remove (pos.X); // Allow dragging the snapped control point.

			AddControlPoint (pos.X, pos.Y);
		}

		if (controller.GetCurrentMouseButton () != MouseButton.Right) {
			InvalidateDrawing ();
			return;
		}

		// user pressed right button
		foreach (var controlPoints in GetActiveControlPoints ()) {
			for (int i = 0; i < controlPoints.Count; i++) {
				int cpx = controlPoints.Keys[i];
				int cpy = Size - 1 - controlPoints.Values[i];

				//we cannot allow user to remove first or last control point

				if (cpx == 0 && cpy == Size - 1)
					continue;

				if (cpx == Size - 1 && cpy == 0)
					continue;

				if (CheckControlPointProximity (cpx, cpy, pos)) {
					controlPoints.RemoveAt (i);
					break;
				}
			}
		}

		InvalidateDrawing ();
	}

	private static void DrawBorder (Context g)
	{
		g.Rectangle (0, 0, Size - 1, Size - 1);
		g.LineWidth = 1;
		g.Stroke ();
	}

	private void DrawPointerCross (Context g)
	{
		int x = last_mouse_pos.X;
		int y = last_mouse_pos.Y;

		if (x < 0 || x >= Size || y < 0 || y >= Size) {
			label_point.SetText (string.Empty);
			return;
		}

		g.LineWidth = 0.5;
		g.MoveTo (x, 0);
		g.LineTo (x, Size);
		g.MoveTo (0, y);
		g.LineTo (Size, y);
		g.Stroke ();

		label_point.SetText ($"({x}, {y})");
	}

	private static void DrawGrid (Context g)
	{
		g.SetDash (new double[] { 4, 4 }, 2);
		g.LineWidth = 1;

		for (int i = 1; i < 4; i++) {
			g.MoveTo (i * Size / 4, 0);
			g.LineTo (i * Size / 4, Size);
			g.MoveTo (0, i * Size / 4);
			g.LineTo (Size, i * Size / 4);
		}

		g.MoveTo (0, Size - 1);
		g.LineTo (Size - 1, 0);
		g.Stroke ();

		g.SetDash (Array.Empty<double> (), 0);
	}

	//cpx, cpyx - control point's x and y coordinates
	private static bool CheckControlPointProximity (int cpx, int cpy, PointI pos)
		=> Math.Sqrt (Math.Pow (cpx - pos.X, 2) + Math.Pow (cpy - pos.Y, 2)) < Radius;

	private IEnumerator<ControlPointDrawingInfo> GetDrawingInfos ()
	{
		if (Mode == ColorTransferMode.Luminosity) {
			curves_drawing.GetStyleContext ().GetColor (out var fg_color);
			yield return new ControlPointDrawingInfo {
				Color = fg_color,
				IsActive = true
			};
			yield break;
		}

		yield return new ControlPointDrawingInfo {
			Color = new Color (0.9, 0, 0),
			IsActive = check_red.Active
		};

		yield return new ControlPointDrawingInfo {
			Color = new Color (0, 0.9, 0),
			IsActive = check_green.Active
		};

		yield return new ControlPointDrawingInfo {
			Color = new Color (0, 0, 0.9),
			IsActive = check_blue.Active
		};
	}

	private void DrawControlPoints (Context g)
	{
		PointI lastMousePos = last_mouse_pos;

		var infos = GetDrawingInfos ();

		foreach (var controlPoints in ControlPoints) {

			infos.MoveNext ();
			var info = infos.Current;

			for (int i = 0; i < controlPoints.Count; i++) {
				int cpx = controlPoints.Keys[i];
				int cpy = Size - 1 - controlPoints.Values[i];
				RectangleD rect;

				if (info.IsActive) {
					if (CheckControlPointProximity (cpx, cpy, lastMousePos)) {
						rect = new RectangleD (cpx - (Radius + 2) / 2, cpy - (Radius + 2) / 2, Radius + 2, Radius + 2);
						g.DrawEllipse (rect, new Color (0.2, 0.2, 0.2), 2);
						rect = new RectangleD (cpx - Radius / 2, cpy - Radius / 2, Radius, Radius);
						g.FillEllipse (rect, new Color (0.9, 0.9, 0.9));
					} else {
						rect = new RectangleD (cpx - Radius / 2, cpy - Radius / 2, Radius, Radius);
						g.DrawEllipse (rect, info.Color, 2);
					}
				}

				rect = new RectangleD (cpx - (Radius - 2) / 2, cpy - (Radius - 2) / 2, Radius - 2, Radius - 2);
				g.FillEllipse (rect, info.Color);
			}
		}

		g.Stroke ();
	}

	private void DrawSpline (Context g)
	{
		var infos = GetDrawingInfos ();

		g.Save ();

		Span<PointD> line = stackalloc PointD[Size];

		foreach (var controlPoints in ControlPoints) {

			int points = controlPoints.Count;
			SplineInterpolator interpolator = new SplineInterpolator ();
			IList<int> xa = controlPoints.Keys;
			IList<int> ya = controlPoints.Values;

			for (int i = 0; i < points; i++)
				interpolator.Add (xa[i], ya[i]);

			for (int i = 0; i < line.Length; i++) {
				line[i] = new (
					X: i,
					Y: (float) (Math.Clamp (Size - 1 - interpolator.Interpolate (i), 0, Size - 1))
				);
			}

			g.LineWidth = 2;
			g.LineJoin = LineJoin.Round;

			g.MoveTo (line[0].X, line[0].Y);
			for (int i = 1; i < line.Length; i++)
				g.LineTo (line[i].X, line[i].Y);

			infos.MoveNext ();
			var info = infos.Current;

			g.SetSourceColor (info.Color);
			g.LineWidth = info.IsActive ? 2 : 1;
			g.Stroke ();
		}

		g.Restore ();
	}

	private void HandleDrawingDrawnEvent (Context g)
	{
		curves_drawing.GetStyleContext ().GetColor (out var fg_color);
		g.SetSourceColor (fg_color);

		DrawBorder (g);
		DrawPointerCross (g);
		DrawSpline (g);
		DrawGrid (g);
		DrawControlPoints (g);
	}
}
