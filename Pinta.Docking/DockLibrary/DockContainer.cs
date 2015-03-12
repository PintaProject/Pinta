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
using System.Linq;

namespace Pinta.Docking
{
	class DockContainer: Container
	{
		DockLayout layout;
		DockFrame frame;
		
		List<TabStrip> notebooks = new List<TabStrip> ();
		List<DockItem> items = new List<DockItem> ();

		List<SplitterWidget> splitters = new List<SplitterWidget> ();

		bool needsRelayout = true;

		PlaceholderWindow placeholderWindow;
		PadTitleWindow padTitleWindow;
		
		public DockContainer (DockFrame frame)
		{
            GtkWorkarounds.FixContainerLeak (this);
			
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask | EventMask.LeaveNotifyMask;
			this.frame = frame;
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
			HidePlaceholder ();

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
			
			if (this.GdkWindow != null)
				this.GdkWindow.MoveResize (rect);
			
			// This container has its own window, so allocation of children
			// is relative to 0,0
			rect.X = rect.Y = 0;
			LayoutWidgets ();
			layout.Size = -1;
			layout.SizeAllocate (rect);

			usedSplitters = 0;
			layout.DrawSeparators (Gdk.Rectangle.Zero, null, 0, DrawSeparatorOperation.Allocate, null);
		}

		int usedSplitters;

		internal void AllocateSplitter (DockGroup grp, int index, Gdk.Rectangle a)
		{
			var s = splitters[usedSplitters++];
			if (a.Height > a.Width) {
				a.Width = 5;
				a.X -= 2;
			}
			else {
				a.Height = 5;
				a.Y -= 2;
			}
			s.SizeAllocate (a);
			s.Init (grp, index);
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			List<Widget> widgets = new List<Widget> ();
			foreach (Widget w in notebooks)
				widgets.Add (w);
			foreach (DockItem it in items) {
				if (it.HasWidget && it.Widget.Parent == this) {
					widgets.Add (it.Widget);
					if (it.TitleTab.Parent == this)
						widgets.Add (it.TitleTab);
				}
			}
			foreach (var s in splitters.Where (w => w.Parent != null))
				widgets.Add (s);

			foreach (Widget w in widgets)
				callback (w);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			bool res = base.OnExposeEvent (evnt);
			
			if (layout != null) {
				layout.Draw (evnt.Area, null, 0);
			}
			return res;
		}

		protected override void OnAdded (Widget widget)
		{
			System.Diagnostics.Debug.Assert (
				widget.Parent == null,
				"Widget is already parented on another widget");

			widget.Parent = this;
		}

		protected override void OnRemoved (Widget widget)
		{
			System.Diagnostics.Debug.Assert (
				widget.Parent == this,
				"Widget is not parented on this widget");

			widget.Unparent ();
		}

