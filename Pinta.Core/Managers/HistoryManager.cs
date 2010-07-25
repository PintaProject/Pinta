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
		public Gtk.ListStore ListStore {
			get { return PintaCore.Workspace.ActiveWorkspace.History.ListStore; }
		}
		
		public int Pointer {
			get { return PintaCore.Workspace.ActiveWorkspace.History.Pointer; }
		}
		
		public BaseHistoryItem Current {
			get { return PintaCore.Workspace.ActiveWorkspace.History.Current; }
		}
		
		public void PushNewItem (BaseHistoryItem newItem)
		{
			PintaCore.Workspace.ActiveWorkspace.History.PushNewItem (newItem);
		}
		
		public void Undo ()
		{
			PintaCore.Workspace.ActiveWorkspace.History.Undo ();
		}
		
		public void Redo ()
		{
			PintaCore.Workspace.ActiveWorkspace.History.Redo ();
		}
		
		public void Clear ()
		{
			PintaCore.Workspace.ActiveWorkspace.History.Clear ();
		}
		
		#region Protected Methods
		protected internal void OnHistoryItemAdded (BaseHistoryItem item)
		{
			if (HistoryItemAdded != null)
				HistoryItemAdded (this, new HistoryItemAddedEventArgs (item));
		}

		protected internal void OnHistoryItemRemoved (BaseHistoryItem item)
		{
			if (HistoryItemRemoved != null)
				HistoryItemRemoved (this, new HistoryItemRemovedEventArgs (item));
		}

		protected internal void OnActionUndone ()
		{
			if (ActionUndone != null)
				ActionUndone (this, EventArgs.Empty);
		}

		protected internal void OnActionRedone ()
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
