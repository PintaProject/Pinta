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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Gtk;
using Gdk;
using Xwt.Motion;

namespace Pinta.Docking
{
	public class DockFrame: HBox, IAnimatable
	{
		internal const double ItemDockCenterArea = 0.4;
		internal const int GroupDockSeparatorSize = 40;
		
		internal bool ShadedSeparators = true;
		
		DockContainer container;
		
		int handleSize = 1;
		int handlePadding = 0;
		int defaultItemWidth = 300;
		int defaultItemHeight = 250;
		uint autoShowDelay = 400;
		uint autoHideDelay = 500;
		
		SortedDictionary<string,DockLayout> layouts = new SortedDictionary<string,DockLayout> ();
		List<DockFrameTopLevel> topLevels = new List<DockFrameTopLevel> ();
		string currentLayout;
		int compactGuiLevel = 3;
		
		DockBar dockBarTop, dockBarBottom, dockBarLeft, dockBarRight;
		VBox mainBox;
		DockVisualStyle defaultStyle;
		Gtk.Widget overlayWidget;

		public DockFrame ()
		{
            GtkWorkarounds.FixContainerLeak (this);

			dockBarTop = new DockBar (this, Gtk.PositionType.Top);
			dockBarBottom = new DockBar (this, Gtk.PositionType.Bottom);
			dockBarLeft = new DockBar (this, Gtk.PositionType.Left);
			dockBarRight = new DockBar (this, Gtk.PositionType.Right);
			
			container = new DockContainer (this);
			HBox hbox = new HBox ();
			hbox.PackStart (dockBarLeft, false, false, 0);
			hbox.PackStart (container, true, true, 0);
			hbox.PackStart (dockBarRight, false, false, 0);
			mainBox = new VBox ();
			mainBox.PackStart (dockBarTop, false, false, 0);
			mainBox.PackStart (hbox, true, true, 0);
			mainBox.PackStart (dockBarBottom, false, false, 0);
			Add (mainBox);
			mainBox.ShowAll ();
			mainBox.NoShowAll = true;
			CompactGuiLevel = 2;
			UpdateDockbarsVisibility ();

			DefaultVisualStyle = new DockVisualStyle ();
		}

		public bool DockbarsVisible {
			get {
				return !OverlayWidgetVisible;
			}
		}

		internal bool UseWindowsForTopLevelFrames {
			get { return Platform.IsMac; }
		}
		
		/// <summary>
		/// Compactness level of the gui, from 1 (not compact) to 5 (very compact).
		/// </summary>
		public int CompactGuiLevel {
			get { return compactGuiLevel; }
			set {
				compactGuiLevel = value;
/*				switch (compactGuiLevel) {
					case 1: handleSize = 6; break;
					case 2: 
					case 3: handleSize = IsWindows ? 4 : 6; break;
					case 4:
					case 5: handleSize = 3; break;
				}
*/				handlePadding = 0;
				dockBarTop.OnCompactLevelChanged ();
				dockBarBottom.OnCompactLevelChanged ();
				dockBarLeft.OnCompactLevelChanged ();
				dockBarRight.OnCompactLevelChanged ();
				container.RelayoutWidgets ();
			}
		}

		internal bool OverlayWidgetVisible { get; set; }

		public void AddOverlayWidget (Widget widget, bool animate = false)
		{
			RemoveOverlayWidget (false);

			this.overlayWidget = widget;
			widget.Parent = this;
			OverlayWidgetVisible = true;
			MinimizeAllAutohidden ();
			if (animate) {
				currentOverlayPosition = Math.Max (0, Allocation.Y + Allocation.Height);
				this.Animate (
					"ShowOverlayWidget", 
					ShowOverlayWidgetAnimation,
					easing: Easing.CubicOut);
			} else {
				currentOverlayPosition = Math.Max (0, Allocation.Y);
				QueueResize ();
			}

			UpdateDockbarsVisibility ();
		}

