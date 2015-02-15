// 
// BaseHistoryItem.cs
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
using Gtk;

namespace Pinta.Core
{
	public enum HistoryItemState { Undo, Redo }

	public class BaseHistoryItem : IDisposable
	{
		public string Icon { get; set; }
		public string Text { get; set; }
		public HistoryItemState State { get; set; }
		public TreeIter Id;
		public virtual bool CausesDirty { get { return true; } }
		
		public BaseHistoryItem ()
		{
		}
		
		public BaseHistoryItem (string icon, string text)
		{
			Icon = icon;
			Text = text;
			State = HistoryItemState.Undo;
		}
		
		public BaseHistoryItem (string icon, string text, HistoryItemState state)
		{
			Icon = icon;
			Text = text;
			State = state;
		}

		public virtual void Undo ()
		{
		}

		public virtual void Redo ()
		{
		}

        protected void Swap<T> (ref T x, ref T y)
        {
            T temp = x;
            x = y;
            y = temp;
        }

		#region IDisposable Members
		public virtual void Dispose ()
		{
		}
		#endregion
	}
}
