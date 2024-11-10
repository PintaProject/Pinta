using System;
using Gtk;
using Pinta.Core;
using Pinta.Core.Managers;

namespace Pinta;

public sealed class CanvasGridSettingsDialog : Dialog
{
	private readonly CanvasGridManager canvas_grid;

	private readonly bool initial_show_grid;
	private readonly int initial_grid_width;
	private readonly int initial_grid_height;

	private readonly CheckButton show_grid_checkbox;
	private readonly SpinButton grid_width_spinner;
	private readonly SpinButton grid_height_spinner;

	private const int Spacing = 6;

	internal CanvasGridSettingsDialog (
		ChromeManager chrome,
		CanvasGridManager canvasGrid)
	{
		canvas_grid = canvasGrid;

		initial_show_grid = canvas_grid.ShowGrid;
		initial_grid_width = canvas_grid.CellWidth;
		initial_grid_height = canvas_grid.CellHeight;

		show_grid_checkbox = CreateShowGridCheckbox (initial_show_grid, SettingsChanged);
		grid_width_spinner = CreateSpinner (initial_grid_width, SettingsChanged);
		grid_height_spinner = CreateSpinner (initial_grid_height, SettingsChanged);

		Grid grid = new () {
			RowSpacing = Spacing,
			ColumnSpacing = Spacing,
			ColumnHomogeneous = false,
		};

		grid.Attach (show_grid_checkbox, 0, 0, 2, 1);

		grid.Attach (CreateLabel ("Width:", Align.End), 0, 1, 1, 1);
		grid.Attach (grid_width_spinner, 1, 1, 1, 1);
		grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 1, 1, 1);

		grid.Attach (CreateLabel ("Height:", Align.End), 0, 2, 1, 1);
		grid.Attach (grid_height_spinner, 1, 2, 1, 1);
		grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 2, 1, 1);

		Box mainVbox = new () { Spacing = Spacing };
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
		Label result = Label.New (Translations.GetString (text));
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
		canvas_grid.ShowGrid = show_grid_checkbox.Active;
		canvas_grid.CellWidth = grid_width_spinner.GetValueAsInt ();
		canvas_grid.CellHeight = grid_height_spinner.GetValueAsInt ();
	}

	/// <summary>
	/// Reverts the changes that the dialog made to the canvas grid.
	/// </summary>
	public void RevertChanges ()
	{
		canvas_grid.ShowGrid = initial_show_grid;
		canvas_grid.CellWidth = initial_grid_width;
		canvas_grid.CellHeight = initial_grid_height;
	}
}

