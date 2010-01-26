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

namespace Pinta
{
	public partial class ResizeCanvasDialog : Gtk.Dialog
	{
		private bool value_changing;
		private Anchor anchor;
		
		public ResizeCanvasDialog ()
		{
			this.Build ();
			
			Icon = PintaCore.Resources.GetIcon ("Menu.Image.CanvasSize.png");
			
			widthSpinner.Value = PintaCore.Workspace.ImageSize.X;
			heightSpinner.Value = PintaCore.Workspace.ImageSize.Y;
			
			percentageRadio.Toggled += new EventHandler (percentageRadio_Toggled);
			absoluteRadio.Toggled += new EventHandler (absoluteRadio_Toggled);
			
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
		}

		#region Public Methods
		public void SaveChanges ()
		{
			PintaCore.Workspace.ResizeCanvas (widthSpinner.ValueAsInt, heightSpinner.ValueAsInt, anchor);
		}
		#endregion

		#region Private Methods
		private void heightSpinner_ValueChanged (object sender, EventArgs e)
		{
			if (value_changing)
				return;
			
			if (aspectCheckbox.Active) {
				value_changing = true;
				widthSpinner.Value = (int)((heightSpinner.Value * PintaCore.Workspace.ImageSize.X) / PintaCore.Workspace.ImageSize.Y);
				value_changing = false;
			}
		}

		private void widthSpinner_ValueChanged (object sender, EventArgs e)
		{
			if (value_changing)
				return;
			
			if (aspectCheckbox.Active) {
				value_changing = true;
				heightSpinner.Value = (int)((widthSpinner.Value * PintaCore.Workspace.ImageSize.Y) / PintaCore.Workspace.ImageSize.X);
				value_changing = false;
			}
		}

		private void percentageSpinner_ValueChanged (object sender, EventArgs e)
		{
			widthSpinner.Value = (int)(PintaCore.Workspace.ImageSize.X * (percentageSpinner.ValueAsInt / 100f));
			heightSpinner.Value = (int)(PintaCore.Workspace.ImageSize.Y * (percentageSpinner.ValueAsInt / 100f));
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
		#endregion
	}
}

