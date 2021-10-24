// 
// PasteIntoNewLayerAction.cs
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
using Pinta.Core;

namespace Pinta.Actions
{
	class PasteIntoNewLayerAction : IActionHandler
	{
		public void Initialize () => PintaCore.Actions.Edit.PasteIntoNewLayer.Activated += Activated;

		public void Uninitialize () => PintaCore.Actions.Edit.PasteIntoNewLayer.Activated -= Activated;

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
			// The 'true' argument indicates that paste should be
			// performed into a new layer.
			PasteAction.Paste (doc, true, (int) canvasPos.X, (int) canvasPos.Y);
		}
	}
}
