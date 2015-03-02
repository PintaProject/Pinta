//
// MonoDevelop.Components.Docking.cs
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
using System.Xml;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Components.Docking
{
	class DockGroup: DockObject
	{
		DockGroupType type;
		List<DockObject> dockObjects = new List<DockObject> ();
		List<DockObject> visibleObjects;
		AllocStatus allocStatus = AllocStatus.NotSet;
		TabStrip boundTabStrip;
		DockGroupItem tabFocus;
		int currentTabPage;
		
		enum AllocStatus { NotSet, Invalid, RestorePending, NewSizeRequest, Valid };
		
		public DockGroup (DockFrame frame, DockGroupType type): base (frame)
		{
			this.type = type;
		}
		
		internal DockGroup (DockFrame frame): base (frame)
		{
		}
		
		public DockGroupType Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public List<DockObject> Objects {
			get { return dockObjects; }
		}
		
		void MarkForRelayout ()
		{
			if (allocStatus == AllocStatus.Valid)
				allocStatus = AllocStatus.Invalid;
		}
		
		public void AddObject (DockObject obj)
		{
			obj.ParentGroup = this;
			dockObjects.Add (obj);
			ResetVisibleGroups ();
		}
		
		public DockGroupItem AddObject (DockItem obj, DockPosition pos, string relItemId)
		{
			int npos = -1;
			if (relItemId != null) {
				for (int n=0; n<dockObjects.Count; n++) {
					DockGroupItem it = dockObjects [n] as DockGroupItem;
					if (it != null && it.Id == relItemId)
						npos = n;
				}
			}
			
			if (npos == -1) {
				if (pos == DockPosition.Left || pos == DockPosition.Top)
					npos = 0;
				else
					npos = dockObjects.Count - 1;
			}
			
			DockGroupItem gitem = null;
			
			if (pos == DockPosition.Left || pos == DockPosition.Right) {
				if (type != DockGroupType.Horizontal)
					gitem = Split (DockGroupType.Horizontal, pos == DockPosition.Left, obj, npos);
				else
					gitem = InsertObject (obj, npos, pos);
			}
			else if (pos == DockPosition.Top || pos == DockPosition.Bottom) {
				if (type != DockGroupType.Vertical)
					gitem = Split (DockGroupType.Vertical, pos == DockPosition.Top, obj, npos);
				else
					gitem = InsertObject (obj, npos, pos);
			}
			else if (pos == DockPosition.CenterBefore || pos == DockPosition.Center) {
				if (type != DockGroupType.Tabbed)
					gitem = Split (DockGroupType.Tabbed, pos == DockPosition.CenterBefore, obj, npos);
				else {
					if (pos == DockPosition.Center)
						npos++;
					gitem = new DockGroupItem (Frame, obj);
					dockObjects.Insert (npos, gitem);
					gitem.ParentGroup = this;
				}
			}
			ResetVisibleGroups ();
			return gitem;
		}
		
		DockGroupItem InsertObject (DockItem obj, int npos, DockPosition pos)
		{
			if (pos == DockPosition.Bottom || pos == DockPosition.Right)
				npos++;
				
			DockGroupItem gitem = new DockGroupItem (Frame, obj);
			dockObjects.Insert (npos, gitem);
			gitem.ParentGroup = this;
			return gitem;
		}
		
		DockGroupItem Split (DockGroupType newType, bool addFirst, DockItem obj, int npos)
		{
			DockGroupItem item = new DockGroupItem (Frame, obj);
			
			if (npos == -1 || type == DockGroupType.Tabbed) {
				if (ParentGroup != null && ParentGroup.Type == newType) {
					// No need to split. Just add the new item as a sibling of this one.
					int i = ParentGroup.Objects.IndexOf (this);
					if (addFirst)
						ParentGroup.Objects.Insert (i, item);
					else
						ParentGroup.Objects.Insert (i+1, item);
					item.ParentGroup = ParentGroup;
					item.ResetDefaultSize ();
				}
				else {
					DockGroup grp = Copy ();
					dockObjects.Clear ();
					if (addFirst) {
						dockObjects.Add (item);
						dockObjects.Add (grp);
					} else {
						dockObjects.Add (grp);
						dockObjects.Add (item);
					}
					item.ParentGroup = this;
					item.ResetDefaultSize ();
					grp.ParentGroup = this;
					grp.ResetDefaultSize ();
					Type = newType;
				}
			}
			else {
				DockGroup grp = new DockGroup (Frame, newType);
				DockObject replaced = dockObjects[npos];
				if (addFirst) {
					grp.AddObject (item);
					grp.AddObject (replaced);
				} else {
					grp.AddObject (replaced);
					grp.AddObject (item);
				}
				grp.CopySizeFrom (replaced);
				dockObjects [npos] = grp;
				grp.ParentGroup = this;
			}
			return item;
		}
		
		internal DockGroup FindGroupContaining (string id)
		{
			DockGroupItem it = FindDockGroupItem (id);
			if (it != null)
				return it.ParentGroup;
			else
				return null;
		}
		
		internal DockGroupItem FindDockGroupItem (string id)
		{
			foreach (DockObject ob in dockObjects) {
				DockGroupItem it = ob as DockGroupItem;
				if (it != null && it.Id == id)
					return it;
				DockGroup g = ob as DockGroup;
				if (g != null) {
					it = g.FindDockGroupItem (id);
					if (it != null)
						return it;
				}
			}
			return null;
		}
		
		DockGroup Copy ()
		{
			DockGroup grp = new DockGroup (Frame, type);
			grp.dockObjects = new List<MonoDevelop.Components.Docking.DockObject> (dockObjects);
			foreach (DockObject obj in grp.dockObjects)
				obj.ParentGroup = grp;
			
			grp.CopySizeFrom (this);
			return grp;
		}
		
		public int GetObjectIndex (DockObject obj)
		{
			for (int n=0; n<dockObjects.Count; n++) {
				if (dockObjects [n] == obj)
					return n;
			}
			return -1;
		}
		
		public bool RemoveItemRec (DockItem item)
		{
			foreach (DockObject ob in dockObjects) {
				if (ob is DockGroup) {
					if (((DockGroup)ob).RemoveItemRec (item))
						return true;
				} else {
					DockGroupItem dit = ob as DockGroupItem;
					if (dit != null && dit.Item == item) {
						Remove (ob);
						return true;
					}
				}
			}
			return false;
		}
		
		public void Remove (DockObject obj)
		{
			dockObjects.Remove (obj);
			Reduce ();
			obj.ParentGroup = null;
			visibleObjects = null;
			
			if (VisibleObjects.Count > 0) {
				CalcNewSizes ();
				MarkForRelayout ();
			} else
				ParentGroup.UpdateVisible (this);
		}
		
		public void Reduce ()
		{
			if (ParentGroup != null && dockObjects.Count == 1) {
				DockObject obj = dockObjects [0];
				int n = ParentGroup.GetObjectIndex (this);
				ParentGroup.dockObjects [n] = obj;
				obj.ParentGroup = ParentGroup;
				obj.CopySizeFrom (this);
				dockObjects.Clear ();
				ResetVisibleGroups ();
				ParentGroup.ResetVisibleGroups ();
			}
		}
		
		internal List<DockObject> VisibleObjects {
			get {
				if (visibleObjects == null) {
					visibleObjects = new List<DockObject> ();
					foreach (DockObject obj in dockObjects)
						if (obj.Visible)
							visibleObjects.Add (obj);
				}
				return visibleObjects;
			}
		}
		
		void ResetVisibleGroups ()
		{
			visibleObjects = null;
			MarkForRelayout ();
		}
		
		internal void UpdateVisible (DockObject child)
		{
			visibleObjects = null;
			bool visChanged;
			MarkForRelayout ();
			
			visChanged = child.Visible ? VisibleObjects.Count == 1 : VisibleObjects.Count == 0;
			
			if (visChanged && ParentGroup != null)
				ParentGroup.UpdateVisible (this);
		}
		
		internal override void RestoreAllocation ()
		{
			base.RestoreAllocation ();
			allocStatus = Size >= 0 ? AllocStatus.RestorePending : AllocStatus.NotSet;
			
			// Make a copy because RestoreAllocation can fire events such as VisibleChanged,
			// and subscribers may do changes in the list.
			List<DockObject> copy = new List<DockObject> (dockObjects);
			foreach (DockObject ob in copy)
				ob.RestoreAllocation ();
		}
		
		internal override void StoreAllocation ()
		{
			base.StoreAllocation ();
			foreach (DockObject ob in dockObjects)
				ob.StoreAllocation ();
			if (Type == DockGroupType.Tabbed && boundTabStrip != null)
				currentTabPage = boundTabStrip.CurrentTab;
		}
		
		public override bool Expand {
			get {
				foreach (DockObject ob in dockObjects)
					if (ob.Expand)
						return true;
				return false;
			}
		}
		
		public override void SizeAllocate (Gdk.Rectangle newAlloc)
		{
			Gdk.Rectangle oldAlloc = Allocation;
			base.SizeAllocate (newAlloc);
			
			if (type == DockGroupType.Tabbed) {
				if (boundTabStrip != null) {
					int tabsHeight = boundTabStrip.SizeRequest ().Height;
					boundTabStrip.SizeAllocate (new Gdk.Rectangle (newAlloc.X, newAlloc.Y, newAlloc.Width, tabsHeight));
				}
				if (allocStatus == AllocStatus.Valid && newAlloc == oldAlloc) {
					// Even if allocation has not changed, SizeAllocation has to be called on all items to avoid redrawing issues.
					foreach (DockObject ob in VisibleObjects)
						ob.SizeAllocate (ob.Allocation);
					return;
				}
				if (VisibleObjects.Count > 1 && boundTabStrip != null) {
					int tabsHeight = boundTabStrip.SizeRequest ().Height;
					newAlloc.Height -= tabsHeight;
					newAlloc.Y += tabsHeight;
					boundTabStrip.QueueDraw ();
				} else if (VisibleObjects.Count != 0) {
					((DockGroupItem)VisibleObjects [0]).Item.Widget.Show ();
				}
				allocStatus = AllocStatus.Valid;
				foreach (DockObject ob in VisibleObjects) {
					ob.Size = ob.PrefSize = -1;
					ob.SizeAllocate (newAlloc);
				}
				return;
			}
			
			bool horiz = type == DockGroupType.Horizontal;
			int pos = horiz ? Allocation.Left : Allocation.Top;
			
			if (allocStatus == AllocStatus.Valid && newAlloc == oldAlloc) {
				// The layout of this group (as a whole) has not changed, but the layout
				// of child items may have changed. Assign the new sizes.
				
				if (CheckMinSizes ())
					allocStatus = AllocStatus.NewSizeRequest;
				else {
					foreach (DockObject ob in VisibleObjects) {
						Gdk.Rectangle rect;
						int ins = ob.AllocSize;
						if (horiz)
							rect = new Gdk.Rectangle (pos, Allocation.Y, ins, Allocation.Height);
						else
							rect = new Gdk.Rectangle (Allocation.X, pos, Allocation.Width, ins);
						ob.SizeAllocate (rect);
						pos += ins + Frame.TotalHandleSize;
					}
					return;
				}
			}
			
			// This is the space available for the child items (excluding size
			// required for the resize handles)
			int realSize = GetRealSize (VisibleObjects);
			
			if (allocStatus == AllocStatus.NotSet/* || allocStatus == AllocStatus.RestorePending*/) {
				// It is the first size allocation. Calculate all sizes.
				CalcNewSizes ();
			}
			else if (allocStatus != AllocStatus.NewSizeRequest) {

				// Don't proportionally resize the pads. Instead, resize only those pads with the Expand flag.
				// This logic is implemented in CalcNewSizes, so no need to reimplement it
				CalcNewSizes ();

				// Disabled the proportional resize of pads for the above reason

/*				// Available space has changed, so the size of the items must be changed.
				// First of all, get the change fraction
				double change;
				if (horiz)
					change = (double) newAlloc.Width / (double) oldAlloc.Width;
				else
					change = (double) newAlloc.Height / (double) oldAlloc.Height;

				// Get the old total size of the visible objects. Used to calculate the
				// proportion of size of each item.
				double tsize = 0;
				double rsize = 0;
				foreach (DockObject ob in VisibleObjects) {
					tsize += ob.PrefSize;
					rsize += ob.Size;
				}
				
				foreach (DockObject ob in dockObjects) {
					if (ob.Visible) {
						// Proportionally spread the new available space among all visible objects
						ob.Size = ob.PrefSize = (ob.PrefSize / tsize) * (double) realSize;
					} else {
						// For non-visible objects, change the size by the same grow fraction. In this
						// way, when the item is shown again, it size will have the correct proportions.
						ob.Size = ob.Size * change;
						ob.PrefSize = ob.PrefSize * change;
					}
					ob.DefaultSize = ob.DefaultSize * change;
				}
				CheckMinSizes ();*/
			}

			allocStatus = AllocStatus.Valid;

			// Sizes for all items have been set. 
			// Sizes are real numbers to ensure that the values are not degradated when resizing
			// pixel by pixel. Now those have to be converted to integers, that is, actual allocated sizes.
			
			int ts = 0;
			for (int n=0; n<VisibleObjects.Count; n++) {
				DockObject ob = VisibleObjects [n];

				int ins = (int) Math.Truncate (ob.Size);
				
				if (n == VisibleObjects.Count - 1)
					ins = realSize - ts;
				
				ts += ins;
				
				if (ins < 0)
					ins = 0;
				
				ob.AllocSize = ins;
				
				if (horiz)
					ob.SizeAllocate (new Gdk.Rectangle (pos, Allocation.Y, ins, Allocation.Height));
				else
					ob.SizeAllocate (new Gdk.Rectangle (Allocation.X, pos, Allocation.Width, ins));
				
				pos += ins + Frame.TotalHandleSize;
			}
		}
		
		int GetRealSize (List<DockObject> objects)
		{
			// Returns the space available for the child items (excluding size
			// required for the resize handles)
			
			int realSize;
			if (type == DockGroupType.Horizontal)
				realSize = Allocation.Width;
			else
				realSize = Allocation.Height;
			
			// Ignore space required for the handles
			if (objects.Count > 1)
				realSize -= (Frame.TotalHandleSize * (objects.Count - 1));
			
			return realSize;
		}
		
		internal void CalcNewSizes ()
		{
			// Calculates the size assigned by default to each child item.
			// Size is proportionally assigned to each item, taking into account
			// the available space, and the default size of each item.
			
			// If there are items with the Expand flag set, those will proportionally
			// take the space left after allocating the other (not exandable) items.
			
			// This is the space available for the child items (excluding size
			// required for the resize handles)
			double realSize = (double) GetRealSize (VisibleObjects);
			
			bool hasExpandItems = false;
			double noexpandSize = 0;
			double minExpandSize = 0;
			double defaultExpandSize = 0;
			
			for (int n=0; n<VisibleObjects.Count; n++) {
				DockObject ob = VisibleObjects [n];
				if (ob.Expand) {
					minExpandSize += ob.MinSize;
					defaultExpandSize += ob.DefaultSize;
					hasExpandItems = true;
				}
				else {
					ob.Size = ob.DefaultSize;
					noexpandSize += ob.DefaultSize;
				}
			}

			double expandSize = realSize - noexpandSize;
			foreach (DockObject ob in VisibleObjects) {
				if (!hasExpandItems)
					ob.Size = (ob.DefaultSize / noexpandSize) * realSize;
				else if (ob.Expand)
					ob.Size = (ob.DefaultSize / defaultExpandSize) * expandSize;
				ob.PrefSize = ob.Size;
			}

			CheckMinSizes ();
		}
		
		bool CheckMinSizes ()
		{
			// Checks if any of the items has a size smaller than permitted.
			// In this case it tries to regain size by reducing other items.
			
			// First of all calculate the size to be regained, and the size available
			// from other items
			
			bool sizesChanged = false;
			
			double avSize = 0;
			double regSize = 0;
			foreach (DockObject ob in VisibleObjects) {
				if (ob.Size < ob.MinSize) {
					regSize += ob.MinSize - ob.Size;
					ob.Size = ob.MinSize;
					sizesChanged = true;
				} else {
					avSize += ob.Size - ob.MinSize;
				}
			}
			
			if (!sizesChanged)
				return false;
			
			// Now spread the required size among the resizable items 
			
			if (regSize > avSize)
				regSize = avSize;
			
			double ratio = (avSize - regSize) / avSize;
			foreach (DockObject ob in VisibleObjects) {
				if (ob.Size <= ob.MinSize)
					continue;
				double avs = ob.Size - ob.MinSize;
				ob.Size = ob.MinSize + avs * ratio;
			}
			return sizesChanged;
		}
		
		internal override Gtk.Requisition SizeRequest ()
		{
			bool getMaxW = true, getMaxH = true;
			if (type == DockGroupType.Horizontal)
				getMaxW = false;
			else if (type == DockGroupType.Vertical)
				getMaxH = false;
			
			Requisition ret = new Requisition ();
			ret.Height = VisibleObjects.Count * Frame.TotalHandleSize;
			foreach (DockObject ob in VisibleObjects) {
				Requisition req = ob.SizeRequest ();
				if (getMaxH) {
					if (req.Height > ret.Height)
						ret.Height = req.Height;
				} else
					ret.Height += req.Height;
				
				if (getMaxW) {
					if (req.Width > ret.Width)
						ret.Width = req.Width;
				} else
					ret.Width += req.Width;
			}
			if (type == DockGroupType.Tabbed && VisibleObjects.Count > 1 && boundTabStrip != null) {
				Gtk.Requisition tabs = boundTabStrip.SizeRequest ();
				ret.Height += tabs.Height;
				if (ret.Width < tabs.Width)
					ret.Width = tabs.Width;
			}
			return ret;
		}

		internal void UpdateNotebook (TabStrip ts)
		{
			Gtk.Widget oldpage = null;
			int oldtab = -1;
			
			if (tabFocus != null) {
				oldpage = tabFocus.Item.Widget;
				tabFocus = null;
			} else if (boundTabStrip != null) {
				oldpage = boundTabStrip.CurrentPage;
				oldtab = boundTabStrip.CurrentTab;
			}
			
			ts.Clear ();
			
			// Add missing pages
			foreach (DockObject ob in VisibleObjects) {
				DockGroupItem it = ob as DockGroupItem;
				ts.AddTab (it.Item.TitleTab);
			}

			boundTabStrip = ts;
			
			if (oldpage != null) {
				boundTabStrip.CurrentPage = oldpage;
			}
			else if (currentTabPage != -1 && currentTabPage < boundTabStrip.TabCount) {
				boundTabStrip.CurrentTab = currentTabPage;
			}

			// Discard the currentTabPage value. Current page is now tracked by the tab strip
			currentTabPage = -1;

			if (boundTabStrip.CurrentTab == -1) {
				if (oldtab != -1) {
					if (oldtab < boundTabStrip.TabCount)
						boundTabStrip.CurrentTab = oldtab;
					else
						boundTabStrip.CurrentTab = boundTabStrip.TabCount - 1;
				} else
					boundTabStrip.CurrentTab = 0;
			}
			if (Frame.CompactGuiLevel == 3 && IsNextToMargin (PositionType.Bottom, true))
				boundTabStrip.BottomPadding = 3;
			else
				boundTabStrip.BottomPadding = 0;
		}
		
		internal void Present (DockItem it, bool giveFocus)
		{
			if (type == DockGroupType.Tabbed) {
				for (int n=0; n<VisibleObjects.Count; n++) {
					DockGroupItem dit = VisibleObjects[n] as DockGroupItem;
					if (dit.Item == it) {
						currentTabPage = n;
						if (boundTabStrip != null)
							boundTabStrip.CurrentPage = it.Widget;
						break;
					}
				}
			}
			if (giveFocus && it.Visible)
				it.SetFocus ();
		}

		internal bool IsSelectedPage (DockItem it)
		{
			if (type != DockGroupType.Tabbed || boundTabStrip == null || boundTabStrip.CurrentTab == -1 || VisibleObjects == null || boundTabStrip.CurrentTab >= VisibleObjects.Count)
				return false;
			DockGroupItem dit = VisibleObjects[boundTabStrip.CurrentTab] as DockGroupItem;
			return dit.Item == it;
		}
		
		internal void UpdateTitle (DockItem it)
		{
			if (it.Visible && type == DockGroupType.Tabbed && boundTabStrip != null)
				boundTabStrip.SetTabLabel (it.Widget, it.Icon, it.Label);
		}
				
		internal void UpdateStyle (DockItem it)
		{
			if (it.Visible && type == DockGroupType.Tabbed && boundTabStrip != null)
				boundTabStrip.UpdateStyle (it);
		}
		
		internal void FocusItem (DockGroupItem it)
		{
			tabFocus = it;
		}
		
		internal void ResetNotebook ()
		{
			boundTabStrip = null;
		}
		
		public void LayoutWidgets ()
		{
			Frame.UpdateRegionStyle (this);

			foreach (DockObject ob in VisibleObjects) {
				DockGroupItem it = ob as DockGroupItem;
				if (it != null) {
					if (it.Visible) {
						Frame.UpdateRegionStyle (it);
						it.Item.SetRegionStyle (it.VisualStyle);
					}
				}
				else
					((DockGroup)ob).LayoutWidgets ();
			}
		}
	
		public void AddRemoveWidgets ()
		{
			foreach (DockObject ob in Objects) {
				DockGroupItem it = ob as DockGroupItem;
				if (it != null) {
					if (it.Visible) {
						// Add the dock item to the container and show it if visible
						if (it.Item.Widget.Parent != Frame.Container) {
							if (it.Item.Widget.Parent != null) {
								((Gtk.Container)it.Item.Widget.Parent).Remove (it.Item.Widget);
							}
							Frame.Container.Add (it.Item.Widget);
						}
						if (!it.Item.Widget.Visible && type != DockGroupType.Tabbed)
							it.Item.Widget.Show ();

						// Do the same for the title tab
						if ((type != DockGroupType.Tabbed || VisibleObjects.Count == 1) && (it.Item.Behavior & DockItemBehavior.NoGrip) == 0) {
							var tab = it.Item.TitleTab;
							if (tab.Parent != Frame.Container) {
								if (tab.Parent != null) {
									((Gtk.Container)tab.Parent).Remove (tab);
								}
								Frame.Container.Add (tab);
								tab.Active = true;
							}
							tab.ShowAll ();
						}
					} else {
						if (it.Item.Widget.Parent == Frame.Container)
							Frame.Container.Remove (it.Item.Widget);
						var tab = it.Item.TitleTab;
						if (tab.Parent == Frame.Container)
							Frame.Container.Remove (tab);
					}
				}
				else
					((DockGroup)ob).AddRemoveWidgets ();
			}
		}

		internal override void GetDefaultSize (out int width, out int height)
		{
			if (type == DockGroupType.Tabbed) {
				width = -1;
				height = -1;
				foreach (DockObject ob in VisibleObjects) {
					int dh, dw;
					ob.GetDefaultSize (out dw, out dh);
					if (dw > width)
						width = dw;
					if (dh > height)
						height = dh;
				}
			}
			else if (type == DockGroupType.Vertical) {
				height = VisibleObjects.Count > 0 ? (VisibleObjects.Count - 1) * Frame.TotalHandleSize : 0;
				width = -1;
				foreach (DockObject ob in VisibleObjects) {
					int dh, dw;
					ob.GetDefaultSize (out dw, out dh);
					if (dw > width)
						width = dw;
					height += dh;
				}
			}
			else {
				width = VisibleObjects.Count > 0 ? (VisibleObjects.Count - 1) * Frame.TotalHandleSize : 0;
				height = -1;
				foreach (DockObject ob in VisibleObjects) {
					int dh, dw;
					ob.GetDefaultSize (out dw, out dh);
					if (dh > height)
						height = dh;
					width += dw;
				}
			}
		}
		
		internal override void GetMinSize (out int width, out int height)
		{
			if (type == DockGroupType.Tabbed) {
				width = -1;
				height = -1;
				foreach (DockObject ob in VisibleObjects) {
					int dh, dw;
					ob.GetMinSize (out dw, out dh);
					if (dw > width)
						width = dw;
					if (dh > height)
						height = dh;
				}
			}
			else if (type == DockGroupType.Vertical) {
				height = VisibleObjects.Count > 1 ? (VisibleObjects.Count - 1) * Frame.TotalHandleSize : 0;
				width = -1;
				foreach (DockObject ob in VisibleObjects) {
					int dh, dw;
					ob.GetMinSize (out dw, out dh);
					if (dw > width)
						width = dw;
					height += dh;
				}
			}
			else {
				width = VisibleObjects.Count > 0 ? (VisibleObjects.Count - 1) * Frame.TotalHandleSize : 0;
				height = -1;
				foreach (DockObject ob in VisibleObjects) {
					int dh, dw;
					ob.GetMinSize (out dw, out dh);
					if (dh > height)
						height = dh;
					width += dw;
				}
			}
		}
		
		public void Draw (Gdk.Rectangle exposedArea, DockGroup currentHandleGrp, int currentHandleIndex)
		{
			if (type != DockGroupType.Tabbed) {
				DrawSeparators (exposedArea, currentHandleGrp, currentHandleIndex, DrawSeparatorOperation.Draw, false, null);
				foreach (DockObject it in VisibleObjects) {
					DockGroup grp = it as DockGroup;
					if (grp != null)
						grp.Draw (exposedArea, currentHandleGrp, currentHandleIndex);
				}
			}
		}
		
		public void DrawSeparators (Gdk.Rectangle exposedArea, DockGroup currentHandleGrp, int currentHandleIndex, DrawSeparatorOperation oper, List<Gdk.Rectangle> areasList)
		{
			DrawSeparators (exposedArea, currentHandleGrp, currentHandleIndex, oper, true, areasList);
		}
		
		void DrawSeparators (Gdk.Rectangle exposedArea, DockGroup currentHandleGrp, int currentHandleIndex, DrawSeparatorOperation oper, bool drawChildrenSep, List<Gdk.Rectangle> areasList)
		{
			if (type == DockGroupType.Tabbed || VisibleObjects.Count == 0)
				return;
			
			DockObject last = VisibleObjects [VisibleObjects.Count - 1];
			
			bool horiz = type == DockGroupType.Horizontal;
			int x = Allocation.X;
			int y = Allocation.Y;
			int hw = horiz ? Frame.HandleSize : Allocation.Width;
			int hh = horiz ? Allocation.Height : Frame.HandleSize;

			Gdk.GC hgc = null;

			if (areasList == null && oper == DrawSeparatorOperation.Draw) {
				hgc = new Gdk.GC (Frame.Container.GdkWindow);
				hgc.RgbFgColor = Styles.DockFrameBackground;
			}

			for (int n=0; n<VisibleObjects.Count; n++) {
				DockObject ob = VisibleObjects [n];
				DockGroup grp = ob as DockGroup;
				if (grp != null && drawChildrenSep)
					grp.DrawSeparators (exposedArea, currentHandleGrp, currentHandleIndex, oper, areasList);
				if (ob != last) {
					if (horiz)
						x += ob.Allocation.Width + Frame.HandlePadding;
					else
						y += ob.Allocation.Height + Frame.HandlePadding;

					switch (oper) {
					case DrawSeparatorOperation.CollectAreas:
						if (Frame.ShadedSeparators)
							areasList.Add (new Gdk.Rectangle (x, y, hw, hh));
						break;
					case DrawSeparatorOperation.Invalidate:
						Frame.Container.QueueDrawArea (x, y, hw, hh);
						break;
					case DrawSeparatorOperation.Draw:
						Frame.Container.GdkWindow.DrawRectangle (hgc, true, x, y, hw, hh);
						break;
					case DrawSeparatorOperation.Allocate:
						Frame.Container.AllocateSplitter (this, n, new Gdk.Rectangle (x, y, hw, hh));
						break;
					}
					
					if (horiz)
						x += Frame.HandleSize + Frame.HandlePadding;
					else
						y += Frame.HandleSize + Frame.HandlePadding;
				}
			}
			if (hgc != null)
				hgc.Dispose ();
		}
		
		public void ResizeItem (int index, int newSize)
		{
			DockObject o1 = VisibleObjects [index];
			DockObject o2 = VisibleObjects [index+1];
			
			int dsize;
			
			dsize = newSize - o1.AllocSize;
			if (dsize < 0 && o1.AllocSize + dsize < o1.MinSize)
				dsize = o1.MinSize - o1.AllocSize;
			else if (dsize > 0 && o2.AllocSize - dsize < o2.MinSize)
				dsize = o2.AllocSize - o2.MinSize;
			
			// Assign the new sizes, applying the current ratio
			double sizeDif = (double)dsize;
			
			o1.AllocSize += dsize;
			o2.AllocSize -= dsize;
			
			o1.DefaultSize += (o1.DefaultSize * sizeDif) / o1.Size;
			o1.Size = o1.AllocSize;
			o1.PrefSize = o1.Size;
			
			o2.DefaultSize -= (o2.DefaultSize * sizeDif) / o2.Size;
			o2.Size = o2.AllocSize;
			o2.PrefSize = o2.Size;
			
			o1.QueueResize ();
			o2.QueueResize ();
		}
		
		internal override void QueueResize ()
		{
			foreach (DockObject obj in VisibleObjects)
				obj.QueueResize ();
		}
		
		internal double GetObjectsSize ()
		{
			double total = 0;
			foreach (DockObject obj in VisibleObjects)
				total += obj.Size;
			return total;
		}
		
		void DockTarget (DockItem item, int n)
		{
			DockGroupItem gitem = new DockGroupItem (Frame, item);
			dockObjects.Insert (n, gitem);
			gitem.ParentGroup = this;
			gitem.SetVisible (true);
			ResetVisibleGroups ();
			CalcNewSizes ();
		}
		
		internal override bool GetDockTarget (DockItem item, int px, int py, out DockDelegate dockDelegate, out Gdk.Rectangle rect)
		{
			dockDelegate = null;
			rect = Gdk.Rectangle.Zero;

			if (!Allocation.Contains (px, py) || VisibleObjects.Count == 0)
				return false;

			if (type == DockGroupType.Tabbed) {
				// Tabs can only contain DockGroupItems
				var sel = boundTabStrip != null ? VisibleObjects[boundTabStrip.CurrentTab] : VisibleObjects[VisibleObjects.Count - 1];
				return ((DockGroupItem)sel).GetDockTarget (item, px, py, Allocation, out dockDelegate, out rect);
			}
			else if (type == DockGroupType.Horizontal) {
				if (px >= Allocation.Right - DockFrame.GroupDockSeparatorSize) {
					// Check if the item is allowed to be docked here
					var s = Frame.GetRegionStyleForObject (VisibleObjects[VisibleObjects.Count - 1]);
					if (s.SingleColumnMode.Value)
						return false;
					// Dock to the right of the group
					dockDelegate = delegate (DockItem it) {
						DockTarget (it, dockObjects.Count);
					};
					rect = new Gdk.Rectangle (Allocation.Right - DockFrame.GroupDockSeparatorSize, Allocation.Y, DockFrame.GroupDockSeparatorSize, Allocation.Height);
					return true;
				}
				else if (px <= Allocation.Left + DockFrame.GroupDockSeparatorSize) {
					// Check if the item is allowed to be docked here
					var s = Frame.GetRegionStyleForObject (VisibleObjects[0]);
					if (s.SingleColumnMode.Value)
						return false;
					// Dock to the left of the group
					dockDelegate = delegate (DockItem it) {
						DockTarget (it, 0);
					};
					rect = new Gdk.Rectangle (Allocation.Left, Allocation.Y, DockFrame.GroupDockSeparatorSize, Allocation.Height);
					return true;
				}
				// Dock in a separator
				for (int n=0; n<VisibleObjects.Count; n++) {
					DockObject ob = VisibleObjects [n];
					if (n < VisibleObjects.Count - 1 &&
					    px >= ob.Allocation.Right - DockFrame.GroupDockSeparatorSize/2 &&
					    px <= ob.Allocation.Right + DockFrame.GroupDockSeparatorSize/2)
					{
						// Check if the item is allowed to be docked here
						int dn = dockObjects.IndexOf (ob) + 1;
						var s = Frame.GetRegionStyleForPosition (this, dn, true);
						if (s.SingleColumnMode.Value)
							return false;
						dockDelegate = delegate (DockItem it) {
							DockTarget (it, dn);
						};
						rect = new Gdk.Rectangle (ob.Allocation.Right - DockFrame.GroupDockSeparatorSize/2, Allocation.Y, DockFrame.GroupDockSeparatorSize, Allocation.Height);
						return true;
					}
					else if (ob.GetDockTarget (item, px, py, out dockDelegate, out rect))
						return true;
				}
			}
			else if (type == DockGroupType.Vertical) {
				if (py >= Allocation.Bottom - DockFrame.GroupDockSeparatorSize) {
					// Check if the item is allowed to be docked here
					var s = Frame.GetRegionStyleForObject (VisibleObjects[VisibleObjects.Count - 1]);
					if (s.SingleRowMode.Value)
						return false;
					// Dock to the bottom of the group
					dockDelegate = delegate (DockItem it) {
						DockTarget (it, dockObjects.Count);
					};
					rect = new Gdk.Rectangle (Allocation.X, Allocation.Bottom - DockFrame.GroupDockSeparatorSize, Allocation.Width, DockFrame.GroupDockSeparatorSize);
					return true;
				}
				else if (py <= Allocation.Top + DockFrame.GroupDockSeparatorSize) {
					// Check if the item is allowed to be docked here
					var s = Frame.GetRegionStyleForObject (VisibleObjects[0]);
					if (s.SingleRowMode.Value)
						return false;
					// Dock to the top of the group
					dockDelegate = delegate (DockItem it) {
						DockTarget (it, 0);
					};
					rect = new Gdk.Rectangle (Allocation.X, Allocation.Top, Allocation.Width, DockFrame.GroupDockSeparatorSize);
					return true;
				}
				// Dock in a separator
				for (int n=0; n<VisibleObjects.Count; n++) {
					DockObject ob = VisibleObjects [n];
					if (n < VisibleObjects.Count - 1 &&
					    py >= ob.Allocation.Bottom - DockFrame.GroupDockSeparatorSize/2 &&
					    py <= ob.Allocation.Bottom + DockFrame.GroupDockSeparatorSize/2)
					{
						// Check if the item is allowed to be docked here
						int dn = dockObjects.IndexOf (ob) + 1;
						var s = Frame.GetRegionStyleForPosition (this, dn, true);
						if (s.SingleRowMode.Value)
							return false;
						dockDelegate = delegate (DockItem it) {
							DockTarget (it, dn);
						};
						rect = new Gdk.Rectangle (Allocation.X, ob.Allocation.Bottom - DockFrame.GroupDockSeparatorSize/2, Allocation.Width, DockFrame.GroupDockSeparatorSize);
						return true;
					}
					else if (ob.GetDockTarget (item, px, py, out dockDelegate, out rect))
						return true;
				}
			}
			dockDelegate = null;
			rect = Gdk.Rectangle.Zero;
			return false;
		}
		
		public void ReplaceItem (DockObject ob1, DockObject ob2)
		{
			int i = dockObjects.IndexOf (ob1);
			dockObjects [i] = ob2;
			ob2.ParentGroup = this;
			ob2.ResetDefaultSize ();
			ob2.Size = ob1.Size;
			ob2.DefaultSize = ob1.DefaultSize;
			ob2.AllocSize = ob1.AllocSize;
			ResetVisibleGroups ();
		}
		
		public override void CopyFrom (DockObject other)
		{
			base.CopyFrom (other);
			DockGroup grp = (DockGroup) other;
			dockObjects = new List<DockObject> ();
			foreach (DockObject ob in grp.dockObjects) {
				DockObject cob = ob.Clone ();
				cob.ParentGroup = this;
				dockObjects.Add (cob);
			}
			type = grp.type;
			ResetVisibleGroups ();
			boundTabStrip = null;
			tabFocus = null;
		}
		
		internal override bool Visible {
			get {
				foreach (DockObject ob in dockObjects)
					if (ob.Visible)
						return true;
				return false;
			}
		}
		
		internal void Dump ()
		{
			Dump (0);
		}
		
		internal override void Dump (int ind)
		{
			Console.WriteLine (new string (' ', ind) + "Group (" + type + ") size:" + Size + " DefaultSize:" + DefaultSize + " alloc:" + Allocation);
			foreach (DockObject ob in dockObjects) {
				ob.Dump (ind + 2);
			}
		}
		
		internal override void Write (XmlWriter writer)
		{
			base.Write (writer);
			writer.WriteAttributeString ("type", type.ToString ());
			if (type == DockGroupType.Tabbed && currentTabPage != -1)
				writer.WriteAttributeString ("currentTabPage", currentTabPage.ToString ());
			
			foreach (DockObject ob in dockObjects) {
				if (ob is DockGroupItem)
					writer.WriteStartElement ("item");
				else
					writer.WriteStartElement ("group");
				ob.Write (writer);
				writer.WriteEndElement ();
			}
		}
		
		internal override void Read (XmlReader reader)
		{
			base.Read (reader);
			type = (DockGroupType) Enum.Parse (typeof(DockGroupType), reader.GetAttribute ("type"));
			if (type == DockGroupType.Tabbed) {
				string s = reader.GetAttribute ("currentTabPage");
				if (s != null)
					currentTabPage = int.Parse (s);
			}
			
			reader.MoveToElement ();
			if (reader.IsEmptyElement) {
				reader.Skip ();
				return;
			}
			
			reader.ReadStartElement ();
			reader.MoveToContent ();
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element) {
					if (reader.LocalName == "item") {
						string id = reader.GetAttribute ("id");
						DockItem it = Frame.GetItem (id);
						if (it == null) {
							it = Frame.AddItem (id);
							it.IsPositionMarker = true;
						}
						DockGroupItem gitem = new DockGroupItem (Frame, it);
						gitem.Read (reader);
						AddObject (gitem);
						
						reader.MoveToElement ();
						reader.Skip ();
					}
					else if (reader.LocalName == "group") {
						DockGroup grp = new DockGroup (Frame);
						grp.Read (reader);
						AddObject (grp);
					}
				}
				else
					reader.Skip ();
				reader.MoveToContent ();
			}
			reader.ReadEndElement ();
		}

		public bool IsChildNextToMargin (Gtk.PositionType margin, DockObject obj, bool visibleOnly)
		{
			if (type == DockGroupType.Tabbed)
				return true;
			else if (type == DockGroupType.Horizontal) {
				if (margin == PositionType.Top || margin == PositionType.Bottom)
					return true;
				int i = visibleOnly ? VisibleObjects.IndexOf (obj) : Objects.IndexOf (obj);
				if (margin == PositionType.Left && i == 0)
					return true;
				if (margin == PositionType.Right && i == (visibleOnly ? VisibleObjects.Count - 1 : Objects.Count - 1))
					return true;
			}
			else if (type == DockGroupType.Vertical) {
				if (margin == PositionType.Left || margin == PositionType.Right)
					return true;
				int i = visibleOnly ? VisibleObjects.IndexOf (obj) : Objects.IndexOf (obj);
				if (margin == PositionType.Top && i == 0)
					return true;
				if (margin == PositionType.Bottom && i == (visibleOnly ? VisibleObjects.Count - 1 : Objects.Count - 1))
					return true;
			}
			return false;
		}
		
		internal TabStrip TabStrip {
			get { return boundTabStrip; }
		}
		
		public override string ToString ()
		{
			return "[DockGroup " + type + "]";
		}
	}
}
