// 
// DocumentViewContent.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2015 Jonathan Pobst
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
using Pinta.Core;
using Pinta.Docking.Gui;

namespace Pinta
{
    class DocumentViewContent : IViewContent
    {
        private CanvasWindow canvas_window;

        public Document Document { get; private set; }

        public DocumentViewContent (Document document, CanvasWindow canvasWindow)
        {
            this.Document = document;
            this.canvas_window = canvasWindow;

            document.IsDirtyChanged += (o, e) => IsDirty = document.IsDirty;
            document.Renamed += (o, e) => { if (ContentNameChanged != null) ContentNameChanged (this, EventArgs.Empty); };
        }

        #region IViewContent Members
        public event EventHandler ContentNameChanged;
        public event EventHandler ContentChanged;
        public event EventHandler DirtyChanged;
        public event EventHandler BeforeSave;

        public string ContentName {
            get { return Document.Filename; }
            set { Document.Filename = value; }
        }

        public string FullContentName {
            get { return Document.PathAndFileName; }
            set { Document.PathAndFileName = value; }
        }

        public string UntitledName { get; set; }

        // We don't put icons on the tabs
        public string StockIconId {
            get { return string.Empty; }
        }

        public bool IsUntitled {
            get { return false; }
        }

        public bool IsViewOnly {
            get { return false; }
        }

        public bool IsFile {
            get { return true; }
        }

        public bool IsDirty {
            get { return Document.IsDirty; }
            set {
                if (DirtyChanged != null)
                    DirtyChanged (this, EventArgs.Empty);
            }
        }

        // can remove?
        public bool IsReadOnly {
            get { return false; }
        }

        public void Load (string fileName)
        {
        }

        public void LoadNew (System.IO.Stream content, string mimeType)
        {
        }

        public void Save (string fileName)
        {
        }

        public void Save ()
        {
        }

        public void DiscardChanges ()
        {
        }
        #endregion

        #region IBaseViewContent Members
        public IWorkbenchWindow WorkbenchWindow { get; set; }

        public Gtk.Widget Control {
            get { return canvas_window; }
        }

        public string TabPageLabel {
            get { return string.Empty; }
        }

        public object GetContent (Type type)
        {
            return null;
        }

        public bool CanReuseView (string fileName)
        {
            return false;
        }

        public void RedrawContent ()
        {
        }
        #endregion

        #region IDisposable Members
        public void Dispose ()
        {
            if (canvas_window != null)
                canvas_window.Dispose ();
        }
        #endregion
    }
}
