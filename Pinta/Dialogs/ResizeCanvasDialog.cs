// 
// ResizeCanvasDialog.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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

namespace Pinta;

public sealed class ResizeCanvasDialog : Dialog
{
	private CheckButton percentage_radio;
	private CheckButton absolute_radio;
	private SpinButton percentage_spinner;
	private SpinButton width_spinner;
	private SpinButton height_spinner;
	private CheckButton aspect_checkbox;

	private Button nw_button;
	private Button n_button;
	private Button ne_button;
	private Button w_button;
	private Button e_button;
	private Button center_button;
	private Button sw_button;
	private Button s_button;
	private Button se_button;

	private bool value_changing;
	private Anchor anchor;

	public ResizeCanvasDialog ()
	{
		Title = Translations.GetString ("Resize Canvas");
		TransientFor = PintaCore.Chrome.MainWindow;
		Modal = true;
		this.AddCancelOkButtons ();
		this.SetDefaultResponse (ResponseType.Ok);

		Build ();

		aspect_checkbox.Active = true;

		width_spinner.Value = PintaCore.Workspace.ImageSize.Width;
		height_spinner.Value = PintaCore.Workspace.ImageSize.Height;

		percentage_radio.OnToggled += percentageRadio_Toggled;
		absolute_radio.OnToggled += absoluteRadio_Toggled;
		percentage_radio.Active = true;

		percentage_spinner.Value = 100;
		percentage_spinner.OnValueChanged += percentageSpinner_ValueChanged;

		width_spinner.OnValueChanged += widthSpinner_ValueChanged;
		height_spinner.OnValueChanged += heightSpinner_ValueChanged;

		nw_button.OnClicked += HandleNWButtonClicked;
		n_button.OnClicked += HandleNButtonClicked;
		ne_button.OnClicked += HandleNEButtonClicked;
		w_button.OnClicked += HandleWButtonClicked;
		center_button.OnClicked += HandleCenterButtonClicked;
		e_button.OnClicked += HandleEButtonClicked;
		sw_button.OnClicked += HandleSWButtonClicked;
		s_button.OnClicked += HandleSButtonClicked;
		se_button.OnClicked += HandleSEButtonClicked;

		SetAnchor (Anchor.Center);

		width_spinner.SetActivatesDefault (true);
		height_spinner.SetActivatesDefault (true);
		percentage_spinner.SetActivatesDefault (true);

		percentage_spinner.GrabFocus ();
	}

	#region Public Methods
	public void SaveChanges ()
	{
		PintaCore.Workspace.ResizeCanvas (width_spinner.GetValueAsInt (), height_spinner.GetValueAsInt (), anchor, null);
	}
	#endregion

	#region Private Methods
	private void heightSpinner_ValueChanged (object? sender, EventArgs e)
	{
		if (value_changing)
			return;

		if (!aspect_checkbox.Active)
			return;

		value_changing = true;
		width_spinner.Value = (int) ((height_spinner.Value * PintaCore.Workspace.ImageSize.Width) / PintaCore.Workspace.ImageSize.Height);
		value_changing = false;
	}

	private void widthSpinner_ValueChanged (object? sender, EventArgs e)
	{
		if (value_changing)
			return;

		if (!aspect_checkbox.Active)
			return;

		value_changing = true;
		height_spinner.Value = (int) ((width_spinner.Value * PintaCore.Workspace.ImageSize.Height) / PintaCore.Workspace.ImageSize.Width);
		value_changing = false;
	}

	private void percentageSpinner_ValueChanged (object? sender, EventArgs e)
	{
		width_spinner.Value = (int) (PintaCore.Workspace.ImageSize.Width * (percentage_spinner.GetValueAsInt () / 100f));
		height_spinner.Value = (int) (PintaCore.Workspace.ImageSize.Height * (percentage_spinner.GetValueAsInt () / 100f));
	}

	private void absoluteRadio_Toggled (object? sender, EventArgs e)
	{
		RadioToggle ();
	}

	private void percentageRadio_Toggled (object? sender, EventArgs e)
	{
		RadioToggle ();
	}

	private void RadioToggle ()
	{
		if (percentage_radio.Active) {
			percentage_spinner.Sensitive = true;

			width_spinner.Sensitive = false;
			height_spinner.Sensitive = false;
			aspect_checkbox.Sensitive = false;
		} else {
			percentage_spinner.Sensitive = false;

			width_spinner.Sensitive = true;
			height_spinner.Sensitive = true;
			aspect_checkbox.Sensitive = true;
		}
	}

	private void HandleSEButtonClicked (object? sender, EventArgs e)
	{
		SetAnchor (Anchor.SE);
	}

	private void HandleSButtonClicked (object? sender, EventArgs e)
	{
		SetAnchor (Anchor.S);
	}

