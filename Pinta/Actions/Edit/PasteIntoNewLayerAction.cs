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
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class PasteIntoNewLayerAction : IActionHandler
{
	private readonly ActionManager action_manager;
	private readonly ChromeManager chrome_manager;
	private readonly WorkspaceManager workspace_manager;
	private readonly ToolManager tool_manager;
	internal PasteIntoNewLayerAction (
		ActionManager actionManager,
		ChromeManager chromeManager,
		WorkspaceManager workspaceManager,
		ToolManager toolManager)
	{
		action_manager = actionManager;
		chrome_manager = chromeManager;
		workspace_manager = workspaceManager;
		tool_manager = toolManager;
	}

	void IActionHandler.Initialize ()
	{
		action_manager.Edit.PasteIntoNewLayer.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		action_manager.Edit.PasteIntoNewLayer.Activated -= Activated;
	}

	private void Activated (object sender, EventArgs e)
	{
		// If no documents are open, activate the
		// PasteIntoNewImage action and abort this Paste action.
		if (!workspace_manager.HasOpenDocuments) {
			action_manager.Edit.PasteIntoNewImage.Activate ();
			return;
		}

		var doc = workspace_manager.ActiveDocument;

		// Get the scroll position in canvas coordinates
		var view = (Gtk.Viewport) doc.Workspace.Canvas.Parent!;

		PointD viewPoint = new (
			X: view.Hadjustment!.Value,
			Y: view.Vadjustment!.Value
		);

		PointD canvasPos = doc.Workspace.ViewPointToCanvas (viewPoint);

		// Paste into the active document.
		// The 'true' argument indicates that paste should be
		// performed into a new layer.
		PasteAction.Paste (
			actions: action_manager,
			chrome: chrome_manager,
			workspace: workspace_manager,
			tools: tool_manager,
			doc: doc,
			toNewLayer: true,
			pastePosition: canvasPos.ToInt ()
		);
	}
}
