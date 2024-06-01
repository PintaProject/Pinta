// 
// PointPicker.cs
//  
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
// 
// Copyright (c) 2010 Olivier Dufour
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class PointPickerWidget : Box
{
	private readonly Label label;
	private readonly PointPickerGraphic point_picker_graphic;
	private readonly Button button_reset_x;
	private readonly Button button_reset_y;
	private readonly SpinButton spin_x;
	private readonly SpinButton spin_y;
	private readonly PointI adjusted_initial_point;

	bool active = true;

	public PointPickerWidget (PointI initialPoint)
	{
		// Build

		const int spacing = 6;

		adjusted_initial_point = AdjustToWidgetSize (initialPoint);

		// Section label + line
		Box labelAndTitle = new () { Spacing = spacing };
		labelAndTitle.SetOrientation (Orientation.Horizontal);


		label = new Label ();
		label.AddCssClass (AdwaitaStyles.Title4);
		labelAndTitle.Append (label);

		// PointPickerGraphic
		Box pointPickerBox = new () { Spacing = spacing };
		pointPickerBox.SetOrientation (Orientation.Horizontal);

		point_picker_graphic = new PointPickerGraphic {
			Hexpand = true,
			Halign = Align.Center
		};
		pointPickerBox.Append (point_picker_graphic);

		// X spinner
		var xLabel = Gtk.Label.New ("X:");

		spin_x = SpinButton.NewWithRange (0, 100, 1);
		spin_x.CanFocus = true;
		spin_x.ClimbRate = 1;
		spin_x.Numeric = true;
		spin_x.Adjustment!.PageIncrement = 10;
		spin_x.Valign = Align.Start;

		button_reset_x = new Button {
			IconName = Resources.StandardIcons.GoPrevious,
			WidthRequest = 28,
			HeightRequest = 24,
			CanFocus = true,
			UseUnderline = true,
			Valign = Align.Start
		};

		Box xControls = new () { Spacing = spacing };
		xControls.SetOrientation (Orientation.Horizontal);

		xControls.Append (xLabel);
		xControls.Append (spin_x);
		xControls.Append (button_reset_x);

		// Y spinner
		var yLabel = Gtk.Label.New ("Y:");

		spin_y = SpinButton.NewWithRange (0, 100, 1);
		spin_y.CanFocus = true;
		spin_y.ClimbRate = 1;
		spin_y.Numeric = true;
		spin_y.Adjustment!.PageIncrement = 10;
		spin_y.Valign = Align.Start;

		button_reset_y = new Button {
			IconName = Resources.StandardIcons.GoPrevious,
			WidthRequest = 28,
			HeightRequest = 24,
			CanFocus = true,
			UseUnderline = true,
			Valign = Align.Start
		};

		Box yControls = new () { Spacing = spacing };
		yControls.SetOrientation (Orientation.Horizontal);

		yControls.Append (yLabel);
		yControls.Append (spin_y);
		yControls.Append (button_reset_y);

		// Vbox for spinners
		Box spinnersBox = new () { Spacing = spacing };
		spinnersBox.SetOrientation (Orientation.Vertical);
		spinnersBox.Append (xControls);
		spinnersBox.Append (yControls);

		pointPickerBox.Append (spinnersBox);

		// Main layout
		SetOrientation (Orientation.Vertical);
		Spacing = spacing;
		Append (labelAndTitle);
		Append (pointPickerBox);

		// ---------------

		spin_x.Adjustment!.Upper = PintaCore.Workspace.ImageSize.Width;
		spin_y.Adjustment!.Upper = PintaCore.Workspace.ImageSize.Height;
		spin_x.Adjustment!.Lower = 0;
		spin_y.Adjustment!.Lower = 0;

		OnRealize += (_, _) => HandleShown ();

		spin_x.SetActivatesDefault (true);
		spin_y.SetActivatesDefault (true);
	}

	private static PointI AdjustToWidgetSize (PointI logicalPoint)
		=> new (
			X: (int) ((logicalPoint.X + 1.0) * PintaCore.Workspace.ImageSize.Width / 2.0),
			Y: (int) ((logicalPoint.Y + 1.0) * PintaCore.Workspace.ImageSize.Height / 2.0)
		);

	public string Label {
		get => label.GetText ();
		set => label.SetText (value);
	}

	public PointI Point {
		get => new (spin_x.GetValueAsInt (), spin_y.GetValueAsInt ());
		set {
			if (value.X == spin_x.GetValueAsInt () && value.Y == spin_y.GetValueAsInt ())
				return;

			spin_x.Value = value.X;
			spin_y.Value = value.Y;

			OnPointPicked ();
		}
	}

	public PointD Offset
		=> new (
			X: (spin_x.Value * 2.0 / PintaCore.Workspace.ImageSize.Width) - 1.0,
			Y: (spin_y.Value * 2.0 / PintaCore.Workspace.ImageSize.Height) - 1.0
		);

	private void HandlePointpickergraphic1PositionChanged (object? sender, EventArgs e)
	{
		if (Point == point_picker_graphic.Position) return;
		active = false;
		spin_x.Value = point_picker_graphic.Position.X;
		spin_y.Value = point_picker_graphic.Position.Y;
		active = true;
		OnPointPicked ();
	}

	private void HandleSpinXValueChanged (object? sender, EventArgs e)
	{
		if (!active) return;
		point_picker_graphic.Position = Point;
		OnPointPicked ();
	}

	private void HandleSpinYValueChanged (object? sender, EventArgs e)
	{
		if (!active) return;
		point_picker_graphic.Position = Point;
		OnPointPicked ();
	}

	private void HandleShown ()
	{
		Point = adjusted_initial_point;

		spin_x.OnValueChanged += HandleSpinXValueChanged;
		spin_y.OnValueChanged += HandleSpinYValueChanged;
		point_picker_graphic.PositionChanged += HandlePointpickergraphic1PositionChanged;
		button_reset_x.OnClicked += ResetX;
		button_reset_y.OnClicked += ResetY;

		point_picker_graphic.Init (adjusted_initial_point);
	}

	private void ResetX (object? sender, EventArgs e)
	{
		spin_x.Value = adjusted_initial_point.X;
	}

	private void ResetY (object? sender, EventArgs e)
	{
		spin_y.Value = adjusted_initial_point.Y;
	}

	private void OnPointPicked ()
		=> PointPicked?.Invoke (this, EventArgs.Empty);

	public event EventHandler? PointPicked;
}