		public void RemoveOverlayWidget (bool animate = false)
		{
			this.AbortAnimation ("ShowOverlayWidget");
			this.AbortAnimation ("HideOverlayWidget");
			OverlayWidgetVisible = false;

			if (overlayWidget != null) {
				if (animate) {
					currentOverlayPosition = Allocation.Y;
					this.Animate (
						"HideOverlayWidget", 
						HideOverlayWidgetAnimation,
						finished: (a,b) => { 
							if (overlayWidget != null) {
								overlayWidget.Unparent ();
								overlayWidget = null;
							}
						},
						easing: Easing.SinOut);
				} else {
					overlayWidget.Unparent ();
					overlayWidget = null;
					QueueResize ();
				}
			}

			UpdateDockbarsVisibility ();
		}

		int currentOverlayPosition;

		void UpdateDockbarsVisibility ()
		{
			dockBarTop.UpdateVisibility ();
			dockBarBottom.UpdateVisibility ();
			dockBarLeft.UpdateVisibility ();
			dockBarRight.UpdateVisibility ();
		}

		void ShowOverlayWidgetAnimation (double value)
		{
			currentOverlayPosition = Allocation.Y + (int)((double)Allocation.Height * (1f - value));
			overlayWidget.SizeAllocate (new Rectangle (Allocation.X, currentOverlayPosition, Allocation.Width, Allocation.Height));
		}

		void HideOverlayWidgetAnimation (double value)
		{
			currentOverlayPosition = Allocation.Y + (int)((double)Allocation.Height * value);
			overlayWidget.SizeAllocate (new Rectangle (Allocation.X, currentOverlayPosition, Allocation.Width, Allocation.Height));
		}

		void IAnimatable.BatchBegin ()
		{
		}

		void IAnimatable.BatchCommit ()
		{
		}

		// Registered region styles. We are using a list instead of a dictionary because
		// the registering order is important
		List<Tuple<string,DockVisualStyle>> regionStyles = new List<Tuple<string, DockVisualStyle>> ();

		// Styles specific to items
		Dictionary<string,DockVisualStyle> stylesById = new Dictionary<string, DockVisualStyle> ();

		public DockVisualStyle DefaultVisualStyle {
			get {
				return defaultStyle;
			}
			set {
				defaultStyle = DockVisualStyle.CreateDefaultStyle ();
				defaultStyle.CopyValuesFrom (value);
			}
		}

		/// <summary>
		/// Sets the style for a region of the dock frame
		/// </summary>
		/// <param name='regionPosition'>
		/// A region is a collection with the format: "ItemId1/Position1;ItemId2/Position2..."
		/// ItemId is the id of a dock item. Position is one of the values of the DockPosition enumeration
		/// </param>
		/// <param name='style'>
		/// Style.
		/// </param>
		public void SetRegionStyle (string regionPosition, DockVisualStyle style)
		{
			// Remove any old region style and add it
			regionStyles.RemoveAll (s => s.Item1 == regionPosition);
			if (style != null)
				regionStyles.Add (new Tuple<string,DockVisualStyle> (regionPosition, style));
		}

		public void SetDockItemStyle (string itemId, DockVisualStyle style)
		{
			if (style != null)
				stylesById [itemId] = style;
			else
				stylesById.Remove (itemId);
		}

		internal void UpdateRegionStyle (DockObject obj)
		{
			obj.VisualStyle = GetRegionStyleForObject (obj);
		}

		/// <summary>
		/// Gets the style for a dock object, which will inherit values from all region/style definitions
		/// </summary>
		internal DockVisualStyle GetRegionStyleForObject (DockObject obj)
		{
			DockVisualStyle mergedStyle = null;
			if (obj is DockGroupItem) {
				DockVisualStyle s;
				if (stylesById.TryGetValue (((DockGroupItem)obj).Id, out s)) {
					mergedStyle = DefaultVisualStyle.Clone ();
					mergedStyle.CopyValuesFrom (s);
				}
			}
			foreach (var e in regionStyles) {
				if (InRegion (e.Item1, obj)) {
					if (mergedStyle == null)
						mergedStyle = DefaultVisualStyle.Clone ();
					mergedStyle.CopyValuesFrom (e.Item2);
				}
			}
			return mergedStyle ?? DefaultVisualStyle;
		}

