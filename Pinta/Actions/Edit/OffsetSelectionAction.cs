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
using System.Threading.Tasks;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class OffsetSelectionAction : IActionHandler
{
	private readonly EditActions edit;
	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	private readonly IToolService tools;

	internal OffsetSelectionAction (
		EditActions edit,
		IChromeService chrome,
		IWorkspaceService workspace,
		IToolService tools)
	{
		this.edit = edit;
		this.chrome = chrome;
		this.workspace = workspace;
		this.tools = tools;
	}

	void IActionHandler.Initialize ()
	{
		edit.OffsetSelection.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		edit.OffsetSelection.Activated -= Activated;
	}

	private async void Activated (object sender, EventArgs e)
	{
		int? response = await PromptOffsetSelection ();

		if (!response.HasValue) return;

		int newOffset = response.Value;

		tools.Commit ();

		Document document = workspace.ActiveDocument;

		document.Layers.ToolLayer.Clear ();

		SelectionHistoryItem historyItem = new (
			workspace,
			Resources.Icons.EditSelectionOffset,
			Translations.GetString ("Offset Selection"));

		historyItem.TakeSnapshot ();

		document.Selection.Offset (newOffset);
		document.History.PushNewItem (historyItem);
		document.Workspace.Invalidate ();
	}

	private async Task<int?> PromptOffsetSelection ()
	{
		using OffsetSelectionDialog dialog = new (chrome);
		try {
			Gtk.ResponseType response = await dialog.RunAsync ();
			if (response != Gtk.ResponseType.Ok) return null;
			return dialog.Offset;
		} finally {
			dialog.Destroy ();
		}
	}
}
