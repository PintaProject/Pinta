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

using System;
using Cairo;

namespace Pinta.Core
{
	public class SelectionHistoryItem : BaseHistoryItem
	{
		private Path old_path;
		private bool show_selection;

		public override bool CausesDirty { get { return false; } }

		public SelectionHistoryItem (string icon, string text) : base (icon, text)
		{
		}

		public override void Undo ()
		{
			Swap ();
		}

		public override void Redo ()
		{
			Swap ();
		}

		public override void Dispose ()
		{
			if (old_path != null)
				(old_path as IDisposable).Dispose ();
		}

		private void Swap ()
		{
			Path swap_path = PintaCore.Layers.SelectionPath;
			bool swap_show = PintaCore.Layers.ShowSelection;

			PintaCore.Layers.SelectionPath = old_path;
			PintaCore.Layers.ShowSelection = show_selection;

			old_path = swap_path;
			show_selection = swap_show;
			
			PintaCore.Workspace.Invalidate ();
		}
		
		public void TakeSnapshot ()
		{
			old_path = PintaCore.Layers.SelectionPath.Clone ();
			show_selection = PintaCore.Layers.ShowSelection;
		}
	}
}
