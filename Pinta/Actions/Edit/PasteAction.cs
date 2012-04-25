// 
// PasteAction.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>, Cameron White <cameronwhite91@gmail.com>
// 
// Copyright (c) 2012 Jonathan Pobst, Cameron White
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
using Cairo;
using Gtk;
using Mono.Unix;
using Pinta.Core;

namespace Pinta.Actions
{
	class PasteAction : IActionHandler
	{
		private const string markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}";

		#region IActionHandler Members
		public void Initialize ()
		{
			PintaCore.Actions.Edit.Paste.Activated += Activated;
		}

		public void Uninitialize ()
		{
			PintaCore.Actions.Edit.Paste.Activated -= Activated;
		}
		#endregion

		private void Activated (object sender, EventArgs e)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			PintaCore.Tools.Commit ();

			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

			Path p;

			// Don't dispose this, as we're going to give it to the history
			Gdk.Pixbuf image = cb.WaitForImage ();

			if (image == null)
			{
				Dialogs.ClipboardEmptyDialog.Show ();
				return;
			}

			Gdk.Size canvas_size = PintaCore.Workspace.ImageSize;

			// If the image being pasted is larger than the canvas size, allow the user to optionally resize the canvas
			if (image.Width > canvas_size.Width || image.Height > canvas_size.Height)
			{
				ResponseType response = ShowExpandCanvasDialog ();

				if (response == ResponseType.Accept)
				{
					PintaCore.Workspace.ResizeCanvas (image.Width, image.Height, Pinta.Core.Anchor.Center);
					PintaCore.Actions.View.UpdateCanvasScale ();
				}
				else if (response == ResponseType.Cancel || response == ResponseType.DeleteEvent)
				{
					return;
				}
			}

			// Copy the paste to the temp layer
			doc.CreateSelectionLayer ();
			doc.ShowSelectionLayer = true;

			using (Cairo.Context g = new Cairo.Context (doc.SelectionLayer.Surface))
			{
				g.DrawPixbuf (image, new Cairo.Point (0, 0));
				p = g.CreateRectanglePath (new Rectangle (0, 0, image.Width, image.Height));
			}

			PintaCore.Tools.SetCurrentTool (Catalog.GetString ("Move Selected Pixels"));

			Path old_path = doc.SelectionPath;
			bool old_show_selection = doc.ShowSelection;

			doc.SelectionPath = p;
			doc.ShowSelection = true;

			doc.Workspace.Invalidate ();

			doc.History.PushNewItem (new PasteHistoryItem (image, old_path, old_show_selection));
		}

		private ResponseType ShowExpandCanvasDialog ()
		{
			string primary = Catalog.GetString ("Image larger than canvas");
			string secondary = Catalog.GetString ("The image being pasted is larger than the canvas size. What would you like to do?");
			string message = string.Format (markup, primary, secondary);

			var enlarge_dialog = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal, MessageType.Question, ButtonsType.None, message);
			enlarge_dialog.AddButton (Catalog.GetString ("Expand canvas"), ResponseType.Accept);
			enlarge_dialog.AddButton (Catalog.GetString ("Don't change canvas size"), ResponseType.Reject);
			enlarge_dialog.AddButton (Stock.Cancel, ResponseType.Cancel);
			enlarge_dialog.DefaultResponse = ResponseType.Accept;

			ResponseType response = (ResponseType)enlarge_dialog.Run ();
			enlarge_dialog.Destroy ();

			return response;
		}
	}
}
