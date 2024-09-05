using System;
using Gtk;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class AlignmentDialog : Gtk.Dialog
{
	private readonly Gtk.ToggleButton topLeft;
	private readonly Gtk.ToggleButton topCenter;
	private readonly Gtk.ToggleButton topRight;
	private readonly Gtk.ToggleButton centerLeft;
	private readonly Gtk.ToggleButton center;
	private readonly Gtk.ToggleButton centerRight;
	private readonly Gtk.ToggleButton bottomLeft;
	private readonly Gtk.ToggleButton bottomCenter;
	private readonly Gtk.ToggleButton bottomRight;

	public AlignPosition SelectedPosition { get; private set; }

	public event EventHandler? PositionChanged;

	public AlignmentDialog (IChromeService chrome)
	{
		const int spacing = 6;

		var grid = new Gtk.Grid {
			RowSpacing = spacing,
			ColumnSpacing = spacing,
			RowHomogeneous = true,
			ColumnHomogeneous = true,
			MarginStart = 12,
			MarginEnd = 12,
			MarginTop = 12,
			MarginBottom = 12
		};

		topLeft = CreateIconButton ("Top Left", Resources.Icons.ResizeCanvasNW, AlignPosition.TopLeft);
		topCenter = CreateIconButton ("Top Center", Resources.Icons.ResizeCanvasUp, AlignPosition.TopCenter);
		topRight = CreateIconButton ("Top Right", Resources.Icons.ResizeCanvasNE, AlignPosition.TopRight);
		centerLeft = CreateIconButton ("Center Left", Resources.Icons.ResizeCanvasLeft, AlignPosition.CenterLeft);
		center = CreateIconButton ("Center", Resources.Icons.ResizeCanvasBase, AlignPosition.Center);
		centerRight = CreateIconButton ("Center Right", Resources.Icons.ResizeCanvasRight, AlignPosition.CenterRight);
		bottomLeft = CreateIconButton ("Bottom Left", Resources.Icons.ResizeCanvasSW, AlignPosition.BottomLeft);
		bottomCenter = CreateIconButton ("Bottom Center", Resources.Icons.ResizeCanvasDown, AlignPosition.BottomCenter);
		bottomRight = CreateIconButton ("Bottom Right", Resources.Icons.ResizeCanvasSE, AlignPosition.BottomRight);

		// Add buttons to the grid
		grid.Attach (topLeft, 0, 0, 1, 1);
		grid.Attach (topCenter, 1, 0, 1, 1);
		grid.Attach (topRight, 2, 0, 1, 1);
		grid.Attach (centerLeft, 0, 1, 1, 1);
		grid.Attach (center, 1, 1, 1, 1);
		grid.Attach (centerRight, 2, 1, 1, 1);
		grid.Attach (bottomLeft, 0, 2, 1, 1);
		grid.Attach (bottomCenter, 1, 2, 1, 1);
		grid.Attach (bottomRight, 2, 2, 1, 1);

		// Set the default selection
		SetSelectedPosition (AlignPosition.Center);

		var content_area = this.GetContentAreaBox ();
		content_area.Append (grid);

		Title = Translations.GetString ("Align Object");

		TransientFor = chrome.MainWindow;

		Modal = true;

		Resizable = false;

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		Show ();
	}

	private Gtk.ToggleButton CreateIconButton (string tooltip, string iconName, AlignPosition position)
	{
		var button = new Gtk.ToggleButton ();
		button.SetIconName (iconName);

		button.TooltipText = Translations.GetString (tooltip);

		button.OnClicked += (sender, args) => {
			SetSelectedPosition (position);
			PositionChanged?.Invoke (this, EventArgs.Empty);
		};

		return button;
	}

	private void SetSelectedPosition (AlignPosition position)
	{
		SelectedPosition = position;

		topLeft.SetActive (position == AlignPosition.TopLeft);
		topCenter.SetActive (position == AlignPosition.TopCenter);
		topRight.SetActive (position == AlignPosition.TopRight);
		centerLeft.SetActive (position == AlignPosition.CenterLeft);
		center.SetActive (position == AlignPosition.Center);
		centerRight.SetActive (position == AlignPosition.CenterRight);
		bottomLeft.SetActive (position == AlignPosition.BottomLeft);
		bottomCenter.SetActive (position == AlignPosition.BottomCenter);
		bottomRight.SetActive (position == AlignPosition.BottomRight);
	}

	public void RunDialog ()
	{
		Present ();

		this.OnResponse += (sender, args) => {
			Destroy ();
		};
	}
}
