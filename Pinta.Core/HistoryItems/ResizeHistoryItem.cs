// 
// ResizeHistoryItem.cs
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

public sealed class ResizeHistoryItem : CompoundHistoryItem
{
	private Size old_size;

	public ResizeHistoryItem (Size oldSize) : base ()
	{
		old_size = oldSize;

		Icon = Resources.Icons.ImageResize;
		Text = Translations.GetString ("Resize Image");
	}

	public DocumentSelection? RestoreSelection { get; internal set; }

	public override void Undo ()
	{
		var doc = PintaCore.Workspace.ActiveDocument;

		// maintain the current scaling setting after the operation
		double scale = PintaCore.Workspace.Scale;

		Size swap = PintaCore.Workspace.ImageSize;

		PintaCore.Workspace.ImageSize = old_size;
		PintaCore.Workspace.CanvasSize = old_size;

		old_size = swap;

		base.Undo ();

		if (RestoreSelection != null) {
			doc.Selection = RestoreSelection.Clone ();
		} else {
			doc.ResetSelectionPaths ();
		}

		PintaCore.Workspace.Invalidate ();

		PintaCore.Workspace.Scale = scale;
	}

	public override void Redo ()
	{
		var doc = PintaCore.Workspace.ActiveDocument;

		// maintain the current scaling setting after the operation
		double scale = PintaCore.Workspace.Scale;

		Size swap = PintaCore.Workspace.ImageSize;

		PintaCore.Workspace.ImageSize = old_size;
		PintaCore.Workspace.CanvasSize = old_size;

		old_size = swap;

		base.Redo ();

		doc.ResetSelectionPaths ();
		PintaCore.Workspace.Invalidate ();

		PintaCore.Workspace.Scale = scale;
	}
}
