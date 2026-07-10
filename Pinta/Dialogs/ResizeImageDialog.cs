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
using System.Diagnostics.CodeAnalysis;
using Pinta.Core;

namespace Pinta;

[GObject.Subclass<Gtk.Dialog>]
public sealed partial class ResizeImageDialog
{
	private Gtk.SpinButton percentage_spinner;
	private Gtk.SpinButton width_spinner;
	private Gtk.SpinButton height_spinner;
	private Gtk.CheckButton aspect_checkbox;
	private Gtk.CheckButton percentage_radio;
	private Gtk.CheckButton absolute_radio;
	private Gtk.ComboBoxText resampling_combobox;

	private IWorkspaceService workspace = null!; // NRT - set by factory method
	private ISettingsService settings = null!;

	private bool value_changing;

	const int SPACING = 6;

	[MemberNotNull (nameof (percentage_spinner), nameof (width_spinner), nameof (height_spinner))]
	[MemberNotNull (nameof (aspect_checkbox), nameof (absolute_radio), nameof (percentage_radio), nameof (resampling_combobox))]
	partial void Initialize ()
	{
		BoxStyle spacedHorizontal = new (
			orientation: Gtk.Orientation.Horizontal,
			spacing: SPACING);

		BoxStyle spacedVertical = new (
			orientation: Gtk.Orientation.Vertical,
			spacing: SPACING);

		Gtk.SpinButton percentageSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		percentageSpinner.OnValueChanged += percentageSpinner_ValueChanged;
		percentageSpinner.SetActivatesDefaultImmediate (true);

		Gtk.SpinButton widthSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		widthSpinner.OnValueChanged += widthSpinner_ValueChanged;
		widthSpinner.SetActivatesDefaultImmediate (true);

		Gtk.SpinButton heightSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		heightSpinner.OnValueChanged += heightSpinner_ValueChanged;
		heightSpinner.SetActivatesDefaultImmediate (true);

		Gtk.CheckButton aspectCheckbox = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Maintain aspect ratio"));

		Gtk.Button resetButton = Gtk.Button.NewFromIconName (Resources.StandardIcons.EditUndo);
		resetButton.WidthRequest = 24;
		resetButton.HeightRequest = 24;
		resetButton.TooltipText = Translations.GetString ("Reset to image size");
		resetButton.OnClicked += OnResetButtonClicked;

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
		absoluteRadio.BindProperty (
			Gtk.CheckButton.ActivePropertyDefinition.UnmanagedName,
			resetButton,
			Gtk.Button.SensitivePropertyDefinition.UnmanagedName,
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

		Gtk.Grid grid = Gtk.Grid.New ();
		grid.RowSpacing = SPACING;
		grid.ColumnSpacing = SPACING;
		grid.ColumnHomogeneous = false;
		grid.Attach (widthLabel, 0, 0, 1, 1);
		grid.Attach (widthSpinner, 1, 0, 1, 1);
		grid.Attach (Gtk.Label.New (Translations.GetString ("pixels")), 2, 0, 1, 1);
		grid.Attach (resetButton, 3, 0, 1, 1);
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
		Modal = true;

		IconName = Resources.Icons.ImageResize;

		DefaultWidth = 300;
		DefaultHeight = 200;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);
		OnResponse += OnDialogResponse;

		// --- Initialization

		Gtk.Box contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (12);
		contentArea.Append (mainVbox);

		// --- References to keep

		percentage_spinner = percentageSpinner;
		width_spinner = widthSpinner;
		height_spinner = heightSpinner;
		aspect_checkbox = aspectCheckbox;
		absolute_radio = absoluteRadio;
		percentage_radio = percentageRadio;
		resampling_combobox = resamplingCombobox;
	}

	private void Configure (IChromeService chrome, IWorkspaceService workspace, ISettingsService settings)
	{
		TransientFor = chrome.MainWindow;
		this.workspace = workspace;
		this.settings = settings;

		width_spinner.Value = settings.GetSetting (SettingNames.RESIZE_IMAGE_WIDTH, workspace.ImageSize.Width);
		height_spinner.Value = settings.GetSetting (SettingNames.RESIZE_IMAGE_HEIGHT, workspace.ImageSize.Height);
		percentage_spinner.Value = settings.GetSetting (SettingNames.RESIZE_IMAGE_PERCENTAGE, 100);
		aspect_checkbox.Active = settings.GetSetting (SettingNames.RESIZE_IMAGE_MAINTAIN_ASPECT, true);
		resampling_combobox.Active = settings.GetSetting (SettingNames.RESIZE_IMAGE_RESAMPLING, 0);

		// Final initialization
		if (settings.GetSetting (SettingNames.RESIZE_IMAGE_USE_PERCENTAGE, true))
			percentage_radio.Active = true;
		else
			absolute_radio.Active = true;

		percentage_spinner.GrabFocus ();
	}

	internal static ResizeImageDialog New (IChromeService chrome, IWorkspaceService workspace, ISettingsService settings)
	{
		ResizeImageDialog dialog = NewWithProperties ([]);
		dialog.Configure (chrome, workspace, settings);
		return dialog;
	}

	private void OnDialogResponse (Gtk.Dialog sender, ResponseSignalArgs args)
	{
		if (args.ResponseId != (int) Gtk.ResponseType.Ok)
			return;

		// Save settings for next time
		settings.PutSetting (SettingNames.RESIZE_IMAGE_MAINTAIN_ASPECT, aspect_checkbox.Active);
		settings.PutSetting (SettingNames.RESIZE_IMAGE_USE_PERCENTAGE, percentage_radio.Active);
		settings.PutSetting (SettingNames.RESIZE_IMAGE_PERCENTAGE, percentage_spinner.GetValueAsInt ());
		settings.PutSetting (SettingNames.RESIZE_IMAGE_WIDTH, width_spinner.GetValueAsInt ());
		settings.PutSetting (SettingNames.RESIZE_IMAGE_HEIGHT, height_spinner.GetValueAsInt ());
		settings.PutSetting (SettingNames.RESIZE_IMAGE_RESAMPLING, resampling_combobox.Active);
	}

	private static Gtk.ComboBoxText CreateResamplingCombobox ()
	{
		Gtk.ComboBoxText result = Gtk.ComboBoxText.New ();
		result.Hexpand = true;
		result.Halign = Gtk.Align.Fill;

		foreach (ResamplingMode mode in Enum.GetValues (typeof (ResamplingMode)))
			result.AppendText (mode.GetLabel ());

		result.Active = 0;

		return result;
	}

	public ResizeImageOptions GetResizeImageOptions ()
	{
		Size newSize = new (
			Width: width_spinner.GetValueAsInt (),
			Height: height_spinner.GetValueAsInt ());
		ResamplingMode resamplingMode = (ResamplingMode) resampling_combobox.Active;
		return new (newSize, resamplingMode);
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

	void OnResetButtonClicked (Gtk.Button button, EventArgs eventArgs)
	{
		value_changing = true;
		width_spinner.Value = workspace.ImageSize.Width;
		height_spinner.Value = workspace.ImageSize.Height;
		value_changing = false;
	}
}

