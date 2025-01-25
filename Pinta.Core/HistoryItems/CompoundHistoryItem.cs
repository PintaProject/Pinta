// 
// CompoundHistoryItem.cs
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

using System.Collections.Generic;
using Cairo;

namespace Pinta.Core;

public class CompoundHistoryItem : BaseHistoryItem
{
	protected List<BaseHistoryItem> history_stack = [];
	private List<ImageSurface>? snapshots;

	public CompoundHistoryItem () : base ()
	{
	}

	public CompoundHistoryItem (string icon, string text) : base (icon, text)
	{
	}

	public void Push (BaseHistoryItem item)
	{
		history_stack.Add (item);
	}

	public override void Undo ()
	{
		for (int i = history_stack.Count - 1; i >= 0; i--)
			history_stack[i].Undo ();
	}

	public override void Redo ()
	{
		// We want to redo the actions in the
		// opposite order than the undo order
		foreach (var item in history_stack)
			item.Redo ();
	}

	public void StartSnapshotOfImage ()
	{
		snapshots = [];
		foreach (UserLayer item in PintaCore.Workspace.ActiveDocument.Layers.UserLayers) {
			snapshots.Add (item.Surface.Clone ());
		}
	}

	public void FinishSnapshotOfImage ()
	{
		for (int i = 0; i < snapshots!.Count; ++i) { // NRT - Set in StartSnapshotOfImage
			history_stack.Add (new SimpleHistoryItem (string.Empty, string.Empty, snapshots[i], i));
		}
		snapshots.Clear ();
	}
}
