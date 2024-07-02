// 
// OffsetSelectionAction.cs
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
internal sealed class OffsetSelectionAction : IActionHandler
{
	private readonly EditActions edit_actions;
	private readonly ChromeManager chrome_manager;
	private readonly WorkspaceManager workspace_manager;
	private readonly ToolManager tool_manager;

	internal OffsetSelectionAction (
		EditActions editActions,
		ChromeManager chromeManager,
		WorkspaceManager workspaceManager,
		ToolManager toolManager)
	{
		edit_actions = editActions;
		chrome_manager = chromeManager;
		workspace_manager = workspaceManager;
		tool_manager = toolManager;
	}

	void IActionHandler.Initialize ()
	{
		edit_actions.OffsetSelection.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		edit_actions.OffsetSelection.Activated -= Activated;
	}

	private void Activated (object sender, EventArgs e)
	{
		OffsetSelectionDialog dialog = new (chrome_manager);

		dialog.OnResponse += (_, args) => {

			if (args.ResponseId == (int) Gtk.ResponseType.Ok) {

				tool_manager.Commit ();

				Document document = workspace_manager.ActiveDocument;

				document.Layers.ToolLayer.Clear ();

				SelectionHistoryItem historyItem = new (Resources.Icons.EditSelectionOffset, Translations.GetString ("Offset Selection"));
				historyItem.TakeSnapshot ();

				document.Selection.Offset (dialog.Offset);

				document.History.PushNewItem (historyItem);
				document.Workspace.Invalidate ();
			}

			dialog.Destroy ();
		};

		dialog.Show ();
	}
}
