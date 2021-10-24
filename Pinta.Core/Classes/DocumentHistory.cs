// 
// DocumentHistory.cs
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

namespace Pinta.Core
{
	public class DocumentHistory
	{
		private readonly Document document;
		private readonly List<BaseHistoryItem> history = new List<BaseHistoryItem> ();
		private int clean_pointer = -1;

		public event EventHandler<HistoryItemAddedEventArgs>? HistoryItemAdded;
		public event EventHandler? ActionUndone;
		public event EventHandler? ActionRedone;

		internal DocumentHistory (Document document)
		{
			this.document = document;
		}

		public bool CanRedo => Pointer < history.Count - 1;
		public bool CanUndo => Pointer > 0;

		public BaseHistoryItem? Current {
			get {
				if (Pointer > -1 && Pointer < history.Count)
					return history[Pointer];
				else
					return null;
			}
		}

		public ListStore ListStore { get; } = new ListStore (typeof (BaseHistoryItem));

		public int Pointer { get; private set; } = -1;

		public void PushNewItem (BaseHistoryItem newItem)
		{
			// Remove all old redos starting from the end of the list
			for (var i = history.Count - 1; i >= 0; i--) {				
				var item = history[i];

				if (item.State == HistoryItemState.Redo) {
					history.RemoveAt (i);
					item.Dispose ();

					//Remove from ListStore
					ListStore.Remove (ref item.Id);
				} else if (item.State == HistoryItemState.Undo) {
					break;
				}
			}

			//Add new undo to ListStore
			newItem.Id = ListStore.AppendValues (newItem);
			history.Add (newItem);
			Pointer = history.Count - 1;

			if (newItem.CausesDirty)
				document.IsDirty = true;

			if (history.Count > 1)
				PintaCore.Actions.Edit.Undo.Sensitive = true;

			PintaCore.Actions.Edit.Redo.Sensitive = false;

			HistoryItemAdded?.Invoke (this, new HistoryItemAddedEventArgs (newItem));
		}

		public void Undo ()
		{
			if (Pointer < 0) {
				throw new InvalidOperationException ("Undo stack is empty");
			} else {
				var item = history[Pointer];
				item.Undo ();
				item.State = HistoryItemState.Redo;

				if (item.CausesDirty)
					document.IsDirty = true;

				ListStore.SetValue (item.Id, 0, item);
				history[Pointer] = item;
				Pointer--;
			}

			if (Pointer == clean_pointer)
				document.IsDirty = false;

			if (Pointer == 0)
				PintaCore.Actions.Edit.Undo.Sensitive = false;

			PintaCore.Actions.Edit.Redo.Sensitive = true;

			ActionUndone?.Invoke (this, EventArgs.Empty);
		}

		public void Redo ()
		{
			if (Pointer >= history.Count - 1)
				throw new InvalidOperationException ("Redo stack is empty");

			Pointer++;

			var item = history[Pointer];
			item.Redo ();
			item.State = HistoryItemState.Undo;
			ListStore.SetValue (item.Id, 0, item);
			history[Pointer] = item;

			if (Pointer == history.Count - 1)
				PintaCore.Actions.Edit.Redo.Sensitive = false;

			if (Pointer == clean_pointer)
				document.IsDirty = false;
			else if (item.CausesDirty)
				document.IsDirty = true;

			if (history.Count > 1)
				PintaCore.Actions.Edit.Undo.Sensitive = true;

			ActionRedone?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Mark the document as being clean at the current point in the history stack.
		/// This might be used after e.g. saving the document to disk.
		/// The document's IsDirty property is also set to false.
		/// </summary>
		public void SetClean ()
		{
			clean_pointer = Pointer;
			document.IsDirty = false;
		}

		/// <summary>
		/// Mark the document history as being dirty. No matter where we are in the history
		/// stack the user will be prompted to Save if the document is closed.
		/// </summary>
		public void SetDirty ()
		{
			clean_pointer = -1;
			document.IsDirty = true;
		}

		public void Clear ()
		{
			history.ForEach (delegate (BaseHistoryItem item) { item.Dispose (); });
			history.Clear ();
			ListStore.Clear ();
			Pointer = -1;
			clean_pointer = -1;

			document.IsDirty = false;

			PintaCore.Actions.Edit.Redo.Sensitive = false;
			PintaCore.Actions.Edit.Undo.Sensitive = false;
		}
	}
}