	private void HandleSWButtonClicked (object? sender, EventArgs e)
	{
		SetAnchor (Anchor.SW);
	}

	private void HandleEButtonClicked (object? sender, EventArgs e)
	{
		SetAnchor (Anchor.E);
	}

	private void HandleCenterButtonClicked (object? sender, EventArgs e)
	{
		SetAnchor (Anchor.Center);
	}

	private void HandleWButtonClicked (object? sender, EventArgs e)
	{
		SetAnchor (Anchor.W);
	}

	private void HandleNEButtonClicked (object? sender, EventArgs e)
	{
		SetAnchor (Anchor.NE);
	}

	private void HandleNButtonClicked (object? sender, EventArgs e)
	{
		SetAnchor (Anchor.N);
	}

	private void HandleNWButtonClicked (object? sender, EventArgs e)
	{
		SetAnchor (Anchor.NW);
	}

	private void SetAnchor (Anchor anchor)
	{
		this.anchor = anchor;

		nw_button.IconName = "";
		n_button.IconName = "";
		ne_button.IconName = "";
		w_button.IconName = "";
		e_button.IconName = "";
		center_button.IconName = "";
		sw_button.IconName = "";
		s_button.IconName = "";
		se_button.IconName = "";

		switch (anchor) {

			case Anchor.NW:
				nw_button.IconName = Resources.Icons.ResizeCanvasBase;
				n_button.IconName = Resources.Icons.ResizeCanvasRight;
				w_button.IconName = Resources.Icons.ResizeCanvasDown;
				center_button.IconName = Resources.Icons.ResizeCanvasSE;
				break;

			case Anchor.N:
				nw_button.IconName = Resources.Icons.ResizeCanvasLeft;
				n_button.IconName = Resources.Icons.ResizeCanvasBase;
				ne_button.IconName = Resources.Icons.ResizeCanvasRight;
				w_button.IconName = Resources.Icons.ResizeCanvasSW;
				e_button.IconName = Resources.Icons.ResizeCanvasSE;
				center_button.IconName = Resources.Icons.ResizeCanvasDown;
				break;

			case Anchor.NE:
				ne_button.IconName = Resources.Icons.ResizeCanvasBase;
				n_button.IconName = Resources.Icons.ResizeCanvasLeft;
				e_button.IconName = Resources.Icons.ResizeCanvasDown;
				center_button.IconName = Resources.Icons.ResizeCanvasSW;
				break;

			case Anchor.W:
				nw_button.IconName = Resources.Icons.ResizeCanvasUp;
				n_button.IconName = Resources.Icons.ResizeCanvasNE;
				sw_button.IconName = Resources.Icons.ResizeCanvasDown;
				w_button.IconName = Resources.Icons.ResizeCanvasBase;
				s_button.IconName = Resources.Icons.ResizeCanvasSE;
				center_button.IconName = Resources.Icons.ResizeCanvasRight;
				break;

			case Anchor.Center:
				nw_button.IconName = Resources.Icons.ResizeCanvasNW;
				n_button.IconName = Resources.Icons.ResizeCanvasUp;
				ne_button.IconName = Resources.Icons.ResizeCanvasNE;
				w_button.IconName = Resources.Icons.ResizeCanvasLeft;
				e_button.IconName = Resources.Icons.ResizeCanvasRight;
				sw_button.IconName = Resources.Icons.ResizeCanvasSW;
				s_button.IconName = Resources.Icons.ResizeCanvasDown;
				se_button.IconName = Resources.Icons.ResizeCanvasSE;
				center_button.IconName = Resources.Icons.ResizeCanvasBase;
				break;

			case Anchor.E:
				ne_button.IconName = Resources.Icons.ResizeCanvasUp;
				n_button.IconName = Resources.Icons.ResizeCanvasNW;
				se_button.IconName = Resources.Icons.ResizeCanvasDown;
				e_button.IconName = Resources.Icons.ResizeCanvasBase;
				s_button.IconName = Resources.Icons.ResizeCanvasSW;
				center_button.IconName = Resources.Icons.ResizeCanvasLeft;
				break;

			case Anchor.SW:
				sw_button.IconName = Resources.Icons.ResizeCanvasBase;
				s_button.IconName = Resources.Icons.ResizeCanvasRight;
				w_button.IconName = Resources.Icons.ResizeCanvasUp;
				center_button.IconName = Resources.Icons.ResizeCanvasNE;
				break;

			case Anchor.S:
				sw_button.IconName = Resources.Icons.ResizeCanvasLeft;
				s_button.IconName = Resources.Icons.ResizeCanvasBase;
				se_button.IconName = Resources.Icons.ResizeCanvasRight;
				w_button.IconName = Resources.Icons.ResizeCanvasNW;
				e_button.IconName = Resources.Icons.ResizeCanvasNE;
				center_button.IconName = Resources.Icons.ResizeCanvasUp;
				break;

			case Anchor.SE:
				se_button.IconName = Resources.Icons.ResizeCanvasBase;
				s_button.IconName = Resources.Icons.ResizeCanvasLeft;
				e_button.IconName = Resources.Icons.ResizeCanvasUp;
				center_button.IconName = Resources.Icons.ResizeCanvasNW;
				break;
		}
	}

