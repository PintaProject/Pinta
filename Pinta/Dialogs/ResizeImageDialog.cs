// 
// ResizeImageDialog.cs
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
using Pinta.Core;

namespace Pinta;

public sealed class ResizeImageDialog : Gtk.Dialog
{
	private readonly Gtk.CheckButton percentage_radio;
	private readonly Gtk.SpinButton percentage_spinner;
	private readonly Gtk.SpinButton width_spinner;
	private readonly Gtk.SpinButton height_spinner;
	private readonly Gtk.CheckButton aspect_checkbox;
	private readonly Gtk.ComboBoxText resampling_combobox;

	private bool value_changing;

	const int SPACING = 6;

	public ResizeImageDialog ()
	{
		Gtk.CheckButton percentageRadio = CreatePercentageRadio ();
		Gtk.CheckButton absoluteRadio = CreateAbsoluteRadio (percentageRadio);
		Gtk.SpinButton percentageSpinner = CreatePercentageSpinner ();
		Gtk.SpinButton widthSpinner = CreateWidthSpinner ();
		Gtk.SpinButton heightSpinner = CreateHeightSpinner ();
		Gtk.CheckButton aspectCheckbox = CreateAspectCheckbox ();
		Gtk.ComboBoxText resamplingCombobox = CreateResamplingCombobox ();
		Gtk.Box hboxPercent = CreatePercentBox (percentageRadio, percentageSpinner);
		Gtk.Label widthLabel = CreateWidthLabel ();

		Gtk.Label heightLabel = CreateHeightLabel ();

		Gtk.Grid grid = new () {
			RowSpacing = SPACING,
			ColumnSpacing = SPACING,
			ColumnHomogeneous = false,
		};
		grid.Attach (widthLabel, 0, 0, 1, 1);
		grid.Attach (widthSpinner, 1, 0, 1, 1);
		grid.Attach (Gtk.Label.New (Translations.GetString ("pixels")), 2, 0, 1, 1);
		grid.Attach (heightLabel, 0, 1, 1, 1);
		grid.Attach (heightSpinner, 1, 1, 1, 1);
		grid.Attach (Gtk.Label.New (Translations.GetString ("pixels")), 2, 1, 1, 1);
		grid.Attach (aspectCheckbox, 0, 2, 3, 1);
		grid.Attach (Gtk.Label.New (Translations.GetString ("Resampling:")), 0, 3, 1, 1);
		grid.Attach (resamplingCombobox, 1, 3, 2, 1);

		Gtk.Box mainVbox = new () { Spacing = SPACING };
		mainVbox.SetOrientation (Gtk.Orientation.Vertical);
		mainVbox.Append (hboxPercent);
		mainVbox.Append (absoluteRadio);
		mainVbox.Append (grid);

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString ("Resize Image");
		TransientFor = PintaCore.Chrome.MainWindow;
		Modal = true;

		IconName = Resources.Icons.ImageResize;

		DefaultWidth = 300;
		DefaultHeight = 200;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- Initialization

		Gtk.Box contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (12);
		contentArea.Append (mainVbox);

		percentageSpinner.GrabFocus ();

		// --- References to keep

		percentage_radio = percentageRadio;
		percentage_spinner = percentageSpinner;
		width_spinner = widthSpinner;
		height_spinner = heightSpinner;
		aspect_checkbox = aspectCheckbox;
		resampling_combobox = resamplingCombobox;

		// Initialization which depends on members (via event handlers).
		percentage_radio.Active = true;
	}

	private static Gtk.CheckButton CreateAspectCheckbox ()
	{
		Gtk.CheckButton result = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Maintain aspect ratio"));
		result.Active = true;
		return result;
	}

	private static Gtk.ComboBoxText CreateResamplingCombobox ()
	{
		Gtk.ComboBoxText result = new () {
			Hexpand = true,
			Halign = Gtk.Align.Fill,
		};

		foreach (ResamplingMode mode in Enum.GetValues (typeof (ResamplingMode)))
			result.AppendText (mode.GetLabel ());

		result.Active = 0;

		return result;
	}