		internal DockVisualStyle GetRegionStyleForItem (DockItem item)
		{
			DockVisualStyle s;
			if (stylesById.TryGetValue (item.Id, out s)) {
				var ds = DefaultVisualStyle.Clone ();
				ds.CopyValuesFrom (s);
				return ds;
			}
			return DefaultVisualStyle;
		}

		/// <summary>
		/// Gets the style assigned to a specific position of the layout
		/// </summary>
		/// <returns>
		/// The region style for position.
		/// </returns>
		/// <param name='parentGroup'>
		/// Group which contains the position
		/// </param>
		/// <param name='childIndex'>
		/// Index of the position inside the group
		/// </param>
		/// <param name='insertingPosition'>
		/// If true, the position will be inserted (meaning that the objects in childIndex will be shifted 1 position)
		/// </param>
		internal DockVisualStyle GetRegionStyleForPosition (DockGroup parentGroup, int childIndex, bool insertingPosition)
		{
			DockVisualStyle mergedStyle = null;
			foreach (var e in regionStyles) {
				if (InRegion (e.Item1, parentGroup, childIndex, insertingPosition)) {
					if (mergedStyle == null)
						mergedStyle = DefaultVisualStyle.Clone ();
					mergedStyle.CopyValuesFrom (e.Item2);
				}
			}
			return mergedStyle ?? DefaultVisualStyle;
		}

		internal bool InRegion (string location, DockObject obj)
		{
			if (obj.ParentGroup == null)
				return false;
			return InRegion (location, obj.ParentGroup, obj.ParentGroup.GetObjectIndex (obj), false);
		}

		internal bool InRegion (string location, DockGroup objToFindParent, int objToFindIndex, bool insertingPosition)
		{
			// Checks if the object is in the specified region.
			// A region is a collection with the format: "ItemId1/Position1;ItemId2/Position2..."
			string[] positions = location.Split (';');
			foreach (string pos in positions) {
				// We individually check each entry in the region specification
				int i = pos.IndexOf ('/');
				if (i == -1) continue;
				string id = pos.Substring (0,i).Trim ();
				DockGroup g = container.Layout.FindGroupContaining (id);
				if (g != null) {
					DockPosition dpos;
					try {
						dpos = (DockPosition) Enum.Parse (typeof(DockPosition), pos.Substring(i+1).Trim(), true);
					}
					catch {
						continue;
					}

					var refItem = g.FindDockGroupItem (id);
					if (InRegion (g, dpos, refItem, objToFindParent, objToFindIndex, insertingPosition))
						return true;
				}
			}
			return false;
		}

		bool InRegion (DockGroup grp, DockPosition pos, DockObject refObject, DockGroup objToFindParent, int objToFindIndex, bool insertingPosition)
		{
			if (grp == null)
				return false;

			if (grp.Type == DockGroupType.Tabbed) {
				if (pos != DockPosition.Center &&  pos != DockPosition.CenterBefore)
					return InRegion (grp.ParentGroup, pos, grp, objToFindParent, objToFindIndex, insertingPosition);
			}
			if (grp.Type == DockGroupType.Horizontal) {
				if (pos != DockPosition.Left && pos != DockPosition.Right)
					return InRegion (grp.ParentGroup, pos, grp, objToFindParent, objToFindIndex, insertingPosition);
			}
			if (grp.Type == DockGroupType.Vertical) {
				if (pos != DockPosition.Top && pos != DockPosition.Bottom)
					return InRegion (grp.ParentGroup, pos, grp, objToFindParent, objToFindIndex, insertingPosition);
			}

			bool foundAtLeftSide = true;
			bool findingLeft = pos == DockPosition.Left || pos == DockPosition.Top || pos == DockPosition.CenterBefore;

			if (objToFindParent == grp) {
				// Check positions beyond the current range of items
				if (objToFindIndex < 0 && findingLeft)
					return true;
				if (objToFindIndex >= grp.Objects.Count && !findingLeft)
					return true;
			}

			for (int n=0; n<grp.Objects.Count; n++) {
				var ob = grp.Objects[n];

				bool foundRefObject = ob == refObject;
				bool foundTargetObject = objToFindParent == grp && objToFindIndex == n;

				if (foundRefObject) {
					// Found the reference object, but if insertingPosition=true it is in the position that the new item will have,
					// so this position still has to be considered to be at the left side
					if (foundTargetObject && insertingPosition)
						return foundAtLeftSide == findingLeft;
					foundAtLeftSide = false;
				}
				else if (foundTargetObject)
					return foundAtLeftSide == findingLeft;
				else if (ob is DockGroup) {
					DockGroup gob = (DockGroup)ob;
					if (gob == objToFindParent || ObjectHasAncestor (objToFindParent, gob))
						return foundAtLeftSide == findingLeft;
				}
			}
			return InRegion (grp.ParentGroup, pos, grp, objToFindParent, objToFindIndex, insertingPosition);
		}

