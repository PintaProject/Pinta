using System;

namespace Pinta.Core.Managers;


public interface ICanvasGridService
{
	bool ShowGrid { get; set; }
	int CellWidth { get; set; }
	int CellHeight { get; set; }

	public event EventHandler? SettingsChanged;
}


public sealed class CanvasGridManager : ICanvasGridService
{
	private bool show_grid = false;
	private int cell_width = 1;
	private int cell_height = 1;

	public bool ShowGrid {
		get => show_grid;
		set {
			show_grid = value;
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public int CellWidth {
		get => cell_width;
		set {
			cell_width = value;
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public int CellHeight {
		get => cell_height;
		set {
			cell_height = value;
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public CanvasGridManager (WorkspaceManager workspace)
	{
		// Invalidate the workspace if the grid is changed to redraw the grid
		SettingsChanged += (_, __) => {
			workspace.Invalidate();
		};
	}

	public event EventHandler? SettingsChanged;
}
