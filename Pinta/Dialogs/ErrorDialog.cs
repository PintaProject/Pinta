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

		using Gtk.TextView text_view = Gtk.TextView.New ();
		text_view.Buffer!.SetText (details, -1);

		using Gtk.ScrolledWindow scroll = Gtk.ScrolledWindow.New ();
		scroll.HeightRequest = 250;
		scroll.SetChild (text_view);

		using Gtk.Expander expander = Gtk.Expander.New (Translations.GetString ("Details"));
		expander.SetChild (scroll);

		using Adw.MessageDialog dialog = Adw.MessageDialog.New (parent, message, body);
		dialog.SetExtraChild (expander);
		dialog.AddResponse (nameof (ErrorDialogResponse.Bug), Translations.GetString ("Report Bug..."));
		dialog.SetResponseAppearance (nameof (ErrorDialogResponse.Bug), Adw.ResponseAppearance.Suggested);
		dialog.AddResponse (nameof (ErrorDialogResponse.OK), Translations.GetString ("_OK"));
		dialog.DefaultResponse = nameof (ErrorDialogResponse.OK);
		dialog.CloseResponse = nameof (ErrorDialogResponse.OK);

		string responseText = await dialog.RunAsync ();

		return Enum.Parse<ErrorDialogResponse> (responseText);
	}
}
