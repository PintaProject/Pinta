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
using Pinta.Core;

namespace Pinta;

public sealed class ResizeCanvasDialog : Gtk.Dialog
{
	private readonly Gtk.CheckButton percentage_radio;
	private readonly Gtk.SpinButton percentage_spinner;
	private readonly Gtk.SpinButton width_spinner;
	private readonly Gtk.SpinButton height_spinner;
	private readonly Gtk.CheckButton aspect_checkbox;

	private readonly Gtk.Button nw_button;
	private readonly Gtk.Button n_button;
	private readonly Gtk.Button ne_button;
	private readonly Gtk.Button w_button;
	private readonly Gtk.Button e_button;
	private readonly Gtk.Button center_button;
	private readonly Gtk.Button sw_button;
	private readonly Gtk.Button s_button;
	private readonly Gtk.Button se_button;

	private bool value_changing;
	private Anchor anchor;

	private readonly WorkspaceManager workspace_manager;

	public ResizeCanvasDialog (ChromeManager chromeManager, WorkspaceManager workspaceManager)
	{
		const int spacing = 6;

		Gtk.CheckButton percentageRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("By percentage:"));
		percentageRadio.OnToggled += percentageRadio_Toggled;

		Gtk.SpinButton percentageSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		percentageSpinner.Value = 100;
		percentageSpinner.OnValueChanged += percentageSpinner_ValueChanged;
		percentageSpinner.SetActivatesDefaultImmediate (true);

		Gtk.Box hboxPercent = new () { Spacing = spacing };
		hboxPercent.SetOrientation (Gtk.Orientation.Horizontal);
		hboxPercent.Append (percentageRadio);
		hboxPercent.Append (percentageSpinner);
		hboxPercent.Append (Gtk.Label.New ("%"));

		Gtk.Label widthLabel = Gtk.Label.New (Translations.GetString ("Width:"));
		widthLabel.Halign = Gtk.Align.End;

		Gtk.SpinButton widthSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		widthSpinner.Value = workspaceManager.ImageSize.Width;
		widthSpinner.OnValueChanged += widthSpinner_ValueChanged;
		widthSpinner.SetActivatesDefaultImmediate (true);

		Gtk.Label heightLabel = Gtk.Label.New (Translations.GetString ("Height:"));
		heightLabel.Halign = Gtk.Align.End;

		Gtk.SpinButton heightSpinner = Gtk.SpinButton.NewWithRange (1, int.MaxValue, 1);
		heightSpinner.Value = workspaceManager.ImageSize.Height;
		heightSpinner.OnValueChanged += heightSpinner_ValueChanged;
		heightSpinner.SetActivatesDefaultImmediate (true);

		Gtk.Grid hwGrid = new () {
			RowSpacing = spacing,
			ColumnSpacing = spacing,
			ColumnHomogeneous = false,
		};
		hwGrid.Attach (widthLabel, 0, 0, 1, 1);
		hwGrid.Attach (widthSpinner, 1, 0, 1, 1);
		hwGrid.Attach (Gtk.Label.New (Translations.GetString ("pixels")), 2, 0, 1, 1);
		hwGrid.Attach (heightLabel, 0, 1, 1, 1);
		hwGrid.Attach (heightSpinner, 1, 1, 1, 1);
		hwGrid.Attach (Gtk.Label.New (Translations.GetString ("pixels")), 2, 1, 1, 1);

