// 
// DocumentWorkspace.cs
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
using Gtk;
using System.IO;

namespace Pinta.Core
{
	public class DocumentWorkspaceHistory
	{
		private Document document;
		private DocumentWorkspace workspace;
		List<ProxyHistoryItem> history = new List<ProxyHistoryItem> ();
		int historyPointer = -1;
		FileStream stream;
		readonly int MemoryStackSize = 1;

		internal DocumentWorkspaceHistory (Document document, DocumentWorkspace workspace)
		{
			this.document = document;
			this.workspace = workspace;
			ListStore = new ListStore (typeof (ProxyHistoryItem));
			stream = File.Open("history.dat", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
		}

		public Gtk.ListStore ListStore { get; private set; }
		
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

		public void PushNewItem (BaseHistoryItem newItem)
		{
			
			//Remove all old redos starting from the end of the list
			for (int i = history.Count - 1; i >= 0; i--) {
			
				ProxyHistoryItem item = history[i];
				
				if (item.State == HistoryItemState.Redo) {
					history.RemoveAt(i);
					item.Dispose();
					//Remove from ListStore
					ListStore.Remove (ref item.Id);
					//TODO seek to last cached item
				} else if (item.State == HistoryItemState.Undo) {
					break;
				}
			}
		
			//Add new undo to ListStore
			ProxyHistoryItem item2 = new ProxyHistoryItem(newItem);
			item2.Id = ListStore.AppendValues (item2);
			history.Add (item2);
			historyPointer = history.Count - 1;
			
			if (newItem.CausesDirty)
				document.IsDirty = true;
				
			if (history.Count > 1) {
				PintaCore.Actions.Edit.Undo.Sensitive = true;
				CanUndo = true;
			}

			if (history.Count > MemoryStackSize) {
				BinaryWriter writer = new BinaryWriter (stream);
				history[history.Count - MemoryStackSize - 1].Save (writer);
			}

			PintaCore.Actions.Edit.Redo.Sensitive = false;
			CanRedo = false;
			PintaCore.History.OnHistoryItemAdded (newItem);
		}
		
		public void Undo ()
		{
			if (historyPointer < 0) {
				throw new InvalidOperationException ("Undo stack is empty");
			} else {
				ProxyHistoryItem item = history[historyPointer];
				item.Undo ();
				item.State = HistoryItemState.Redo;
				ListStore.SetValue (item.Id, 0, item);
				history[historyPointer] = item;
				historyPointer--;
			}	
			
			if (historyPointer == 0) {
				document.IsDirty = false;
				PintaCore.Actions.Edit.Undo.Sensitive = false;
				CanUndo = false;
			}
			
			PintaCore.Actions.Edit.Redo.Sensitive = true;
			CanRedo = true;
			PintaCore.History.OnActionUndone ();
		}
		
		public void Redo ()
		{
			if (historyPointer >= history.Count - 1)
				throw new InvalidOperationException ("Redo stack is empty");

			historyPointer++;
			ProxyHistoryItem item = history[historyPointer];
			item.Redo ();
			item.State = HistoryItemState.Undo;
			ListStore.SetValue (item.Id, 0, item);
			history[historyPointer] = item;

			if (historyPointer == history.Count - 1) {
				PintaCore.Actions.Edit.Redo.Sensitive = false;
				CanRedo = false;
			}
				
			if (item.CausesDirty)
				document.IsDirty = true;

			if (history.Count > 1) {
				PintaCore.Actions.Edit.Undo.Sensitive = true;
				CanUndo = true;
			}

			if (history.Count > MemoryStackSize) {
				BinaryWriter writer = new BinaryWriter (stream);
				history[historyPointer - MemoryStackSize].Save(writer);
			}

			PintaCore.History.OnActionRedone ();
		}
		
		public void Clear ()
		{
			history.ForEach (delegate(ProxyHistoryItem item) { item.Dispose (); } );
			history.Clear();	
			ListStore.Clear ();	
			historyPointer = -1;

			document.IsDirty = false;
			PintaCore.Actions.Edit.Redo.Sensitive = false;
			PintaCore.Actions.Edit.Undo.Sensitive = false;

			stream.Seek (0, SeekOrigin.Begin);
			
			CanRedo = false;
			CanUndo = false;

		}

		public BaseHistoryItem LoadCachedItem (long offset)
		{
			BinaryReader reader = new BinaryReader (stream);
			stream.Seek (offset, System.IO.SeekOrigin.Begin);
			BaseHistoryItem item = BaseHistoryItem.Load (reader);
			//I have load data so I can ovewrite this cache
			// but this will corrupt next item if I overflow the buffer
			//so seek end for security
			//seems to not be the issue of corrupted image surface
			//reader.BaseStream.Seek (offset, System.IO.SeekOrigin.Begin);
			reader.BaseStream.Seek (0, System.IO.SeekOrigin.End);
			return item;
		}
		
		public bool CanRedo { get; private set; }
		public bool CanUndo { get; private set; }
	}
}
