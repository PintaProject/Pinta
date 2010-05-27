//
// DockContainer.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//



using System;
using System.Collections.Generic;
using Gtk;
using Gdk;

namespace MonoDevelop.Components.Docking
{
	class DockContainer: Container, IShadedWidget
	{
		DockLayout layout;
		DockFrame frame;
		
		List<TabStrip> notebooks = new List<TabStrip> ();
		List<DockItem> items = new List<DockItem> ();
		
		bool needsRelayout = true;
		
		DockGroup currentHandleGrp;
		int currentHandleIndex;
		bool dragging;
		int dragPos;
		int dragSize;
		
		PlaceholderWindow placeholderWindow;
		
		static Gdk.Cursor hresizeCursor = new Gdk.Cursor (CursorType.SbHDoubleArrow);
		static Gdk.Cursor vresizeCursor = new Gdk.Cursor (CursorType.SbVDoubleArrow);
		
		public DockContainer (DockFrame frame)
		{
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask | EventMask.LeaveNotifyMask;
			this.frame = frame;
			frame.ShadedContainer.Add (this);
		}

		internal DockGroupItem FindDockGroupItem (string id)
		{
			if (layout == null)
				return null;
			else
				return layout.FindDockGroupItem (id);
		}
		
		public List<DockItem> Items {
			get { return items; }
		}

		public DockLayout Layout {
			get { return layout; }
			set { layout = value; }
		}

		public void Clear ()
		{
			layout = null;
		}

		public void LoadLayout (DockLayout dl)
		{
			// Sticky items currently selected in notebooks will remain
			// selected after switching the layout
			List<DockItem> sickyOnTop = new List<DockItem> ();
			foreach (DockItem it in items) {
				if ((it.Behavior & DockItemBehavior.Sticky) != 0) {
					DockGroupItem gitem = FindDockGroupItem (it.Id);
					if (gitem != null && gitem.ParentGroup.IsSelectedPage (it))
						sickyOnTop.Add (it);
				}
			}			
			
			if (layout != null)
				layout.StoreAllocation ();
			layout = dl;
			layout.RestoreAllocation ();
			
			// Make sure items not present in this layout are hidden
			foreach (DockItem it in items) {
				if ((it.Behavior & DockItemBehavior.Sticky) != 0)
					it.Visible = it.StickyVisible;
				if (layout.FindDockGroupItem (it.Id) == null)
					it.HideWidget ();
			}
			
			RelayoutWidgets ();

			foreach (DockItem it in sickyOnTop)
				it.Present (false);
		}
		
		public void StoreAllocation ()
		{
			if (layout != null)
				layout.StoreAllocation ();
		}
		
		protected override void OnSizeRequested (ref Requisition req)
		{
			if (layout != null) {
				LayoutWidgets ();
				req = layout.SizeRequest ();
			}
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle rect)
		{
			base.OnSizeAllocated (rect);
			if (layout == null)
				return;
			
			// This container has its own window, so allocation of children
			// is relative to 0,0
			rect.X = rect.Y = 0;
			LayoutWidgets ();
			layout.Size = -1;
			layout.SizeAllocate (rect);
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			List<Widget> widgets = new List<Widget> ();
			foreach (Widget w in notebooks)
				widgets.Add (w);
			foreach (DockItem it in items) {
				if (it.HasWidget && it.Widget.Parent == this)
					widgets.Add (it.Widget);
			}
			foreach (Widget w in widgets)
				callback (w);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			bool res = base.OnExposeEvent (evnt);
			
			if (layout != null) {
				layout.Draw (evnt.Area, currentHandleGrp, currentHandleIndex);
			}
			return res;
		}


		public void RelayoutWidgets ()
		{
			needsRelayout = true;
			QueueResize ();
		}
		
		void LayoutWidgets ()
		{
			if (!needsRelayout)
				return;
			needsRelayout = false;
			
			// Create the needed notebooks and place the widgets in there
			
			List<DockGroup> tabbedGroups = new List<DockGroup> ();
			GetTabbedGroups (layout, tabbedGroups);
			
			for (int n=0; n<tabbedGroups.Count; n++) {
				DockGroup grp = tabbedGroups [n];
				TabStrip ts;
				if (n < notebooks.Count) {
					ts = notebooks [n];
				}
				else {
					ts = new TabStrip (frame);
					ts.Show ();
					notebooks.Add (ts);
					ts.Parent = this;
				}
				grp.UpdateNotebook (ts);
			}
			
			// Remove spare tab strips
			for (int n = notebooks.Count - 1; n >= tabbedGroups.Count; n--) {
				TabStrip ts = notebooks [n];
				notebooks.RemoveAt (n);
				ts.Clear ();
				ts.Unparent ();
				ts.Destroy ();
			}
				
			// Add widgets to the container
			
			layout.LayoutWidgets ();
			NotifySeparatorsChanged ();
		}
		
