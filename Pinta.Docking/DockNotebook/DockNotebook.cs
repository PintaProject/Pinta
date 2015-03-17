// DragNotebook.cs
//
// Author:
//   Todd Berman  <tberman@off.net>
//
// Copyright (c) 2004 Todd Berman
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

using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Pinta.Docking;
using Pinta.Docking.Gui;

namespace Pinta.Docking.DockNotebook
{
	public delegate void TabsReorderedHandler (Widget widget, int oldPlacement, int newPlacement);

	public class DockNotebook : Gtk.VBox
	{
		List<DockNotebookTab> pages = new List<DockNotebookTab> ();
		List<DockNotebookTab> pagesHistory = new List<DockNotebookTab> ();
		TabStrip tabStrip;
		Gtk.EventBox contentBox;
		ReadOnlyCollection<DockNotebookTab> pagesCol;
		const int MAX_LASTACTIVEWINDOWS = 10;

		DockNotebookTab currentTab;

		static DockNotebook activeNotebook;
		static List<DockNotebook> allNotebooks = new List<DockNotebook> ();

		enum TargetList {
			UriList = 100
		}

		static Gtk.TargetEntry[] targetEntryTypes = new Gtk.TargetEntry[] {
			new Gtk.TargetEntry ("text/uri-list", 0, (uint)TargetList.UriList)
		};

		public DockNotebook ()
		{
			pagesCol = new ReadOnlyCollection<DockNotebookTab> (pages);
			AddEvents ((Int32)(EventMask.AllEventsMask));

			tabStrip = new TabStrip (this);

			PackStart (tabStrip, false, false, 0);

			contentBox = new EventBox ();
			PackStart (contentBox, true, true, 0);

			ShowAll ();

            tabStrip.Visible = DockNotebookManager.TabStripVisible;
            DockNotebookManager.TabStripVisibleChanged += (o, e) => tabStrip.Visible = DockNotebookManager.TabStripVisible;

			contentBox.NoShowAll = true;

			tabStrip.DropDownButton.Sensitive = false;

			tabStrip.DropDownButton.MenuCreator = delegate {
				Gtk.Menu menu = new Menu ();
				foreach (var tab in pages) {
					var mi = new Gtk.ImageMenuItem ("");
					menu.Insert (mi, -1);
					var label = (Gtk.AccelLabel) mi.Child;
					if (tab.Markup != null)
						label.Markup = tab.Markup;
					else
						label.Text = tab.Text;
					var locTab = tab;
					mi.Activated += delegate {
						CurrentTab = locTab;
					};
				}
				menu.ShowAll ();
				return menu;
			};

			Gtk.Drag.DestSet (this, Gtk.DestDefaults.Motion | Gtk.DestDefaults.Highlight | Gtk.DestDefaults.Drop, targetEntryTypes, Gdk.DragAction.Copy);
			DragDataReceived += new Gtk.DragDataReceivedHandler (OnDragDataReceived);

			DragMotion += delegate {
				// Bring this window to the front. Otherwise, the drop may end being done in another window that overlaps this one
				if (!Platform.IsWindows) {
					var window = ((Gtk.Window)Toplevel);
					if (window is DockWindow)
						window.Present ();
				}
			};

			allNotebooks.Add (this);
		}

		internal static DockNotebook ActiveNotebook {
			get { return activeNotebook; }
			set {
				if (activeNotebook != value) {
					if (activeNotebook != null)
						activeNotebook.tabStrip.IsActiveNotebook = false;
					activeNotebook = value;
					if (activeNotebook != null)
						activeNotebook.tabStrip.IsActiveNotebook = true;

                    DockNotebookManager.OnActiveNotebookChanged ();
				}
			}
		}

		internal static IEnumerable<DockNotebook> AllNotebooks {
			get { return allNotebooks; }
		}

		Cursor fleurCursor = new Cursor (CursorType.Fleur);

		public event EventHandler<TabEventArgs> TabActivated;

		public event EventHandler PageAdded;
		public event EventHandler PageRemoved;
		public event EventHandler SwitchPage;

		public event EventHandler PreviousButtonClicked {
			add { tabStrip.PreviousButton.Clicked += value; }
			remove { tabStrip.PreviousButton.Clicked -= value; }
		}

		public event EventHandler NextButtonClicked {
			add { tabStrip.NextButton.Clicked += value; }
			remove { tabStrip.NextButton.Clicked -= value; }
		}

		public bool PreviousButtonEnabled {
			get { return tabStrip.PreviousButton.Sensitive; }
			set { tabStrip.PreviousButton.Sensitive = value; }
		}

		public bool NextButtonEnabled {
			get { return tabStrip.NextButton.Sensitive; }
			set { tabStrip.NextButton.Sensitive = value; }
		}

		public bool NavigationButtonsVisible {
			get { return tabStrip.NavigationButtonsVisible; }
			set { tabStrip.NavigationButtonsVisible = value; }
		}

		public ReadOnlyCollection<DockNotebookTab> Tabs {
			get { return pagesCol; }
		}

		public DockNotebookTab CurrentTab {
			get { return currentTab; }
			set {
				if (currentTab != value) {
					currentTab = value;
					if (contentBox.Child != null)
						contentBox.Remove (contentBox.Child);

					if (currentTab != null) {
						if (currentTab.Content != null) {
							contentBox.Add (currentTab.Content);
							contentBox.ChildFocus (DirectionType.Down);
						}
						pagesHistory.Remove (currentTab);
						pagesHistory.Insert (0, currentTab);
						if (pagesHistory.Count > MAX_LASTACTIVEWINDOWS)
							pagesHistory.RemoveAt (pagesHistory.Count - 1);
					}

					tabStrip.Update ();

					if (SwitchPage != null)
						SwitchPage (this, EventArgs.Empty);

                    DockNotebookManager.OnActiveTabChanged ();
				}
			}
		}

