// 
// HistoryManager.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinta.Core
{
	public class HistoryManager
	{
		List<BaseHistoryItem> history = new List<BaseHistoryItem> ();
		int stack_pointer = -1;
		
		public void PushNewItem (BaseHistoryItem item)
		{
			// If we have un-did items on the history stack, they
			// all get destroyed before we add a new item
			while (history.Count - 1 > stack_pointer) {
				BaseHistoryItem base_item = history[history.Count - 1];
				history.RemoveAt (history.Count - 1);
				base_item.Dispose ();

				PintaCore.Actions.Edit.Redo.Sensitive = false;
				// TODO: Delete from ListStore
			}
			
			history.Add (item);
			stack_pointer++;

			PintaCore.Workspace.IsDirty = true;
			PintaCore.Actions.Edit.Undo.Sensitive = true;
			
			OnHistoryItemAdded (item);
		}
		
		public void Undo ()
		{
			if (stack_pointer < 0)
				throw new InvalidOperationException ("Undo stack is empty");
			
			BaseHistoryItem item = history[stack_pointer--];
			item.Undo ();
			
			if (stack_pointer == -1)
				PintaCore.Actions.Edit.Undo.Sensitive = false;
			
			PintaCore.Actions.Edit.Redo.Sensitive = true;
			OnActionUndone ();
			OnHistoryItemRemoved (item);
		}
		
		public void Redo ()
		{
			if (stack_pointer == history.Count - 1)
				throw new InvalidOperationException ("Redo stack is empty");

			BaseHistoryItem item = history[++stack_pointer];
			item.Redo ();

			if (stack_pointer == history.Count - 1)
				PintaCore.Actions.Edit.Redo.Sensitive = false;

			PintaCore.Actions.Edit.Undo.Sensitive = true;
			OnActionUndone ();
			OnHistoryItemAdded (item);
		}
		
		public void Clear ()
		{
			while (history.Count > 0) {
				BaseHistoryItem base_item = history[history.Count - 1];
				history.RemoveAt (history.Count - 1);
				base_item.Dispose ();

				// TODO: Delete from ListStore
			}
			
			stack_pointer = -1;
			
			PintaCore.Actions.Edit.Redo.Sensitive = false;
			PintaCore.Actions.Edit.Undo.Sensitive = false;
		}

		#region Protected Methods
		protected void OnHistoryItemAdded (BaseHistoryItem item)
		{
			if (HistoryItemAdded != null)
				HistoryItemAdded (this, new HistoryItemAddedEventArgs (item));
		}
		
		protected void OnHistoryItemRemoved (BaseHistoryItem item)
		{
			if (HistoryItemRemoved != null)
			{
				HistoryItemRemoved (this, new HistoryItemRemovedEventArgs (item));
			}
		}

		protected void OnActionUndone ()
		{
			if (ActionUndone != null)
				ActionUndone (this, EventArgs.Empty);
		}

		protected void OnActionRedone ()
		{
			if (ActionRedone != null)
				ActionRedone (this, EventArgs.Empty);
		}
		#endregion

		#region Events
		public event EventHandler<HistoryItemAddedEventArgs> HistoryItemAdded;
		public event EventHandler<HistoryItemRemovedEventArgs> HistoryItemRemoved;
		public event EventHandler ActionUndone;
		public event EventHandler ActionRedone;
		#endregion
	}
}