		void GetTabbedGroups (DockGroup grp, List<DockGroup> tabbedGroups)
		{
			if (grp.Type == DockGroupType.Tabbed) {
				if (grp.VisibleObjects.Count > 1)
					tabbedGroups.Add (grp);
				else
					grp.ResetNotebook ();
			}
			else {
				// Make sure it doesn't have a notebook bound to it
				grp.ResetNotebook ();
				foreach (DockObject ob in grp.Objects) {
					if (ob is DockGroup)
						GetTabbedGroups ((DockGroup) ob, tabbedGroups);
				}
			}
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			if (currentHandleGrp != null) {
				dragging = true;
				dragPos = (currentHandleGrp.Type == DockGroupType.Horizontal) ? (int)ev.XRoot : (int)ev.YRoot;
				DockObject obj = currentHandleGrp.VisibleObjects [currentHandleIndex];
				dragSize = (currentHandleGrp.Type == DockGroupType.Horizontal) ? obj.Allocation.Width : obj.Allocation.Height;
			}
			return base.OnButtonPressEvent (ev);
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton e)
		{
			dragging = false;
			return base.OnButtonReleaseEvent (e);
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			if (dragging) {
				NotifySeparatorsChanged ();
				int newpos = (currentHandleGrp.Type == DockGroupType.Horizontal) ? (int)e.XRoot : (int)e.YRoot;
				if (newpos != dragPos) {
					int nsize = dragSize + (newpos - dragPos);
					currentHandleGrp.ResizeItem (currentHandleIndex, nsize);
					layout.DrawSeparators (Allocation, currentHandleGrp, currentHandleIndex, true, null);
				}
			}
			else if (layout != null && placeholderWindow == null) {
				int index;
				DockGroup grp;
				if (FindHandle (layout, (int)e.X, (int)e.Y, out grp, out index)) {
					if (currentHandleGrp != grp || currentHandleIndex != index) {
						if (grp.Type == DockGroupType.Horizontal)
							this.GdkWindow.Cursor = hresizeCursor;
						else
							this.GdkWindow.Cursor = vresizeCursor;
						currentHandleGrp = grp;
						currentHandleIndex = index;
						layout.DrawSeparators (Allocation, currentHandleGrp, currentHandleIndex, true, null);
					}
				}
				else if (currentHandleGrp != null) {
					ResetHandleHighlight ();
				}
			}
			return base.OnMotionNotifyEvent (e);
		}
		
		void ResetHandleHighlight ()
		{
			this.GdkWindow.Cursor = null;
			currentHandleGrp = null;
			currentHandleIndex = -1;
			if (layout != null)
				layout.DrawSeparators (Allocation, null, -1, true, null);
		}
		
		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			if (!dragging && evnt.Mode != CrossingMode.Grab)
				ResetHandleHighlight ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		
		bool FindHandle (DockGroup grp, int x, int y, out DockGroup foundGrp, out int objectIndex)
		{
			if (grp.Type != DockGroupType.Tabbed && grp.Allocation.Contains (x, y)) {
				for (int n=0; n<grp.VisibleObjects.Count; n++) {
					DockObject obj = grp.VisibleObjects [n];
					if (n < grp.Objects.Count - 1) {
						if ((grp.Type == DockGroupType.Horizontal && x > obj.Allocation.Right && x < obj.Allocation.Right + frame.TotalHandleSize) ||
						    (grp.Type == DockGroupType.Vertical && y > obj.Allocation.Bottom && y < obj.Allocation.Bottom + frame.TotalHandleSize))
						{
							foundGrp = grp;
							objectIndex = n;
							return true;
						}
					}
					if (obj is DockGroup) {
						if (FindHandle ((DockGroup) obj, x, y, out foundGrp, out objectIndex))
							return true;
					}
				}
			}
			
			foundGrp = null;
			objectIndex = 0;
			return false;
		}
		
