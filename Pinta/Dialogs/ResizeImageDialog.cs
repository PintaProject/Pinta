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
	private readonly WorkspaceManager workspace;
	private bool value_changing;

	const int SPACING = 6;

	internal ResizeImageDialog (ChromeManager chrome, WorkspaceManager workspace)
	{
		BoxStyle spacedHorizontal = new (
			orientation: Gtk.Orientation.Horizontal,
			spacing: SPACING);

		BoxStyle spacedVertical = new (
			orientation: Gtk.Orientation.Vertical,
			spacing: SPACING);

		Gtk.SpinButton percentageSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		percentageSpinner.Value = 100;
		percentageSpinner.OnValueChanged += percentageSpinner_ValueChanged;
		percentageSpinner.SetActivatesDefaultImmediate (true);

		Gtk.SpinButton widthSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		widthSpinner.Value = workspace.ImageSize.Width;
		widthSpinner.OnValueChanged += widthSpinner_ValueChanged;
		widthSpinner.SetActivatesDefaultImmediate (true);

		Gtk.SpinButton heightSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		heightSpinner.Value = workspace.ImageSize.Height;
		heightSpinner.OnValueChanged += heightSpinner_ValueChanged;
		heightSpinner.SetActivatesDefaultImmediate (true);

		Gtk.CheckButton aspectCheckbox = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Maintain aspect ratio"));
		aspectCheckbox.Active = true;

		Gtk.CheckButton percentageRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("By percentage:"));
		percentageRadio.BindProperty (
			Gtk.CheckButton.ActivePropertyDefinition.UnmanagedName,
			percentageSpinner,
			Gtk.SpinButton.SensitivePropertyDefinition.UnmanagedName,
			GObject.BindingFlags.SyncCreate);

		Gtk.CheckButton absoluteRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("By absolute size:"));
		absoluteRadio.SetGroup (percentageRadio);
		absoluteRadio.BindProperty (
			Gtk.CheckButton.ActivePropertyDefinition.UnmanagedName,
			widthSpinner,
			Gtk.SpinButton.SensitivePropertyDefinition.UnmanagedName,
			GObject.BindingFlags.SyncCreate);
		absoluteRadio.BindProperty (
			Gtk.CheckButton.ActivePropertyDefinition.UnmanagedName,
			heightSpinner,
			Gtk.SpinButton.SensitivePropertyDefinition.UnmanagedName,
			GObject.BindingFlags.SyncCreate);
		absoluteRadio.BindProperty (
			Gtk.CheckButton.ActivePropertyDefinition.UnmanagedName,
			aspectCheckbox,
			Gtk.CheckButton.SensitivePropertyDefinition.UnmanagedName,
			GObject.BindingFlags.SyncCreate);

		Gtk.ComboBoxText resamplingCombobox = CreateResamplingCombobox ();

		Gtk.Box hboxPercent = GtkExtensions.Box (
			spacedHorizontal,
			[
				percentageRadio,
				percentageSpinner,
				Gtk.Label.New ("%"),
			]);

		Gtk.Label widthLabel = Gtk.Label.New (Translations.GetString ("Width:"));
		widthLabel.Halign = Gtk.Align.End;

		Gtk.Label heightLabel = Gtk.Label.New (Translations.GetString ("Height:"));
		heightLabel.Halign = Gtk.Align.End;

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

		Gtk.Box mainVbox = GtkExtensions.Box (
			spacedVertical,
			[
				hboxPercent,
				absoluteRadio,
				grid,
			]);

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString ("Resize Image");
		TransientFor = chrome.MainWindow;
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
		this.workspace = workspace;

		// --- Initialization which depends on members (via event handlers).

		percentage_radio.Active = true;
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

	public void SaveChanges ()
	{
		Size newSize = new (
			Width: width_spinner.GetValueAsInt (),
			Height: height_spinner.GetValueAsInt ());

		workspace.ResizeImage (
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
		width_spinner.Value = (int) (height_spinner.Value * workspace.ImageSize.Width / workspace.ImageSize.Height);
		value_changing = false;
	}

	private void widthSpinner_ValueChanged (object? sender, EventArgs e)
	{
		if (value_changing)
			return;

		if (!aspect_checkbox.Active)
			return;

		value_changing = true;
		height_spinner.Value = (int) (width_spinner.Value * workspace.ImageSize.Height / workspace.ImageSize.Width);
		value_changing = false;
	}

	private void percentageSpinner_ValueChanged (object? sender, EventArgs e)
	{
		float proportion = percentage_spinner.GetValueAsInt () / 100f;
		width_spinner.Value = (int) (workspace.ImageSize.Width * proportion);
		height_spinner.Value = (int) (workspace.ImageSize.Height * proportion);
	}
}

