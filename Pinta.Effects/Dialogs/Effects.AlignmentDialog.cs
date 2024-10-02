using System;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class AlignmentDialog : Gtk.Dialog
{
	private readonly Gtk.ToggleButton top_left;
	private readonly Gtk.ToggleButton top_center;
	private readonly Gtk.ToggleButton top_right;
	private readonly Gtk.ToggleButton center_left;
	private readonly Gtk.ToggleButton center;
	private readonly Gtk.ToggleButton center_right;
	private readonly Gtk.ToggleButton bottom_left;
	private readonly Gtk.ToggleButton bottom_center;
	private readonly Gtk.ToggleButton bottom_right;

	public AlignPosition SelectedPosition { get; private set; }

	public event EventHandler? PositionChanged;

	public AlignmentDialog (IChromeService chrome)
	{
		const int spacing = 6;

		// --- Control creation

		Gtk.ToggleButton topLeftToggle = CreateIconButton (Translations.GetString ("Top Left"), Resources.Icons.ResizeCanvasNW, AlignPosition.TopLeft);
		Gtk.ToggleButton topCenterToggle = CreateIconButton (Translations.GetString ("Top Center"), Resources.Icons.ResizeCanvasUp, AlignPosition.TopCenter);
		Gtk.ToggleButton topRightToggle = CreateIconButton (Translations.GetString ("Top Right"), Resources.Icons.ResizeCanvasNE, AlignPosition.TopRight);
		Gtk.ToggleButton centerLeftToggle = CreateIconButton (Translations.GetString ("Center Left"), Resources.Icons.ResizeCanvasLeft, AlignPosition.CenterLeft);
		Gtk.ToggleButton centerToggle = CreateIconButton (Translations.GetString ("Center"), Resources.Icons.ResizeCanvasBase, AlignPosition.Center);
		Gtk.ToggleButton centerRightToggle = CreateIconButton (Translations.GetString ("Center Right"), Resources.Icons.ResizeCanvasRight, AlignPosition.CenterRight);
		Gtk.ToggleButton bottomLeftToggle = CreateIconButton (Translations.GetString ("Bottom Left"), Resources.Icons.ResizeCanvasSW, AlignPosition.BottomLeft);
		Gtk.ToggleButton bottomCenterToggle = CreateIconButton (Translations.GetString ("Bottom Center"), Resources.Icons.ResizeCanvasDown, AlignPosition.BottomCenter);
		Gtk.ToggleButton bottomRightToggle = CreateIconButton (Translations.GetString ("Bottom Right"), Resources.Icons.ResizeCanvasSE, AlignPosition.BottomRight);

		Gtk.Grid grid = new () {
			RowSpacing = spacing,
			ColumnSpacing = spacing,
			RowHomogeneous = true,
			ColumnHomogeneous = true,
			MarginStart = 12,
			MarginEnd = 12,
			MarginTop = 12,
			MarginBottom = 12,
		};
		grid.Attach (topLeftToggle, 0, 0, 1, 1);
		grid.Attach (topCenterToggle, 1, 0, 1, 1);
		grid.Attach (topRightToggle, 2, 0, 1, 1);
		grid.Attach (centerLeftToggle, 0, 1, 1, 1);
		grid.Attach (centerToggle, 1, 1, 1, 1);
		grid.Attach (centerRightToggle, 2, 1, 1, 1);
		grid.Attach (bottomLeftToggle, 0, 2, 1, 1);
		grid.Attach (bottomCenterToggle, 1, 2, 1, 1);
		grid.Attach (bottomRightToggle, 2, 2, 1, 1);

		// --- References to keep

		top_left = topLeftToggle;
		top_center = topCenterToggle;
		top_right = topRightToggle;
		center_left = centerLeftToggle;
		center = centerToggle;
		center_right = centerRightToggle;
		bottom_left = bottomLeftToggle;
		bottom_center = bottomCenterToggle;
		bottom_right = bottomRightToggle;

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString ("Align Object");
		TransientFor = chrome.MainWindow;
		Modal = true;
		Resizable = false;

		// --- Initialization (Gtk.Dialog)

		Gtk.Box content_area = this.GetContentAreaBox ();
		content_area.Append (grid);

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- Initialization (AlignmentDialog)

		SetSelectedPosition (AlignPosition.Center); // Set the default selection

		// --- Initial behavior execution

		Show ();
	}

	private Gtk.ToggleButton CreateIconButton (
		string tooltip,
		string iconName,
		AlignPosition position)
	{
		Gtk.ToggleButton button = new ();

		button.SetIconName (iconName);

		button.TooltipText = tooltip;

		button.OnClicked += (_, _) => {
			SetSelectedPosition (position);
			PositionChanged?.Invoke (this, EventArgs.Empty);
		};

		return button;
	}

	private void SetSelectedPosition (AlignPosition position)
	{
		SelectedPosition = position;

		top_left.SetActive (position == AlignPosition.TopLeft);
		top_center.SetActive (position == AlignPosition.TopCenter);
		top_right.SetActive (position == AlignPosition.TopRight);
		center_left.SetActive (position == AlignPosition.CenterLeft);
		center.SetActive (position == AlignPosition.Center);
		center_right.SetActive (position == AlignPosition.CenterRight);
		bottom_left.SetActive (position == AlignPosition.BottomLeft);
		bottom_center.SetActive (position == AlignPosition.BottomCenter);
		bottom_right.SetActive (position == AlignPosition.BottomRight);
	}

	public void RunDialog ()
	{
		Present ();
		OnResponse += (_, _) => Destroy ();
	}
}
