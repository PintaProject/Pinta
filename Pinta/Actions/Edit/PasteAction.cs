// 
// PasteAction.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//       Cameron White <cameronwhite91@gmail.com>
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
using Gtk;
using Pinta.Core;

namespace Pinta.Actions
{
	class PasteAction : IActionHandler
	{
		public void Initialize () => PintaCore.Actions.Edit.Paste.Activated += Activated;

		public void Uninitialize () => PintaCore.Actions.Edit.Paste.Activated -= Activated;

		private void Activated (object sender, EventArgs e)
		{
			// If no documents are open, activate the
			// PasteIntoNewImage action and abort this Paste action.
			if (!PintaCore.Workspace.HasOpenDocuments) {
				PintaCore.Actions.Edit.PasteIntoNewImage.Activate ();
				return;
			}

			var doc = PintaCore.Workspace.ActiveDocument;

			// Get the scroll position in canvas co-ordinates
			var view = (Viewport) doc.Workspace.Canvas.Parent;
			var canvasPos = doc.Workspace.WindowPointToCanvas (
				view.Hadjustment.Value,
				view.Vadjustment.Value);

			// Paste into the active document.
			// The 'false' argument indicates that paste should be
			// performed into the current (not a new) layer.
			Paste (doc, false, (int) canvasPos.X, (int) canvasPos.Y);
		}

		/// <summary>
		/// Pastes an image from the clipboard.
		/// </summary>
		/// <param name="toNewLayer">Set to TRUE to paste into a
		/// new layer.  Otherwise, will paste to the current layer.</param>
		/// <param name="x">Optional. Location within image to paste to.
		/// Position will be adjusted if pasted image would hang
		/// over right or bottom edges of canvas.</param>
		/// <param name="y">Optional. Location within image to paste to.
		/// Position will be adjusted if pasted image would hang
		/// over right or bottom edges of canvas.</param>
		public static void Paste (Document doc, bool toNewLayer, int x = 0, int y = 0)
		{
			// Create a compound history item for recording several
			// operations so that they can all be undone/redone together.
			var history_text = toNewLayer ? Translations.GetString ("Paste Into New Layer") : Translations.GetString ("Paste");
			var paste_action = new CompoundHistoryItem (Resources.StandardIcons.EditPaste, history_text);

			var cb = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

			// See if the current tool wants to handle the paste
			// operation (e.g., the text tool could paste text)
			if (!toNewLayer) {
				if (PintaCore.Tools.DoHandlePaste (doc, cb))
					return;
			}

			// Commit any unfinished tool actions
			PintaCore.Tools.Commit ();

			// Don't dispose this, as we're going to give it to the history
			Gdk.Pixbuf? cbImage = null;

			if (cb.WaitIsImageAvailable ())
				cbImage = cb.WaitForImage ();

			if (cbImage is null) {
				ShowClipboardEmptyDialog ();
				return;
			}

			var canvas_size = PintaCore.Workspace.ImageSize;

			// If the image being pasted is larger than the canvas size, allow the user to optionally resize the canvas
			if (cbImage.Width > canvas_size.Width || cbImage.Height > canvas_size.Height) {
				var response = ShowExpandCanvasDialog ();

				if (response == ResponseType.Accept) {
					var new_width = Math.Max (canvas_size.Width, cbImage.Width);
					var new_height = Math.Max (canvas_size.Height, cbImage.Height);
					PintaCore.Workspace.ResizeCanvas (new_width, new_height, Pinta.Core.Anchor.Center, paste_action);
					PintaCore.Actions.View.UpdateCanvasScale ();
				} else if (response == ResponseType.Cancel || response == ResponseType.DeleteEvent) {
					return;
				}
			}

			// If the pasted image would fall off bottom- or right-
			// side of image, adjust paste position
			x = Math.Max (0, Math.Min (x, canvas_size.Width - cbImage.Width));
			y = Math.Max (0, Math.Min (y, canvas_size.Height - cbImage.Height));

			// If requested, create a new layer, make it the current
			// layer and record it's creation in the history
			if (toNewLayer) {
				var l = doc.Layers.AddNewLayer (string.Empty);
				doc.Layers.SetCurrentUserLayer (l);
				paste_action.Push (new AddLayerHistoryItem (Resources.Icons.LayerNew, Translations.GetString ("Add New Layer"), doc.Layers.IndexOf (l)));
			}

			// Copy the paste to the temp layer, which should be at least the size of this document.
			doc.Layers.CreateSelectionLayer (Math.Max (doc.ImageSize.Width, cbImage.Width),
							 Math.Max (doc.ImageSize.Height, cbImage.Height));
			doc.Layers.ShowSelectionLayer = true;

			using (var g = new Cairo.Context (doc.Layers.SelectionLayer.Surface))
				g.DrawPixbuf (cbImage, new Cairo.Point (0, 0));

			doc.Layers.SelectionLayer.Transform.InitIdentity ();
			doc.Layers.SelectionLayer.Transform.Translate (x, y);

			PintaCore.Tools.SetCurrentTool ("MoveSelectedTool");

			var old_selection = doc.Selection.Clone ();

			doc.Selection.CreateRectangleSelection (new Cairo.Rectangle (x, y, cbImage.Width, cbImage.Height));
			doc.Selection.Visible = true;

			doc.Workspace.Invalidate ();

			paste_action.Push (new PasteHistoryItem (cbImage, old_selection));
			doc.History.PushNewItem (paste_action);
		}

		public static void ShowClipboardEmptyDialog ()
		{
			var primary = Translations.GetString ("Image cannot be pasted");
			var secondary = Translations.GetString ("The clipboard does not contain an image.");
			var markup = $"<span weight=\"bold\" size=\"larger\">{primary}</span>\n\n{secondary}\n";

			DialogExtensions.ShowErrorDialog (markup);
		}

		public static ResponseType ShowExpandCanvasDialog ()
		{
			var primary = Translations.GetString ("Image larger than canvas");
			var secondary = Translations.GetString ("The image being pasted is larger than the canvas size. What would you like to do?");
			var markup = $"<span weight=\"bold\" size=\"larger\">{primary}</span>\n\n{secondary}";

			return DialogExtensions.ShowQuestionDialog (markup,
				new DialogButton (Translations.GetString ("Expand canvas"), ResponseType.Accept, true),
				new DialogButton (Translations.GetString ("Don't change canvas size"), ResponseType.Reject),
				new DialogButton (Stock.Cancel, ResponseType.Cancel));
		}
	}
}
