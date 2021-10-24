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
		private RadioButton percentageRadio;
		private RadioButton absoluteRadio;
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
		
		public ResizeCanvasDialog () : base (Translations.GetString ("Resize Canvas"), PintaCore.Chrome.MainWindow,
		                                     DialogFlags.Modal,
											 Core.GtkExtensions.DialogButtonsCancelOk())
		{
			Build ();

			aspectCheckbox.Active = true;
			
			widthSpinner.Value = PintaCore.Workspace.ImageSize.Width;
			heightSpinner.Value = PintaCore.Workspace.ImageSize.Height;

			percentageRadio.Toggled += new EventHandler (percentageRadio_Toggled);
			absoluteRadio.Toggled += new EventHandler (absoluteRadio_Toggled);
			percentageRadio.Toggle ();

			percentageSpinner.Value = 100;
			percentageSpinner.ValueChanged += new EventHandler (percentageSpinner_ValueChanged);

			widthSpinner.ValueChanged += new EventHandler (widthSpinner_ValueChanged);
			heightSpinner.ValueChanged += new EventHandler (heightSpinner_ValueChanged);
			
			NWButton.Clicked += HandleNWButtonClicked;
			NButton.Clicked += HandleNButtonClicked;
			NEButton.Clicked += HandleNEButtonClicked;
			WButton.Clicked += HandleWButtonClicked;
			CenterButton.Clicked += HandleCenterButtonClicked;
			EButton.Clicked += HandleEButtonClicked;
			SWButton.Clicked += HandleSWButtonClicked;
			SButton.Clicked += HandleSButtonClicked;
			SEButton.Clicked += HandleSEButtonClicked;
			
			SetAnchor (Anchor.Center);
			DefaultResponse = Gtk.ResponseType.Ok;

			widthSpinner.ActivatesDefault = true;
			heightSpinner.ActivatesDefault = true;
			percentageSpinner.ActivatesDefault = true;
			percentageSpinner.GrabFocus();
		}

		#region Public Methods
		public void SaveChanges ()
		{
			PintaCore.Workspace.ResizeCanvas (widthSpinner.ValueAsInt, heightSpinner.ValueAsInt, anchor, null);
		}
		#endregion

		#region Private Methods
		private void heightSpinner_ValueChanged (object? sender, EventArgs e)
		{
			if (value_changing)
				return;
			
			if (aspectCheckbox.Active) {
				value_changing = true;
				widthSpinner.Value = (int)((heightSpinner.Value * PintaCore.Workspace.ImageSize.Width) / PintaCore.Workspace.ImageSize.Height);
				value_changing = false;
			}
		}

		private void widthSpinner_ValueChanged (object? sender, EventArgs e)
		{
			if (value_changing)
				return;
			
			if (aspectCheckbox.Active) {
				value_changing = true;
				heightSpinner.Value = (int)((widthSpinner.Value * PintaCore.Workspace.ImageSize.Height) / PintaCore.Workspace.ImageSize.Width);
				value_changing = false;
			}
		}

		private void percentageSpinner_ValueChanged (object? sender, EventArgs e)
		{
			widthSpinner.Value = (int)(PintaCore.Workspace.ImageSize.Width * (percentageSpinner.ValueAsInt / 100f));
			heightSpinner.Value = (int)(PintaCore.Workspace.ImageSize.Height * (percentageSpinner.ValueAsInt / 100f));
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
			
			NWButton.Image = null;
			NButton.Image = null;
			NEButton.Image = null;
			WButton.Image = null;
			EButton.Image = null;
			CenterButton.Image = null;
			SWButton.Image = null;
			SButton.Image = null;
			SEButton.Image = null;
			
			switch (anchor) {
				
			case Anchor.NW:
				NWButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasBase, IconSize.Button);
				NButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasRight, IconSize.Button);
				WButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasDown, IconSize.Button);
				CenterButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasSE, IconSize.Button);
				break;
			
			case Anchor.N:
				NWButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasLeft, IconSize.Button);
				NButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasBase, IconSize.Button);
				NEButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasRight, IconSize.Button);
				WButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasSW, IconSize.Button);
				EButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasSE, IconSize.Button);
				CenterButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasDown, IconSize.Button);
					break;			

			case Anchor.NE:
				NEButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasBase, IconSize.Button);
				NButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasLeft, IconSize.Button);
				EButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasDown, IconSize.Button);
				CenterButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasSW, IconSize.Button);
				break;
				
			case Anchor.W:
				NWButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasUp, IconSize.Button);
				NButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasNE, IconSize.Button);
				SWButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasDown, IconSize.Button);
				WButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasBase, IconSize.Button);
				SButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasSE, IconSize.Button);
				CenterButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasRight, IconSize.Button);
				break;

			case Anchor.Center:
				NWButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasNW, IconSize.Button);
				NButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasUp, IconSize.Button);
				NEButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasNE, IconSize.Button);
				WButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasLeft, IconSize.Button);
				EButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasRight, IconSize.Button);
				SWButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasSW, IconSize.Button);
				SButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasDown, IconSize.Button);
				SEButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasSE, IconSize.Button);
				CenterButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasBase, IconSize.Button);
				break;
			
			case Anchor.E:
				NEButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasUp, IconSize.Button);
				NButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasNW, IconSize.Button);
				SEButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasDown, IconSize.Button);
				EButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasBase, IconSize.Button);
				SButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasSW, IconSize.Button);
				CenterButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasLeft, IconSize.Button);
				break;
				
			case Anchor.SW:
				SWButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasBase, IconSize.Button);
				SButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasRight, IconSize.Button);
				WButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasUp, IconSize.Button);
				CenterButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasNE, IconSize.Button);
				break;
			
			case Anchor.S:
				SWButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasLeft, IconSize.Button);
				SButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasBase, IconSize.Button);
				SEButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasRight, IconSize.Button);
				WButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasNW, IconSize.Button);
				EButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasNE, IconSize.Button);
				CenterButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasUp, IconSize.Button);
				break;			

			case Anchor.SE:
				SEButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasBase, IconSize.Button);
				SButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasLeft, IconSize.Button);
				EButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasUp, IconSize.Button);
				CenterButton.Image = Image.NewFromIconName(Resources.Icons.ResizeCanvasNW, IconSize.Button);
				break;
			}
		}

		[MemberNotNull (nameof (percentageRadio), nameof (absoluteRadio), nameof (percentageSpinner), nameof (widthSpinner), nameof (heightSpinner),
				nameof (aspectCheckbox), nameof (NWButton), nameof (NButton), nameof (NEButton), nameof (EButton), nameof (SEButton), 
				nameof (SButton), nameof (SWButton), nameof (WButton), nameof (CenterButton))]
		private void Build()
        {
			IconName = Resources.Icons.ImageResizeCanvas;

			WindowPosition = WindowPosition.CenterOnParent;

			DefaultWidth = 300;
			DefaultHeight = 200;

			percentageRadio = new RadioButton (Translations.GetString ("By percentage:"));
			absoluteRadio = new RadioButton (percentageRadio, Translations.GetString ("By absolute size:"));

			percentageSpinner = new SpinButton (1, 1000, 1);
			widthSpinner = new SpinButton (1, 10000, 1);
			heightSpinner = new SpinButton (1, 10000, 1);

			aspectCheckbox = new CheckButton (Translations.GetString ("Maintain aspect ratio"));

			const int spacing = 6;
			var main_vbox = new VBox () { Spacing = spacing, BorderWidth = 12 };

			var hbox_percent = new HBox () { Spacing = spacing };
			hbox_percent.PackStart (percentageRadio, true, true, 0);
			hbox_percent.PackStart (percentageSpinner, false, false, 0);
			hbox_percent.PackEnd (new Label ("%"), false, false, 0);
			main_vbox.PackStart (hbox_percent, false, false, 0);

			main_vbox.PackStart (absoluteRadio, false, false, 0);

			var hbox_width = new HBox () { Spacing = spacing };
			hbox_width.PackStart (new Label (Translations.GetString ("Width:")), false, false, 0);
			hbox_width.PackStart (widthSpinner, false, false, 0);
			hbox_width.PackStart (new Label (Translations.GetString ("pixels")), false, false, 0);
			main_vbox.PackStart (hbox_width, false, false, 0);

			var hbox_height = new HBox () { Spacing = spacing };
			hbox_height.PackStart (new Label (Translations.GetString ("Height:")), false, false, 0);
			hbox_height.PackStart (heightSpinner, false, false, 0);
			hbox_height.PackStart (new Label (Translations.GetString ("pixels")), false, false, 0);
			main_vbox.PackStart (hbox_height, false, false, 0);

			main_vbox.PackStart (aspectCheckbox, false, false, 0);
			main_vbox.PackStart (new HSeparator (), false, false, 0);

			var align_label = new Label (Translations.GetString ("Anchor:")) { Xalign = 0 };
			main_vbox.PackStart (align_label, false, false, 0);

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
			grid.Attach (NWButton, 0, 0, 1, 1);
			grid.Attach (NButton, 1, 0, 1, 1);
			grid.Attach (NEButton, 2, 0, 1, 1);
			grid.Attach (WButton, 0, 1, 1, 1);
			grid.Attach (CenterButton, 1, 1, 1, 1);
			grid.Attach (EButton, 2, 1, 1, 1);
			grid.Attach (SWButton, 0, 2, 1, 1);
			grid.Attach (SButton, 1, 2, 1, 1);
			grid.Attach (SEButton, 2, 2, 1, 1);

			var grid_align = new Alignment (0.5f, 0.5f, 0, 0);
			grid_align.Add (grid);

			main_vbox.PackStart (grid_align, false, false, 0);

			ContentArea.BorderWidth = 2;
			ContentArea.Add (main_vbox);

			ShowAll ();
		}

		private Button CreateAnchorButton()
        {
			return new Button () { WidthRequest = 30, HeightRequest = 30 };
        }
		#endregion
	}
}

