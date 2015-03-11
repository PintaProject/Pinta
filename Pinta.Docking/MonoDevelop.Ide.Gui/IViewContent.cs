// IViewContent.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//

using System;

namespace Pinta.Docking.Gui
{
    public interface IViewContent : IBaseViewContent
	{
        string ContentName { get; set; }
        string UntitledName { get; set; }
        string StockIconId { get; }

        bool IsUntitled { get; }
        bool IsViewOnly { get;  }
        bool IsFile { get; }
        bool IsDirty { get; set; }
        bool IsReadOnly { get; }

        void Load (string fileName);
		void LoadNew (System.IO.Stream content, string mimeType);
        void Save (string fileName);
        void Save ();
		
		/// <summary>
		/// Discards all changes. This method is called before a dirty file is closed. It tells the view 
		/// content to remove all autosave data of the file.
		/// </summary>
		void DiscardChanges ();

        event EventHandler ContentNameChanged;
        event EventHandler ContentChanged;
        event EventHandler DirtyChanged;
        event EventHandler BeforeSave;
	}
}