		bool ObjectHasAncestor (DockObject obj, DockGroup ancestorToFind)
		{
			return obj != null && (obj.ParentGroup == ancestorToFind || ObjectHasAncestor (obj.ParentGroup, ancestorToFind));
		}
		
		public DockBar ExtractDockBar (PositionType pos)
		{
			DockBar db = new DockBar (this, pos);
			switch (pos) {
				case PositionType.Left: db.OriginalBar = dockBarLeft; dockBarLeft = db; break;
				case PositionType.Top: db.OriginalBar = dockBarTop; dockBarTop = db; break;
				case PositionType.Right: db.OriginalBar = dockBarRight; dockBarRight = db; break;
				case PositionType.Bottom: db.OriginalBar = dockBarBottom; dockBarBottom = db; break;
			}
			return db;
		}
		
		internal DockBar GetDockBar (PositionType pos)
		{
			switch (pos) {
				case Gtk.PositionType.Top: return dockBarTop;
				case Gtk.PositionType.Bottom: return dockBarBottom;
				case Gtk.PositionType.Left: return dockBarLeft;
				case Gtk.PositionType.Right: return dockBarRight;
			}
			return null;
		}
		
		internal DockContainer Container {
			get { return container; }
		}

		public int HandleSize {
			get {
				return handleSize;
			}
			set {
				handleSize = value;
			}
		}
		
		public int HandlePadding {
			get {
				return handlePadding;
			}
			set {
				handlePadding = value;
			}
		}

		public int DefaultItemWidth {
			get {
				return defaultItemWidth;
			}
			set {
				defaultItemWidth = value;
			}
		}

		public int DefaultItemHeight {
			get {
				return defaultItemHeight;
			}
			set {
				defaultItemHeight = value;
			}
		}
		
		internal int TotalHandleSize {
			get { return handleSize + handlePadding*2; }
		}

		internal int TotalSensitiveHandleSize {
			get { return 6; }
		}
		
		public DockItem AddItem (string id)
		{
			foreach (DockItem dit in container.Items) {
				if (dit.Id == id) {
					if (dit.IsPositionMarker) {
						dit.IsPositionMarker = false;
						return dit;
					}
					throw new InvalidOperationException ("An item with id '" + id + "' already exists.");
				}
			}
			
			DockItem it = new DockItem (this, id);
			container.Items.Add (it);
			return it;
		}
		
		public void RemoveItem (DockItem it)
		{
			if (container.Layout != null)
				container.Layout.RemoveItemRec (it);
			foreach (DockGroup grp in layouts.Values)
				grp.RemoveItemRec (it);
			container.Items.Remove (it);
		}
		
		public DockItem GetItem (string id)
		{
			foreach (DockItem it in container.Items) {
				if (it.Id == id) {
					if (!it.IsPositionMarker)
					    return it;
					else
					    return null;
				}
			}
			return null;
		}
		
		public IEnumerable<DockItem> GetItems ()
		{
			return container.Items;
		}
		
		bool LoadLayout (string layoutName)
		{
			DockLayout dl;
			if (!layouts.TryGetValue (layoutName, out dl))
				return false;

			var focus = GetActiveWidget ();

			container.LoadLayout (dl);

			// Keep the currently focused widget when switching layouts
			if (focus != null && focus.IsRealized && focus.Visible)
				DockItem.SetFocus (focus);

			return true;
		}

