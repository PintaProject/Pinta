// 
// CanvasPad.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2011 Jonathan Pobst
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
using Mono.Unix;
using Pinta.Docking;
using Pinta.Docking.DockNotebook;

namespace Pinta
{
	public class CanvasPad : IDockPad
	{
        public DockNotebookContainer NotebookContainer { get; private set; }

		public void Initialize (DockFrame workspace, Menu padMenu)
		{
            var tab = new DockNotebook () {
                NavigationButtonsVisible = false
            };

            NotebookContainer = new DockNotebookContainer (tab, true);

            tab.InitSize ();

            var canvas_dock = workspace.AddItem ("Canvas");
            canvas_dock.Behavior = DockItemBehavior.Locked;
            canvas_dock.Expand = true;

            canvas_dock.DrawFrame = false;
            canvas_dock.Label = Catalog.GetString ("Canvas");
            canvas_dock.Content = NotebookContainer;
        }
	}
}
