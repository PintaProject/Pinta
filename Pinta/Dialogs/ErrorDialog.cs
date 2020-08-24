// 
// ErrorDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Unix;

namespace Pinta
{
	public class ErrorDialog : Gtk.Dialog
	{
		private Label description_label;
		private Expander expander;
		private TextView details_text;
		private Button bug_report_button;

		public ErrorDialog (Window parent) : base("Pinta", parent, DialogFlags.Modal | DialogFlags.DestroyWithParent)
		{
			Build ();

			expander.Activated += (sender, e) => {
				GLib.Timeout.Add (100, new GLib.TimeoutHandler (UpdateSize));
			};

			bug_report_button.Clicked += (sender, e) => {
				PintaCore.Actions.Help.Bugs.Activate ();
			};

			TransientFor = parent;
			
			expander.Visible = false;
			DefaultResponse = ResponseType.Ok;
		}

		public void SetMessage (string message)
		{
			description_label.Markup = message;
		}
		
		public void AddDetails (string text)
		{
			TextIter it = details_text.Buffer.EndIter;
			details_text.Buffer.Insert (ref it, text);
			expander.Visible = true;
		}

		private bool UpdateSize ()
		{
			int w, h;
			GetSize (out w, out h);
			Resize (w, 1);
			return false;
		}
		
		private void Build ()
		{
			var hbox = new HBox ();
			hbox.Spacing = 6;
			hbox.BorderWidth = 12;

			var error_icon = new Image ();
			error_icon.Pixbuf = PintaCore.Resources.GetIcon (Stock.DialogError, 32);
			error_icon.Yalign = 0;
			hbox.PackStart (error_icon, false, false, 0);

			var vbox = new VBox ();
			vbox.Spacing = 6;

			description_label = new Label ();
			description_label.Wrap = true;
			description_label.Xalign = 0;
			vbox.PackStart (description_label, false, false, 0);

			expander = new Expander (Catalog.GetString ("Details"));
			details_text = new TextView ();
			var scroll = new ScrolledWindow ();
			scroll.Add (details_text);
			scroll.HeightRequest = 250;
			expander.Add (scroll);
			vbox.Add (expander);

			hbox.Add (vbox);
			this.VBox.Add (hbox);
			
			bug_report_button = new Button (Catalog.GetString ("Report Bug...."));
			bug_report_button.CanFocus = false;
			ActionArea.Add (bug_report_button);

			var ok_button = new Button (Gtk.Stock.Ok);
			ok_button.CanDefault = true;
			AddActionWidget (ok_button, ResponseType.Ok);

			DefaultWidth = 600;
			DefaultHeight = 142;

			ShowAll ();
		}
	}
}

