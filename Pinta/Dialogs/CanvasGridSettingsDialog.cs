using System;
using Pinta.Core;

namespace Pinta;

public sealed class CanvasGridSettingsDialog : Gtk.Dialog
{
	internal event EventHandler<Settings>? Updated;

	private readonly Gtk.CheckButton show_grid_checkbox;
	private readonly Gtk.SpinButton grid_width_spinner;
	private readonly Gtk.SpinButton grid_height_spinner;
	private readonly Gtk.CheckButton show_axonometric_grid_checkbox;
	private readonly Gtk.SpinButton grid_axonometric_width_spinner;

	private const int SPACING = 6;

	internal CanvasGridSettingsDialog (ChromeManager chrome, Settings initialSettings)
	{
		Gtk.SpinButton widthSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		widthSpinner.Value = initialSettings.CellSize.Width;
		widthSpinner.OnValueChanged += SettingsChanged;
		widthSpinner.SetActivatesDefaultImmediate (true);

		Gtk.SpinButton heightSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		heightSpinner.Value = initialSettings.CellSize.Height;
		heightSpinner.OnValueChanged += SettingsChanged;
		heightSpinner.SetActivatesDefaultImmediate (true);

		Gtk.CheckButton showGridCheckBox = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Show Grid"));
		showGridCheckBox.Active = initialSettings.ShowGrid;
		showGridCheckBox.OnToggled += SettingsChanged;
		showGridCheckBox.BindProperty (
			Gtk.CheckButton.ActivePropertyDefinition.UnmanagedName,
			widthSpinner,
			Gtk.SpinButton.SensitivePropertyDefinition.UnmanagedName,
			GObject.BindingFlags.SyncCreate);
		showGridCheckBox.BindProperty (
			Gtk.CheckButton.ActivePropertyDefinition.UnmanagedName,
			heightSpinner,
			Gtk.SpinButton.SensitivePropertyDefinition.UnmanagedName,
			GObject.BindingFlags.SyncCreate);

		Gtk.SpinButton axonometricWidthSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		axonometricWidthSpinner.Value = initialSettings.AxonometricWidth;
		axonometricWidthSpinner.OnValueChanged += SettingsChanged;
		axonometricWidthSpinner.SetActivatesDefaultImmediate (true);

		Gtk.CheckButton showAxonometricGridCheckBox = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Show Axonometric Grid"));
		showAxonometricGridCheckBox.Active = initialSettings.ShowAxonometricGrid;
		showAxonometricGridCheckBox.OnToggled += SettingsChanged;
		showAxonometricGridCheckBox.BindProperty (
			Gtk.CheckButton.ActivePropertyDefinition.UnmanagedName,
			axonometricWidthSpinner,
			Gtk.SpinButton.SensitivePropertyDefinition.UnmanagedName,
			GObject.BindingFlags.SyncCreate);

		Gtk.Grid grid = new () {
			RowSpacing = SPACING,
			ColumnSpacing = SPACING,
			ColumnHomogeneous = false,
		};

		grid.Attach (showGridCheckBox, 0, 0, 2, 1);

		grid.Attach (CreateLabel (Translations.GetString ("Width:"), Gtk.Align.End), 0, 1, 1, 1);
		grid.Attach (widthSpinner, 1, 1, 1, 1);
		grid.Attach (Gtk.Label.New (Translations.GetString ("pixels")), 2, 1, 1, 1);

		grid.Attach (CreateLabel (Translations.GetString ("Height:"), Gtk.Align.End), 0, 2, 1, 1);
		grid.Attach (heightSpinner, 1, 2, 1, 1);
		grid.Attach (Gtk.Label.New (Translations.GetString ("pixels")), 2, 2, 1, 1);

		grid.Attach (showAxonometricGridCheckBox, 0, 3, 2, 1);

		grid.Attach (CreateLabel (Translations.GetString ("Width:"), Gtk.Align.End), 0, 4, 1, 1);
		grid.Attach (axonometricWidthSpinner, 1, 4, 1, 1);
		grid.Attach (Gtk.Label.New (Translations.GetString ("pixels")), 2, 4, 1, 1);

		Gtk.Box mainVbox = new () { Spacing = SPACING };
		mainVbox.SetOrientation (Gtk.Orientation.Vertical);
		mainVbox.Append (grid);

		// --- Initialization (Gtk.Box)

		Gtk.Box contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (12);
		contentArea.Append (mainVbox);

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString ("Canvas Grid Settings");
		TransientFor = chrome.MainWindow;
		Modal = true;
		IconName = Resources.Icons.ViewGrid;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- References to keep

		show_grid_checkbox = showGridCheckBox;
		grid_width_spinner = widthSpinner;
		grid_height_spinner = heightSpinner;
		show_axonometric_grid_checkbox = showAxonometricGridCheckBox;
		grid_axonometric_width_spinner = axonometricWidthSpinner;
	}

	private static Gtk.Label CreateLabel (string text, Gtk.Align horizontalAlign)
	{
		Gtk.Label result = Gtk.Label.New (text);
		result.Halign = horizontalAlign;
		return result;
	}

	private void SettingsChanged (object? sender, EventArgs e)
	{
		Settings newSettings = new (
			show_grid_checkbox.Active,
			show_axonometric_grid_checkbox.Active,
			new (
				grid_width_spinner.GetValueAsInt (),
				grid_height_spinner.GetValueAsInt ()
			), grid_axonometric_width_spinner.GetValueAsInt ());

		Updated?.Invoke (this, newSettings);
	}

	internal record struct Settings (bool ShowGrid, bool ShowAxonometricGrid, Size CellSize, int AxonometricWidth);
}

