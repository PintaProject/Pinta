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
		canvas_grid = canvasGrid;
	}

	void IActionHandler.Initialize ()
	{
		view.EditCanvasGrid.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		view.EditCanvasGrid.Activated -= Activated;
	}

	private void Activated (object sender, EventArgs e)
	{
		CanvasGridSettingsDialog dialog = new (chrome, canvas_grid);

		dialog.OnResponse += (_, args) => {
			if (args.ResponseId == (int) Gtk.ResponseType.Ok) {
				canvas_grid.SaveGridSettings ();
			} else {
				dialog.RevertChanges ();
			}

			dialog.Destroy ();
		};

		dialog.Show ();
	}
}

