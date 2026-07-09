using System;
using System.Diagnostics.CodeAnalysis;
using Pinta.Core;

namespace Pinta.Effects;

[GObject.Subclass<Gtk.Dialog>]
public sealed partial class AlignmentDialog
{
	private Gtk.ToggleButton top_left;
	private Gtk.ToggleButton top_center;
	private Gtk.ToggleButton top_right;
	private Gtk.ToggleButton center_left;
	private Gtk.ToggleButton center;
	private Gtk.ToggleButton center_right;
	private Gtk.ToggleButton bottom_left;
	private Gtk.ToggleButton bottom_center;
	private Gtk.ToggleButton bottom_right;

	public AlignPosition SelectedPosition { get; private set; }

	public event EventHandler? PositionChanged;

	[MemberNotNull (nameof (top_left), nameof (top_center), nameof (top_right))]
	[MemberNotNull (nameof (center_left), nameof (center), nameof (center_right))]
	[MemberNotNull (nameof (bottom_left), nameof (bottom_center), nameof (bottom_right))]
	partial void Initialize ()
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

		Gtk.Grid grid = Gtk.Grid.New ();
		grid.RowSpacing = spacing;
		grid.ColumnSpacing = spacing;
		grid.RowHomogeneous = true;
		grid.ColumnHomogeneous = true;
		grid.MarginStart = 12;
		grid.MarginEnd = 12;
		grid.MarginTop = 12;
		grid.MarginBottom = 12;
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
		Modal = true;
		Resizable = false;

		// --- Initialization (Gtk.Dialog)

		Gtk.Box content_area = this.GetContentAreaBox ();
		content_area.Append (grid);

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- Initialization (AlignmentDialog)

		SetSelectedPosition (AlignPosition.Center); // Set the default selection
	}

	public static AlignmentDialog New (IChromeService chrome)
	{
		AlignmentDialog dialog = NewWithProperties ([]);
		dialog.TransientFor = chrome.MainWindow;
		return dialog;
	}

	private Gtk.ToggleButton CreateIconButton (
		string tooltip,
		string iconName,
		AlignPosition position)
	{
		Gtk.ToggleButton button = Gtk.ToggleButton.New ();

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
}