		Gtk.Widget GetActiveWidget ()
		{
			Gtk.Widget widget = this;
			while (widget is Gtk.Container) {
				Gtk.Widget child = ((Gtk.Container)widget).FocusChild;
				if (child != null)
					widget = child;
				else
					break;
			}
			return widget;
		}

		public void CreateLayout (string name)
		{
			CreateLayout (name, false);
		}
		
		public void DeleteLayout (string name)
		{
			layouts.Remove (name);
		}
		
		public void CreateLayout (string name, bool copyCurrent)
		{
			DockLayout dl;
			if (container.Layout == null || !copyCurrent) {
				dl = GetDefaultLayout ();
			} else {
				container.StoreAllocation ();
				dl = (DockLayout) container.Layout.Clone ();
			}
			dl.Name = name;
			layouts [name] = dl;
		}
		
		public string CurrentLayout {
			get {
				return currentLayout;
			}
			set {
				if (currentLayout == value)
					return;
				if (LoadLayout (value)) {
					currentLayout = value;
				}
			}
		}
		
		public bool HasLayout (string id)
		{
			return layouts.ContainsKey (id);
		}
		
		public string[] Layouts {
			get {
				if (layouts.Count == 0)
					return new string [0];
				string[] arr = new string [layouts.Count];
				layouts.Keys.CopyTo (arr, 0);
				return arr;
			}
		}

		public uint AutoShowDelay {
			get {
				return autoShowDelay;
			}
			set {
				autoShowDelay = value;
			}
		}

		public uint AutoHideDelay {
			get {
				return autoHideDelay;
			}
			set {
				autoHideDelay = value;
			}
		}
		
		public void SaveLayouts (string file)
		{
			using (XmlTextWriter w = new XmlTextWriter (file, System.Text.Encoding.UTF8)) {
				w.Formatting = Formatting.Indented;
				SaveLayouts (w);
			}
		}
		
		public void SaveLayouts (XmlWriter writer)
		{
			if (container.Layout != null)
				container.Layout.StoreAllocation ();
			writer.WriteStartElement ("layouts");
			foreach (DockLayout la in layouts.Values)
				la.Write (writer);
			writer.WriteEndElement ();
		}
		
		public void LoadLayouts (string file)
		{
			using (XmlReader r = new XmlTextReader (new System.IO.StreamReader (file))) {
				LoadLayouts (r);
			}
		}
		
		public void LoadLayouts (XmlReader reader)
		{
			layouts.Clear ();
			container.Clear ();
			currentLayout = null;
			
			reader.MoveToContent ();
			if (reader.IsEmptyElement) {
				reader.Skip ();
				return;
			}
			reader.ReadStartElement ("layouts");
			reader.MoveToContent ();
			while (reader.NodeType != XmlNodeType.EndElement) {
				if (reader.NodeType == XmlNodeType.Element) {
					DockLayout layout = DockLayout.Read (this, reader);
					layouts.Add (layout.Name, layout);
				}
				else
					reader.Skip ();
				reader.MoveToContent ();
			}
			reader.ReadEndElement ();
			container.RelayoutWidgets ();
		}

		internal void UpdateTitle (DockItem item)
		{
			DockGroupItem gitem = container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return;
			
			gitem.ParentGroup.UpdateTitle (item);
			dockBarTop.UpdateTitle (item);
			dockBarBottom.UpdateTitle (item);
			dockBarLeft.UpdateTitle (item);
			dockBarRight.UpdateTitle (item);
		}
		
		internal void UpdateStyle (DockItem item)
		{
			DockGroupItem gitem = container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return;
			
			gitem.ParentGroup.UpdateStyle (item);
			dockBarTop.UpdateStyle (item);
			dockBarBottom.UpdateStyle (item);
			dockBarLeft.UpdateStyle (item);
			dockBarRight.UpdateStyle (item);
		}
		
		internal void Present (DockItem item, bool giveFocus)
		{
			DockGroupItem gitem = container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return;
			
			gitem.ParentGroup.Present (item, giveFocus);
		}
		
		internal bool GetVisible (DockItem item)
		{
			DockGroupItem gitem = container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return false;
			return gitem.VisibleFlag;
		}
		
