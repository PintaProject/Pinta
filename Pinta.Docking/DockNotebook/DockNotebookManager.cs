// 
// DockNotebookManager.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gtk;

namespace Pinta.Docking.DockNotebook
{
    public static class DockNotebookManager
    {
        private static List<DockNotebookContainer> all_containers = new List<DockNotebookContainer> ();
        private static bool tab_strip_visible = true;

        public static event EventHandler ActiveNotebookChanged;
        public static event EventHandler ActiveTabChanged;
        public static event EventHandler<DragDataReceivedArgs> NotebookDragDataReceived;
        public static event EventHandler TabStripVisibleChanged;
        public static event EventHandler<TabClosedEventArgs> TabClosed;

        public static IEnumerable<DockNotebookContainer> AllContainers {
            get { return new ReadOnlyCollection<DockNotebookContainer> (all_containers); }
        }

		public static IEnumerable<DockNotebook> AllNotebooks {
			get { return DockNotebook.AllNotebooks; }
		}

        public static IEnumerable<DockNotebookTab> AllTabs {
            get { return AllNotebooks.SelectMany (p => p.Tabs); }
        }

        public static DockNotebook ActiveNotebook {
            get { return DockNotebook.ActiveNotebook; }
            set { DockNotebook.ActiveNotebook = value; }
        }

        public static DockNotebookContainer ActiveNotebookContainer {
            get {
                return ActiveNotebook == null ? null : ActiveNotebook.Parent as DockNotebookContainer;
            }
        }

        public static DockNotebookTab ActiveTab {
            get {
                return ActiveNotebook == null ? null : ActiveNotebook.CurrentTab;
            }
        }

        public static bool TabStripVisible {
            get { return tab_strip_visible; }
            set {
                if (tab_strip_visible != value) {
                    tab_strip_visible = value;

                    if (TabStripVisibleChanged != null)
                        TabStripVisibleChanged (null, EventArgs.Empty);
                }
            }
        }

        #region Internal Methods
        internal static void AddContainer (DockNotebookContainer container)
        {
            all_containers.Add (container);
        }

        internal static void RemoveContainer (DockNotebookContainer container)
        {
            all_containers.Remove (container);
        }

        internal static void OnActiveNotebookChanged ()
        {
            if (ActiveNotebookChanged != null)
                ActiveNotebookChanged (null, EventArgs.Empty);
        }

        internal static void OnActiveTabChanged ()
        {
            if (ActiveTabChanged != null)
                ActiveTabChanged (null, EventArgs.Empty);
        }

        internal static void OnDragDataReceived (object sender, Gtk.DragDataReceivedArgs e)
        {
            if (NotebookDragDataReceived != null)
                NotebookDragDataReceived (sender, e);
        }

        internal static void OnTabClosed (object sender, TabClosedEventArgs e)
        {
            if (TabClosed != null)
                TabClosed (sender, e);
        }
        #endregion
    }
}