		Gtk.CheckButton absoluteRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("By absolute size:"));
		absoluteRadio.SetGroup (percentageRadio);
		absoluteRadio.OnToggled += absoluteRadio_Toggled;

		Gtk.CheckButton aspectCheckBox = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Maintain aspect ratio"));
		aspectCheckBox.Active = true;

		Gtk.Separator sep = new ();
		sep.SetOrientation (Gtk.Orientation.Horizontal);

		Gtk.Label alignLabel = Gtk.Label.New (Translations.GetString ("Anchor:"));
		alignLabel.Xalign = 0;

		Gtk.Button nwButton = CreateAnchorButton ();
		nwButton.OnClicked += HandleNWButtonClicked;

		Gtk.Button nButton = CreateAnchorButton ();
		nButton.OnClicked += HandleNButtonClicked;

		Gtk.Button neButton = CreateAnchorButton ();
		neButton.OnClicked += HandleNEButtonClicked;

		Gtk.Button wButton = CreateAnchorButton ();
		wButton.OnClicked += HandleWButtonClicked;

		Gtk.Button eButton = CreateAnchorButton ();
		eButton.OnClicked += HandleEButtonClicked;

		Gtk.Button centerButton = CreateAnchorButton ();
		centerButton.OnClicked += HandleCenterButtonClicked;

		Gtk.Button swButton = CreateAnchorButton ();
		swButton.OnClicked += HandleSWButtonClicked;

		Gtk.Button sButton = CreateAnchorButton ();
		sButton.OnClicked += HandleSButtonClicked;

		Gtk.Button seButton = CreateAnchorButton ();
		seButton.OnClicked += HandleSEButtonClicked;

		Gtk.Grid grid = new () {
			RowSpacing = spacing,
			ColumnSpacing = spacing,
			Halign = Gtk.Align.Center,
			Valign = Gtk.Align.Center,
		};
		grid.Attach (nwButton, 0, 0, 1, 1);
		grid.Attach (nButton, 1, 0, 1, 1);
		grid.Attach (neButton, 2, 0, 1, 1);
		grid.Attach (wButton, 0, 1, 1, 1);
		grid.Attach (centerButton, 1, 1, 1, 1);
		grid.Attach (eButton, 2, 1, 1, 1);
		grid.Attach (swButton, 0, 2, 1, 1);
		grid.Attach (sButton, 1, 2, 1, 1);
		grid.Attach (seButton, 2, 2, 1, 1);

		Gtk.Box mainVbox = new () { Spacing = spacing };
		mainVbox.SetOrientation (Gtk.Orientation.Vertical);
		mainVbox.Append (hboxPercent);
		mainVbox.Append (absoluteRadio);
		mainVbox.Append (hwGrid);
		mainVbox.Append (aspectCheckBox);
		mainVbox.Append (sep);
		mainVbox.Append (alignLabel);
		mainVbox.Append (grid);

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString ("Resize Canvas");
		TransientFor = chromeManager.MainWindow;
		Modal = true;
		IconName = Resources.Icons.ImageResizeCanvas;
		DefaultWidth = 300;
		DefaultHeight = 200;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- Initialization

		var contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (12);
		contentArea.Append (mainVbox);

		percentageSpinner.GrabFocus ();

		// --- References to keep

		workspace_manager = workspaceManager;

		percentage_radio = percentageRadio;
		percentage_spinner = percentageSpinner;
		width_spinner = widthSpinner;
		height_spinner = heightSpinner;
		aspect_checkbox = aspectCheckBox;

		nw_button = nwButton;
		n_button = nButton;
		ne_button = neButton;
		w_button = wButton;
		e_button = eButton;
		center_button = centerButton;
		sw_button = swButton;
		s_button = sButton;
		se_button = seButton;

		// Final initialization
		percentageRadio.Active = true;
		SetAnchor (Anchor.Center);
	}

	private static Gtk.Button CreateAnchorButton ()
		=> new () {
			WidthRequest = 30,
			HeightRequest = 30,
		};

	public void SaveChanges ()
	{
		Size newSize = new (
			Width: width_spinner.GetValueAsInt (),
			Height: height_spinner.GetValueAsInt ()
		);

		workspace_manager.ResizeCanvas (newSize, anchor, null);
	}

	private void heightSpinner_ValueChanged (object? sender, EventArgs e)
	{
		if (value_changing)
			return;

		if (!aspect_checkbox.Active)
			return;

		value_changing = true;
		width_spinner.Value = (int) (height_spinner.Value * workspace_manager.ImageSize.Width / workspace_manager.ImageSize.Height);
		value_changing = false;
	}

	private void widthSpinner_ValueChanged (object? sender, EventArgs e)
	{
		if (value_changing)
			return;

		if (!aspect_checkbox.Active)
			return;

		value_changing = true;
		height_spinner.Value = (int) (width_spinner.Value * workspace_manager.ImageSize.Height / workspace_manager.ImageSize.Width);
		value_changing = false;
	}

	private void percentageSpinner_ValueChanged (object? sender, EventArgs e)
	{
		width_spinner.Value = (int) (workspace_manager.ImageSize.Width * (percentage_spinner.GetValueAsInt () / 100f));
		height_spinner.Value = (int) (workspace_manager.ImageSize.Height * (percentage_spinner.GetValueAsInt () / 100f));
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
}

