// 
// FileUnsupportedFormatDialog.cs
//  
// Author:
//       Mykola Franchuk <thekolian1996@gmail.com>
// 
// Copyright (c) 2020 Mykola Franchuk
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

namespace Pinta
{
    class FileUnsupportedFormatDialog : Gtk.Dialog
    {
		private Label description_label;

		public FileUnsupportedFormatDialog(Window parent) : base("Pinta", parent, DialogFlags.Modal | DialogFlags.DestroyWithParent)
		{
			Build();

			TransientFor = parent;
		}

		public void SetMessage(string message)
		{
			description_label.Markup = message;
		}

		private void Build()
		{
			var hbox = new HBox();
			hbox.Spacing = 6;
			hbox.BorderWidth = 12;

			var error_icon = new Image();
			error_icon.Pixbuf = PintaCore.Resources.GetIcon(Stock.DialogError, 32);
			error_icon.Yalign = 0;
			hbox.PackStart(error_icon, false, false, 0);

			var vbox = new VBox();
			vbox.Spacing = 6;

			description_label = new Label();
			description_label.Wrap = true;
			description_label.Xalign = 0;
			vbox.PackStart(description_label, false, false, 0);

			hbox.Add(vbox);
			this.VBox.Add(hbox);

			//PintaCore.System.ImageFormats.Formats

			var ok_button = new Button(Gtk.Stock.Ok);
			ok_button.CanDefault = true;
			AddActionWidget(ok_button, ResponseType.Ok);

			DefaultWidth = 600;
			DefaultHeight = 142;

			ShowAll();
		}

	}
}
