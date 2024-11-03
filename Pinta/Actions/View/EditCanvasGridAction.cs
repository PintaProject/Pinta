using System;
using Pinta.Core;
using Gtk;
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
		Dialog dialog = new Dialog
		{
			Title = Translations.GetString("Grid Size"),
			TransientFor = chrome.MainWindow,
			Modal = true
		};
		dialog.AddCancelOkButtons();
		dialog.SetDefaultResponse(ResponseType.Ok);

		// Create a container box for arranging widgets vertically
		var vbox = new Box { Spacing = 6 };
		vbox.SetOrientation(Orientation.Vertical);

		// Add Width SpinButton
		var widthLabel = Label.New(Translations.GetString("Grid width:"));
		widthLabel.Xalign = 0;
		var widthSpinButton = SpinButton.NewWithRange(1, 256, 1);
		widthSpinButton.Value = canvasGrid.CellWidth;

		var widthBox = new Box { Spacing = 6 };
		widthBox.SetOrientation(Orientation.Horizontal);
		widthBox.Append(widthLabel);
		widthBox.Append(widthSpinButton);

		// Add Height SpinButton
		var heightLabel = Label.New(Translations.GetString("Grid height:"));
		heightLabel.Xalign = 0;
		var heightSpinButton = SpinButton.NewWithRange(1, 256, 1);
		heightSpinButton.Value = canvasGrid.CellHeight;

		var heightBox = new Box { Spacing = 6 };
		heightBox.SetOrientation(Orientation.Horizontal);
		heightBox.Append(heightLabel);
		heightBox.Append(heightSpinButton);

		// Add both width and height boxes to the main vertical box
		vbox.Append(widthBox);
		vbox.Append(heightBox);

		// Add the main vertical box to the dialog content area
		var contentArea = dialog.GetContentAreaBox();
		contentArea.SetAllMargins(12);
		contentArea.Append(vbox);

		dialog.OnResponse += (_, args) =>
		{
			if (args.ResponseId == (int)ResponseType.Ok)
			{
				canvasGrid.CellWidth = widthSpinButton.GetValueAsInt();
				canvasGrid.CellHeight = heightSpinButton.GetValueAsInt();
			}
			dialog.Destroy();
		};

		dialog.Present();
	}
}