		public void RelayoutWidgets ()
		{
			if (layout != null)
				layout.AddRemoveWidgets ();

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
				frame.UpdateRegionStyle (grp);
				ts.VisualStyle = grp.VisualStyle;
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

			// Create and add the required splitters

			int reqSpliters = CountRequiredSplitters (layout);

			for (int n=0; n < splitters.Count; n++) {
				var s = splitters [n];
				if (s.Parent != null)
					Remove (s);
			}

			// Hide the splitters that are not required

			for (int n=reqSpliters; n < splitters.Count; n++)
				splitters[n].Hide ();

			// Add widgets to the container
			
			layout.LayoutWidgets ();

			// Create and add the required splitters

			for (int n=0; n < reqSpliters; n++) {
				if (n < splitters.Count) {
					var s = splitters [n];
					if (!s.Visible)
						s.Show ();
					Add (s);
				} else {
					var s = new SplitterWidget ();
					splitters.Add (s);
					s.Show ();
					Add (s);
				}
			}
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

		int CountRequiredSplitters (DockGroup grp)
		{
			if (grp.Type == DockGroupType.Tabbed)
				return 0;
			else {
				int num = grp.VisibleObjects.Count - 1;
				if (num < 0)
					return 0;
				foreach (var c in grp.VisibleObjects.OfType<DockGroup> ())
					num += CountRequiredSplitters (c);
				return num;
			}
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
			this.WidgetFlags &= ~WidgetFlags.NoWindow;
			
			//GdkWindow.SetBackPixmap (null, true);

			ModifyBase (StateType.Normal, Styles.DockFrameBackground);
		}
		
		protected override void OnUnrealized ()
		{
			if (this.GdkWindow != null) {
				this.GdkWindow.UserData = IntPtr.Zero;
				this.GdkWindow.Destroy ();
				this.WidgetFlags |= WidgetFlags.NoWindow;
			}
			base.OnUnrealized ();
		}
		
		internal void ShowPlaceholder (DockItem draggedItem)
		{
			padTitleWindow = new PadTitleWindow (frame, draggedItem);
			placeholderWindow = new PlaceholderWindow (frame);
		}
		
		internal bool UpdatePlaceholder (DockItem item, Gdk.Size size, bool allowDocking)
		{
			if (placeholderWindow == null)
				return false;
			
			int px, py;
			GetPointer (out px, out py);
			
			placeholderWindow.AllowDocking = allowDocking;
			
			int ox, oy;
			GdkWindow.GetOrigin (out ox, out oy);

			int tw, th;
			padTitleWindow.GetSize (out tw, out th);
			padTitleWindow.Move (ox + px - tw/2, oy + py - th/2);
			padTitleWindow.GdkWindow.KeepAbove = true;

			DockDelegate dockDelegate;
			Gdk.Rectangle rect;
			if (allowDocking && layout.GetDockTarget (item, px, py, out dockDelegate, out rect)) {
				placeholderWindow.Relocate (ox + rect.X, oy + rect.Y, rect.Width, rect.Height, true);
				placeholderWindow.Show ();
				placeholderWindow.SetDockInfo (dockDelegate, rect);
				return true;
			} else {
				int w,h;
				var gi = layout.FindDockGroupItem (item.Id);
				if (gi != null) {
					w = gi.Allocation.Width;
					h = gi.Allocation.Height;
				} else {
					w = item.DefaultWidth;
					h = item.DefaultHeight;
				}
				placeholderWindow.Relocate (ox + px - w / 2, oy + py - h / 2, w, h, false);
				placeholderWindow.Show ();
				placeholderWindow.AllowDocking = false;
			}

			return false;
		}
		
		internal void DockInPlaceholder (DockItem item)
		{
			if (placeholderWindow == null || !placeholderWindow.Visible)
				return;
			
			if (placeholderWindow.AllowDocking && placeholderWindow.DockDelegate != null) {
				item.Status = DockItemStatus.Dockable;
				DockGroupItem dummyItem = new DockGroupItem (frame, new DockItem (frame, "__dummy"));
				DockGroupItem gitem = layout.FindDockGroupItem (item.Id);
				gitem.ParentGroup.ReplaceItem (gitem, dummyItem);
				placeholderWindow.DockDelegate (item);
				dummyItem.ParentGroup.Remove (dummyItem);
				RelayoutWidgets ();
			} else {
				int px, py;
				GetPointer (out px, out py);
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
			if (padTitleWindow != null) {
				padTitleWindow.Destroy ();
				padTitleWindow = null;
			}
		}
		
		internal class SplitterWidget: EventBox
		{
			static Gdk.Cursor hresizeCursor = new Gdk.Cursor (CursorType.SbHDoubleArrow);
			static Gdk.Cursor vresizeCursor = new Gdk.Cursor (CursorType.SbVDoubleArrow);

			bool dragging;
			int dragPos;
			int dragSize;

			DockGroup dockGroup;
			int dockIndex;
	
			public SplitterWidget ()
			{
				this.VisibleWindow = false;
				this.AboveChild = true;
			}

			public void Init (DockGroup grp, int index)
			{
				dockGroup = grp;
				dockIndex = index;
			}

			protected override void OnSizeAllocated (Rectangle allocation)
			{
				base.OnSizeAllocated (allocation);
			}

			protected override void OnRealized ()
			{
				base.OnRealized ();

				// For testing purposes. Not being shown while VisibleWindow = false
				ModifyBg (StateType.Normal, new Gdk.Color (255,0,0));
				ModifyBase (StateType.Normal, new Gdk.Color (255,0,0));
				ModifyFg (StateType.Normal, new Gdk.Color (255,0,0));
			}

			protected override bool OnEnterNotifyEvent (EventCrossing evnt)
			{
				if (Allocation.Height > Allocation.Width)
					GdkWindow.Cursor = hresizeCursor;
				else
					GdkWindow.Cursor = vresizeCursor;
				return base.OnEnterNotifyEvent (evnt);
			}

			protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
			{
				GdkWindow.Cursor = null;
				return base.OnLeaveNotifyEvent (evnt);
			}

			protected override bool OnButtonPressEvent (Gdk.EventButton ev)
			{
				dragging = true;
				dragPos = (dockGroup.Type == DockGroupType.Horizontal) ? (int)ev.XRoot : (int)ev.YRoot;
				DockObject obj = dockGroup.VisibleObjects [dockIndex];
				dragSize = (dockGroup.Type == DockGroupType.Horizontal) ? obj.Allocation.Width : obj.Allocation.Height;
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
					int newpos = (dockGroup.Type == DockGroupType.Horizontal) ? (int)e.XRoot : (int)e.YRoot;
					if (newpos != dragPos) {
						int nsize = dragSize + (newpos - dragPos);
						dockGroup.ResizeItem (dockIndex, nsize);
					}
				}
				return base.OnMotionNotifyEvent (e);
			}
		}
	}

	enum DrawSeparatorOperation
	{
		Draw,
		Invalidate,
		Allocate,
		CollectAreas
	}
}