	[MemberNotNull (nameof (percentage_radio), nameof (absolute_radio), nameof (percentage_spinner), nameof (width_spinner), nameof (height_spinner),
			nameof (aspect_checkbox), nameof (nw_button), nameof (n_button), nameof (ne_button), nameof (e_button), nameof (se_button),
			nameof (s_button), nameof (sw_button), nameof (w_button), nameof (center_button))]
	private void Build ()
	{
		IconName = Resources.Icons.ImageResizeCanvas;

		DefaultWidth = 300;
		DefaultHeight = 200;

		percentage_radio = CheckButton.NewWithLabel (Translations.GetString ("By percentage:"));
		absolute_radio = CheckButton.NewWithLabel (Translations.GetString ("By absolute size:"));
		absolute_radio.SetGroup (percentage_radio);

		percentage_spinner = SpinButton.NewWithRange (1, int.MaxValue, 1);
		width_spinner = SpinButton.NewWithRange (1, int.MaxValue, 1);
		height_spinner = SpinButton.NewWithRange (1, int.MaxValue, 1);

		aspect_checkbox = CheckButton.NewWithLabel (Translations.GetString ("Maintain aspect ratio"));

		const int spacing = 6;
		var main_vbox = new Box () { Spacing = spacing };
		main_vbox.SetOrientation (Orientation.Vertical);

		var hbox_percent = new Box () { Spacing = spacing };
		hbox_percent.SetOrientation (Orientation.Horizontal);
		hbox_percent.Append (percentage_radio);
		hbox_percent.Append (percentage_spinner);
		hbox_percent.Append (Label.New ("%"));
		main_vbox.Append (hbox_percent);

		main_vbox.Append (absolute_radio);

		var hw_grid = new Grid () { RowSpacing = spacing, ColumnSpacing = spacing, ColumnHomogeneous = false };
		var width_label = Label.New (Translations.GetString ("Width:"));
		width_label.Halign = Align.End;
		hw_grid.Attach (width_label, 0, 0, 1, 1);
		hw_grid.Attach (width_spinner, 1, 0, 1, 1);
		hw_grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 0, 1, 1);

		var height_label = Label.New (Translations.GetString ("Height:"));
		height_label.Halign = Align.End;
		hw_grid.Attach (height_label, 0, 1, 1, 1);
		hw_grid.Attach (height_spinner, 1, 1, 1, 1);
		hw_grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 1, 1, 1);

		main_vbox.Append (hw_grid);

		main_vbox.Append (aspect_checkbox);
		var sep = new Separator ();
		sep.SetOrientation (Orientation.Horizontal);
		main_vbox.Append (sep);

		var align_label = Label.New (Translations.GetString ("Anchor:"));
		align_label.Xalign = 0;
		main_vbox.Append (align_label);

		nw_button = CreateAnchorButton ();
		n_button = CreateAnchorButton ();
		ne_button = CreateAnchorButton ();
		w_button = CreateAnchorButton ();
		e_button = CreateAnchorButton ();
		center_button = CreateAnchorButton ();
		sw_button = CreateAnchorButton ();
		s_button = CreateAnchorButton ();
		se_button = CreateAnchorButton ();

		var grid = new Grid {
			RowSpacing = spacing,
			ColumnSpacing = spacing,
			Halign = Align.Center,
			Valign = Align.Center
		};
		grid.Attach (nw_button, 0, 0, 1, 1);
		grid.Attach (n_button, 1, 0, 1, 1);
		grid.Attach (ne_button, 2, 0, 1, 1);
		grid.Attach (w_button, 0, 1, 1, 1);
		grid.Attach (center_button, 1, 1, 1, 1);
		grid.Attach (e_button, 2, 1, 1, 1);
		grid.Attach (sw_button, 0, 2, 1, 1);
		grid.Attach (s_button, 1, 2, 1, 1);
		grid.Attach (se_button, 2, 2, 1, 1);

		main_vbox.Append (grid);

		var content_area = this.GetContentAreaBox ();
		content_area.SetAllMargins (12);
		content_area.Append (main_vbox);
	}

	private static Button CreateAnchorButton ()
	{
		return new Button () { WidthRequest = 30, HeightRequest = 30 };
	}
	#endregion
}

