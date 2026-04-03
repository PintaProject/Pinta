using System;
using System.Diagnostics.CodeAnalysis;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta;

[GObject.Subclass<Gtk.Dialog>]
public sealed partial class CanvasGridSettingsDialog
{
	internal event EventHandler<Settings>? Updated;

	private Gtk.CheckButton show_grid_checkbox;
	private Gtk.SpinButton grid_width_spinner;
	private Gtk.SpinButton grid_height_spinner;
	private Gtk.CheckButton show_axonometric_grid_checkbox;
	private Gtk.SpinButton grid_axonometric_width_spinner;
	private AnglePickerWidget grid_axonometric_angle_picker;

	private const int SPACING = 6;

	internal static CanvasGridSettingsDialog New (ChromeManager chrome, Settings initialSettings)
	{
		// TODO - this seems to incorrectly load the settings
		CanvasGridSettingsDialog dialog = NewWithProperties ([]);
		dialog.show_grid_checkbox.Active = initialSettings.ShowGrid;
		dialog.grid_width_spinner.Value = initialSettings.CellSize.Width;
		dialog.grid_height_spinner.Value = initialSettings.CellSize.Height;
		dialog.show_axonometric_grid_checkbox.Active = initialSettings.ShowAxonometricGrid;
		dialog.grid_axonometric_angle_picker.Value = initialSettings.AxonometricAngle;
		dialog.grid_axonometric_width_spinner.Value = initialSettings.AxonometricWidth;

		dialog.TransientFor = chrome.MainWindow;

		return dialog;
	}

	[MemberNotNull (nameof (show_grid_checkbox))]
	[MemberNotNull (nameof (grid_width_spinner))]
	[MemberNotNull (nameof (grid_height_spinner))]
	[MemberNotNull (nameof (show_axonometric_grid_checkbox))]
	[MemberNotNull (nameof (grid_axonometric_width_spinner))]
	[MemberNotNull (nameof (grid_axonometric_angle_picker))]
	partial void Initialize ()
	{
		Gtk.SpinButton widthSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		widthSpinner.OnValueChanged += SettingsChanged;
		widthSpinner.SetActivatesDefaultImmediate (true);

		Gtk.SpinButton heightSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		heightSpinner.OnValueChanged += SettingsChanged;
		heightSpinner.SetActivatesDefaultImmediate (true);

		Gtk.CheckButton showGridCheckBox = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Show Grid"));
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
		axonometricWidthSpinner.OnValueChanged += SettingsChanged;
		axonometricWidthSpinner.SetActivatesDefaultImmediate (true);

		AnglePickerWidget axonometricAnglePicker = AnglePickerWidget.NewWithAngle (new (0));
		axonometricAnglePicker.Label = Translations.GetString ("Angle");
		axonometricAnglePicker.ValueChanged += SettingsChanged;

		Gtk.CheckButton showAxonometricGridCheckBox = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Show Axonometric Grid"));
		showAxonometricGridCheckBox.OnToggled += SettingsChanged;
		showAxonometricGridCheckBox.BindProperty (
			Gtk.CheckButton.ActivePropertyDefinition.UnmanagedName,
			axonometricWidthSpinner,
			Gtk.SpinButton.SensitivePropertyDefinition.UnmanagedName,
			GObject.BindingFlags.SyncCreate);
		showAxonometricGridCheckBox.BindProperty (
			Gtk.CheckButton.ActivePropertyDefinition.UnmanagedName,
			axonometricAnglePicker,
			Gtk.Widget.SensitivePropertyDefinition.UnmanagedName,
			GObject.BindingFlags.SyncCreate);

		Gtk.Grid grid = Gtk.Grid.New ();
		grid.RowSpacing = SPACING;
		grid.ColumnSpacing = SPACING;
		grid.ColumnHomogeneous = false;

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

		grid.Attach (axonometricAnglePicker, 0, 5, 3, 1);

		Gtk.Box mainVbox = Gtk.Box.New (Gtk.Orientation.Vertical, SPACING);
		mainVbox.Append (grid);

		// --- Initialization (Gtk.Box)

		Gtk.Box contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (12);
		contentArea.Append (mainVbox);

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString ("Canvas Grid Settings");
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
		grid_axonometric_angle_picker = axonometricAnglePicker;
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
			),
			grid_axonometric_width_spinner.GetValueAsInt (),
			grid_axonometric_angle_picker.Value);

		Updated?.Invoke (this, newSettings);
	}

	internal record struct Settings (
		bool ShowGrid,
		bool ShowAxonometricGrid,
		Size CellSize,
		int AxonometricWidth,
		DegreesAngle AxonometricAngle);
}

