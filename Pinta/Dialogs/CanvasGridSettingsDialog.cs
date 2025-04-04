using System;
using Gtk;
using Pinta.Core;

namespace Pinta;

public sealed class CanvasGridSettingsDialog : Dialog
{
	internal event EventHandler<Settings>? Updated;

	private readonly CheckButton show_grid_checkbox;
	private readonly SpinButton grid_width_spinner;
	private readonly SpinButton grid_height_spinner;

	private const int SPACING = 6;

	internal CanvasGridSettingsDialog (ChromeManager chrome, Settings initialSettings)
	{
		CheckButton showGridCheckBox = CheckButton.NewWithLabel (Translations.GetString ("Show Grid"));
		showGridCheckBox.Active = initialSettings.ShowGrid;
		showGridCheckBox.OnToggled += SettingsChanged;

		SpinButton widthSpinner = SpinButton.NewWithRange (1, int.MaxValue, 1);
		widthSpinner.Value = initialSettings.CellSize.Width;
		widthSpinner.OnValueChanged += SettingsChanged;
		widthSpinner.SetActivatesDefaultImmediate (true);

		SpinButton heightSpinner = SpinButton.NewWithRange (1, int.MaxValue, 1);
		heightSpinner.Value = initialSettings.CellSize.Height;
		heightSpinner.OnValueChanged += SettingsChanged;
		heightSpinner.SetActivatesDefaultImmediate (true);

		Grid grid = new () {
			RowSpacing = SPACING,
			ColumnSpacing = SPACING,
			ColumnHomogeneous = false,
		};

		grid.Attach (showGridCheckBox, 0, 0, 2, 1);

		grid.Attach (CreateLabel (Translations.GetString ("Width:"), Align.End), 0, 1, 1, 1);
		grid.Attach (widthSpinner, 1, 1, 1, 1);
		grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 1, 1, 1);

		grid.Attach (CreateLabel (Translations.GetString ("Height:"), Align.End), 0, 2, 1, 1);
		grid.Attach (heightSpinner, 1, 2, 1, 1);
		grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 2, 1, 1);

		Box mainVbox = new () { Spacing = SPACING };
		mainVbox.SetOrientation (Orientation.Vertical);
		mainVbox.Append (grid);

		// --- Initialization (Gtk.Box)

		Box contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (12);
		contentArea.Append (mainVbox);

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString ("Canvas Grid Settings");
		TransientFor = chrome.MainWindow;
		Modal = true;
		IconName = Resources.Icons.ViewGrid;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (ResponseType.Ok);

		// --- References to keep

		show_grid_checkbox = showGridCheckBox;
		grid_width_spinner = widthSpinner;
		grid_height_spinner = heightSpinner;
	}

	private static Label CreateLabel (string text, Align horizontalAlign)
	{
		Label result = Label.New (text);
		result.Halign = horizontalAlign;
		return result;
	}

	private void SettingsChanged (object? sender, EventArgs e)
	{
		Settings newSettings = new (
			show_grid_checkbox.Active,
			new (
				grid_width_spinner.GetValueAsInt (),
				grid_height_spinner.GetValueAsInt ()));

		Updated?.Invoke (this, newSettings);
	}

	internal record struct Settings (bool ShowGrid, Size CellSize);
}