		protected override void OnRealized ()
		{
			WidgetFlags |= WidgetFlags.Realized;
			
			Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
			attributes.X = Allocation.X;
			attributes.Y = Allocation.Y;
			attributes.Height = Allocation.Height;
			attributes.Width = Allocation.Width;
			attributes.WindowType = Gdk.WindowType.Child;
			attributes.Wclass = Gdk.WindowClass.InputOutput;
			attributes.Visual = Visual;
			attributes.Colormap = Colormap;
			attributes.EventMask = (int)(Events |
				Gdk.EventMask.ExposureMask |
				Gdk.EventMask.Button1MotionMask |
				Gdk.EventMask.ButtonPressMask |
				Gdk.EventMask.ButtonReleaseMask);
		
			Gdk.WindowAttributesType attributes_mask =
				Gdk.WindowAttributesType.X |
				Gdk.WindowAttributesType.Y |
				Gdk.WindowAttributesType.Colormap |
				Gdk.WindowAttributesType.Visual;
			GdkWindow = new Gdk.Window (ParentWindow, attributes, (int)attributes_mask);
			GdkWindow.UserData = Handle;

			Style = Style.Attach (GdkWindow);
			Style.SetBackground (GdkWindow, State);
			
			//GdkWindow.SetBackPixmap (null, true);
		}
		
		internal void ShowPlaceholder ()
		{
			placeholderWindow = new PlaceholderWindow (frame);
		}
		
		internal bool UpdatePlaceholder (DockItem item, Gdk.Size size, bool allowDocking)
		{
			if (placeholderWindow == null)
				return false;
			
			int px, py;
			GetPointer (out px, out py);
			
			placeholderWindow.AllowDocking = allowDocking;
			
			DockDelegate dockDelegate;
			Gdk.Rectangle rect;
			if (allowDocking && layout.GetDockTarget (item, px, py, out dockDelegate, out rect)) {
				int ox, oy;
				GdkWindow.GetOrigin (out ox, out oy);
				
				placeholderWindow.Relocate (ox + rect.X, oy + rect.Y, rect.Width, rect.Height, true);
				placeholderWindow.Show ();
				return true;
			} else {
				int ox, oy;
				GdkWindow.GetOrigin (out ox, out oy);
				placeholderWindow.Relocate (ox + px - size.Width / 2, oy + py - 18, size.Width, size.Height, false);
				placeholderWindow.Show ();
			}
			return false;
		}
		
		internal void DockInPlaceholder (DockItem item)
		{
			if (placeholderWindow == null || !placeholderWindow.Visible)
				return;
			
			item.Status = DockItemStatus.Dockable;
			
			int px, py;
			GetPointer (out px, out py);
			
			DockDelegate dockDelegate;
			Gdk.Rectangle rect;
			if (placeholderWindow.AllowDocking && layout.GetDockTarget (item, px, py, out dockDelegate, out rect)) {
				DockGroupItem dummyItem = new DockGroupItem (frame, new DockItem (frame, "__dummy"));
				DockGroupItem gitem = layout.FindDockGroupItem (item.Id);
				gitem.ParentGroup.ReplaceItem (gitem, dummyItem);
				dockDelegate (item);
				dummyItem.ParentGroup.Remove (dummyItem);
				RelayoutWidgets ();
			} else {
				DockGroupItem gi = FindDockGroupItem (item.Id);
				int pw, ph;
				placeholderWindow.GetPosition (out px, out py);
				placeholderWindow.GetSize (out pw, out ph);
				gi.FloatRect = new Rectangle (px, py, pw, ph);
				item.Status = DockItemStatus.Floating;
			}
		}
		
		internal void HidePlaceholder ()
		{
			if (placeholderWindow != null) {
				placeholderWindow.Destroy ();
				placeholderWindow = null;
			}
		}
		
		public IEnumerable<Rectangle> GetShadedAreas ()
		{
			List<Gdk.Rectangle> rects = new List<Gdk.Rectangle> ();
			if (layout != null)
				layout.DrawSeparators (Allocation, currentHandleGrp, currentHandleIndex, true, rects);
			return rects;
		}
		
		internal void NotifySeparatorsChanged ()
		{
			if (AreasChanged != null)
				AreasChanged (this, EventArgs.Empty);
		}
		
		public event EventHandler AreasChanged;

	}
}
