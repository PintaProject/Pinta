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
using Gtk;
using Pinta.Core;

namespace Pinta;

public sealed class ResizeImageDialog : Dialog
{
	private readonly CheckButton percentage_radio;
	private readonly CheckButton absolute_radio;
	private readonly SpinButton percentage_spinner;
	private readonly SpinButton width_spinner;
	private readonly SpinButton height_spinner;
	private readonly CheckButton aspect_checkbox;
	private readonly ComboBoxText resampling_combobox;

	private bool value_changing;

	public ResizeImageDialog ()
	{
		Title = Translations.GetString ("Resize Image");
		TransientFor = PintaCore.Chrome.MainWindow;
		Modal = true;
		this.AddCancelOkButtons ();
		this.SetDefaultResponse (ResponseType.Ok);

		IconName = Resources.Icons.ImageResize;

		DefaultWidth = 300;
		DefaultHeight = 200;

		percentage_radio = CheckButton.NewWithLabel (Translations.GetString ("By percentage:"));
		absolute_radio = CheckButton.NewWithLabel (Translations.GetString ("By absolute size:"));
		absolute_radio.SetGroup (percentage_radio);

		percentage_spinner = SpinButton.NewWithRange (1, int.MaxValue, 1);
		width_spinner = SpinButton.NewWithRange (1, int.MaxValue, 1);
		height_spinner = SpinButton.NewWithRange (1, int.MaxValue, 1);

		aspect_checkbox = CheckButton.NewWithLabel (Translations.GetString ("Maintain aspect ratio"));

		resampling_combobox = new () { Hexpand = true, Halign = Align.Fill };
		foreach (ResamplingMode mode in Enum.GetValues (typeof (ResamplingMode)))
			resampling_combobox.AppendText (mode.GetLabel ());

		resampling_combobox.Active = 0;

		const int spacing = 6;
		var main_vbox = new Box { Spacing = spacing };
		main_vbox.SetOrientation (Orientation.Vertical);

		var hbox_percent = new Box { Spacing = spacing };
		hbox_percent.SetOrientation (Orientation.Horizontal);
		hbox_percent.Append (percentage_radio);
		hbox_percent.Append (percentage_spinner);
		hbox_percent.Append (Label.New ("%"));
		main_vbox.Append (hbox_percent);

		main_vbox.Append (absolute_radio);

		var grid = new Grid { RowSpacing = spacing, ColumnSpacing = spacing, ColumnHomogeneous = false };
		var width_label = Label.New (Translations.GetString ("Width:"));
		width_label.Halign = Align.End;
		grid.Attach (width_label, 0, 0, 1, 1);
		grid.Attach (width_spinner, 1, 0, 1, 1);
		grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 0, 1, 1);

		var height_label = Label.New (Translations.GetString ("Height:"));
		height_label.Halign = Align.End;
		grid.Attach (height_label, 0, 1, 1, 1);
		grid.Attach (height_spinner, 1, 1, 1, 1);
		grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 1, 1, 1);

		grid.Attach (aspect_checkbox, 0, 2, 3, 1);

		grid.Attach (Label.New (Translations.GetString ("Resampling:")), 0, 3, 1, 1);
		grid.Attach (resampling_combobox, 1, 3, 2, 1);

		main_vbox.Append (grid);

		var content_area = this.GetContentAreaBox ();
		content_area.SetAllMargins (12);
		content_area.Append (main_vbox);

		aspect_checkbox.Active = true;

		width_spinner.Value = PintaCore.Workspace.ImageSize.Width;
		height_spinner.Value = PintaCore.Workspace.ImageSize.Height;

		percentage_radio.OnToggled += percentageRadio_Toggled;
		absolute_radio.OnToggled += absoluteRadio_Toggled;
		percentage_radio.Active = true;

		percentage_spinner.Value = 100;
		percentage_spinner.OnValueChanged += percentageSpinner_ValueChanged;

		width_spinner.OnValueChanged += widthSpinner_ValueChanged;
		height_spinner.OnValueChanged += heightSpinner_ValueChanged;

		width_spinner.SetActivatesDefault (true);
		height_spinner.SetActivatesDefault (true);
		percentage_spinner.SetActivatesDefault (true);

		percentage_spinner.GrabFocus ();
	}

	#region Public Methods
	public void SaveChanges ()
	{
		var resamplingMode = (ResamplingMode) resampling_combobox.Active;

		Size newSize = new (
			Width: width_spinner.GetValueAsInt (),
			Height: height_spinner.GetValueAsInt ()
		);

		PintaCore.Workspace.ResizeImage (newSize, resamplingMode);
	}
	#endregion

	#region Private Methods
	private void heightSpinner_ValueChanged (object? sender, EventArgs e)
	{
		if (value_changing)
			return;

		if (!aspect_checkbox.Active)
			return;

		value_changing = true;
		width_spinner.Value = (int) ((height_spinner.Value * PintaCore.Workspace.ImageSize.Width) / PintaCore.Workspace.ImageSize.Height);
		value_changing = false;
	}

	private void widthSpinner_ValueChanged (object? sender, EventArgs e)
	{
		if (value_changing)
			return;

		if (!aspect_checkbox.Active)
			return;

		value_changing = true;
		height_spinner.Value = (int) ((width_spinner.Value * PintaCore.Workspace.ImageSize.Height) / PintaCore.Workspace.ImageSize.Width);
		value_changing = false;
	}

	private void percentageSpinner_ValueChanged (object? sender, EventArgs e)
	{
		width_spinner.Value = (int) (PintaCore.Workspace.ImageSize.Width * (percentage_spinner.GetValueAsInt () / 100f));
		height_spinner.Value = (int) (PintaCore.Workspace.ImageSize.Height * (percentage_spinner.GetValueAsInt () / 100f));
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

	#endregion
}