		public int CurrentTabIndex {
			get { return currentTab != null ? currentTab.Index : -1; }
			set { 
				if (value > pages.Count - 1)
					CurrentTab = null;
				else
					CurrentTab = pages [value]; 
			}
		}

		void SelectLastActiveTab (int lastClosed)
		{
			if (pages.Count == 0) {
				CurrentTab = null;
				return;
			}

			while (pagesHistory.Count > 0 && pagesHistory [0].Content == null)
				pagesHistory.RemoveAt (0);

			if (pagesHistory.Count > 0)
				CurrentTab = pagesHistory [0];
			else {
				if (lastClosed + 1 < pages.Count)
					CurrentTab = pages [lastClosed + 1];
				else
					CurrentTab = pages [lastClosed - 1];
			}
		}

		public int TabCount {
			get { return pages.Count; }
		}

		public int BarHeight {
			get { return tabStrip.BarHeight; }
		}

		public void InitSize ()
		{
			tabStrip.InitSize ();
		}

		void OnDragDataReceived (object o, Gtk.DragDataReceivedArgs args)
		{
            DockNotebookManager.OnDragDataReceived (o, args);
        }

		public DockNotebookContainer Container {
			get {
				var container = (DockNotebookContainer)Parent;
				return container.MotherContainer () ?? container;
			}
		}

		/// <summary>
		/// Returns the next notebook in the same window
		/// </summary>
		public DockNotebook GetNextNotebook ()
		{
			return Container.GetNextNotebook (this);
		}

		/// <summary>
		/// Returns the previous notebook in the same window
		/// </summary>
		public DockNotebook GetPreviousNotebook ()
		{
			return Container.GetPreviousNotebook (this);
		}

		public Action<DockNotebook, int,Gdk.EventButton> DoPopupMenu { get; set; }

		public DockNotebookTab AddTab (Gtk.Widget content = null)
		{
			var t = InsertTab (-1);
			if (content != null)
				t.Content = content;
			return t;
		}

        public DockNotebookTab InsertTab (IViewContent content, int index)
        {
            // Create the new tab
            var tab = InsertTab (index);

            // Create a content window and add it to the tab
            var window = new SdiWorkspaceWindow (content, this, tab);
            tab.Content = window;

            tab.Content.Show ();

            return tab;
        }

		public DockNotebookTab InsertTab (int index)
		{
			var tab = new DockNotebookTab (this, tabStrip);
			if (index == -1) {
				pages.Add (tab);
				tab.Index = pages.Count - 1;
			} else {
				pages.Insert (index, tab);
				tab.Index = index;
				UpdateIndexes (index + 1);
			}

			pagesHistory.Add (tab);

			if (pages.Count == 1)
				CurrentTab = tab;

			tabStrip.StartOpenAnimation ((DockNotebookTab)tab);
			tabStrip.Update ();
			tabStrip.DropDownButton.Sensitive = pages.Count > 0;

			if (PageAdded != null)
				PageAdded (this, EventArgs.Empty);

			return tab;
		}

		void UpdateIndexes (int startIndex)
		{
			for (int n=startIndex; n < pages.Count; n++)
				((DockNotebookTab)pages [n]).Index = n;
		}

		public DockNotebookTab GetTab (int n)
		{
			if (n < 0 || n >= pages.Count)
				return null;
			else
				return pages [n];
		}

		public void RemoveTab (int page, bool animate)
		{
			var tab = pages [page];
			if (animate)
				tabStrip.StartCloseAnimation ((DockNotebookTab)tab);
			pagesHistory.Remove (tab);
			if (pages.Count == 1)
				CurrentTab = null;
			else if (page == CurrentTabIndex)
				SelectLastActiveTab (page);
			pages.RemoveAt (page);
			UpdateIndexes (page);
			tabStrip.Update ();
			tabStrip.DropDownButton.Sensitive = pages.Count > 0;

			if (PageRemoved != null)
				PageRemoved (this, EventArgs.Empty);
		}

		internal void ReorderTab (DockNotebookTab tab, DockNotebookTab targetTab)
		{
			if (tab == targetTab)
				return;
			int targetPos = targetTab.Index;
			if (tab.Index > targetTab.Index) {
				pages.RemoveAt (tab.Index);
				pages.Insert (targetPos, tab);
			} else {
				pages.Insert (targetPos + 1, tab);
				pages.RemoveAt (tab.Index);
			}
            // JONTODO
			//IdeApp.Workbench.ReorderDocuments (tab.Index, targetPos);
			UpdateIndexes (Math.Min (tab.Index, targetPos));
			tabStrip.Update ();
		}

        // Returns true if the tab was successfully closed
		internal bool OnCloseTab (DockNotebookTab tab)
		{
            var e = new TabClosedEventArgs () { Tab = tab };

			DockNotebookManager.OnTabClosed (this, e);

            return !e.Cancel;
		}

		internal void OnActivateTab (DockNotebookTab tab)
		{
			if (TabActivated != null)
				TabActivated (this, new TabEventArgs () { Tab = tab });
		}
		
		internal void ShowContent (DockNotebookTab tab)
		{
			if (tab == currentTab)
				contentBox.Child = tab.Content;
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			ActiveNotebook = this;
			return base.OnButtonPressEvent (evnt);
		}

		protected override void OnDestroyed ()
		{
			allNotebooks.Remove (this);
			if (ActiveNotebook == this)
				ActiveNotebook = null;
			if (fleurCursor != null) {
				fleurCursor.Dispose ();
				fleurCursor = null;
			}
			base.OnDestroyed ();
		}
	}
}
