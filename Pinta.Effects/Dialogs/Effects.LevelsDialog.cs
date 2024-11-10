/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

// Additional code:
//
// LevelsDialog.cs
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
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public partial class LevelsDialog : Gtk.Dialog
{
	private record struct ChannelsMask (bool R, bool G, bool B)
	{
		public bool this[int index] {

			set {
				switch (index) {
					case 0: R = value; break;
					case 1: G = value; break;
					case 2: B = value; break;
					default: throw new ArgumentOutOfRangeException (nameof (index));
				}
			}

			readonly get => index switch {
				0 => R,
				1 => G,
				2 => B,
				_ => throw new ArgumentOutOfRangeException (nameof (index))
			};
		}
	}

	private ChannelsMask mask = new (R: true, G: true, B: true);

	private readonly Gtk.CheckButton check_red;
	private readonly Gtk.CheckButton check_green;
	private readonly Gtk.CheckButton check_blue;
	private readonly Gtk.Button button_auto;
	private readonly Gtk.Button button_reset;
	private readonly Gtk.SpinButton spin_in_low;
	private readonly Gtk.SpinButton spin_in_high;
	private readonly Gtk.SpinButton spin_out_low;
	private readonly Gtk.SpinButton spin_out_high;
	private readonly Gtk.SpinButton spin_out_gamma;

	private readonly ColorGradientWidget gradient_input;
	private readonly ColorGradientWidget gradient_output;
	private readonly ColorPanelWidget colorpanel_in_high;
	private readonly ColorPanelWidget colorpanel_in_low;
	private readonly ColorPanelWidget colorpanel_out_high;
	private readonly ColorPanelWidget colorpanel_out_mid;
	private readonly ColorPanelWidget colorpanel_out_low;
	private readonly HistogramWidget histogram_input;
	private readonly HistogramWidget histogram_output;
	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;

	public LevelsData EffectData { get; }

	public LevelsDialog (
		IChromeService chrome,
		IWorkspaceService workspace,
		LevelsData effectData)
	{
		const int spacing = 6;

		Gtk.CheckButton checkRed = new () { Label = Translations.GetString ("Red"), Active = true };
		checkRed.OnToggled += HandleCheckRedToggled;

		Gtk.CheckButton checkGreen = new () { Label = Translations.GetString ("Green"), Active = true };
		checkGreen.OnToggled += HandleCheckGreenToggled;

		Gtk.CheckButton checkBlue = new () { Label = Translations.GetString ("Blue"), Active = true };
		checkBlue.OnToggled += HandleCheckBlueToggled;

		Gtk.Box hboxChecks = new () { Spacing = spacing };
		hboxChecks.SetOrientation (Gtk.Orientation.Horizontal);
		hboxChecks.Append (checkRed);
		hboxChecks.Append (checkGreen);
		hboxChecks.Append (checkBlue);

		Gtk.SpinButton spinInLow = Gtk.SpinButton.NewWithRange (0, 254, 1);
		spinInLow.OnValueChanged += HandleSpinInLowValueChanged;
		spinInLow.SetActivatesDefaultImmediate (true);

		Gtk.SpinButton spinInHigh = Gtk.SpinButton.NewWithRange (1, 255, 1);
		spinInHigh.Value = 255;
		spinInHigh.OnValueChanged += HandleSpinInHighValueChanged;
		spinInHigh.SetActivatesDefaultImmediate (true);

		Gtk.SpinButton spinOutLow = Gtk.SpinButton.NewWithRange (0, 252, 1);
		spinOutLow.OnValueChanged += HandleSpinOutLowValueChanged;
		spinOutLow.SetActivatesDefaultImmediate (true);

		Gtk.SpinButton spinOutHigh = Gtk.SpinButton.NewWithRange (2, 255, 1);
		spinOutHigh.Value = 255;
		spinOutHigh.OnValueChanged += HandleSpinOutHighValueChanged;
		spinOutHigh.SetActivatesDefaultImmediate (true);

		Gtk.SpinButton spinOutGamma = Gtk.SpinButton.NewWithRange (0, 100, 0.1);
		spinOutGamma.Value = 1;
		spinOutGamma.OnValueChanged += HandleSpinOutGammaValueChanged;
		spinOutGamma.SetActivatesDefaultImmediate (true);

		ColorGradientWidget gradientInput = new (2) { WidthRequest = 40 };
		gradientInput.ClickGesture.OnPressed += HandleGradientButtonPressEvent;
		gradientInput.ClickGesture.OnReleased += HandleGradientButtonReleaseEvent;
		gradientInput.ValueChanged += HandleGradientInputValueChanged;

		ColorGradientWidget gradientOutput = new (3) { WidthRequest = 40 };
		gradientOutput.ClickGesture.OnPressed += HandleGradientButtonPressEvent;
		gradientOutput.ClickGesture.OnReleased += HandleGradientButtonReleaseEvent;
		gradientOutput.ValueChanged += HandleGradientOutputValueChanged;

		ColorPanelWidget colorPanelInHigh = new () { HeightRequest = 24 };
		colorPanelInHigh.ClickGesture.OnPressed += HandleColorPanelButtonPressEvent;

		ColorPanelWidget colorPanelInLow = new () {
			HeightRequest = 24,
			Valign = Gtk.Align.End,
			Vexpand = true
		};
		colorPanelInLow.ClickGesture.OnPressed += HandleColorPanelButtonPressEvent;

		ColorPanelWidget colorPanelOutLow = new () { HeightRequest = 24 };
		colorPanelOutLow.ClickGesture.OnPressed += HandleColorPanelButtonPressEvent;

		ColorPanelWidget colorPanelOutMid = new () { HeightRequest = 24 };

		ColorPanelWidget colorPanelOutHigh = new () { HeightRequest = 24 };
		colorPanelOutHigh.ClickGesture.OnPressed += HandleColorPanelButtonPressEvent;

		HistogramWidget histogramInput = new () { WidthRequest = 130, FlipHorizontal = true };
		HistogramWidget histogramOutput = new () { WidthRequest = 130 };

		Gtk.Box vboxInput = new () { Spacing = spacing };
		vboxInput.SetOrientation (Gtk.Orientation.Vertical);
		vboxInput.Append (spinInHigh);
		vboxInput.Append (colorPanelInHigh);
		vboxInput.Append (colorPanelInLow);
		vboxInput.Append (spinInLow);

		Gtk.Box hboxInput = new () { Spacing = spacing };
		hboxInput.SetOrientation (Gtk.Orientation.Horizontal);
		hboxInput.Append (vboxInput);
		hboxInput.Append (gradientInput);

		Gtk.Box vboxOutput = new () { Spacing = spacing };
		vboxOutput.SetOrientation (Gtk.Orientation.Vertical);
		vboxOutput.Append (spinOutHigh);
		vboxOutput.Append (colorPanelOutHigh);
		vboxOutput.Append (spinOutGamma);
		vboxOutput.Append (colorPanelOutMid);
		vboxOutput.Append (colorPanelOutLow);
		vboxOutput.Append (spinOutLow);

		Gtk.Box hboxOutput = new () { Spacing = spacing };
		hboxOutput.SetOrientation (Gtk.Orientation.Horizontal);
		hboxOutput.Append (gradientOutput);
		hboxOutput.Append (vboxOutput);

		// --- References to keep

		check_red = checkRed;
		check_green = checkGreen;
		check_blue = checkBlue;

		spin_in_low = spinInLow;
		spin_in_high = spinInHigh;

		spin_out_low = spinOutLow;
		spin_out_high = spinOutHigh;
		spin_out_gamma = spinOutGamma;

		gradient_input = gradientInput;
		gradient_output = gradientOutput;

		colorpanel_in_high = colorPanelInHigh;
		colorpanel_in_low = colorPanelInLow;
		colorpanel_out_low = colorPanelOutLow;
		colorpanel_out_mid = colorPanelOutMid;
		colorpanel_out_high = colorPanelOutHigh;

		histogram_input = histogramInput;
		histogram_output = histogramOutput;

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString ("Levels Adjustment");
		TransientFor = chrome.MainWindow;
		Modal = true;
		Resizable = false;

		// --- Initialization (LevelsDialog)

		this.chrome = chrome;
		this.workspace = workspace;

		EffectData = effectData;

		// --- TODO: Refactor

		button_auto = (Gtk.Button) AddButton (Translations.GetString ("Auto"), (int) Gtk.ResponseType.None);
		button_auto.OnClicked += HandleButtonAutoClicked;

		button_reset = (Gtk.Button) AddButton (Translations.GetString ("Reset"), (int) Gtk.ResponseType.None);
		button_reset.OnClicked += HandleButtonResetClicked;

		AddActionWidget (hboxChecks, (int) Gtk.ResponseType.None);

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		Gtk.Box hboxLayout = new () { Spacing = spacing };
		hboxLayout.SetOrientation (Gtk.Orientation.Horizontal);
		hboxLayout.SetAllMargins (spacing);
		hboxLayout.Append (CreateLabelledWidget (histogram_input, Translations.GetString ("Input Histogram")));
		hboxLayout.Append (CreateLabelledWidget (hboxInput, Translations.GetString ("Input")));
		hboxLayout.Append (CreateLabelledWidget (hboxOutput, Translations.GetString ("Output")));
		hboxLayout.Append (CreateLabelledWidget (histogram_output, Translations.GetString ("Output Histogram")));

		Gtk.Box contentArea = this.GetContentAreaBox ();
		contentArea.Append (hboxLayout);

		UpdateInputHistogram ();
		Reset ();
		UpdateLevels ();

		static Gtk.Box CreateLabelledWidget (Gtk.Widget widget, string label)
		{
			widget.Vexpand = true;
			widget.Valign = Gtk.Align.Fill;

			Gtk.Label label_widget = Gtk.Label.New (label);
			label_widget.Halign = Gtk.Align.Start;

			Gtk.Box vbox = new () { Spacing = spacing };
			vbox.SetOrientation (Gtk.Orientation.Vertical);
			vbox.Append (label_widget);
			vbox.Append (widget);

			return vbox;
		}
	}

	private UnaryPixelOps.Level Levels {
		get => EffectData.Levels;
		set => EffectData.Levels = value;
	}

	private void UpdateLivePreview ()
		=> EffectData.FirePropertyChanged (nameof (Levels));

	private void UpdateInputHistogram ()
	{
		var doc = workspace.ActiveDocument;

		ImageSurface surface = doc.Layers.CurrentUserLayer.Surface;
		RectangleI rect = doc.Selection.SelectionPath.GetBounds ();
		histogram_input.Histogram.UpdateHistogram (surface, rect);
		UpdateOutputHistogram ();
	}

	private void UpdateOutputHistogram ()
		=> histogram_output.Histogram.SetFromLeveledHistogram (histogram_input.Histogram, Levels);

	private void Reset ()
	{
		histogram_output.ResetHistogram ();

		spin_in_low.Value = 0;
		spin_in_high.Value = 255;
		spin_out_low.Value = 0;
		spin_out_gamma.Value = 1.0;
		spin_out_high.Value = 255;
	}

	private void HandleButtonResetClicked (object? sender, EventArgs e)
		=> Reset ();

	private void UpdateFromLevelsOp ()
	{
		disable_updating = true;

		spin_in_high.Value = MaskAvg (Levels.ColorInHigh);
		spin_in_low.Value = MaskAvg (Levels.ColorInLow);

		float gamma = MaskGamma ();
		int lo = MaskAvg (Levels.ColorOutLow);
		int hi = MaskAvg (Levels.ColorOutHigh);

		spin_out_high.Value = hi;
		spin_out_gamma.Value = gamma;
		spin_out_low.Value = lo;

		disable_updating = false;
	}

	private void HandleButtonAutoClicked (object? sender, EventArgs e)
	{
		Levels = histogram_input.Histogram.MakeLevelsAuto ();

		UpdateFromLevelsOp ();
		UpdateLevels ();
	}

	private void HandleSpinInLowValueChanged (object? sender, EventArgs e)
		=> gradient_input.SetValue (0, spin_in_low.GetValueAsInt ());

	private void HandleSpinInHighValueChanged (object? sender, EventArgs e)
		=> gradient_input.SetValue (1, spin_in_high.GetValueAsInt ());

	private void HandleSpinOutLowValueChanged (object? sender, EventArgs e)
		=> gradient_output.SetValue (0, spin_out_low.GetValueAsInt ());

	private int FromGammaValue ()
	{
		int lo = gradient_output.GetValue (0);
		int hi = gradient_output.GetValue (2);
		int med = (int) (lo + (hi - lo) * Math.Pow (0.5, spin_out_gamma.Value));
		return med;
	}

	private void HandleSpinOutGammaValueChanged (object? sender, EventArgs e)
		=> gradient_output.SetValue (1, FromGammaValue ());

	private void HandleSpinOutHighValueChanged (object? sender, EventArgs e)
		=> gradient_output.SetValue (2, spin_out_high.GetValueAsInt ());

	private int MaskAvg (ColorBgra before)
	{
		int count = 0, total = 0;

		for (int c = 0; c < 3; c++) {
			if (mask[c]) {
				total += before[c];
				count++;
			}
		}

		if (count > 0)
			return total / count;
		else
			return 0;
	}

	private ColorBgra UpdateByMask (ColorBgra before, byte val)
	{
		if (!(mask.R || mask.G || mask.B))
			return before;

		ColorBgra after = before;
		int average = -1;
		int oldaverage;

		do {
			oldaverage = average;
			average = MaskAvg (after);

			if (average == 0)
				break;

			float factor = (float) val / average;

			for (int c = 0; c < 3; c++)
				if (mask[c])
					after[c] = Utility.ClampToByte (after[c] * factor);

		} while (average != val && oldaverage != average);

		while (average != val) {
			average = MaskAvg (after);
			int diff = val - average;

			for (int c = 0; c < 3; c++)
				if (mask[c])
					after[c] = Utility.ClampToByte (after[c] + diff);
		}

		after.A = 255;
		return after;
	}

	private float MaskGamma ()
	{
		int count = 0;
		float total = 0;

		for (int c = 0; c < 3; c++) {
			if (mask[c]) {
				total += Levels.GetGamma (c);
				count++;
			}
		}

		if (count > 0)
			return total / count;
		else
			return 1;
	}

	private void UpdateGammaByMask (float val)
	{
		if (!(mask.R || mask.G || mask.B))
			return;

		float average;

		do {
			average = MaskGamma ();

			float factor = val / average;

			for (int c = 0; c < 3; c++)
				if (mask[c])
					Levels.SetGamma (c, factor * Levels.GetGamma (c));

		} while (Math.Abs (val - average) > 0.001);
	}

	private Color GetOutMidColor ()
		=> Levels.Apply (histogram_input.Histogram.GetMeanColor ()).ToCairoColor ();

	//hack to avoid recurrent invocation of UpdateLevels
	private bool disable_updating;

	//when user moves triangles inside gradient widget,
	//we don't want to redraw histogram each time Levels values change.
	//maximum number of skipped updates
	private const int Max_skip = 5;

	//skipped updates counter
	private int skip_counter = Max_skip;
	private bool button_down = false;

	private void UpdateLevels ()
	{
		if (disable_updating)
			return;

		disable_updating = true;

		if (skip_counter == Max_skip || !button_down) {

			Levels.ColorOutHigh = UpdateByMask (Levels.ColorOutHigh, (byte) spin_out_high.Value);
			Levels.ColorOutLow = UpdateByMask (Levels.ColorOutLow, (byte) spin_out_low.Value);
			UpdateGammaByMask ((float) spin_out_gamma.Value);

			Levels.ColorInHigh = UpdateByMask (Levels.ColorInHigh, (byte) spin_in_high.Value);
			Levels.ColorInLow = UpdateByMask (Levels.ColorInLow, (byte) spin_in_low.Value);

			colorpanel_in_low.CairoColor = Levels.ColorInLow.ToCairoColor ();
			colorpanel_in_high.CairoColor = Levels.ColorInHigh.ToCairoColor ();

			colorpanel_out_low.CairoColor = Levels.ColorOutLow.ToCairoColor ();
			colorpanel_out_mid.CairoColor = GetOutMidColor ();
			colorpanel_out_high.CairoColor = Levels.ColorOutHigh.ToCairoColor ();

			UpdateOutputHistogram ();
			skip_counter = 0;
		} else
			skip_counter++;

		disable_updating = false;

		UpdateLivePreview ();
	}

	private void HandleGradientButtonPressEvent (
		Gtk.GestureClick controller,
		Gtk.GestureClick.PressedSignalArgs args)
	{
		button_down = true;
	}

	private void HandleGradientButtonReleaseEvent (
		Gtk.GestureClick controller,
		Gtk.GestureClick.ReleasedSignalArgs args)
	{
		button_down = false;

		if (skip_counter != 0)
			UpdateLevels ();
	}

	private void HandleGradientInputValueChanged (object? sender, IndexEventArgs e)
	{
		int val = gradient_input.GetValue (e.Index);

		if (e.Index == 0)
			spin_in_low.Value = val;
		else
			spin_in_high.Value = val;

		UpdateLevels ();
	}

	private void HandleGradientOutputValueChanged (object? sender, IndexEventArgs e)
	{
		if (gradient_output.ValueIndex != -1 && gradient_output.ValueIndex != e.Index)
			return;

		int val = gradient_output.GetValue (e.Index);
		int hi = gradient_output.GetValue (2);
		int lo = gradient_output.GetValue (0);
		int med = FromGammaValue ();

		switch (e.Index) {
			case 0:
				spin_out_low.Value = val;
				gradient_output.SetValue (1, med);
				break;

			case 1:
				med = gradient_output.GetValue (1);
				spin_out_gamma.Value = Math.Clamp (1 / Math.Log (0.5, (med - lo) / (float) (hi - lo)), 0.1, 10.0);
				break;

			case 2:
				spin_out_high.Value = val;
				gradient_output.SetValue (1, med);
				break;
		}

		UpdateLevels ();
	}

	private void MaskChanged ()
	{
		ColorBgra max = ColorBgra.Black;

		max.Bgra |= mask.R ? (uint) 0xFF0000 : 0;
		max.Bgra |= mask.G ? (uint) 0xFF00 : 0;
		max.Bgra |= mask.B ? (uint) 0xFF : 0;

		Color maxcolor = max.ToCairoColor ();
		gradient_input.MaxColor = maxcolor;
		gradient_output.MaxColor = maxcolor;

		for (int i = 0; i < 3; i++) {
			histogram_input.SetSelected (i, mask[i]);
			histogram_output.SetSelected (i, mask[i]);
		}
	}

	private void HandleCheckRedToggled (object? sender, EventArgs e)
	{
		mask.R = check_red.Active;
		MaskChanged ();
	}

	private void HandleCheckGreenToggled (object? sender, EventArgs e)
	{
		mask.G = check_green.Active;
		MaskChanged ();
	}

	private void HandleCheckBlueToggled (object? sender, EventArgs e)
	{
		mask.B = check_blue.Active;
		MaskChanged ();
	}

	private void HandleColorPanelButtonPressEvent (
		Gtk.GestureClick controller,
		Gtk.GestureClick.PressedSignalArgs args)
	{
		if (args.NPress != 2) // double click
			return;

		ColorPanelWidget panel = (ColorPanelWidget?) controller.GetWidget () ??
				throw new Exception ("Controller widget should be non-null");

		var ccd = Gtk.ColorChooserDialog.New (Translations.GetString ("Choose Color"), chrome.MainWindow);
		ccd.UseAlpha = true;
		ccd.SetColor (panel.CairoColor);

		var response = ccd.RunBlocking ();
		if (response == Gtk.ResponseType.Ok) {
			ccd.GetColor (out var cairo_color);
			ColorBgra col = cairo_color.ToColorBgra ();

			if (panel == colorpanel_in_low) {
				Levels.ColorInLow = col;
			} else if (panel == colorpanel_in_high) {
				Levels.ColorInHigh = col;
			} else if (panel == colorpanel_out_low) {
				Levels.ColorOutLow = col;
				//                } else if (panel == colorpanelOutMid) {
				//                    ColorBgra lo = Levels.ColorInLow;
				//                    ColorBgra md = histogramInput.Histogram.GetMeanColor();
				//                    ColorBgra hi = Levels.ColorInHigh;
				//                    ColorBgra out_lo = Levels.ColorOutLow;
				//                    ColorBgra out_hi = Levels.ColorOutHigh;
				//
				//                    for (int i = 0; i < 3; i++) {
				//                        double logA = (col[i] - out_lo[i]) / (out_hi[i] - out_lo[i]);
				//                        double logBase = (md[i] - lo[i]) / (hi[i] - lo[i]);
				//                        double logVal = (logBase == 1.0) ? 0.0 : Math.Log (logA, logBase);
				//
				//                        Levels.SetGamma(i, (float)Utility.Clamp (logVal, 0.1, 10.0));
				//                    }
			} else if (panel == colorpanel_out_high) {
				Levels.ColorOutHigh = col;
			}
		}

		ccd.Destroy ();

		UpdateFromLevelsOp ();
		UpdateLevels ();
	}
}
