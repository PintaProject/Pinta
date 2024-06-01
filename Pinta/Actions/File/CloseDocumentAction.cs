// 
// CloseDocumentAction.cs
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

namespace Pinta.Actions;

internal sealed class CloseDocumentAction : IActionHandler
{
	void IActionHandler.Initialize ()
	{
		PintaCore.Actions.File.Close.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		PintaCore.Actions.File.Close.Activated -= Activated;
	}

	private void Activated (object sender, EventArgs e)
	{
		// Commit any pending changes
		PintaCore.Tools.Commit ();

		// If it's not dirty, just close it
		if (!PintaCore.Workspace.ActiveDocument.IsDirty) {
			PintaCore.Workspace.CloseActiveDocument ();
			return;
		}

		string heading = Translations.GetString (
			"Save changes to image \"{0}\" before closing?",
			PintaCore.Workspace.ActiveDocument.DisplayName);

		string body = Translations.GetString ("If you don't save, all changes will be permanently lost.");

		Adw.MessageDialog dialog = Adw.MessageDialog.New (PintaCore.Chrome.MainWindow, heading, body);

		const string cancel_response = "cancel";
		const string discard_response = "discard";
		const string save_response = "save";

		dialog.AddResponse (cancel_response, Translations.GetString ("_Cancel"));
		dialog.AddResponse (discard_response, Translations.GetString ("_Discard"));
		dialog.AddResponse (save_response, Translations.GetString ("_Save"));

		// Configure the styling for the save / discard buttons.
		dialog.SetResponseAppearance (discard_response, Adw.ResponseAppearance.Destructive);
		dialog.SetResponseAppearance (save_response, Adw.ResponseAppearance.Suggested);

		dialog.CloseResponse = cancel_response;
		dialog.DefaultResponse = save_response;

		string response = dialog.RunBlocking ();
		if (response == save_response) {
			PintaCore.Workspace.ActiveDocument.Save (false);

			// If the image is still dirty, the user
			// must have cancelled the Save dialog
			if (!PintaCore.Workspace.ActiveDocument.IsDirty)
				PintaCore.Workspace.CloseActiveDocument ();
		} else if (response == discard_response) {
			PintaCore.Workspace.CloseActiveDocument ();
		}

	}
}
