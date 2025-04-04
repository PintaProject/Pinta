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
		show_grid_checkbox = CreateShowGridCheckbox (initialSettings.ShowGrid, SettingsChanged);
		grid_width_spinner = CreateSpinner (initialSettings.CellSize.Width, SettingsChanged);
		grid_height_spinner = CreateSpinner (initialSettings.CellSize.Height, SettingsChanged);

		Grid grid = new () {
			RowSpacing = SPACING,
			ColumnSpacing = SPACING,
			ColumnHomogeneous = false,
		};

		grid.Attach (show_grid_checkbox, 0, 0, 2, 1);

		grid.Attach (CreateLabel (Translations.GetString ("Width:"), Align.End), 0, 1, 1, 1);
		grid.Attach (grid_width_spinner, 1, 1, 1, 1);
		grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 1, 1, 1);

		grid.Attach (CreateLabel (Translations.GetString ("Height:"), Align.End), 0, 2, 1, 1);
		grid.Attach (grid_height_spinner, 1, 2, 1, 1);
		grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 2, 1, 1);

		Box mainVbox = new () { Spacing = SPACING };
		mainVbox.SetOrientation (Orientation.Vertical);
		mainVbox.Append (grid);

		Title = Translations.GetString ("Canvas Grid Settings");
		TransientFor = chrome.MainWindow;
		Modal = true;
		IconName = Resources.Icons.ViewGrid;

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (ResponseType.Ok);

		Box contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (12);
		contentArea.Append (mainVbox);
	}

	private static CheckButton CreateShowGridCheckbox (bool active, GObject.SignalHandler<CheckButton> onValueChanged)
	{
		CheckButton result = CheckButton.NewWithLabel (Translations.GetString ("Show Grid"));
		result.Active = active;
		result.OnToggled += onValueChanged;
		return result;
	}

	private static Label CreateLabel (string text, Align horizontalAlign)
	{
		Label result = Label.New (text);
		result.Halign = horizontalAlign;
		return result;
	}

	private static SpinButton CreateSpinner (int startValue, GObject.SignalHandler<SpinButton> onValueChanged)
	{
		SpinButton result = SpinButton.NewWithRange (1, int.MaxValue, 1);
		result.Value = startValue;
		result.OnValueChanged += onValueChanged;
		result.SetActivatesDefaultImmediate (true);
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

