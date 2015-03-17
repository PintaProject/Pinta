//
// DockNotebookContainer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using Pinta.Docking.Gui;
using MonoDevelop.Components;

namespace Pinta.Docking.DockNotebook
{
	public class DockNotebookContainer : EventBox
	{
		bool isMasterTab;
		bool splitsInitialized;
		DockNotebook tabControl;

		int MAX_SPLITS = 1;

		public bool AllowLeftInsert {
			get {
				return SplitCount < MAX_SPLITS;
			}
		}

		public bool AllowRightInsert {
			get {
				return SplitCount < MAX_SPLITS;
			}
		}

		public DockNotebook TabControl {
			get {
				return tabControl;
			}
		}

		public DockNotebookContainer (DockNotebook tabControl, bool isMasterTab = false)
		{
			this.isMasterTab = isMasterTab;
			this.tabControl = tabControl;
			Child = tabControl;
			
            DockNotebookManager.AddContainer (this);

			if (!isMasterTab)
				tabControl.PageRemoved += HandlePageRemoved;
		}

		public int SplitCount {
			get {
				if (Child is DockNotebook)
					return 0;
				return GetSplitCount (Child); 
			}
		}

		int GetSplitCount (Widget w)
		{
			var p = w as Paned;
			if (p != null)
				return 1 + GetSplitCount (p.Child1) + GetSplitCount (p.Child2);
			else
				return ((DockNotebookContainer)w).SplitCount;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (!splitsInitialized) {
				splitsInitialized = true;
				if (Child is HPaned) {
					var p = (HPaned)Child;
					p.Position = allocation.Width / 2;
				}
				else if (Child is VPaned) {
					var p = (VPaned)Child;
					p.Position = allocation.Height / 2;
				}
			}
		}

		internal DockNotebookContainer MotherContainer ()
		{
			if (Parent == null)
				return null;

			var paned = Parent as Paned;
			return paned != null ? (DockNotebookContainer)paned.Parent : null;
		}

		public void SetSingleMode ()
		{
			var notebooks = GetNotebooks ().ToArray ();
			if (notebooks.Length <= 1)
				return;

			var single = notebooks [0];
			for (int n = 1; n < notebooks.Length; n++) {
				var nb = notebooks [n];
				var tabCount = nb.TabCount;

				for (var i = 0; i < tabCount; i++) {
					var tab = nb.GetTab (0);
					var window = (SdiWorkspaceWindow)tab.Content;
					nb.RemoveTab (0, false);

					var newTab = single.AddTab (window);
					window.SetDockNotebook (single, newTab);
				}
			}
		}

        public void ActivateTab (DockNotebookTab tab)
        {
            DockNotebook.ActiveNotebook = tab.Notebook;
            tab.Notebook.CurrentTab = tab;
        }

        public void ActivateTabWithWidget (Widget widget)
        {
            foreach (var notebook in GetNotebooks ())
            foreach (var tab in notebook.Tabs) {
                if (tab.Content == widget) {
                    DockNotebook.ActiveNotebook = notebook;
                    notebook.CurrentTab = tab;

                    return;
                }
            }
        }

        public void CloseTab (DockNotebookTab tab)
        {
            var notebook = tab.Notebook;
            var index = notebook.Tabs.IndexOf (tab);

            notebook.RemoveTab (index, true);
        }

        public void CloseTabWithWidget (Widget widget)
        {
            foreach (var notebook in GetNotebooks ())
                for (var i = 0; i < notebook.Tabs.Count; i++) {
                    var tab = notebook.Tabs[i];

                    if (tab.Content == widget) {
                        notebook.RemoveTab (i, true);

                        return;
                    }
                }
        }

		public static DockWindow MoveToFloatingWindow (SdiWorkspaceWindow workspaceWindow)
		{
			return MoveToFloatingWindow (workspaceWindow, 0, 0, 640, 480);
		}

		public static DockWindow MoveToFloatingWindow (SdiWorkspaceWindow workspaceWindow, int x, int y, int width, int height)
		{
			var window = new DockWindow ();
			var notebook = window.Container.GetFirstNotebook ();
			var tab = notebook.AddTab ();
			tab.Content = workspaceWindow;

            // jontodo
			//window.Title = DefaultWorkbench.GetTitle (workspaceWindow);

			workspaceWindow.SetDockNotebook (notebook, tab);

			window.Move (x, y);
			window.Resize (width, height);
			window.ShowAll ();

			return window;
		}

