using System;
using Pinta.Core;
using Pinta.Core.Managers;

namespace Pinta.Actions;

internal sealed class EditCanvasGridAction : IActionHandler
{
	private readonly ViewActions view;
	private readonly ChromeManager chrome;
	private readonly CanvasGridManager canvasGrid;

	internal EditCanvasGridAction (
		ViewActions view,
		ChromeManager chrome,
		CanvasGridManager canvasGrid)
	{
		this.view = view;
		this.chrome = chrome;
		this.canvasGrid = canvasGrid;
	}

	void IActionHandler.Initialize ()
	{
		view.EditCanvasGrid.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		view.EditCanvasGrid.Activated -= Activated;
	}

	private void Activated(object sender, EventArgs e)
	{
		CanvasGridSettingsDialog dialog = new (chrome, canvasGrid);

		dialog.OnResponse += (_, args) => {
			if (args.ResponseId == (int) Gtk.ResponseType.Ok) {
				canvasGrid.SaveGridSettings ();
			} else {
				dialog.RevertChanges ();
			}

			dialog.Destroy ();
		};

		dialog.Show ();
	}
}