		internal bool GetVisible (DockItem item, string layoutName)
		{
			DockLayout dl;
			if (!layouts.TryGetValue (layoutName, out dl))
				return false;
			
			DockGroupItem gitem = dl.FindDockGroupItem (item.Id);
			if (gitem == null)
				return false;
			return gitem.VisibleFlag;
		}
		
		internal void SetVisible (DockItem item, bool visible)
		{
			if (container.Layout == null)
				return;
			DockGroupItem gitem = container.FindDockGroupItem (item.Id);
			
			if (gitem == null) {
				if (visible) {
					// The item is not present in the layout. Add it now.
					if (!string.IsNullOrEmpty (item.DefaultLocation))
						gitem = AddDefaultItem (container.Layout, item);
						
					if (gitem == null) {
						// No default position
						gitem = new DockGroupItem (this, item);
						container.Layout.AddObject (gitem);
					}
				} else
					return; // Already invisible
			}
			gitem.SetVisible (visible);
			container.RelayoutWidgets ();
		}
		
		internal DockItemStatus GetStatus (DockItem item)
		{
			DockGroupItem gitem = container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return DockItemStatus.Dockable;
			return gitem.Status;
		}
		
		internal void SetStatus (DockItem item, DockItemStatus status)
		{
			DockGroupItem gitem = container.FindDockGroupItem (item.Id);
			if (gitem == null) {
				item.DefaultStatus = status;
				return;
			}
			gitem.StoreAllocation ();
			gitem.Status = status;
			container.RelayoutWidgets ();
		}

		internal void SetDockLocation (DockItem item, string placement)
		{
			bool vis = item.Visible;
			DockItemStatus stat = item.Status;
			item.ResetMode ();
			container.Layout.RemoveItemRec (item);
			AddItemAtLocation (container.Layout, item, placement, vis, stat);
		}
		
		DockLayout GetDefaultLayout ()
		{
			DockLayout group = new DockLayout (this);
			
			// Add items which don't have relative defaut positions
			
			List<DockItem> todock = new List<DockItem> ();
			foreach (DockItem item in container.Items) {
				if (string.IsNullOrEmpty (item.DefaultLocation)) {
					DockGroupItem dgt = new DockGroupItem (this, item);
					dgt.SetVisible (item.DefaultVisible);
					group.AddObject (dgt);
				}
				else
					todock.Add (item);
			}
			
			// Add items with relative positions.
			int lastCount = 0;
			while (lastCount != todock.Count) {
				lastCount = todock.Count;
				for (int n=0; n<todock.Count; n++) {
					DockItem it = todock [n];
					if (AddDefaultItem (group, it) != null) {
						todock.RemoveAt (n);
						n--;
					}
				}
			}
			
			// Items which could not be docked because of an invalid default location
			foreach (DockItem item in todock) {
				DockGroupItem dgt = new DockGroupItem (this, item);
				dgt.SetVisible (false);
				group.AddObject (dgt);
			}
//			group.Dump ();
			return group;
		}
		
		DockGroupItem AddDefaultItem (DockGroup grp, DockItem it)
		{
			return AddItemAtLocation (grp, it, it.DefaultLocation, it.DefaultVisible, it.DefaultStatus);
		}
		
		DockGroupItem AddItemAtLocation (DockGroup grp, DockItem it, string location, bool visible, DockItemStatus status)
		{
			string[] positions = location.Split (';');
			foreach (string pos in positions) {
				int i = pos.IndexOf ('/');
				if (i == -1) continue;
				string id = pos.Substring (0,i).Trim ();
				DockGroup g = grp.FindGroupContaining (id);
				if (g != null) {
					DockPosition dpos;
					try {
						dpos = (DockPosition) Enum.Parse (typeof(DockPosition), pos.Substring(i+1).Trim(), true);
					}
					catch {
						continue;
					}
					DockGroupItem dgt = g.AddObject (it, dpos, id);
					dgt.SetVisible (visible);
					dgt.Status = status;
					return dgt;
				}
			}
			return null;
		}
		
