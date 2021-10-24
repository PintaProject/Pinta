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
using Pinta.Docking;

namespace Pinta
{
    class DocumentViewContent : IDockNotebookItem
    {
        private CanvasWindow canvas_window;

        public Document Document { get; private set; }

        public DocumentViewContent (Document document, CanvasWindow canvasWindow)
        {
            this.Document = document;
            this.canvas_window = canvasWindow;

            document.IsDirtyChanged += (o, e) => LabelChanged?.Invoke (this, EventArgs.Empty);
            document.Renamed += (o, e) => { LabelChanged?.Invoke(this, EventArgs.Empty); };
        }

        public event EventHandler? LabelChanged;

        public string Label => Document.Filename + (Document.IsDirty ? "*" : string.Empty);

        public Gtk.Widget Widget { get { return canvas_window; } }
    }
}
