// 
// ResizeImageDialog.cs
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
	public class ResizeImageDialog : Dialog
	{
		private CheckButton percentageRadio;
		private CheckButton absoluteRadio;
		private SpinButton percentageSpinner;
		private SpinButton widthSpinner;
		private SpinButton heightSpinner;
		private CheckButton aspectCheckbox;

		private bool value_changing;

		public ResizeImageDialog ()
		{
			Title = Translations.GetString ("Resize Image");
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

			widthSpinner.SetActivatesDefault (true);
			heightSpinner.SetActivatesDefault (true);
			percentageSpinner.SetActivatesDefault (true);

			percentageSpinner.GrabFocus ();
		}

		#region Public Methods
		public void SaveChanges ()
		{
			PintaCore.Workspace.ResizeImage (widthSpinner.GetValueAsInt (), heightSpinner.GetValueAsInt ());
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

		[MemberNotNull (nameof (percentageRadio), nameof (absoluteRadio), nameof (percentageSpinner), nameof (widthSpinner), nameof (heightSpinner), nameof (aspectCheckbox))]
		private void Build ()
		{
			IconName = Resources.Icons.ImageResize;

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
			var main_vbox = new Box () { Orientation = Orientation.Vertical, Spacing = spacing };

			var hbox_percent = new Box () { Orientation = Orientation.Horizontal, Spacing = spacing };
			hbox_percent.Append (percentageRadio);
			hbox_percent.Append (percentageSpinner);
			hbox_percent.Append (Label.New ("%"));
			main_vbox.Append (hbox_percent);

			main_vbox.Append (absoluteRadio);

			var grid = new Grid () { RowSpacing = spacing, ColumnSpacing = spacing, ColumnHomogeneous = false };
			var width_label = Label.New (Translations.GetString ("Width:"));
			width_label.Halign = Align.End;
			grid.Attach (width_label, 0, 0, 1, 1);
			grid.Attach (widthSpinner, 1, 0, 1, 1);
			grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 0, 1, 1);

			var height_label = Label.New (Translations.GetString ("Height:"));
			height_label.Halign = Align.End;
			grid.Attach (height_label, 0, 1, 1, 1);
			grid.Attach (heightSpinner, 1, 1, 1, 1);
			grid.Attach (Label.New (Translations.GetString ("pixels")), 2, 1, 1, 1);

			main_vbox.Append (grid);

			main_vbox.Append (aspectCheckbox);

			var content_area = this.GetContentAreaBox ();
			content_area.SetAllMargins (12);
			content_area.Append (main_vbox);
		}
		#endregion
	}
}