		internal void AddTopLevel (DockFrameTopLevel w, int x, int y, int width, int height)
		{
			w.X = x;
			w.Y = y;

			if (UseWindowsForTopLevelFrames) {
				var win = new Gtk.Window (Gtk.WindowType.Toplevel);
				win.AcceptFocus = false;
				win.SkipTaskbarHint = true;
				win.Decorated = false;
				win.TypeHint = Gdk.WindowTypeHint.Toolbar;
				w.ContainerWindow = win;
				w.Size = new Size (width, height);
				win.Add (w);
				w.Show ();
				var p = this.GetScreenCoordinates (new Gdk.Point (x, y));
				win.Opacity = 0.0;
				win.Move (p.X, p.Y);
				win.Resize (width, height);
				win.Show ();
                // Pinta TODO: May be needed on Mac?
				//Ide.DesktopService.AddChildWindow ((Gtk.Window)Toplevel, win);
				win.AcceptFocus = true;
				win.Opacity = 1.0;

				/* When we use real windows for frames, it's possible for pads to be over other
				 * windows. For some reason simply presenting or raising those dialogs doesn't
				 * seem to work, so we hide/show them in order to force them above the pad. */
				var toplevels = Gtk.Window.ListToplevels ().Where (t => t.IsRealized && t.TypeHint == WindowTypeHint.Dialog); // && t.TransientFor != null);
				foreach (var t in toplevels) {
					t.Hide ();
					t.Show ();
				}
			} else {
				w.Parent = this;
				w.Size = new Size (width, height);
				Requisition r = w.SizeRequest ();
				w.Allocation = new Gdk.Rectangle (Allocation.X + x, Allocation.Y + y, r.Width, r.Height);
				topLevels.Add (w);
			}
		}
		
		internal void RemoveTopLevel (DockFrameTopLevel w)
		{
			w.Unparent ();
			topLevels.Remove (w);
			QueueResize ();
		}
		
		public Gdk.Rectangle GetCoordinates (Gtk.Widget w)
		{
			int px, py;
			if (!w.TranslateCoordinates (this, 0, 0, out px, out py))
				return new Gdk.Rectangle (0,0,0,0);

			Gdk.Rectangle rect = w.Allocation;
			rect.X = px - Allocation.X;
			rect.Y = py - Allocation.Y;
			return rect;
		}
		
		internal void ShowPlaceholder (DockItem draggedItem)
		{
			container.ShowPlaceholder (draggedItem);
		}
		
		internal void DockInPlaceholder (DockItem item)
		{
			container.DockInPlaceholder (item);
		}
		
		internal void HidePlaceholder ()
		{
			container.HidePlaceholder ();
		}
		
		internal void UpdatePlaceholder (DockItem item, Gdk.Size size, bool allowDocking)
		{
			container.UpdatePlaceholder (item, size, allowDocking);
		}
		
		internal DockBarItem BarDock (Gtk.PositionType pos, DockItem item, int size)
		{
			return GetDockBar (pos).AddItem (item, size);
		}
		
		internal AutoHideBox AutoShow (DockItem item, DockBar bar, int size)
		{
			AutoHideBox aframe = new AutoHideBox (this, item, bar.Position, size);
			Gdk.Size sTop = GetBarFrameSize (dockBarTop);
			Gdk.Size sBot = GetBarFrameSize (dockBarBottom);
			Gdk.Size sLeft = GetBarFrameSize (dockBarLeft);
			Gdk.Size sRgt = GetBarFrameSize (dockBarRight);

			int x,y,w,h;
			if (bar == dockBarLeft || bar == dockBarRight) {
				h = Allocation.Height - sTop.Height - sBot.Height;
				w = size;
				y = sTop.Height;
				if (bar == dockBarLeft)
					x = sLeft.Width;
				else
					x = Allocation.Width - size - sRgt.Width;
			} else {
				w = Allocation.Width - sLeft.Width - sRgt.Width;
				h = size;
				x = sLeft.Width;
				if (bar == dockBarTop)
					y = sTop.Height;
				else
					y = Allocation.Height - size - sBot.Height;
			}

			AddTopLevel (aframe, x, y, w, h);
			aframe.AnimateShow ();

			return aframe;
		}
		
