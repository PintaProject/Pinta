using System;

namespace Pinta.Core;


public interface ICanvasGridService
{
	bool ShowGrid { get; set; }
	int CellWidth { get; set; }
	int CellHeight { get; set; }

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
	}

	public void LoadGridSettings ()
	{
		ShowGrid = settings.GetSetting (SettingNames.SHOW_CANVAS_GRID, false);
		CellWidth = settings.GetSetting (SettingNames.CANVAS_GRID_WIDTH, 64);
		CellHeight = settings.GetSetting (SettingNames.CANVAS_GRID_HEIGHT, 64);
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