	private static Gtk.Box CreatePercentBox (
		Gtk.CheckButton percentageRadio,
		Gtk.SpinButton percentageSpinner)
	{
		Gtk.Box result = new () { Spacing = SPACING };
		result.SetOrientation (Gtk.Orientation.Horizontal);
		result.Append (percentageRadio);
		result.Append (percentageSpinner);
		result.Append (Gtk.Label.New ("%"));
		return result;
	}

	private static Gtk.Label CreateWidthLabel ()
	{
		Gtk.Label result = Gtk.Label.New (Translations.GetString ("Width:"));
		result.Halign = Gtk.Align.End;
		return result;
	}

	private static Gtk.Label CreateHeightLabel ()
	{
		Gtk.Label result = Gtk.Label.New (Translations.GetString ("Height:"));
		result.Halign = Gtk.Align.End;
		return result;
	}

	private Gtk.CheckButton CreatePercentageRadio ()
	{
		Gtk.CheckButton result = Gtk.CheckButton.NewWithLabel (Translations.GetString ("By percentage:"));
		result.OnToggled += percentageRadio_Toggled;
		return result;
	}

	private Gtk.CheckButton CreateAbsoluteRadio (Gtk.CheckButton percentageRadio)
	{
		Gtk.CheckButton result = Gtk.CheckButton.NewWithLabel (Translations.GetString ("By absolute size:"));
		result.SetGroup (percentageRadio);
		result.OnToggled += absoluteRadio_Toggled;
		return result;
	}

	private Gtk.SpinButton CreatePercentageSpinner ()
	{
		Gtk.SpinButton result = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		result.Value = 100;
		result.OnValueChanged += percentageSpinner_ValueChanged;
		result.SetActivatesDefaultImmediate (true);
		return result;
	}

	private Gtk.SpinButton CreateWidthSpinner ()
	{
		Gtk.SpinButton result = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		result.Value = PintaCore.Workspace.ImageSize.Width;
		result.OnValueChanged += widthSpinner_ValueChanged;
		result.SetActivatesDefaultImmediate (true);
		return result;
	}

	private Gtk.SpinButton CreateHeightSpinner ()
	{
		Gtk.SpinButton result = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		result.Value = PintaCore.Workspace.ImageSize.Height;
		result.OnValueChanged += heightSpinner_ValueChanged;
		result.SetActivatesDefaultImmediate (true);
		return result;
	}

	public void SaveChanges ()
	{
		Size newSize = new (
			Width: width_spinner.GetValueAsInt (),
			Height: height_spinner.GetValueAsInt ());

		PintaCore.Workspace.ResizeImage (
			newSize,
			(ResamplingMode) resampling_combobox.Active);
	}

	private void heightSpinner_ValueChanged (object? sender, EventArgs e)
	{
		if (value_changing)
			return;

		if (!aspect_checkbox.Active)
			return;

		value_changing = true;
		width_spinner.Value = (int) (height_spinner.Value * PintaCore.Workspace.ImageSize.Width / PintaCore.Workspace.ImageSize.Height);
		value_changing = false;
	}

	private void widthSpinner_ValueChanged (object? sender, EventArgs e)
	{
		if (value_changing)
			return;

		if (!aspect_checkbox.Active)
			return;

		value_changing = true;
		height_spinner.Value = (int) (width_spinner.Value * PintaCore.Workspace.ImageSize.Height / PintaCore.Workspace.ImageSize.Width);
		value_changing = false;
	}

	private void percentageSpinner_ValueChanged (object? sender, EventArgs e)
	{
		float proportion = percentage_spinner.GetValueAsInt () / 100f;
		width_spinner.Value = (int) (PintaCore.Workspace.ImageSize.Width * proportion);
		height_spinner.Value = (int) (PintaCore.Workspace.ImageSize.Height * proportion);
	}

	private void absoluteRadio_Toggled (object? sender, EventArgs e)
	{
		RadioToggle ();
	}

	private void percentageRadio_Toggled (object? sender, EventArgs e)
	{
		RadioToggle ();
	}

	private void RadioToggle ()
	{
		if (percentage_radio.Active) {
			percentage_spinner.Sensitive = true;

			width_spinner.Sensitive = false;
			height_spinner.Sensitive = false;
			aspect_checkbox.Sensitive = false;
		} else {
			percentage_spinner.Sensitive = false;

			width_spinner.Sensitive = true;
			height_spinner.Sensitive = true;
			aspect_checkbox.Sensitive = true;
		}
	}
}

