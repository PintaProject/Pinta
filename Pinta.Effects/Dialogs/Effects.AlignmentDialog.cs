using System;
using Gtk;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class AlignmentDialog : Gtk.Dialog
{
	private readonly Gtk.CheckButton topLeft;
	private readonly Gtk.CheckButton topCenter;
	private readonly Gtk.CheckButton topRight;
	private readonly Gtk.CheckButton centerLeft;
	private readonly Gtk.CheckButton center;
	private readonly Gtk.CheckButton centerRight;
	private readonly Gtk.CheckButton bottomLeft;
	private readonly Gtk.CheckButton bottomCenter;
	private readonly Gtk.CheckButton bottomRight;

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

		topLeft = CreateCheckButton ("Top Left", AlignPosition.TopLeft);
		topCenter = CreateCheckButton ("Top Center", AlignPosition.TopCenter);
		topRight = CreateCheckButton ("Top Right", AlignPosition.TopRight);
		centerLeft = CreateCheckButton ("Center Left", AlignPosition.CenterLeft);
		center = CreateCheckButton ("Center", AlignPosition.Center);
		centerRight = CreateCheckButton ("Center Right", AlignPosition.CenterRight);
		bottomLeft = CreateCheckButton ("Bottom Left", AlignPosition.BottomLeft);
		bottomCenter = CreateCheckButton ("Bottom Center", AlignPosition.BottomCenter);
		bottomRight = CreateCheckButton ("Bottom Right", AlignPosition.BottomRight);

		// Group the check buttons so they behave like radio buttons
		topCenter.SetGroup (topLeft);
		topRight.SetGroup (topLeft);
		centerLeft.SetGroup (topLeft);
		center.SetGroup (topLeft);
		centerRight.SetGroup (topLeft);
		bottomLeft.SetGroup (topLeft);
		bottomCenter.SetGroup (topLeft);
		bottomRight.SetGroup (topLeft);

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
		center.Active = true;

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

	private Gtk.CheckButton CreateCheckButton (string label, AlignPosition position)
	{
		var button = new Gtk.CheckButton { Label = label };
		button.OnToggled += (sender, args) => {
			if (button.Active) {
				SelectedPosition = position;
				PositionChanged?.Invoke (this, EventArgs.Empty);
			}
		};
		return button;
	}

	public void RunDialog ()
	{
		Present ();

		this.OnResponse += (sender, args) => {
			Destroy ();
		};
	}
}
