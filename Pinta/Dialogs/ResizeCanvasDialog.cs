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
using Gtk;
using Mono.Unix;
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
		
		public ResizeCanvasDialog () : base (Catalog.GetString ("Resize Canvas"), PintaCore.Chrome.MainWindow,
		                                     DialogFlags.Modal,
											 Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
											 Gtk.Stock.Ok, Gtk.ResponseType.Ok)
		{
			Build ();
			
			Icon = PintaCore.Resources.GetIcon ("Menu.Image.CanvasSize.png");
						
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
			
			SetAnchor (Anchor.NW);
			AlternativeButtonOrder = new int[] { (int) Gtk.ResponseType.Ok, (int) Gtk.ResponseType.Cancel };
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
		private void heightSpinner_ValueChanged (object sender, EventArgs e)
		{
			if (value_changing)
				return;
			
			if (aspectCheckbox.Active) {
				value_changing = true;
				widthSpinner.Value = (int)((heightSpinner.Value * PintaCore.Workspace.ImageSize.Width) / PintaCore.Workspace.ImageSize.Height);
				value_changing = false;
			}
		}

		private void widthSpinner_ValueChanged (object sender, EventArgs e)
		{
			if (value_changing)
				return;
			
			if (aspectCheckbox.Active) {
				value_changing = true;
				heightSpinner.Value = (int)((widthSpinner.Value * PintaCore.Workspace.ImageSize.Height) / PintaCore.Workspace.ImageSize.Width);
				value_changing = false;
			}
		}

		private void percentageSpinner_ValueChanged (object sender, EventArgs e)
		{
			widthSpinner.Value = (int)(PintaCore.Workspace.ImageSize.Width * (percentageSpinner.ValueAsInt / 100f));
			heightSpinner.Value = (int)(PintaCore.Workspace.ImageSize.Height * (percentageSpinner.ValueAsInt / 100f));
		}

		private void absoluteRadio_Toggled (object sender, EventArgs e)
		{
			RadioToggle ();
		}

		private void percentageRadio_Toggled (object sender, EventArgs e)
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

		private void HandleSEButtonClicked (object sender, EventArgs e)
		{
			SetAnchor (Anchor.SE);
		}

		private void HandleSButtonClicked (object sender, EventArgs e)
		{
			SetAnchor (Anchor.S);
		}

		private void HandleSWButtonClicked (object sender, EventArgs e)
		{
			SetAnchor (Anchor.SW);
		}

		private void HandleEButtonClicked (object sender, EventArgs e)
		{
			SetAnchor (Anchor.E);
		}

		private void HandleCenterButtonClicked (object sender, EventArgs e)
		{
			SetAnchor (Anchor.Center);
		}

		private void HandleWButtonClicked (object sender, EventArgs e)
		{
			SetAnchor (Anchor.W);
		}

		private void HandleNEButtonClicked (object sender, EventArgs e)
		{
			SetAnchor (Anchor.NE);
		}

		private void HandleNButtonClicked (object sender, EventArgs e)
		{
			SetAnchor (Anchor.N);
		}

		private void HandleNWButtonClicked (object sender, EventArgs e)
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
				NWButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.Image.png"));
				NButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.RightArrow.png"));
				WButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.DownArrow.png"));
				CenterButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.SouthEast.png"));
				break;
			
			case Anchor.N:
				NWButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.LeftArrow.png"));
				NButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.Image.png"));
				NEButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.RightArrow.png"));
				WButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.SouthWest.png"));
				EButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.SouthEast.png"));
				CenterButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.DownArrow.png"));
				break;			

			case Anchor.NE:
				NEButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.Image.png"));
				NButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.LeftArrow.png"));
				EButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.DownArrow.png"));
				CenterButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.SouthWest.png"));
				break;
				
			case Anchor.W:
				NWButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.UpArrow.png"));
				NButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.NorthEast.png"));
				SWButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.DownArrow.png"));
				WButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.Image.png"));
				SButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.SouthEast.png"));
				CenterButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.RightArrow.png"));
				break;

			case Anchor.Center:
				NWButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.NorthWest.png"));
				NButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.UpArrow.png"));
				NEButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.NorthEast.png"));
				WButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.LeftArrow.png"));
				EButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.RightArrow.png"));
				SWButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.SouthWest.png"));
				SButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.DownArrow.png"));
				SEButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.SouthEast.png"));
				CenterButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.Image.png"));
				break;
			
			case Anchor.E:
				NEButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.UpArrow.png"));
				NButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.NorthWest.png"));
				SEButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.DownArrow.png"));
				EButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.Image.png"));
				SButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.SouthWest.png"));
				CenterButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.LeftArrow.png"));
				break;
				
			case Anchor.SW:
				SWButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.Image.png"));
				SButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.RightArrow.png"));
				WButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.UpArrow.png"));
				CenterButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.NorthEast.png"));
				break;
			
			case Anchor.S:
				SWButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.LeftArrow.png"));
				SButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.Image.png"));
				SEButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.RightArrow.png"));
				WButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.NorthWest.png"));
				EButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.NorthEast.png"));
				CenterButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.UpArrow.png"));
				break;			

			case Anchor.SE:
				SEButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.Image.png"));
				SButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.LeftArrow.png"));
				EButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.UpArrow.png"));
				CenterButton.Image = new Gtk.Image (PintaCore.Resources.GetIcon ("ResizeCanvas.NorthWest.png"));
				break;
			}
		}

		private void Build()
        {
			Icon = PintaCore.Resources.GetIcon ("Menu.Image.CanvasSize.png");

			WindowPosition = WindowPosition.CenterOnParent;

			DefaultWidth = 300;
			DefaultHeight = 200;

			absoluteRadio = new RadioButton (Catalog.GetString ("By absolute size:"));
			percentageRadio = new RadioButton (absoluteRadio, Catalog.GetString ("By percentage:"));

			percentageSpinner = new SpinButton (1, 1000, 1);
			widthSpinner = new SpinButton (1, 10000, 1);
			heightSpinner = new SpinButton (1, 10000, 1);

			aspectCheckbox = new CheckButton (Catalog.GetString ("Maintain aspect ratio"));

			const int spacing = 6;
			var main_vbox = new VBox () { Spacing = spacing, BorderWidth = 12 };

			var hbox_percent = new HBox () { Spacing = spacing };
			hbox_percent.PackStart (percentageRadio, true, true, 0);
			hbox_percent.PackStart (percentageSpinner, false, false, 0);
			hbox_percent.PackEnd (new Label ("%"), false, false, 0);

			main_vbox.PackStart (absoluteRadio, false, false, 0);

			var hbox_width = new HBox () { Spacing = spacing };
			hbox_width.PackStart (new Label (Catalog.GetString ("Width:")), false, false, 0);
			hbox_width.PackStart (widthSpinner, false, false, 0);
			hbox_width.PackStart (new Label (Catalog.GetString ("pixels")), false, false, 0);
			main_vbox.PackStart (hbox_width, false, false, 0);

			var hbox_height = new HBox () { Spacing = spacing };
			hbox_height.PackStart (new Label (Catalog.GetString ("Height:")), false, false, 0);
			hbox_height.PackStart (heightSpinner, false, false, 0);
			hbox_height.PackStart (new Label (Catalog.GetString ("pixels")), false, false, 0);
			main_vbox.PackStart (hbox_height, false, false, 0);

			main_vbox.PackStart (aspectCheckbox, false, false, 0);
			main_vbox.PackStart (new HSeparator (), false, false, 0);
			main_vbox.PackStart (hbox_percent, false, false, 0);

			var align_label = new Label (Catalog.GetString ("Anchor:")) { Xalign = 0 };
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

			var grid = new Table (3, 3, false) { RowSpacing = spacing, ColumnSpacing = spacing };
			grid.Attach (NWButton, 0, 1, 0, 1);
			grid.Attach (NButton, 1, 2, 0, 1);
			grid.Attach (NEButton, 2, 3, 0, 1);
			grid.Attach (WButton, 0, 1, 1, 2);
			grid.Attach (CenterButton, 1, 2, 1, 2);
			grid.Attach (EButton, 2, 3, 1, 2);
			grid.Attach (SWButton, 0, 1, 2, 3);
			grid.Attach (SButton, 1, 2, 2, 3);
			grid.Attach (SEButton, 2, 3, 2, 3);

			var grid_align = new Alignment (0.5f, 0.5f, 0, 0);
			grid_align.Add (grid);
			
			main_vbox.PackStart (grid_align, false, false, 0);

			VBox.BorderWidth = 2;
			VBox.Add (main_vbox);

			ShowAll ();
		}

		private Button CreateAnchorButton()
        {
			return new Button () { WidthRequest = 30, HeightRequest = 30 };
        }
		#endregion
	}
}

