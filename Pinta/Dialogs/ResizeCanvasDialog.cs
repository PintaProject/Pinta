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

namespace Pinta
{
	public class ResizeCanvasDialog : Dialog
	{
		private CheckButton percentageRadio;
		private CheckButton absoluteRadio;
		private SpinButton percentageSpinner;
		private SpinButton widthSpinner;
		private SpinButton heightSpinner;
		private CheckButton aspectCheckbox;

		private Button NWButton;
		private Button NButton;
		private Button NEButton;
		private Button WButton;
		private Button EButton;
		private Button CenterButton;
		private Button SWButton;
		private Button SButton;
		private Button SEButton;

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

			aspectCheckbox.Active = true;

			widthSpinner.Value = PintaCore.Workspace.ImageSize.Width;
			heightSpinner.Value = PintaCore.Workspace.ImageSize.Height;

			percentageRadio.OnToggled += percentageRadio_Toggled;
			absoluteRadio.OnToggled += absoluteRadio_Toggled;
			percentageRadio.Active = true;

			percentageSpinner.Value = 100;
			percentageSpinner.OnValueChanged += percentageSpinner_ValueChanged;

			widthSpinner.OnValueChanged += widthSpinner_ValueChanged;
			heightSpinner.OnValueChanged += heightSpinner_ValueChanged;

			NWButton.OnClicked += HandleNWButtonClicked;
			NButton.OnClicked += HandleNButtonClicked;
			NEButton.OnClicked += HandleNEButtonClicked;
			WButton.OnClicked += HandleWButtonClicked;
			CenterButton.OnClicked += HandleCenterButtonClicked;
			EButton.OnClicked += HandleEButtonClicked;
			SWButton.OnClicked += HandleSWButtonClicked;
			SButton.OnClicked += HandleSButtonClicked;
			SEButton.OnClicked += HandleSEButtonClicked;

			SetAnchor (Anchor.Center);

			widthSpinner.SetActivatesDefault (true);
			heightSpinner.SetActivatesDefault (true);
			percentageSpinner.SetActivatesDefault (true);

			percentageSpinner.GrabFocus ();
		}

		#region Public Methods
		public void SaveChanges ()
		{
			PintaCore.Workspace.ResizeCanvas (widthSpinner.GetValueAsInt (), heightSpinner.GetValueAsInt (), anchor, null);
		}
		#endregion

		#region Private Methods
		private void heightSpinner_ValueChanged (object? sender, EventArgs e)
		{
			if (value_changing)
				return;

			if (aspectCheckbox.Active) {
				value_changing = true;
				widthSpinner.Value = (int) ((heightSpinner.Value * PintaCore.Workspace.ImageSize.Width) / PintaCore.Workspace.ImageSize.Height);
				value_changing = false;
			}
		}

		private void widthSpinner_ValueChanged (object? sender, EventArgs e)
		{
			if (value_changing)
				return;

			if (aspectCheckbox.Active) {
				value_changing = true;
				heightSpinner.Value = (int) ((widthSpinner.Value * PintaCore.Workspace.ImageSize.Height) / PintaCore.Workspace.ImageSize.Width);
				value_changing = false;
			}
		}

