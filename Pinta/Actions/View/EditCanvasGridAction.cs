using System;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class EditCanvasGridAction : IActionHandler
{
	private readonly ViewActions view;
	private readonly ChromeManager chrome;
	private readonly CanvasGridManager canvas_grid;

	internal EditCanvasGridAction (
		ViewActions view,
		ChromeManager chrome,
		CanvasGridManager canvasGrid)
	{
		this.view = view;
		this.chrome = chrome;
		this.canvas_grid = canvasGrid;
	}

	void IActionHandler.Initialize ()
	{
		view.EditCanvasGrid.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		view.EditCanvasGrid.Activated -= Activated;
	}

	private void HandleDialogUpdate (object? sender, CanvasGridSettingsDialog.Settings eventArgs)
	{
		canvas_grid.ShowGrid = eventArgs.ShowGrid;
		canvas_grid.CellWidth = eventArgs.CellSize.Width;
		canvas_grid.CellHeight = eventArgs.CellSize.Height;
	}

	private async void Activated (object sender, EventArgs e)
	{
		CanvasGridSettingsDialog.Settings initialSettings = new (
			canvas_grid.ShowGrid,
			new (
				canvas_grid.CellWidth,
				canvas_grid.CellHeight));

		using CanvasGridSettingsDialog dialog = new (chrome, initialSettings);

		try {
			dialog.Updated += HandleDialogUpdate;
			Gtk.ResponseType response = await dialog.RunAsync ();

			if (response == Gtk.ResponseType.Ok) {
				canvas_grid.SaveGridSettings ();
			} else {
				// Revert the changes that the dialog made to the canvas grid.
				canvas_grid.ShowGrid = initialSettings.ShowGrid;
				canvas_grid.CellWidth = initialSettings.CellSize.Width;
				canvas_grid.CellHeight = initialSettings.CellSize.Height;
			}

		} finally {
			dialog.Updated -= HandleDialogUpdate;
			dialog.Destroy ();
		}
	}
}

