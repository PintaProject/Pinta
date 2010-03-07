// 
// HistoryManager.cs
//  
// Authors:
//       Jonathan Pobst <monkey@jpobst.com>
//       Joe Hillenbrand <joehillen@gmail.com>
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
using Gtk;

namespace Pinta.Core
{
	public class HistoryManager
	{
		List<BaseHistoryItem> history = new List<BaseHistoryItem> ();
		int historyPointer = -1;
		
		public int Pointer {
			get { return historyPointer; }
		}
		
		public BaseHistoryItem Current {
			get { 
				if (historyPointer > -1 && historyPointer < history.Count)
					return history[historyPointer]; 
				else
					return null;
			}
		}
		
		public void PushNewItem (BaseHistoryItem new_item)
		{
			
			//Remove all old redos starting from the end of the list
			for (int i = history.Count - 1; i >= 0; i--) {
			
				BaseHistoryItem item = history[i];
				
				if (item.State == HistoryItemState.Redo) {
					history.RemoveAt(i);
					item.Dispose();
					//Remove from ListStore
					PintaCore.HistoryListStore.Remove (ref item.Id);
					
				} else if (item.State == HistoryItemState.Undo) {
					break;
				}
			}
		
			//Add new undo to ListStore
			new_item.Id = PintaCore.HistoryListStore.AppendValues (new_item);
			history.Add (new_item);
			historyPointer = history.Count - 1;
			
			if (new_item.CausesDirty)
				PintaCore.Workspace.IsDirty = true;
				
			PintaCore.Actions.Edit.Undo.Sensitive = true;
			PintaCore.Actions.Edit.Redo.Sensitive = false;
			OnHistoryItemAdded (new_item);
		}
		
		public void Undo ()
		{
			if (historyPointer < 0) {
				throw new InvalidOperationException ("Undo stack is empty");
			} else {
				BaseHistoryItem item = history[historyPointer];
				item.Undo ();
				item.State = HistoryItemState.Redo;
				PintaCore.HistoryListStore.SetValue (item.Id, 0, item);
				history[historyPointer] = item;
				historyPointer--;
			}	
			
			if (historyPointer == -1) {
				PintaCore.Workspace.IsDirty = false;
				PintaCore.Actions.Edit.Undo.Sensitive = false;
			}
			
			PintaCore.Actions.Edit.Redo.Sensitive = true;
			OnActionUndone ();
		}
		
		public void Redo ()
		{
			if (historyPointer >= history.Count - 1)
				throw new InvalidOperationException ("Redo stack is empty");

			historyPointer++;
			BaseHistoryItem item = history[historyPointer];
			item.Redo ();
			item.State = HistoryItemState.Undo;
			PintaCore.HistoryListStore.SetValue (item.Id, 0, item);
			history[historyPointer] = item;

			if (historyPointer == history.Count - 1)
				PintaCore.Actions.Edit.Redo.Sensitive = false;
				
			if (item.CausesDirty)
				PintaCore.Workspace.IsDirty = true;
				
			PintaCore.Actions.Edit.Undo.Sensitive = true;
			OnActionRedone ();
		}
		
		public void Clear ()
		{
			history.ForEach (delegate(BaseHistoryItem item) { item.Dispose (); } );
			history.Clear();	
			PintaCore.HistoryListStore.Clear ();	
			historyPointer = -1;
			
			PintaCore.Workspace.IsDirty = false;
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
				HistoryItemRemoved (this, new HistoryItemRemovedEventArgs (item));
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