		private void percentageSpinner_ValueChanged (object? sender, EventArgs e)
		{
			widthSpinner.Value = (int) (PintaCore.Workspace.ImageSize.Width * (percentageSpinner.GetValueAsInt () / 100f));
			heightSpinner.Value = (int) (PintaCore.Workspace.ImageSize.Height * (percentageSpinner.GetValueAsInt () / 100f));
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
			if (percentageRadio.Active) {
				percentageSpinner.Sensitive = true;

				widthSpinner.Sensitive = false;
				heightSpinner.Sensitive = false;
				aspectCheckbox.Sensitive = false;
			} else {
				percentageSpinner.Sensitive = false;

				widthSpinner.Sensitive = true;
				heightSpinner.Sensitive = true;
				aspectCheckbox.Sensitive = true;
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

			NWButton.IconName = "";
			NButton.IconName = "";
			NEButton.IconName = "";
			WButton.IconName = "";
			EButton.IconName = "";
			CenterButton.IconName = "";
			SWButton.IconName = "";
			SButton.IconName = "";
			SEButton.IconName = "";

			switch (anchor) {

				case Anchor.NW:
					NWButton.IconName = Resources.Icons.ResizeCanvasBase;
					NButton.IconName = Resources.Icons.ResizeCanvasRight;
					WButton.IconName = Resources.Icons.ResizeCanvasDown;
					CenterButton.IconName = Resources.Icons.ResizeCanvasSE;
					break;

				case Anchor.N:
					NWButton.IconName = Resources.Icons.ResizeCanvasLeft;
					NButton.IconName = Resources.Icons.ResizeCanvasBase;
					NEButton.IconName = Resources.Icons.ResizeCanvasRight;
					WButton.IconName = Resources.Icons.ResizeCanvasSW;
					EButton.IconName = Resources.Icons.ResizeCanvasSE;
					CenterButton.IconName = Resources.Icons.ResizeCanvasDown;
					break;

				case Anchor.NE:
					NEButton.IconName = Resources.Icons.ResizeCanvasBase;
					NButton.IconName = Resources.Icons.ResizeCanvasLeft;
					EButton.IconName = Resources.Icons.ResizeCanvasDown;
					CenterButton.IconName = Resources.Icons.ResizeCanvasSW;
					break;

				case Anchor.W:
					NWButton.IconName = Resources.Icons.ResizeCanvasUp;
					NButton.IconName = Resources.Icons.ResizeCanvasNE;
					SWButton.IconName = Resources.Icons.ResizeCanvasDown;
					WButton.IconName = Resources.Icons.ResizeCanvasBase;
					SButton.IconName = Resources.Icons.ResizeCanvasSE;
					CenterButton.IconName = Resources.Icons.ResizeCanvasRight;
					break;

				case Anchor.Center:
					NWButton.IconName = Resources.Icons.ResizeCanvasNW;
					NButton.IconName = Resources.Icons.ResizeCanvasUp;
					NEButton.IconName = Resources.Icons.ResizeCanvasNE;
					WButton.IconName = Resources.Icons.ResizeCanvasLeft;
					EButton.IconName = Resources.Icons.ResizeCanvasRight;
					SWButton.IconName = Resources.Icons.ResizeCanvasSW;
					SButton.IconName = Resources.Icons.ResizeCanvasDown;
					SEButton.IconName = Resources.Icons.ResizeCanvasSE;
					CenterButton.IconName = Resources.Icons.ResizeCanvasBase;
					break;

				case Anchor.E:
					NEButton.IconName = Resources.Icons.ResizeCanvasUp;
					NButton.IconName = Resources.Icons.ResizeCanvasNW;
					SEButton.IconName = Resources.Icons.ResizeCanvasDown;
					EButton.IconName = Resources.Icons.ResizeCanvasBase;
					SButton.IconName = Resources.Icons.ResizeCanvasSW;
					CenterButton.IconName = Resources.Icons.ResizeCanvasLeft;
					break;

				case Anchor.SW:
					SWButton.IconName = Resources.Icons.ResizeCanvasBase;
					SButton.IconName = Resources.Icons.ResizeCanvasRight;
					WButton.IconName = Resources.Icons.ResizeCanvasUp;
					CenterButton.IconName = Resources.Icons.ResizeCanvasNE;
					break;

				case Anchor.S:
					SWButton.IconName = Resources.Icons.ResizeCanvasLeft;
					SButton.IconName = Resources.Icons.ResizeCanvasBase;
					SEButton.IconName = Resources.Icons.ResizeCanvasRight;
					WButton.IconName = Resources.Icons.ResizeCanvasNW;
					EButton.IconName = Resources.Icons.ResizeCanvasNE;
					CenterButton.IconName = Resources.Icons.ResizeCanvasUp;
					break;

				case Anchor.SE:
					SEButton.IconName = Resources.Icons.ResizeCanvasBase;
					SButton.IconName = Resources.Icons.ResizeCanvasLeft;
					EButton.IconName = Resources.Icons.ResizeCanvasUp;
					CenterButton.IconName = Resources.Icons.ResizeCanvasNW;
					break;
			}
		}

		[MemberNotNull (nameof (percentageRadio), nameof (absoluteRadio), nameof (percentageSpinner), nameof (widthSpinner), nameof (heightSpinner),
				nameof (aspectCheckbox), nameof (NWButton), nameof (NButton), nameof (NEButton), nameof (EButton), nameof (SEButton),
				nameof (SButton), nameof (SWButton), nameof (WButton), nameof (CenterButton))]
		private void Build ()
		{
			IconName = Resources.Icons.ImageResizeCanvas;

			DefaultWidth = 300;
			DefaultHeight = 200;

			percentageRadio = CheckButton.NewWithLabel (Translations.GetString ("By percentage:"));
			absoluteRadio = CheckButton.NewWithLabel (Translations.GetString ("By absolute size:"));
			absoluteRadio.SetGroup (percentageRadio);

			percentageSpinner = SpinButton.NewWithRange (1, int.MaxValue, 1);
			widthSpinner = SpinButton.NewWithRange (1, int.MaxValue, 1);
			heightSpinner = SpinButton.NewWithRange (1, int.MaxValue, 1);

			aspectCheckbox = CheckButton.NewWithLabel (Translations.GetString ("Maintain aspect ratio"));

			const int spacing = 6;
			var main_vbox = new Box () { Spacing = spacing };
			main_vbox.SetOrientation (Orientation.Vertical);

			var hbox_percent = new Box () { Spacing = spacing };
			hbox_percent.SetOrientation (Orientation.Horizontal);
			hbox_percent.Append (percentageRadio);
			hbox_percent.Append (percentageSpinner);
			hbox_percent.Append (Label.New ("%"));
			main_vbox.Append (hbox_percent);

			main_vbox.Append (absoluteRadio);

			var hw_grid = new Grid () { RowSpacing = spacing, ColumnSpacing = spacing, ColumnHomogeneous = false };
			var width_label = Label.New (Translations.GetString ("Width:"));
			width_label.Halign = Align.End;
			hw_grid.Attach (width_label, 0, 0, 1, 1);
			hw_grid.Attach (widthSpinner, 1, 0, 1, 1);
			hw_grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 0, 1, 1);

			var height_label = Label.New (Translations.GetString ("Height:"));
			height_label.Halign = Align.End;
			hw_grid.Attach (height_label, 0, 1, 1, 1);
			hw_grid.Attach (heightSpinner, 1, 1, 1, 1);
			hw_grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 1, 1, 1);

