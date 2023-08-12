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
using System.Diagnostics.CodeAnalysis;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public class PointPickerWidget : Box
{
	private Label label;
	private PointPickerGraphic pointpickergraphic1;
	private Button button1;
	private Button button2;
	private SpinButton spin_x;
	private SpinButton spin_y;

	bool active = true;

	public PointPickerWidget ()
	{
		Build ();

		spin_x.Adjustment!.Upper = PintaCore.Workspace.ImageSize.Width;
		spin_y.Adjustment!.Upper = PintaCore.Workspace.ImageSize.Height;
		spin_x.Adjustment!.Lower = 0;
		spin_y.Adjustment!.Lower = 0;

		OnRealize += (_, _) => HandleShown ();

		spin_x.SetActivatesDefault (true);
		spin_y.SetActivatesDefault (true);
	}

	public string Label {
		get => label.GetText ();
		set => label.SetText (value);
	}

	public PointI DefaultPoint { get; set; }

	public PointI Point {
		get => new (spin_x.GetValueAsInt (), spin_y.GetValueAsInt ());
		set {
			if (value.X != spin_x.GetValueAsInt () || value.Y != spin_y.GetValueAsInt ()) {
				spin_x.Value = value.X;
				spin_y.Value = value.Y;
				OnPointPicked ();
			}
		}
	}

	public PointD DefaultOffset {
		get => new PointD ((DefaultPoint.X * 2.0 / PintaCore.Workspace.ImageSize.Width) - 1.0,
						 (DefaultPoint.Y * 2.0 / PintaCore.Workspace.ImageSize.Height) - 1.0);
		set => DefaultPoint = new PointI ((int) ((value.X + 1.0) * PintaCore.Workspace.ImageSize.Width / 2.0),
						      (int) ((value.Y + 1.0) * PintaCore.Workspace.ImageSize.Height / 2.0));
	}

	public PointD Offset
		=> new ((spin_x.Value * 2.0 / PintaCore.Workspace.ImageSize.Width) - 1.0, (spin_y.Value * 2.0 / PintaCore.Workspace.ImageSize.Height) - 1.0);

	private void HandlePointpickergraphic1PositionChanged (object? sender, EventArgs e)
	{
		if (Point != pointpickergraphic1.Position) {
			active = false;
			spin_x.Value = pointpickergraphic1.Position.X;
			spin_y.Value = pointpickergraphic1.Position.Y;
			active = true;
			OnPointPicked ();
		}
	}

	private void HandleSpinXValueChanged (object? sender, EventArgs e)
	{
		if (active) {
			pointpickergraphic1.Position = Point;
			OnPointPicked ();
		}
	}

	private void HandleSpinYValueChanged (object? sender, EventArgs e)
	{
		if (active) {
			pointpickergraphic1.Position = Point;
			OnPointPicked ();
		}
	}

	private void HandleShown ()
	{
		Point = DefaultPoint;

		spin_x.OnValueChanged += HandleSpinXValueChanged;
		spin_y.OnValueChanged += HandleSpinYValueChanged;
		pointpickergraphic1.PositionChanged += HandlePointpickergraphic1PositionChanged;
		button1.OnClicked += HandleButton1Pressed;
		button2.OnClicked += HandleButton2Pressed;

		pointpickergraphic1.Init (DefaultPoint);
	}

	private void HandleButton1Pressed (object? sender, EventArgs e)
	{
		spin_x.Value = DefaultPoint.X;
	}

	private void HandleButton2Pressed (object? sender, EventArgs e)
	{
		spin_y.Value = DefaultPoint.Y;
	}

	protected void OnPointPicked () => PointPicked?.Invoke (this, EventArgs.Empty);

	public event EventHandler? PointPicked;

	[MemberNotNull (nameof (label), nameof (spin_x), nameof (spin_y), nameof (button1), nameof (button2), nameof (pointpickergraphic1))]
	private void Build ()
	{
		const int spacing = 6;

		// Section label + line
		var hbox1 = new Box () { Spacing = spacing };
		hbox1.SetOrientation (Orientation.Horizontal);


		label = new Label ();
		label.AddCssClass (AdwaitaStyles.Title4);
		hbox1.Append (label);

		// PointPickerGraphic
		var hbox2 = new Box () { Spacing = spacing };
		hbox2.SetOrientation (Orientation.Horizontal);

		pointpickergraphic1 = new PointPickerGraphic {
			Hexpand = true,
			Halign = Align.Center
		};
		hbox2.Append (pointpickergraphic1);

		// X spinner
		var label2 = Gtk.Label.New ("X:");

		spin_x = SpinButton.NewWithRange (0, 100, 1);
		spin_x.CanFocus = true;
		spin_x.ClimbRate = 1;
		spin_x.Numeric = true;
		spin_x.Adjustment!.PageIncrement = 10;
		spin_x.Valign = Align.Start;

		button1 = new Button {
			IconName = Resources.StandardIcons.GoPrevious,
			WidthRequest = 28,
			HeightRequest = 24,
			CanFocus = true,
			UseUnderline = true,
			Valign = Align.Start
		};

		var x_hbox = new Box () { Spacing = spacing };
		x_hbox.SetOrientation (Orientation.Horizontal);

		x_hbox.Append (label2);
		x_hbox.Append (spin_x);
		x_hbox.Append (button1);

		// Y spinner
		var label3 = Gtk.Label.New ("Y:");

		spin_y = SpinButton.NewWithRange (0, 100, 1);
		spin_y.CanFocus = true;
		spin_y.ClimbRate = 1;
		spin_y.Numeric = true;
		spin_y.Adjustment!.PageIncrement = 10;
		spin_y.Valign = Align.Start;

		button2 = new Button {
			IconName = Resources.StandardIcons.GoPrevious,
			WidthRequest = 28,
			HeightRequest = 24,
			CanFocus = true,
			UseUnderline = true,
			Valign = Align.Start
		};

		var y_hbox = new Box () { Spacing = spacing };
		y_hbox.SetOrientation (Orientation.Horizontal);

		y_hbox.Append (label3);
		y_hbox.Append (spin_y);
		y_hbox.Append (button2);

		// Vbox for spinners
		var spin_vbox = new Box () { Spacing = spacing };
		spin_vbox.SetOrientation (Orientation.Vertical);
		spin_vbox.Append (x_hbox);
		spin_vbox.Append (y_hbox);

		hbox2.Append (spin_vbox);

		// Main layout
		SetOrientation (Orientation.Vertical);
		Spacing = spacing;
		Append (hbox1);
		Append (hbox2);
	}
}
