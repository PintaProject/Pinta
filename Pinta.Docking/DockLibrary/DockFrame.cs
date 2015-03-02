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
using System.Collections;
using System.Collections.Generic;
using Gtk;
using Gdk;

namespace MonoDevelop.Components.Docking
{
	public class DockFrame: HBox
	{
		internal const double ItemDockCenterArea = 0.4;
		internal const int GroupDockSeparatorSize = 40;
		
		internal bool ShadedSeparators = true;
		
		DockContainer container;
		
		int handleSize = IsWindows ? 4 : 6;
		int handlePadding = 0;
		int defaultItemWidth = 130;
		int defaultItemHeight = 130;
		uint autoShowDelay = 400;
		uint autoHideDelay = 500;
		
		SortedDictionary<string,DockLayout> layouts = new SortedDictionary<string,DockLayout> ();
		List<DockFrameTopLevel> topLevels = new List<DockFrameTopLevel> ();
		string currentLayout;
		int compactGuiLevel = 3;
		
		DockBar dockBarTop, dockBarBottom, dockBarLeft, dockBarRight;
		VBox mainBox;
		ShadedContainer shadedContainer;
		
		public DockFrame ()
		{
			shadedContainer = new ShadedContainer ();
			
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
			dockBarTop.UpdateVisibility ();
			dockBarBottom.UpdateVisibility ();
			dockBarLeft.UpdateVisibility ();
			dockBarRight.UpdateVisibility ();
		}
		
		/// <summary>
		/// Compactness level of the gui, from 1 (not compact) to 5 (very compact).
		/// </summary>
		public int CompactGuiLevel {
			get { return compactGuiLevel; }
			set {
				compactGuiLevel = value;
				switch (compactGuiLevel) {
					case 1: handleSize = 6; break;
					case 2: 
					case 3: handleSize = IsWindows ? 4 : 6; break;
					case 4:
					case 5: handleSize = 3; break;
				}
				handlePadding = 0;
				dockBarTop.OnCompactLevelChanged ();
				dockBarBottom.OnCompactLevelChanged ();
				dockBarLeft.OnCompactLevelChanged ();
				dockBarRight.OnCompactLevelChanged ();
				container.RelayoutWidgets ();
			}
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
		
		public ShadedContainer ShadedContainer {
			get { return this.shadedContainer; }
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
			
			container.LoadLayout (dl);
			return true;
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
		
		internal void AddTopLevel (DockFrameTopLevel w, int x, int y)
		{
			w.Parent = this;
			w.X = x;
			w.Y = y;
			Requisition r = w.SizeRequest ();
			w.Allocation = new Gdk.Rectangle (Allocation.X + x, Allocation.Y + y, r.Width, r.Height);
			topLevels.Add (w);
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
		
		internal void ShowPlaceholder ()
		{
			container.ShowPlaceholder ();
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
			
			int x,y;
			if (bar == dockBarLeft || bar == dockBarRight) {
				aframe.HeightRequest = Allocation.Height - sTop.Height - sBot.Height;
				aframe.WidthRequest = size;
				y = sTop.Height;
				if (bar == dockBarLeft)
					x = sLeft.Width;
				else
					x = Allocation.Width - size - sRgt.Width;
			} else {
				aframe.WidthRequest = Allocation.Width - sLeft.Width - sRgt.Width;
				aframe.HeightRequest = size;
				x = sLeft.Width;
				if (bar == dockBarTop)
					y = sTop.Height;
				else
					y = Allocation.Height - size - sBot.Height;
			}
			AddTopLevel (aframe, x, y);
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
				Gtk.Container parent = (Gtk.Container) item.Widget.Parent;
				parent.Remove (item.Widget);
				RemoveTopLevel (widget);
				widget.Disposed = true;
				widget.Destroy ();
			}
		}
		
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			foreach (DockFrameTopLevel tl in topLevels) {
				Requisition r = tl.SizeRequest ();
				tl.SizeAllocate (new Gdk.Rectangle (allocation.X + tl.X, allocation.Y + tl.Y, r.Width, r.Height));
			}
		}
		
		protected override void ForAll (bool include_internals, Callback callback)
		{
			base.ForAll (include_internals, callback);
			List<DockFrameTopLevel> clone = new List<DockFrameTopLevel> (topLevels);
			foreach (DockFrameTopLevel child in clone)
				callback (child);
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			HslColor cLight = new HslColor (Style.Background (Gtk.StateType.Normal));
			HslColor cDark = cLight;
			cLight.L *= 0.9;
			cDark.L *= 0.8;
			shadedContainer.LightColor = cLight;
			shadedContainer.DarkColor = cDark;
		}


		static internal bool IsWindows {
			get { return System.IO.Path.DirectorySeparatorChar == '\\'; }
		}

		internal static Cairo.Color ToCairoColor (Gdk.Color color)
		{
			return new Cairo.Color (color.Red / (double) ushort.MaxValue, color.Green / (double) ushort.MaxValue, color.Blue / (double) ushort.MaxValue);
		}
	}
	
	
	internal delegate void DockDelegate (DockItem item);
	
}