			main_vbox.Append (hw_grid);

			main_vbox.Append (aspectCheckbox);
			var sep = new Separator ();
			sep.SetOrientation (Orientation.Horizontal);
			main_vbox.Append (sep);

			var align_label = Label.New (Translations.GetString ("Anchor:"));
			align_label.Xalign = 0;
			main_vbox.Append (align_label);

			NWButton = CreateAnchorButton ();
			NButton = CreateAnchorButton ();
			NEButton = CreateAnchorButton ();
			WButton = CreateAnchorButton ();
			EButton = CreateAnchorButton ();
			CenterButton = CreateAnchorButton ();
			SWButton = CreateAnchorButton ();
			SButton = CreateAnchorButton ();
			SEButton = CreateAnchorButton ();

			var grid = new Grid () { RowSpacing = spacing, ColumnSpacing = spacing };
			grid.Halign = Align.Center;
			grid.Valign = Align.Center;
			grid.Attach (NWButton, 0, 0, 1, 1);
			grid.Attach (NButton, 1, 0, 1, 1);
			grid.Attach (NEButton, 2, 0, 1, 1);
			grid.Attach (WButton, 0, 1, 1, 1);
			grid.Attach (CenterButton, 1, 1, 1, 1);
			grid.Attach (EButton, 2, 1, 1, 1);
			grid.Attach (SWButton, 0, 2, 1, 1);
			grid.Attach (SButton, 1, 2, 1, 1);
			grid.Attach (SEButton, 2, 2, 1, 1);

			main_vbox.Append (grid);

			var content_area = this.GetContentAreaBox ();
			content_area.SetAllMargins (12);
			content_area.Append (main_vbox);
		}

		private Button CreateAnchorButton ()
		{
			return new Button () { WidthRequest = 30, HeightRequest = 30 };
		}
		#endregion
	}
}