		static void HandlePageRemoved (object sender, EventArgs e)
		{
			var control = (DockNotebook)sender;
			if (control.TabCount != 0)
				return;
			var controlContainer = control.Parent as DockNotebookContainer;
			if (controlContainer == null || controlContainer.Parent == null || controlContainer.isMasterTab)
				return;
			
			var paned = controlContainer.Parent as Paned;
			if (paned != null) {
				var otherContainer = (paned.Child1 == control.Parent ? paned.Child2 : paned.Child1) as DockNotebookContainer;
				if (otherContainer == null)
					return;
				
				var motherContainer = (DockNotebookContainer)paned.Parent;

				var newChild = otherContainer.Child;
				otherContainer.Remove (newChild);
				
				motherContainer.tabControl = otherContainer.tabControl;
                //if (motherContainer.isMasterTab) {
                //    ((DefaultWorkbench)IdeApp.Workbench).TabControl = (DockNotebook)motherContainer.tabControl;
                //}
				motherContainer.isMasterTab |= otherContainer.isMasterTab;
				motherContainer.Remove (paned);
				motherContainer.Child = newChild;
				motherContainer.ShowAll ();
				paned.Destroy ();
				return;
			}
			
			// window case.
			controlContainer.Parent.Destroy ();
		}

		DockNotebook Insert (SdiWorkspaceWindow window, Action<DockNotebookContainer> callback)
		{
			var newNotebook = new DockNotebook ();

			newNotebook.NavigationButtonsVisible = false;
			newNotebook.InitSize ();
			var newContainer = new DockNotebookContainer (newNotebook);
			newNotebook.PageRemoved += HandlePageRemoved;

			if (window != null) {
				var newTab = newNotebook.AddTab (window);
				window.SetDockNotebook (newNotebook, newTab);
			}
			Remove (Child);

			callback (newContainer);

			tabControl.InitSize ();
			ShowAll ();
			return newNotebook;
		}

		public DockNotebook InsertLeft (SdiWorkspaceWindow window)
		{
			return Insert (window, container => {
				var box = new HPanedThin { GrabAreaSize = 6 };
				var new_container = new DockNotebookContainer (tabControl);

				box.Pack1 (container, true, true);
				box.Pack2 (new_container, true, true);
				Child = box;
			});
		}

		public DockNotebook InsertRight (SdiWorkspaceWindow window)
		{
			return Insert (window, container => {
				var box = new HPanedThin () { GrabAreaSize = 6 };
				var new_container = new DockNotebookContainer (tabControl);

				box.Pack1 (new_container, true, true);
				box.Pack2 (container, true, true);
				box.Position = Allocation.Width / 2;
				Child = box;
			});
		}

		public DockNotebook GetFirstNotebook ()
		{
			var p = Child;
			while (true) {
				if (p is DockNotebook)
					return (DockNotebook)p;
				if (p is DockNotebookContainer)
					return ((DockNotebookContainer)p).TabControl;
				p = ((Paned)p).Child1;
			}
		}

		public DockNotebook GetLastNotebook ()
		{
			var p = Child;
			while (true) {
				if (p is DockNotebook)
					return (DockNotebook)p;
				if (p is DockNotebookContainer)
					return ((DockNotebookContainer)p).TabControl;
				p = ((Paned)p).Child2;
			}
		}

		/// <summary>
		/// Returns the next notebook in the same window
		/// </summary>
		public DockNotebook GetNextNotebook (DockNotebook current)
		{
			var container = (DockNotebookContainer)current.Parent;
			var rootContainer = current.Container;
			if (container == rootContainer)
				return null;

			Widget curChild = container;
			var paned = (Paned)container.Parent;
			do {
				if (paned.Child1 == curChild)
					return ((DockNotebookContainer)paned.Child2).GetFirstNotebook ();
				curChild = paned;
				paned = paned.Parent as Paned;
			}
			while (paned != null);
			return null;
		}

		public IEnumerable<DockNotebook> GetNotebooks ()
		{
			var nb = GetFirstNotebook ();
			while (nb != null) {
				yield return nb;
				nb = GetNextNotebook (nb);
			}
		}

		/// <summary>
		/// Returns the previous notebook in the same window
		/// </summary>
		public DockNotebook GetPreviousNotebook (DockNotebook current)
		{
			var container = (DockNotebookContainer)current.Parent;
			var rootContainer = current.Container;
			if (container == rootContainer)
				return null;

			Widget curChild = container;
			var paned = (Paned)container.Parent;
			do {
				if (paned.Child2 == curChild)
					return ((DockNotebookContainer)paned.Child1).GetLastNotebook ();
				curChild = paned;
				paned = paned.Parent as Paned;
			}
			while (paned != null);
			return null;
		}

        protected override void OnDestroyed ()
        {
            DockNotebookManager.RemoveContainer (this);

            base.OnDestroyed ();
        }
	}
}

