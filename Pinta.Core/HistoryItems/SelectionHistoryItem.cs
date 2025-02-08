// 
// SelectionHistoryItem.cs
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

namespace Pinta.Core;

public sealed class SelectionHistoryItem : BaseHistoryItem
{
	private DocumentSelection? old_selection;
	private DocumentSelection? old_previous_selection;

	private bool hide_tool_layer;
	private readonly IWorkspaceService workspace;

	public override bool CausesDirty => false;

	public SelectionHistoryItem (
		IWorkspaceService workspace,
		string icon,
		string text
	)
		: base (icon, text)
	{
		this.workspace = workspace;
	}

	public override void Undo ()
	{
		Swap ();
	}

	public override void Redo ()
	{
		Swap ();
	}

	private void Swap ()
	{
		var doc = workspace.ActiveDocument;
		DocumentSelection swap_selection = doc.Selection;
		bool swap_hide_tool_layer = doc.Layers.ToolLayer.Hidden;

		doc.Selection = old_selection!; // NRT - Set in TakeSnapshot
		doc.Layers.ToolLayer.Hidden = hide_tool_layer;

		old_selection = swap_selection;
		hide_tool_layer = swap_hide_tool_layer;

		swap_selection = old_previous_selection!;
		old_previous_selection = doc.PreviousSelection;
		doc.PreviousSelection = swap_selection!;

		workspace.Invalidate ();
	}

	public void TakeSnapshot ()
	{
		var doc = workspace.ActiveDocument;
		old_selection = doc.Selection.Clone ();
		old_previous_selection = doc.PreviousSelection.Clone ();
		hide_tool_layer = doc.Layers.ToolLayer.Hidden;
	}
}
