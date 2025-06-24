using System;

namespace Pinta.Core;


public interface ICanvasGridService
{
	bool ShowGrid { get; set; }
	int CellWidth { get; set; }
	int CellHeight { get; set; }

	bool ShowAxonometricGrid { get; set; }
	int AxonometricWidth { get; set; }

	public void SaveGridSettings ();

	public void LoadGridSettings ();

	public event EventHandler SettingsChanged;
}


public sealed class CanvasGridManager : ICanvasGridService
{
	private readonly SettingsManager settings;

	private bool show_grid;
	private int cell_width;
	private int cell_height;

	private bool show_axonometric_grid;
	private int axonometric_width;

	public bool ShowGrid {
		get => show_grid;
		set => SetProperty (ref show_grid, value);
	}

	public int CellWidth {
		get => cell_width;
		set => SetProperty (ref cell_width, value);
	}

	public int CellHeight {
		get => cell_height;
		set => SetProperty (ref cell_height, value);
	}

	public bool ShowAxonometricGrid {
		get => show_axonometric_grid;
		set => SetProperty (ref show_axonometric_grid, value);
	}

	public int AxonometricWidth {
		get => axonometric_width;
		set => SetProperty (ref axonometric_width, value);
	}

	public CanvasGridManager (WorkspaceManager workspace, SettingsManager settings)
	{
		this.settings = settings;

		// Invalidate the workspace if the grid is changed to redraw the grid
		SettingsChanged += (_, __) => {
			workspace.Invalidate ();
		};

		LoadGridSettings ();
	}

	public void SaveGridSettings ()
	{
		settings.PutSetting (SettingNames.SHOW_CANVAS_GRID, ShowGrid);
		settings.PutSetting (SettingNames.CANVAS_GRID_WIDTH, CellWidth);
		settings.PutSetting (SettingNames.CANVAS_GRID_HEIGHT, CellHeight);

		settings.PutSetting (SettingNames.SHOW_CANVAS_AXONOMETRIC_GRID, ShowAxonometricGrid);
		settings.PutSetting (SettingNames.CANVAS_AXONOMETRIC_WIDTH, AxonometricWidth);
	}

	public void LoadGridSettings ()
	{
		ShowGrid = settings.GetSetting (SettingNames.SHOW_CANVAS_GRID, false);
		CellWidth = settings.GetSetting (SettingNames.CANVAS_GRID_WIDTH, 64);
		CellHeight = settings.GetSetting (SettingNames.CANVAS_GRID_HEIGHT, 64);

		ShowAxonometricGrid = settings.GetSetting (SettingNames.SHOW_CANVAS_AXONOMETRIC_GRID, false);
		AxonometricWidth = settings.GetSetting (SettingNames.CANVAS_AXONOMETRIC_WIDTH, 64);
	}

	private void SetProperty<T> (ref T field, T value)
	{
		// If the value hasn't changed, don't do anything
		if (Equals (field, value)) {
			return;
		}

		// Update the field and raise the event
		field = value;
		SettingsChanged?.Invoke (this, EventArgs.Empty);
	}

	public event EventHandler SettingsChanged;
}
