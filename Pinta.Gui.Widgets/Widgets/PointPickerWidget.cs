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
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class PointPickerWidget : Gtk.Box
{
	private readonly Size image_size;
	private readonly Gtk.Label title_label;

	private readonly Gtk.Button button_reset_x;
	private readonly Gtk.Button button_reset_y;

	private readonly Gtk.SpinButton spin_x;
	private readonly Gtk.SpinButton spin_y;

	private readonly PointPickerGraphic point_picker_graphic;

	private readonly PointI adjusted_initial_point;

	bool active = true;

	public PointPickerWidget (
		Size imageSize,
		PointI initialPoint)
	{
		// --- Build

		const int spacing = 6;

		adjusted_initial_point = AdjustToWidgetSize (imageSize, initialPoint);

		// --- Section label + line

		Gtk.Label titleLabel = new ();
		titleLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Box labelAndTitle = new () { Spacing = spacing };
		labelAndTitle.SetOrientation (Gtk.Orientation.Horizontal);
		labelAndTitle.Append (titleLabel);

		// --- PointPickerGraphic

		PointPickerGraphic pointPickerGraphic = new () {
			Hexpand = true,
			Halign = Gtk.Align.Center,
		};

		// --- X spinner

		Gtk.Label xLabel = Gtk.Label.New ("X:");
		Gtk.SpinButton spinX = CreateSpinX (imageSize);
		Gtk.Button buttonResetX = CreateResetButton ();

		// --- Y spinner

		Gtk.Label yLabel = Gtk.Label.New ("Y:");
		Gtk.SpinButton spinY = CreateSpinY (imageSize);
		Gtk.Button buttonResetY = CreateResetButton ();

		// --- Vbox for spinners

		Gtk.Box xControls = new () { Spacing = spacing };
		xControls.SetOrientation (Gtk.Orientation.Horizontal);
		xControls.Append (xLabel);
		xControls.Append (spinX);
		xControls.Append (buttonResetX);

		Gtk.Box yControls = new () { Spacing = spacing };
		yControls.SetOrientation (Gtk.Orientation.Horizontal);
		yControls.Append (yLabel);
		yControls.Append (spinY);
		yControls.Append (buttonResetY);

		Gtk.Box spinnersBox = new () { Spacing = spacing };
		spinnersBox.SetOrientation (Gtk.Orientation.Vertical);
		spinnersBox.Append (xControls);
		spinnersBox.Append (yControls);

		Gtk.Box pointPickerBox = new () { Spacing = spacing };
		pointPickerBox.SetOrientation (Gtk.Orientation.Horizontal);
		pointPickerBox.Append (pointPickerGraphic);
		pointPickerBox.Append (spinnersBox);

		// --- Main layout

		SetOrientation (Gtk.Orientation.Vertical);
		Spacing = spacing;
		Append (labelAndTitle);
		Append (pointPickerBox);

		// --- References to keep

		image_size = imageSize;

		title_label = titleLabel;

		point_picker_graphic = pointPickerGraphic;

		button_reset_x = buttonResetX;
		button_reset_y = buttonResetY;

		spin_x = spinX;
		spin_y = spinY;

		OnRealize += (_, _) => HandleShown ();
	}

	private static Gtk.Button CreateResetButton ()
		=> new () {
			IconName = Resources.StandardIcons.GoPrevious,
			WidthRequest = 28,
			HeightRequest = 24,
			CanFocus = true,
			UseUnderline = true,
			Valign = Gtk.Align.Start,
		};

	private static Gtk.SpinButton CreateSpinX (Size imageSize)
	{
		Gtk.SpinButton result = Gtk.SpinButton.NewWithRange (0, 100, 1);
		result.CanFocus = true;
		result.ClimbRate = 1;
		result.Numeric = true;
		result.Adjustment!.PageIncrement = 10;
		result.Valign = Gtk.Align.Start;
		result.Adjustment!.Upper = imageSize.Width;
		result.Adjustment!.Lower = 0;
		result.SetActivatesDefaultImmediate (true);
		return result;
	}

	private static Gtk.SpinButton CreateSpinY (Size imageSize)
	{
		Gtk.SpinButton result = Gtk.SpinButton.NewWithRange (0, 100, 1);
		result.CanFocus = true;
		result.ClimbRate = 1;
		result.Numeric = true;
		result.Adjustment!.PageIncrement = 10;
		result.Valign = Gtk.Align.Start;
		result.Adjustment!.Upper = imageSize.Height;
		result.Adjustment!.Lower = 0;
		result.SetActivatesDefaultImmediate (true);
		return result;
	}

	private static PointI AdjustToWidgetSize (
		Size imageSize,
		PointI logicalPoint
	) => new (
		X: (int) ((logicalPoint.X + 1.0) * imageSize.Width / 2.0),
		Y: (int) ((logicalPoint.Y + 1.0) * imageSize.Height / 2.0));

	public string Label {
		get => title_label.GetText ();
		set => title_label.SetText (value);
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

	public CenterOffset<double> Offset => new (
		Horizontal: (spin_x.Value * 2.0 / image_size.Width) - 1.0,
		Vertical: (spin_y.Value * 2.0 / image_size.Height) - 1.0);

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