		internal void UpdateSize (DockBar bar, AutoHideBox aframe)
		{
			Gdk.Size sTop = GetBarFrameSize (dockBarTop);
			Gdk.Size sBot = GetBarFrameSize (dockBarBottom);
			Gdk.Size sLeft = GetBarFrameSize (dockBarLeft);
			Gdk.Size sRgt = GetBarFrameSize (dockBarRight);
			
			if (bar == dockBarLeft || bar == dockBarRight) {
				aframe.HeightRequest = Allocation.Height - sTop.Height - sBot.Height;
				if (bar == dockBarRight)
					aframe.X = Allocation.Width - aframe.Allocation.Width - sRgt.Width;
			} else {
				aframe.WidthRequest = Allocation.Width - sLeft.Width - sRgt.Width;
				if (bar == dockBarBottom)
					aframe.Y = Allocation.Height - aframe.Allocation.Height - sBot.Height;
			}
		}
		
		Gdk.Size GetBarFrameSize (DockBar bar)
		{
			if (bar.OriginalBar != null)
				bar = bar.OriginalBar;
			if (!bar.Visible)
				return new Gdk.Size (0,0);
			Gtk.Requisition req = bar.SizeRequest ();
			return new Gdk.Size (req.Width, req.Height);
		}
		
		internal void AutoHide (DockItem item, AutoHideBox widget, bool animate)
		{
			if (animate) {
				widget.Hidden += delegate {
					if (!widget.Disposed)
						AutoHide (item, widget, false);
				};
				widget.AnimateHide ();
			}
			else {
				// The widget may already be removed from the parent
				// so 'parent' can be null
				Gtk.Container parent = (Gtk.Container) item.Widget.Parent;
				if (parent != null) {
					//removing the widget from its parent causes it to unrealize without unmapping
					//so make sure it's unmapped
					if (item.Widget.IsMapped) {
						item.Widget.Unmap ();
					}
					parent.Remove (item.Widget);
				}
				parent = (Gtk.Container) item.TitleTab.Parent;
				if (parent != null) {
					//removing the widget from its parent causes it to unrealize without unmapping
					//so make sure it's unmapped
					if (item.TitleTab.IsMapped) {
						item.TitleTab.Unmap ();
					}
					parent.Remove (item.TitleTab);
				}
				if (widget.ContainerWindow != null) {
					widget.ContainerWindow.Destroy ();
				} else
					RemoveTopLevel (widget);

				widget.Disposed = true;
				widget.Destroy ();
			}
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			if (overlayWidget != null)
				overlayWidget.SizeRequest ();
			base.OnSizeRequested (ref requisition);
		}
		
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			foreach (DockFrameTopLevel tl in topLevels) {
				Requisition r = tl.SizeRequest ();
				tl.SizeAllocate (new Gdk.Rectangle (allocation.X + tl.X, allocation.Y + tl.Y, r.Width, r.Height));
			}
			if (overlayWidget != null)
				overlayWidget.SizeAllocate (new Rectangle (Allocation.X, currentOverlayPosition, allocation.Width, allocation.Height));
		}
		
		protected override void ForAll (bool include_internals, Callback callback)
		{
			base.ForAll (include_internals, callback);
			List<DockFrameTopLevel> clone = new List<DockFrameTopLevel> (topLevels);
			foreach (DockFrameTopLevel child in clone)
				callback (child);
			if (overlayWidget != null)
				callback (overlayWidget);
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			MinimizeAllAutohidden ();
			return base.OnButtonPressEvent (evnt);
		}

		void MinimizeAllAutohidden ()
		{
			foreach (var it in GetItems ()) {
				if (it.Visible && it.Status == DockItemStatus.AutoHide)
					it.Minimize ();
			}
		}

		static internal bool IsWindows {
			get { return System.IO.Path.DirectorySeparatorChar == '\\'; }
		}

		internal static Cairo.Color ToCairoColor (Gdk.Color color)
		{
			return new Cairo.Color (color.Red / (double) ushort.MaxValue, color.Green / (double) ushort.MaxValue, color.Blue / (double) ushort.MaxValue);
		}
	}

	public class DockStyle
	{
		public const string Default = "Default";
		public const string Browser = "Browser";
	}
	
	
	internal delegate void DockDelegate (DockItem item);
	
}
