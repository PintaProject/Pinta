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
using System.Threading.Tasks;
using Pinta.Core;

namespace Pinta;

internal static class ErrorDialog
{
	internal static Task ShowMessage (
		Gtk.Window parent,
		string message,
		string body)
	{
		Console.Error.WriteLine ("Pinta: {0}\n{1}", message, body);

		Adw.MessageDialog dialog = Adw.MessageDialog.New (parent, message, body);

		dialog.AddResponse (nameof (ErrorDialogResponse.OK), Translations.GetString ("_OK"));
		dialog.DefaultResponse = nameof (ErrorDialogResponse.OK);
		dialog.CloseResponse = nameof (ErrorDialogResponse.OK);

		return dialog.RunAsync (dispose: true);
	}

	internal static async Task<ErrorDialogResponse> ShowError (
		Gtk.Window parent,
		string message,
		string body,
		string details)
	{
		Console.Error.WriteLine ("Pinta: {0}\n{1}", message, details);
		PintaErrorDialog dialog = new (parent, message, body, details);
		string responseText = await dialog.RunAsync (dispose: true);
		return Enum.Parse<ErrorDialogResponse> (responseText);
	}

	private sealed class PintaErrorDialog : Adw.MessageDialog
	{
		private readonly Gtk.TextView text_view;
		private readonly Gtk.ScrolledWindow text_scroll;
		private readonly Gtk.Expander details_expander;
		public PintaErrorDialog (
			Gtk.Window parent,
			string message,
			string body,
			string details)
		{
			Gtk.TextView textView = Gtk.TextView.New ();
			textView.Buffer!.SetText (details, -1);

			Gtk.ScrolledWindow textScroll = Gtk.ScrolledWindow.New ();
			textScroll.HeightRequest = 250;
			textScroll.SetChild (textView);

			Gtk.Expander detailsExpander = Gtk.Expander.New (Translations.GetString ("Details"));
			detailsExpander.SetChild (textScroll);

			// --- References to keep

			text_view = textView;
			text_scroll = textScroll;
			details_expander = detailsExpander;

			// --- Initialization

			SetParent (parent);
			SetHeading (message);
			SetBody (body);

			SetExtraChild (detailsExpander);

			AddResponse (nameof (ErrorDialogResponse.Bug), Translations.GetString ("Report Bug..."));
			SetResponseAppearance (nameof (ErrorDialogResponse.Bug), Adw.ResponseAppearance.Suggested);
			AddResponse (nameof (ErrorDialogResponse.OK), Translations.GetString ("_OK"));

			DefaultResponse = nameof (ErrorDialogResponse.OK);
			CloseResponse = nameof (ErrorDialogResponse.OK);
		}

		public override void Dispose ()
		{
			base.Dispose ();
			details_expander.Dispose ();
			text_scroll.Dispose ();
			text_view.Dispose ();
		}
	}
}
