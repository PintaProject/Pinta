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

	private sealed record ControlPointDrawingInfo (
		Color Color,
		bool IsActive);

	private const int SIZE = 256; // drawing area width and height
	private const int RADIUS = 6; // Control point radius

	//last added control point x;
	private int? last_cpx;
	private PointI last_mouse_pos = new (0, 0);
	// Keys of existing control points which cannot be overwritten by a new control point.
	private readonly HashSet<int> orig_cps = [];

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

	public CurvesDialog (
		IChromeService chrome,
		CurvesData effectData)
	{
		const int SPACING = 6;

		Gtk.ComboBoxText comboMap = CreateComboMap ();

		Gtk.Label labelPoint = CreateLabelPoint ();

		Gtk.CheckButton checkRed = CreateColorCheck (Translations.GetString ("Red  "));
		Gtk.CheckButton checkGreen = CreateColorCheck (Translations.GetString ("Green"));
		Gtk.CheckButton checkBlue = CreateColorCheck (Translations.GetString ("Blue "));

		Gtk.Button buttonReset = CreateResetButton ();

		Gtk.Label labelTip = Gtk.Label.New (Translations.GetString ("Tip: Right-click to remove control points."));

		Gtk.EventControllerMotion motionController = CreateCurvesMotionController ();

		Gtk.GestureClick clickController = CreateCurvesClickController ();

		StackStyle horizontalSpaced = new (Gtk.Orientation.Horizontal, SPACING);
		Gtk.Box boxAbove = GtkExtensions.Stack (
			horizontalSpaced,
			[
				Gtk.Label.New (Translations.GetString ("Transfer Map")),
				comboMap,
				labelPoint
			]
		);

		Gtk.Box boxBelow = GtkExtensions.StackHorizontal ([
			checkRed,
			checkGreen,
			checkBlue,
			buttonReset]);

		Gtk.DrawingArea curvesDrawing = new () {
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
		content_area.Spacing = SPACING;
		content_area.AppendMultiple ([
			boxAbove,
			curvesDrawing,
			boxBelow,
			labelTip]);

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

		// Handlers

		void HandleComboMapChanged (object? sender, EventArgs e)
		{
			if (ControlPoints == null)
				ResetControlPoints ();
			else
				UpdateLivePreview (nameof (Mode));

			bool visible = (Mode == ColorTransferMode.Rgb);
			check_red.Visible = check_green.Visible = check_blue.Visible = visible;

			InvalidateDrawing ();
		}
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

		// Handlers

		void HandleButtonResetClicked (object? sender, EventArgs e)
		{
			ResetControlPoints ();
			InvalidateDrawing ();
		}
	}

	private Gtk.CheckButton CreateColorCheck (string label)
	{
		Gtk.CheckButton result = new () { Label = label, Active = true };
		result.Hide ();
		result.OnToggled += (_, _) => InvalidateDrawing ();
		return result;
	}

	private Gtk.EventControllerMotion CreateCurvesMotionController ()
	{
		Gtk.EventControllerMotion result = Gtk.EventControllerMotion.New ();
		result.OnMotion += HandleDrawingMotionNotifyEvent;
		result.OnLeave += (_, _) => InvalidateDrawing ();
		return result;

		// Handlers

		void HandleDrawingMotionNotifyEvent (
			Gtk.EventControllerMotion controller,
			Gtk.EventControllerMotion.MotionSignalArgs args)
		{
			PointI p = new (
				X: (int) args.X,
				Y: (int) args.Y);

			last_mouse_pos = p;

			if (p.X < 0 || p.X >= SIZE || p.Y < 0 || p.Y >= SIZE)
				return;

			if (!controller.GetCurrentEventState ().IsLeftMousePressed ()) {
				InvalidateDrawing ();
				return;
			}

			if (last_cpx is not null) {
				// The first and last control points cannot be removed, so also forbid dragging them away.
				if (last_cpx == 0)
					p = p with { X = 0 };
				else if (last_cpx == SIZE - 1)
					p = p with { X = SIZE - 1 };
				else {
					// Remove the old version of the control point being edited.
					foreach (var controlPoints in GetActiveControlPoints ()) {
						if (controlPoints.ContainsKey (last_cpx.Value))
							controlPoints.Remove (last_cpx.Value);
					}
				}
			}

			// Don't allow overwriting any of the original control points while dragging.
			if (!orig_cps.Contains (p.X))
				AddControlPoint (p);
			else
				last_cpx = null;

			InvalidateDrawing ();
		}
	}

	private Gtk.GestureClick CreateCurvesClickController ()
	{
		Gtk.GestureClick result = Gtk.GestureClick.New ();
		result.SetButton (0); // Handle all buttons
		result.OnPressed += HandleDrawingButtonPressEvent;
		return result;

		// Handlers

		void HandleDrawingButtonPressEvent (
			Gtk.GestureClick controller,
			Gtk.GestureClick.PressedSignalArgs args)
		{
			PointI pos = new (
				X: (int) args.X,
				Y: (int) args.Y);

			if (controller.GetCurrentMouseButton () == MouseButton.Left) {

				orig_cps.Clear ();

				foreach (var controlPoints in GetActiveControlPoints ())
					orig_cps.UnionWith (controlPoints.Keys);

				if (SnapToControlPointProximity (GetActiveControlPoints (), ref pos))
					orig_cps.Remove (pos.X); // Allow dragging the snapped control point.

				AddControlPoint (pos);
			}

			if (controller.GetCurrentMouseButton () != MouseButton.Right) {
				InvalidateDrawing ();
				return;
			}

			// user pressed right button
			foreach (var controlPoints in GetActiveControlPoints ()) {

				for (int i = 0; i < controlPoints.Count; i++) {

					PointI cp = new (
						X: controlPoints.Keys[i],
						Y: SIZE - 1 - controlPoints.Values[i]);

					//we cannot allow user to remove first or last control point

					if (cp.X == 0 && cp.Y == SIZE - 1)
						continue;

					if (cp.X == SIZE - 1 && cp.Y == 0)
						continue;

					if (CheckControlPointProximity (cp, pos)) {
						controlPoints.RemoveAt (i);
						break;
					}
				}
			}

			InvalidateDrawing ();
		}

		/// <summary>
		/// If the provided coordinates are close to an existing control point, snap to the control point's coordinates.
		/// </summary>
		static bool SnapToControlPointProximity (
			IEnumerable<SortedList<int, int>> activeControlPoints,
			ref PointI pos)
		{
			foreach (var controlPoints in activeControlPoints) {

				for (int i = 0; i < controlPoints.Count; i++) {

					PointI cp = new (
						X: controlPoints.Keys[i],
						Y: SIZE - 1 - controlPoints.Values[i]);

					if (!CheckControlPointProximity (cp, pos))
						continue;

					pos = cp;

					return true;
				}
			}

			return false;
		}
	}

	private void UpdateLivePreview (string propertyName)
	{
		if (EffectData == null)
			return;

		EffectData.ControlPoints = ControlPoints;
		EffectData.Mode = Mode;
		EffectData.FirePropertyChanged (propertyName);
	}

	private void ResetControlPoints ()
	{
		ControlPoints = ComputeControlPoints (Mode);
		UpdateLivePreview (nameof (ControlPoints));
	}

	private static SortedList<int, int>[] ComputeControlPoints (ColorTransferMode mode)
	{
		int channels =
			mode == ColorTransferMode.Luminosity
			? 1
			: 3;

		var result = new SortedList<int, int>[channels];

		for (int i = 0; i < channels; i++) {
			result[i] = new () {
				{ 0, 0 },
				{ SIZE - 1, SIZE - 1 },
			};
		}

		return result;
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

	private void AddControlPoint (PointI cp)
	{
		foreach (var controlPoints in GetActiveControlPoints ())
			controlPoints[cp.X] = SIZE - 1 - cp.Y;

		last_cpx = cp.X;

		UpdateLivePreview (nameof (ControlPoints));
	}

	//cpx, cpyx - control point's x and y coordinates
	private static bool CheckControlPointProximity (PointI cp, PointI pos)
		=> Math.Sqrt (Math.Pow (cp.X - pos.X, 2) + Math.Pow (cp.Y - pos.Y, 2)) < RADIUS;

	private IEnumerator<ControlPointDrawingInfo> GetDrawingInfos ()
	{
		if (Mode == ColorTransferMode.Luminosity) {
			curves_drawing.GetStyleContext ().GetColor (out var fg_color);
			yield return new ControlPointDrawingInfo (
				Color: fg_color,
				IsActive: true);
			yield break;
		}

		yield return new (
			Color: new Color (0.9, 0, 0),
			IsActive: check_red.Active);

		yield return new (
			Color: new Color (0, 0.9, 0),
			IsActive: check_green.Active);

		yield return new (
			Color: new Color (0, 0, 0.9),
			IsActive: check_blue.Active);
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

		g.Dispose ();

		return;

		// Methods

		static void DrawBorder (Context g)
		{
			g.Rectangle (0, 0, SIZE - 1, SIZE - 1);
			g.LineWidth = 1;
			g.Stroke ();
		}

		void DrawPointerCross (Context g)
		{
			PointI p = last_mouse_pos;

			if (p.X < 0 || p.X >= SIZE || p.Y < 0 || p.Y >= SIZE) {
				label_point.SetText (string.Empty);
				return;
			}

			g.LineWidth = 0.5;

			g.MoveTo (p.X, 0);
			g.LineTo (p.X, SIZE);

			g.MoveTo (0, p.Y);
			g.LineTo (SIZE, p.Y);

			g.Stroke ();

			label_point.SetText ($"({p})");
		}

		void DrawSpline (Context g)
		{
			var infos = GetDrawingInfos ();

			g.Save ();

			Span<PointD> line = stackalloc PointD[SIZE];

			foreach (var controlPoints in ControlPoints) {

				int points = controlPoints.Count;

				SplineInterpolator<double> interpolator = new ();

				IList<int> xa = controlPoints.Keys;
				IList<int> ya = controlPoints.Values;

				for (int i = 0; i < points; i++)
					interpolator.Add (xa[i], ya[i]);

				for (int i = 0; i < line.Length; i++) {
					line[i] = new (
						X: i,
						Y: (float) (Math.Clamp (SIZE - 1 - interpolator.Interpolate (i), 0, SIZE - 1))
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

		static void DrawGrid (Context g)
		{
			g.SetDash ([4, 4], 2);
			g.LineWidth = 1;

			for (int i = 1; i < 4; i++) {

				g.MoveTo (i * SIZE / 4, 0);
				g.LineTo (i * SIZE / 4, SIZE);

				g.MoveTo (0, i * SIZE / 4);
				g.LineTo (SIZE, i * SIZE / 4);
			}

			g.MoveTo (0, SIZE - 1);
			g.LineTo (SIZE - 1, 0);

			g.Stroke ();

			g.SetDash ([], 0);
		}

		void DrawControlPoints (Context g)
		{
			PointI lastMousePos = last_mouse_pos;

			var infos = GetDrawingInfos ();

			foreach (var controlPoints in ControlPoints) {

				infos.MoveNext ();
				var info = infos.Current;

				for (int i = 0; i < controlPoints.Count; i++) {

					PointI cp = new (
						X: controlPoints.Keys[i],
						Y: SIZE - 1 - controlPoints.Values[i]);

					if (info.IsActive) {

						if (CheckControlPointProximity (cp, lastMousePos)) {

							RectangleD outline = new (
								X: cp.X - (RADIUS + 2) / 2,
								Y: cp.Y - (RADIUS + 2) / 2,
								Width: RADIUS + 2,
								Height: RADIUS + 2);

							g.DrawEllipse (
								outline,
								new Color (0.2, 0.2, 0.2),
								2);

							RectangleD fill = new (
								X: cp.X - RADIUS / 2,
								Y: cp.Y - RADIUS / 2,
								Width: RADIUS,
								Height: RADIUS);

							g.FillEllipse (
								fill,
								new Color (0.9, 0.9, 0.9));

						} else {
							RectangleD outline = new (
								X: cp.X - RADIUS / 2,
								Y: cp.Y - RADIUS / 2,
								Width: RADIUS,
								Height: RADIUS);

							g.DrawEllipse (
								outline,
								info.Color,
								2);
						}
					}

					RectangleD rect = new (
						cp.X - (RADIUS - 2) / 2,
						cp.Y - (RADIUS - 2) / 2,
						RADIUS - 2,
						RADIUS - 2);

					g.FillEllipse (
						rect,
						info.Color);
				}
			}

			g.Stroke ();
		}
	}
}
