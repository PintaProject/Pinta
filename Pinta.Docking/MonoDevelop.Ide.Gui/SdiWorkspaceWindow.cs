//  SdiWorkspaceWindow.cs
//
// Author:
//   Mike Krüger
//   Lluis Sanchez Gual
//
//  This file was derived from a file from #Develop 2.0
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
//  Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// 
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
// 
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Gtk;

using MonoDevelop.Components;
using Pinta.Docking.DockNotebook;

namespace Pinta.Docking.Gui
{
	public class SdiWorkspaceWindow : EventBox, IWorkbenchWindow
	{
        //DefaultWorkbench workbench;
		IViewContent content;
		
		List<IBaseViewContent> viewContents = new List<IBaseViewContent> ();
		Notebook subViewNotebook = null;
		Tabstrip subViewToolbar = null;
		PathBar pathBar = null;
		HBox toolbarBox = null;
		Dictionary<IBaseViewContent,DocumentToolbar> documentToolbars = new Dictionary<IBaseViewContent, DocumentToolbar> ();

		VBox box;
		DockNotebookTab tab;
		Widget tabPage;
		Pinta.Docking.DockNotebook.DockNotebook tabControl;
		
		string myUntitledTitle = null;
		string _titleHolder = "";
		
		string documentType;
		
		bool show_notification = false;

		uint present_timeout = 0;

		public event EventHandler ViewsChanged;
		
		public Pinta.Docking.DockNotebook.DockNotebook TabControl {
			get {
				return this.tabControl;
			}
		}

        internal void SetDockNotebook (Pinta.Docking.DockNotebook.DockNotebook tabControl, DockNotebookTab tabLabel)
		{
			this.tabControl = tabControl;
			this.tab = tabLabel;
			this.tabPage = content.Control;
			SetTitleEvent(null, null);
			SetDockNotebookTabTitle ();
		}

		public SdiWorkspaceWindow (IViewContent content, Pinta.Docking.DockNotebook.DockNotebook tabControl, DockNotebookTab tabLabel) : base ()
		{
			this.tabControl = tabControl;
			this.content = content;
			this.tab = tabLabel;
			this.tabPage = content.Control;
			
			box = new VBox ();

			viewContents.Add (content);

			//this fires an event that the content uses to access this object's ExtensionContext
			content.WorkbenchWindow = this;

			// The previous WorkbenchWindow property assignement may end with a call to AttachViewContent,
			// which will add the content control to the subview notebook. In that case, we don't need to add it to box
			if (subViewNotebook == null)
				box.PackStart (content.Control);
			
			content.ContentNameChanged += new EventHandler(SetTitleEvent);
			content.DirtyChanged       += HandleDirtyChanged;
			content.BeforeSave         += new EventHandler(BeforeSave);
			content.ContentChanged     += new EventHandler (OnContentChanged);
			box.Show ();
			Add (box);
			
			SetTitleEvent(null, null);
		}

		void HandleDirtyChanged (object sender, EventArgs e)
		{
			OnTitleChanged (null);
		}
		
		public Widget TabPage {
			get {
				return tabPage;
			}
			set {
				tabPage = value;
			}
		}
		
		internal DockNotebookTab TabLabel {
			get { return tab; }
		}

		protected override bool OnWidgetEvent (Gdk.Event evt)
		{
			if (evt.Type == Gdk.EventType.ButtonRelease)
                Pinta.Docking.DockNotebook.DockNotebook.ActiveNotebook = (Pinta.Docking.DockNotebook.DockNotebook)Parent.Parent;

			return base.OnWidgetEvent (evt);
		}
		
