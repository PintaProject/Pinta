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
using System.Diagnostics.CodeAnalysis;
using Cairo;
using Gtk;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public partial class LevelsDialog : Gtk.Dialog
	{
		private bool[] mask;

		private CheckButton checkRed;
		private CheckButton checkGreen;
		private CheckButton checkBlue;
		private Button buttonAuto;
		private Button buttonReset;
		private SpinButton spinInLow;
		private SpinButton spinInHigh;
		private SpinButton spinOutLow;
		private SpinButton spinOutHigh;
		private SpinButton spinOutGamma;
		private ColorGradientWidget gradientInput;
		private ColorGradientWidget gradientOutput;
		private ColorPanelWidget colorpanelInHigh;
		private ColorPanelWidget colorpanelInLow;
		private ColorPanelWidget colorpanelOutHigh;
		private ColorPanelWidget colorpanelOutMid;
		private ColorPanelWidget colorpanelOutLow;
		private HistogramWidget histogramInput;
		private HistogramWidget histogramOutput;

		public LevelsData EffectData { get; private set; }

		public LevelsDialog (LevelsData effectData)
		{
			Title = Translations.GetString ("Levels Adjustment");
			TransientFor = PintaCore.Chrome.MainWindow;
			Modal = true;

			Build ();

			EffectData = effectData;
			mask = new bool[] { true, true, true };

			UpdateInputHistogram ();
			Reset ();
			UpdateLevels ();

			checkRed.OnToggled += HandleCheckRedToggled;
			checkGreen.OnToggled += HandleCheckGreenToggled;
			checkBlue.OnToggled += HandleCheckBlueToggled;
			buttonReset.OnClicked += HandleButtonResetClicked;
			buttonAuto.OnClicked += HandleButtonAutoClicked;
			spinInLow.OnValueChanged += HandleSpinInLowValueChanged;
			spinInHigh.OnValueChanged += HandleSpinInHighValueChanged;
			spinOutLow.OnValueChanged += HandleSpinOutLowValueChanged;
			spinOutGamma.OnValueChanged += HandleSpinOutGammaValueChanged;
			spinOutHigh.OnValueChanged += HandleSpinOutHighValueChanged;
			gradientInput.ValueChanged += HandleGradientInputValueChanged;
			gradientOutput.ValueChanged += HandleGradientOutputValueChanged;
			gradientInput.ClickGesture.OnReleased += HandleGradientButtonReleaseEvent;
			gradientOutput.ClickGesture.OnReleased += HandleGradientButtonReleaseEvent;
			gradientInput.ClickGesture.OnPressed += HandleGradientButtonPressEvent;
			gradientOutput.ClickGesture.OnPressed += HandleGradientButtonPressEvent;
			colorpanelInLow.ClickGesture.OnPressed += HandleColorPanelButtonPressEvent;
			colorpanelInHigh.ClickGesture.OnPressed += HandleColorPanelButtonPressEvent;
			colorpanelOutLow.ClickGesture.OnPressed += HandleColorPanelButtonPressEvent;
			colorpanelOutHigh.ClickGesture.OnPressed += HandleColorPanelButtonPressEvent;

			spinInLow.SetActivatesDefault (true);
			spinInHigh.SetActivatesDefault (true);
			spinOutGamma.SetActivatesDefault (true);
			spinOutLow.SetActivatesDefault (true);
			spinOutHigh.SetActivatesDefault (true);
		}

		private UnaryPixelOps.Level Levels {
			get {
				if (EffectData == null)
					throw new InvalidOperationException ("Effect data not set on levels dialog.");

				return EffectData.Levels;
			}

			set {
				if (value == null)
					throw new ArgumentNullException ();

				EffectData.Levels = value;
			}
		}

		private void UpdateLivePreview ()
		{
			if (EffectData != null)
				EffectData.FirePropertyChanged ("Levels");
		}

		private void UpdateInputHistogram ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			ImageSurface surface = doc.Layers.CurrentUserLayer.Surface;
			RectangleI rect = doc.Selection.SelectionPath.GetBounds ();
			histogramInput.Histogram.UpdateHistogram (surface, rect);
			UpdateOutputHistogram ();
		}

		private void UpdateOutputHistogram ()
		{
			histogramOutput.Histogram.SetFromLeveledHistogram (histogramInput.Histogram, Levels);
		}

		private void Reset ()
		{
			histogramOutput.ResetHistogram ();

			spinInLow.Value = 0;
			spinInHigh.Value = 255;
			spinOutLow.Value = 0;
			spinOutGamma.Value = 1.0;
			spinOutHigh.Value = 255;
		}

		private void HandleButtonResetClicked (object? sender, EventArgs e)
		{
			Reset ();
		}

		private void UpdateFromLevelsOp ()
		{
			disable_updating = true;

			spinInHigh.Value = MaskAvg (Levels.ColorInHigh);
			spinInLow.Value = MaskAvg (Levels.ColorInLow);

			float gamma = MaskGamma ();
			int lo = MaskAvg (Levels.ColorOutLow);
			int hi = MaskAvg (Levels.ColorOutHigh);

			spinOutHigh.Value = hi;
			spinOutGamma.Value = gamma;
			spinOutLow.Value = lo;

			disable_updating = false;
		}

		private void HandleButtonAutoClicked (object? sender, EventArgs e)
		{
			Levels = histogramInput.Histogram.MakeLevelsAuto ();

			UpdateFromLevelsOp ();
			UpdateLevels ();
		}

		private void HandleSpinInLowValueChanged (object? sender, EventArgs e)
		{
			gradientInput.SetValue (0, spinInLow.GetValueAsInt ());
		}

		private void HandleSpinInHighValueChanged (object? sender, EventArgs e)
		{
			gradientInput.SetValue (1, spinInHigh.GetValueAsInt ());
		}

		private void HandleSpinOutLowValueChanged (object? sender, EventArgs e)
		{
			gradientOutput.SetValue (0, spinOutLow.GetValueAsInt ());
		}

		private int FromGammaValue ()
		{
			int lo = gradientOutput.GetValue (0);
			int hi = gradientOutput.GetValue (2);
			int med = (int) (lo + (hi - lo) * Math.Pow (0.5, spinOutGamma.Value));

			return med;
		}

		private void HandleSpinOutGammaValueChanged (object? sender, EventArgs e)
		{
			gradientOutput.SetValue (1, FromGammaValue ());
		}

		private void HandleSpinOutHighValueChanged (object? sender, EventArgs e)
		{
			gradientOutput.SetValue (2, spinOutHigh.GetValueAsInt ());
		}

		private int MaskAvg (ColorBgra before)
		{
			int count = 0, total = 0;

			for (int c = 0; c < 3; c++) {
				if (mask[c]) {
					total += before[c];
					count++;
				}
			}

			if (count > 0) {
				return total / count;
			} else {
				return 0;
			}
		}

		private ColorBgra UpdateByMask (ColorBgra before, byte val)
		{
			ColorBgra after = before;
			int average = -1, oldaverage = -1;

			if (!(mask[0] || mask[1] || mask[2])) {
				return before;
			}

			do {
				float factor;

				oldaverage = average;
				average = MaskAvg (after);

				if (average == 0) {
					break;
				}
				factor = (float) val / average;

				for (int c = 0; c < 3; c++) {
					if (mask[c]) {
						after[c] = (byte) Utility.ClampToByte (after[c] * factor);
					}
				}
			} while (average != val && oldaverage != average);

			while (average != val) {
				average = MaskAvg (after);
				int diff = val - average;

				for (int c = 0; c < 3; c++) {
					if (mask[c]) {
						after[c] = (byte) Utility.ClampToByte (after[c] + diff);
					}
				}
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

			if (count > 0) {
				return total / count;
			} else {
				return 1;
			}

		}

		private void UpdateGammaByMask (float val)
		{
			float average = -1;

			if (!(mask[0] || mask[1] || mask[2]))
				return;

			do {
				average = MaskGamma ();
				float factor = val / average;

				for (int c = 0; c < 3; c++) {
					if (mask[c]) {
						Levels.SetGamma (c, factor * Levels.GetGamma (c));
					}
				}
			} while (Math.Abs (val - average) > 0.001);
		}

		private Color GetOutMidColor ()
		{
			return Levels.Apply (histogramInput.Histogram.GetMeanColor ()).ToCairoColor ();
		}

		//hack to avoid reccurent invocation of UpdateLevels
		private bool disable_updating;
		//when user moves triangles inside gradient widget,
		//we don't want to redraw histogram each time Levels values change.
		//maximum number of skipped updates
		private const int max_skip = 5;
		//skipped updates counter
		private int skip_counter = max_skip;
		private bool button_down = false;

		private void UpdateLevels ()
		{
			if (disable_updating)
				return;

			disable_updating = true;

			if (skip_counter == max_skip || !button_down) {

				Levels.ColorOutHigh = UpdateByMask (Levels.ColorOutHigh, (byte) spinOutHigh.Value);
				Levels.ColorOutLow = UpdateByMask (Levels.ColorOutLow, (byte) spinOutLow.Value);
				UpdateGammaByMask ((float) spinOutGamma.Value);

				Levels.ColorInHigh = UpdateByMask (Levels.ColorInHigh, (byte) spinInHigh.Value);
				Levels.ColorInLow = UpdateByMask (Levels.ColorInLow, (byte) spinInLow.Value);

				colorpanelInLow.CairoColor = Levels.ColorInLow.ToCairoColor ();
				colorpanelInHigh.CairoColor = Levels.ColorInHigh.ToCairoColor ();

				colorpanelOutLow.CairoColor = Levels.ColorOutLow.ToCairoColor ();
				colorpanelOutMid.CairoColor = GetOutMidColor ();
				colorpanelOutHigh.CairoColor = Levels.ColorOutHigh.ToCairoColor ();

				UpdateOutputHistogram ();
				skip_counter = 0;
			} else
				skip_counter++;

			QueueDraw ();
			disable_updating = false;

			UpdateLivePreview ();
		}

		private void HandleGradientButtonPressEvent (GestureClick controller, GestureClick.PressedSignalArgs args)
		{
			button_down = true;
		}

		private void HandleGradientButtonReleaseEvent (GestureClick controller, GestureClick.ReleasedSignalArgs args)
		{
			button_down = false;

			if (skip_counter != 0)
				UpdateLevels ();
		}

		private void HandleGradientInputValueChanged (object? sender, IndexEventArgs e)
		{
			int val = gradientInput.GetValue (e.Index);

			if (e.Index == 0)
				spinInLow.Value = val;
			else
				spinInHigh.Value = val;

			UpdateLevels ();
		}

		private void HandleGradientOutputValueChanged (object? sender, IndexEventArgs e)
		{
			if (gradientOutput.ValueIndex != -1 && gradientOutput.ValueIndex != e.Index)
				return;

			int val = gradientOutput.GetValue (e.Index);
			int hi = gradientOutput.GetValue (2);
			int lo = gradientOutput.GetValue (0);
			int med = FromGammaValue ();

			switch (e.Index) {
				case 0:
					spinOutLow.Value = val;
					gradientOutput.SetValue (1, med);
					break;

				case 1:
					med = gradientOutput.GetValue (1);
					spinOutGamma.Value = Utility.Clamp (1 / Math.Log (0.5, (float) (med - lo) / (float) (hi - lo)), 0.1, 10.0);
					break;

				case 2:
					spinOutHigh.Value = val;
					gradientOutput.SetValue (1, med);
					break;
			}

			UpdateLevels ();
		}

		private void MaskChanged ()
		{
			ColorBgra max = ColorBgra.Black;

			max.Bgra |= mask[0] ? (uint) 0xFF0000 : 0;
			max.Bgra |= mask[1] ? (uint) 0xFF00 : 0;
			max.Bgra |= mask[2] ? (uint) 0xFF : 0;

			Color maxcolor = max.ToCairoColor ();
			gradientInput.MaxColor = maxcolor;
			gradientOutput.MaxColor = maxcolor;

			for (int i = 0; i < 3; i++) {
				histogramInput.SetSelected (i, mask[i]);
				histogramOutput.SetSelected (i, mask[i]);
			}

			QueueDraw ();
		}

		private void HandleCheckRedToggled (object? sender, EventArgs e)
		{
			mask[0] = checkRed.Active;
			MaskChanged ();
		}

		private void HandleCheckGreenToggled (object? sender, EventArgs e)
		{
			mask[1] = checkGreen.Active;
			MaskChanged ();
		}

		private void HandleCheckBlueToggled (object? sender, EventArgs e)
		{
			mask[2] = checkBlue.Active;
			MaskChanged ();
		}

		private void HandleColorPanelButtonPressEvent (GestureClick controller, GestureClick.PressedSignalArgs args)
		{
			if (args.NPress != 2) // double click
				return;

			ColorPanelWidget panel = (ColorPanelWidget) controller.GetWidget ();
			var ccd = Gtk.ColorChooserDialog.New (Translations.GetString ("Choose Color"), PintaCore.Chrome.MainWindow);
			ccd.UseAlpha = true;
			ccd.SetColor (panel.CairoColor);

			var response = (Gtk.ResponseType) ccd.RunBlocking ();
			if (response == Gtk.ResponseType.Ok) {
				ccd.GetColor (out var cairo_color);
				ColorBgra col = cairo_color.ToColorBgra ();

				if (panel == colorpanelInLow) {
					Levels.ColorInLow = col;
				} else if (panel == colorpanelInHigh) {
					Levels.ColorInHigh = col;
				} else if (panel == colorpanelOutLow) {
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
				} else if (panel == colorpanelOutHigh) {
					Levels.ColorOutHigh = col;
				}
			}

			ccd.Destroy ();

			UpdateFromLevelsOp ();
			UpdateLevels ();
		}

		[MemberNotNull (nameof (checkRed), nameof (checkGreen), nameof (checkBlue), nameof (buttonReset), nameof (buttonAuto), nameof (spinInLow), nameof (spinInHigh),
				nameof (spinOutLow), nameof (spinOutHigh), nameof (spinOutGamma), nameof (gradientInput), nameof (gradientOutput), nameof (colorpanelInHigh),
				nameof (colorpanelInLow), nameof (colorpanelOutLow), nameof (colorpanelOutMid), nameof (colorpanelOutHigh), nameof (histogramInput), nameof (histogramOutput))]
		private void Build ()
		{
			const int spacing = 6;

			Resizable = false;

			var hboxChecks = new Box () { Orientation = Orientation.Horizontal, Spacing = spacing };
			checkRed = new CheckButton () { Label = Translations.GetString ("Red"), Active = true };
			hboxChecks.Append (checkRed);
			checkGreen = new CheckButton () { Label = Translations.GetString ("Green"), Active = true };
			hboxChecks.Append (checkGreen);
			checkBlue = new CheckButton () { Label = Translations.GetString ("Blue"), Active = true };
			hboxChecks.Append (checkBlue);

			buttonAuto = (Button) AddButton (Translations.GetString ("Auto"), (int) ResponseType.None);
			buttonReset = (Button) AddButton (Translations.GetString ("Reset"), (int) ResponseType.None);
			AddActionWidget (hboxChecks, (int) ResponseType.None);

			this.AddCancelOkButtons ();
			this.SetDefaultResponse (ResponseType.Ok);

			spinInLow = SpinButton.NewWithRange (0, 254, 1);
			spinInHigh = SpinButton.NewWithRange (1, 255, 1);
			spinInHigh.Value = 255;

			spinOutLow = SpinButton.NewWithRange (0, 252, 1);
			spinOutHigh = SpinButton.NewWithRange (2, 255, 1);
			spinOutHigh.Value = 255;
			spinOutGamma = SpinButton.NewWithRange (0, 100, 0.1);
			spinOutGamma.Value = 1;

			gradientInput = new ColorGradientWidget (2) { WidthRequest = 40 };
			gradientOutput = new ColorGradientWidget (3) { WidthRequest = 40 };

			colorpanelInHigh = new ColorPanelWidget () { HeightRequest = 24 };
			colorpanelInLow = new ColorPanelWidget () { HeightRequest = 24 };
			colorpanelOutLow = new ColorPanelWidget () { HeightRequest = 24 };
			colorpanelOutMid = new ColorPanelWidget () { HeightRequest = 24 };
			colorpanelOutHigh = new ColorPanelWidget () { HeightRequest = 24 };

			histogramInput = new HistogramWidget () { WidthRequest = 130, FlipHorizontal = true };
			histogramOutput = new HistogramWidget () { WidthRequest = 130 };

			var hboxLayout = new Box () { Orientation = Orientation.Horizontal, Spacing = spacing };
			hboxLayout.SetAllMargins (spacing);

			static Box CreateLabelledWidget (Widget widget, string label)
			{
				var vbox = new Box () { Orientation = Orientation.Vertical, Spacing = spacing };
				var label_widget = Label.New (label);
				label_widget.Halign = Align.Start;
				vbox.Append (label_widget);
				widget.Vexpand = true;
				widget.Valign = Align.Fill;
				vbox.Append (widget);

				return vbox;
			}

			hboxLayout.Append (CreateLabelledWidget (histogramInput, Translations.GetString ("Input Histogram")));

			var vboxInput = new Box () { Orientation = Orientation.Vertical, Spacing = spacing };
			vboxInput.Append (spinInHigh);
			vboxInput.Append (colorpanelInHigh);
			colorpanelInLow.Valign = Align.End;
			colorpanelInLow.Vexpand = true;
			vboxInput.Append (colorpanelInLow);
			vboxInput.Append (spinInLow);

			var hboxInput = new Box () { Orientation = Orientation.Horizontal, Spacing = spacing };
			hboxInput.Append (vboxInput);
			hboxInput.Append (gradientInput);

			hboxLayout.Append (CreateLabelledWidget (hboxInput, Translations.GetString ("Input")));

			var vboxOutput = new Box () { Orientation = Orientation.Vertical, Spacing = spacing };
			vboxOutput.Append (spinOutHigh);
			vboxOutput.Append (colorpanelOutHigh);
			vboxOutput.Append (spinOutGamma);
			vboxOutput.Append (colorpanelOutMid);
			vboxOutput.Append (colorpanelOutLow);
			vboxOutput.Append (spinOutLow);

			var hboxOutput = new Box () { Orientation = Orientation.Horizontal, Spacing = spacing };
			hboxOutput.Append (gradientOutput);
			hboxOutput.Append (vboxOutput);

			hboxLayout.Append (CreateLabelledWidget (hboxOutput, Translations.GetString ("Output")));

			hboxLayout.Append (CreateLabelledWidget (histogramOutput, Translations.GetString ("Output Histogram")));

			var content_area = this.GetContentAreaBox ();
			content_area.Append (hboxLayout);
		}
	}
}