		protected virtual void OnDocumentChanged (EventArgs e)
		{
			EventHandler handler = this.DocumentChanged;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler DocumentChanged;
		
		public bool ShowNotification {
			get {
				return show_notification;
			}
			set {
				if (show_notification != value) {
					show_notification = value;
					OnTitleChanged (null);
				}
			}
		}
		
		public string Title {
			get {
				//FIXME: This breaks, Why? --Todd
				//_titleHolder = tabControl.GetTabLabelText (tabPage);
				return _titleHolder;
			}
			set {
				_titleHolder = value;
				string fileName = content.ContentName;
				if (fileName == null) {
					fileName = content.UntitledName;
				}
				OnTitleChanged(null);
			}
		}
		
		public IEnumerable<IAttachableViewContent> SubViewContents {
			get {
				return viewContents.OfType<IAttachableViewContent> ();
			}
		}
		
		// caution use activeView with care !!
		IBaseViewContent activeView = null;
		public IBaseViewContent ActiveViewContent {
			get {
				if (activeView != null)
					return activeView;
				if (subViewToolbar != null)
					return viewContents[subViewToolbar.ActiveTab];
				return content;
			}
			set {
				this.activeView = value;
				this.OnActiveViewContentChanged (new ActiveViewContentEventArgs (value));
			}
		}
		
		public void SwitchView (int viewNumber)
		{
			if (subViewNotebook != null)
				ShowPage (viewNumber);
		}

		public void SwitchView (IAttachableViewContent view)
		{
			if (subViewNotebook != null)
				ShowPage (viewContents.IndexOf (view));
		}
		
		public int FindView<T> ()
		{
			for (int i = 0; i < viewContents.Count; i++) {
				if (viewContents[i] is T)
					return i;
			}

			return -1;
		}

		public void OnDeactivated ()
		{
			if (pathBar != null)
				pathBar.HideMenu ();
		}
		
		public void OnActivated ()
		{
			if (subViewToolbar != null)
				subViewToolbar.Tabs [subViewToolbar.ActiveTab].Activate ();
		}
		
		public void SelectWindow()
		{
			var window = tabControl.Toplevel as Gtk.Window;
			if (window != null)
				window.Present ();

			// The tab change must be done now to ensure that the content is created
			// before exiting this method.
			tabControl.CurrentTabIndex = tab.Index;

			// Focus the tab in the next iteration since presenting the window may take some time
			Application.Invoke (delegate {
                Pinta.Docking.DockNotebook.DockNotebook.ActiveNotebook = tabControl;
				DeepGrabFocus (this.ActiveViewContent.Control);
			});
		}

		public bool CanMoveToNextNotebook ()
		{
			return TabControl.GetNextNotebook () != null || (TabControl.Container.AllowRightInsert && TabControl.TabCount > 1);
		}

		public bool CanMoveToPreviousNotebook ()
		{
			return TabControl.GetPreviousNotebook () != null || (TabControl.Container.AllowLeftInsert && TabControl.TabCount > 1);
		}

		public void MoveToNextNotebook ()
		{
			var nextNotebook = TabControl.GetNextNotebook ();
			if (nextNotebook == null) {
				if (TabControl.Container.AllowRightInsert) {
					TabControl.RemoveTab (tab.Index, true);
					TabControl.Container.InsertRight (this);
					SelectWindow ();
				}
				return;
			}

			TabControl.RemoveTab (tab.Index, true);
			var newTab = nextNotebook.AddTab ();
			newTab.Content = this;
			SetDockNotebook (nextNotebook, newTab);
			SelectWindow ();
		}

		public void MoveToPreviousNotebook ()
		{
			var nextNotebook = TabControl.GetPreviousNotebook ();
			if (nextNotebook == null) {
				if (TabControl.Container.AllowLeftInsert) {
					TabControl.RemoveTab (tab.Index, true);
					TabControl.Container.InsertLeft (this);
					SelectWindow ();
				}
				return;
			}

			TabControl.RemoveTab (tab.Index, true);
			var newTab = nextNotebook.AddTab ();
			newTab.Content = this;
			SetDockNotebook (nextNotebook, newTab);
			SelectWindow ();
		}
		
		static void DeepGrabFocus (Gtk.Widget widget)
		{
			Widget first = null;
			foreach (var f in GetFocussableWidgets (widget)) {
				if (f.HasFocus)
					return;
				if (first == null)
					first = f;
			}
			if (first != null) {
				first.GrabFocus ();
			}
		}
		
		static IEnumerable<Gtk.Widget> GetFocussableWidgets (Gtk.Widget widget)
		{
			var c = widget as Container;
			if (widget.CanFocus)
				yield return widget;
			if (c != null) {
				foreach (var f in c.FocusChain.SelectMany (x => GetFocussableWidgets (x)).Where (y => y != null))
					yield return f;
			}
		}

		public DocumentToolbar GetToolbar (IBaseViewContent targetView)
		{
			DocumentToolbar toolbar;
			if (!documentToolbars.TryGetValue (targetView, out toolbar)) {
				toolbar = new DocumentToolbar ();
				documentToolbars [targetView] = toolbar;
				box.PackStart (toolbar.Container, false, false, 0);
				box.ReorderChild (toolbar.Container, 0);
				toolbar.Visible = (targetView == ActiveViewContent);
			}
			return toolbar;
		}

		void BeforeSave(object sender, EventArgs e)
		{
			IAttachableViewContent secondaryViewContent = ActiveViewContent as IAttachableViewContent;
			if (secondaryViewContent != null) {
				secondaryViewContent.BeforeSave ();
			}
		}
		
		public IViewContent ViewContent {
			get {
				return content;
			}
			set {
				content = value;
			}
		}

		public string DocumentType {
			get {
				return documentType;
			}
			set {
				documentType = value;
			}
		}
		
		public void SetTitleEvent(object sender, EventArgs e)
		{
			if (content == null)
				return;
				
			string newTitle = "";
			if (content.ContentName == null) {
				if (myUntitledTitle == null) {
					string baseName  = System.IO.Path.GetFileNameWithoutExtension(content.UntitledName);
					myUntitledTitle = baseName + System.IO.Path.GetExtension (content.UntitledName);
				}
				newTitle = myUntitledTitle;
			} else {
				newTitle = System.IO.Path.GetFileName(content.ContentName);
			}
			
			if (content.IsDirty) {
                // JONTODO
                //if (!String.IsNullOrEmpty (content.ContentName))
                //    IdeApp.ProjectOperations.MarkFileDirty (content.ContentName);
			} else if (content.IsReadOnly) {
				newTitle += "+";
			}
			if (newTitle != Title) {
				Title = newTitle;
			}
		}
		
		public void OnContentChanged (object o, EventArgs e)
		{
			foreach (IAttachableViewContent subContent in SubViewContents) {
				subContent.BaseContentChanged ();
			}
		}
		
		public bool CloseWindow (bool force)
		{
			return CloseWindow (force, false);
		}

		public bool CloseWindow (bool force, bool animate)
		{
			bool wasActive = true;//workbench.ActiveWorkbenchWindow == this;
			WorkbenchWindowEventArgs args = new WorkbenchWindowEventArgs (force, wasActive);
			args.Cancel = false;
			OnClosing (args);
			if (args.Cancel)
				return false;
			
			//workbench.RemoveTab (tabControl, tab.Index, animate);

			OnClosed (args);

			Destroy ();
			return true;
		}

		protected override void OnDestroyed ()
		{
			if (present_timeout != 0) {
				GLib.Source.Remove (present_timeout);
			}

			base.OnDestroyed ();
			if (viewContents != null) {
				foreach (IAttachableViewContent sv in SubViewContents) {
					sv.Dispose ();
				}
				viewContents = null;
			}

			if (content != null) {
				content.ContentNameChanged -= new EventHandler(SetTitleEvent);
				content.DirtyChanged       -= HandleDirtyChanged;
				content.BeforeSave         -= new EventHandler(BeforeSave);
				content.ContentChanged     -= new EventHandler (OnContentChanged);
				content.WorkbenchWindow     = null;
				content.Dispose ();
				content = null;
			}

			if (subViewToolbar != null) {
				subViewToolbar.Dispose ();
				subViewToolbar = null;
			}
		}
		
		#region lazy UI element creation
		
		void CheckCreateSubViewToolbar ()
		{
			if (subViewToolbar != null)
				return;
			
			subViewToolbar = new Tabstrip ();
			subViewToolbar.Show ();
			
			CheckCreateToolbarBox ();
			toolbarBox.PackStart (subViewToolbar, true, true, 0);
		}
		
		void EnsureToolbarBoxSeparator ()
		{
/*			The path bar is now shown at the top

			if (toolbarBox == null || subViewToolbar == null)
				return;

			if (separatorItem != null && pathBar == null) {
				subViewToolbar.Remove (separatorItem);
				separatorItem = null;
			} else if (separatorItem == null && pathBar != null) {
				separatorItem = new SeparatorToolItem ();
				subViewToolbar.PackStart (separatorItem, false, false, 0);
			} else if (separatorItem != null && pathBar != null) {
				Widget[] buttons = subViewToolbar.Children;
				if (separatorItem != buttons [buttons.Length - 1])
					subViewToolbar.ReorderChild (separatorItem, buttons.Length - 1);
			}
			*/
		}
		
		void CheckCreateToolbarBox ()
		{
			if (toolbarBox != null)
				return;
			toolbarBox = new HBox (false, 0);
			toolbarBox.Show ();
			box.PackEnd (toolbarBox, false, false, 0);
		}
		
		void CheckCreateSubViewContents ()
		{
			if (subViewNotebook != null)
				return;

			// The view may call AttachViewContent when initialized, and this
			// may happen before the main content is added to 'box', so we
			// have to check if the content is already parented or not

			if (this.ViewContent.Control.Parent != null)
				box.Remove (this.ViewContent.Control);
			
			subViewNotebook = new Notebook ();
			subViewNotebook.TabPos = PositionType.Bottom;
			subViewNotebook.ShowTabs = false;
			subViewNotebook.ShowBorder = false;
			subViewNotebook.Show ();
			
			//pack them in a box
			subViewNotebook.Show ();
			box.PackStart (subViewNotebook, true, true, 1);
			box.Show ();
		}

		void ShowDocumentToolbar (DocumentToolbar toolbar)
		{
		}

		#endregion

		public void AttachViewContent (IAttachableViewContent subViewContent)
		{
			InsertViewContent (viewContents.Count, subViewContent);
		}

		public void InsertViewContent (int index, IAttachableViewContent subViewContent)
		{
			// need to create child Notebook when first IAttachableViewContent is added
			CheckCreateSubViewContents ();

			viewContents.Insert (index, subViewContent);
			subViewContent.WorkbenchWindow = this;

			OnContentChanged (null, null);

			if (ViewsChanged != null)
				ViewsChanged (this, EventArgs.Empty);
		}

		bool updating = false;
		
		protected void ShowPage (int npage)
		{
			if (updating || npage < 0) return;
			updating = true;
			subViewToolbar.ActiveTab = npage;
			updating = false;
		}
		
		void SetCurrentView (int newIndex)
		{
			IAttachableViewContent subViewContent;

			int oldIndex = subViewNotebook.CurrentPage;
			subViewNotebook.CurrentPage = newIndex;

			if (oldIndex != -1) {
				subViewContent = viewContents[oldIndex] as IAttachableViewContent;
				if (subViewContent != null)
					subViewContent.Deselected ();
			}

			subViewContent = viewContents[newIndex] as IAttachableViewContent;

			var toolbarVisible = false;
			foreach (var t in documentToolbars) {
				toolbarVisible = ActiveViewContent == t.Key;
				t.Value.Container.Visible = toolbarVisible;
			}

			if (subViewContent != null)
				subViewContent.Selected ();

			OnActiveViewContentChanged (new ActiveViewContentEventArgs (this.ActiveViewContent));
		}

		void SetDockNotebookTabTitle ()
		{
			tab.Text = Title;
			tab.Notify = show_notification;
			tab.Dirty = content.IsDirty;
			if (content.ContentName != null && content.ContentName != "") {
				tab.Tooltip = content.ContentName;
			}
            // JONTODO ?
            //try {
            //    if (content.StockIconId != null) {
            //        tab.Icon = ImageService.GetIcon (content.StockIconId, IconSize.Menu);
            //    }
            //    else
            //        if (content.ContentName != null && content.ContentName.IndexOfAny (new char[] {
            //            '*',
            //            '+'
            //        }) == -1) {
            //            tab.Icon = DesktopService.GetIconForFile (content.ContentName, Gtk.IconSize.Menu);
            //        }
            //}
            //catch (Exception ex) {
            //    LoggingService.LogError (ex.ToString ());
            //    tab.Icon = DesktopService.GetIconForType ("gnome-fs-regular", Gtk.IconSize.Menu);
            //}
		}
		
		protected virtual void OnTitleChanged(EventArgs e)
		{
			SetDockNotebookTabTitle ();
			if (TitleChanged != null) {
				TitleChanged(this, e);
			}
		}

		protected virtual void OnClosing (WorkbenchWindowEventArgs e)
		{
			if (Closing != null) {
				Closing (this, e);
			}
		}

		protected virtual void OnClosed (WorkbenchWindowEventArgs e)
		{
			if (Closed != null) {
				Closed (this, e);
			}
		}
		
		protected virtual void OnActiveViewContentChanged (ActiveViewContentEventArgs e)
		{
			if (ActiveViewContentChanged != null)
				ActiveViewContentChanged (this, e);
		}

		public event EventHandler TitleChanged;
		public event WorkbenchWindowEventHandler Closed;
		public event WorkbenchWindowEventHandler Closing;
		public event ActiveViewContentEventHandler ActiveViewContentChanged;
	}
}
