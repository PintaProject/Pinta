//
// DockGroupItem.cs
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
using Gtk;

namespace MonoDevelop.Components.Docking
{
	internal class DockGroupItem: DockObject
	{
		DockItem item;
		bool visibleFlag;
		DockItemStatus status;
		Gdk.Rectangle floatRect;
		Gtk.PositionType barDocPosition;
		int autoHideSize = -1;
		
		public DockItem Item {
			get {
				return item;
			}
			set {
				item = value;
			}
		}
		
		public string Id {
			get { return item.Id; }
		}
		
		public DockGroupItem (DockFrame frame, DockItem item): base (frame)
		{
			this.item = item;
			visibleFlag = item.Visible;
		}
		
		internal override void GetDefaultSize (out int width, out int height)
		{
			width = item.DefaultWidth;
			height = item.DefaultHeight;
		}
		
		internal override void GetMinSize (out int width, out int height)
		{
			Requisition req = SizeRequest ();
			width = req.Width;
			height = req.Height;
		}
		
		internal override Requisition SizeRequest ()
		{
			var req = item.Widget.SizeRequest ();

			if (ParentGroup.Type != DockGroupType.Tabbed || ParentGroup.VisibleObjects.Count == 1) {
				var tr = item.TitleTab.SizeRequest ();
				req.Height += tr.Height;
				return req;
			} else
				return req;
		}

		public override void SizeAllocate (Gdk.Rectangle newAlloc)
		{
			if ((ParentGroup.Type != DockGroupType.Tabbed || ParentGroup.VisibleObjects.Count == 1) && (item.Behavior & DockItemBehavior.NoGrip) == 0) {
				var tr = newAlloc;
				tr.Height = item.TitleTab.SizeRequest ().Height;
				item.TitleTab.SizeAllocate (tr);
				var wr = newAlloc;
				wr.Y += tr.Height;
				wr.Height -= tr.Height;
				item.Widget.SizeAllocate (wr);
			}
			else
				item.Widget.SizeAllocate (newAlloc);

			base.SizeAllocate (newAlloc);
		}
		
		public override bool Expand {
			get { return item.Expand; }
		}
		
		internal override void QueueResize ()
		{
			item.Widget.QueueResize ();
		}
		
		internal override bool GetDockTarget (DockItem item, int px, int py, out DockDelegate dockDelegate, out Gdk.Rectangle rect)
		{
			return GetDockTarget (item, px, py, Allocation, out dockDelegate, out rect);
		}
		
		public bool GetDockTarget (DockItem item, int px, int py, Gdk.Rectangle rect, out DockDelegate dockDelegate, out Gdk.Rectangle outrect)
		{
			outrect = Gdk.Rectangle.Zero;
			dockDelegate = null;
			
			if (item != this.item && this.item.Visible && rect.Contains (px, py)) {

				// Check if the item is allowed to be docked here
				var s = Frame.GetRegionStyleForObject (this);

				int xdockMargin = (int) ((double)rect.Width * (1.0 - DockFrame.ItemDockCenterArea)) / 2;
				int ydockMargin = (int) ((double)rect.Height * (1.0 - DockFrame.ItemDockCenterArea)) / 2;
				DockPosition pos;
				
/*				if (ParentGroup.Type == DockGroupType.Tabbed) {
					rect = new Gdk.Rectangle (rect.X + xdockMargin, rect.Y + ydockMargin, rect.Width - xdockMargin*2, rect.Height - ydockMargin*2);
					pos = DockPosition.CenterAfter;
				}*/				
				if (px <= rect.X + xdockMargin && ParentGroup.Type != DockGroupType.Horizontal) {
					if (s.SingleColumnMode.Value)
						return false;
					outrect = new Gdk.Rectangle (rect.X, rect.Y, xdockMargin, rect.Height);
					pos = DockPosition.Left;
				}
				else if (px >= rect.Right - xdockMargin && ParentGroup.Type != DockGroupType.Horizontal) {
					if (s.SingleColumnMode.Value)
						return false;
					outrect = new Gdk.Rectangle (rect.Right - xdockMargin, rect.Y, xdockMargin, rect.Height);
					pos = DockPosition.Right;
				}
				else if (py <= rect.Y + ydockMargin && ParentGroup.Type != DockGroupType.Vertical) {
					if (s.SingleRowMode.Value)
						return false;
					outrect = new Gdk.Rectangle (rect.X, rect.Y, rect.Width, ydockMargin);
					pos = DockPosition.Top;
				}
				else if (py >= rect.Bottom - ydockMargin && ParentGroup.Type != DockGroupType.Vertical) {
					if (s.SingleRowMode.Value)
						return false;
					outrect = new Gdk.Rectangle (rect.X, rect.Bottom - ydockMargin, rect.Width, ydockMargin);
					pos = DockPosition.Bottom;
				}
				else {
					outrect = new Gdk.Rectangle (rect.X + xdockMargin, rect.Y + ydockMargin, rect.Width - xdockMargin*2, rect.Height - ydockMargin*2);
					pos = DockPosition.Center;
				}
				
				dockDelegate = delegate (DockItem dit) {
					DockGroupItem it = ParentGroup.AddObject (dit, pos, Id);
					it.SetVisible (true);
					ParentGroup.FocusItem (it);
				};
				return true;
			}
			return false;
		}
		
		internal override void Dump (int ind)
		{
			Console.WriteLine (new string (' ', ind) + item.Id + " size:" + Size + " alloc:" + Allocation);
		}
		
		internal override void Write (XmlWriter writer)
		{
			base.Write (writer);
			writer.WriteAttributeString ("id", item.Id);
			writer.WriteAttributeString ("visible", visibleFlag.ToString ());
			writer.WriteAttributeString ("status", status.ToString ());
			
			if (status == DockItemStatus.AutoHide)
				writer.WriteAttributeString ("autoHidePosition", barDocPosition.ToString ());
			
			if (autoHideSize != -1)
				writer.WriteAttributeString ("autoHideSize", autoHideSize.ToString ());
			
			if (!floatRect.Equals (Gdk.Rectangle.Zero)) {
				writer.WriteAttributeString ("floatX", floatRect.X.ToString ());
				writer.WriteAttributeString ("floatY", floatRect.Y.ToString ());
				writer.WriteAttributeString ("floatWidth", floatRect.Width.ToString ());
				writer.WriteAttributeString ("floatHeight", floatRect.Height.ToString ());
			}
		}
		
		internal override void Read (XmlReader reader)
		{
			base.Read (reader);
			visibleFlag = bool.Parse (reader.GetAttribute ("visible")) && !item.IsPositionMarker;
			status = (DockItemStatus) Enum.Parse (typeof (DockItemStatus), reader.GetAttribute ("status"));
			int fx=0, fy=0, fw=0, fh=0;
			string s = reader.GetAttribute ("floatX");
			if (s != null)
				fx = int.Parse (s);
			s = reader.GetAttribute ("floatY");
			if (s != null)
				fy = int.Parse (s);
			s = reader.GetAttribute ("floatWidth");
			if (s != null)
				fw = int.Parse (s);
			s = reader.GetAttribute ("floatHeight");
			if (s != null)
				fh = int.Parse (s);
			s = reader.GetAttribute ("autoHidePosition");
			if (s != null)
				barDocPosition = (PositionType) Enum.Parse (typeof (PositionType), s);
			s = reader.GetAttribute ("autoHideSize");
			if (s != null)
				autoHideSize = int.Parse (s);
			floatRect = new Gdk.Rectangle (fx, fy, fw, fh);
		}
		
		public override void CopyFrom (DockObject ob)
		{
			base.CopyFrom (ob);
			DockGroupItem it = (DockGroupItem)ob;
			item = it.item;
			visibleFlag = it.visibleFlag;
			floatRect = it.floatRect;
		}
		
		internal override bool Visible {
			get { return visibleFlag && status == DockItemStatus.Dockable; }
		}

		internal bool VisibleFlag {
			get { return visibleFlag; }
		}

		public DockItemStatus Status {
			get {
				return status;
			}
			set {
				if (status == value)
					return;
					
				DockItemStatus oldValue = status;
				status = value;
				
				if (status == DockItemStatus.Floating) {
					if (floatRect.Equals (Gdk.Rectangle.Zero)) {
						int x, y;
						item.Widget.TranslateCoordinates (item.Widget.Toplevel, 0, 0, out x, out y);
						Gtk.Window win = Frame.Toplevel as Window;
						if (win != null) {
							int wx, wy;
							win.GetPosition (out wx, out wy);
							floatRect = new Gdk.Rectangle (wx + x, wy + y, Allocation.Width, Allocation.Height);
						}
					}
					item.SetFloatMode (floatRect);
				}
				else if (status == DockItemStatus.AutoHide) {
					SetBarDocPosition ();
					item.SetAutoHideMode (barDocPosition, GetAutoHideSize (barDocPosition));
				}
				else
					item.ResetMode ();
				
				if (oldValue == DockItemStatus.Dockable || status == DockItemStatus.Dockable) {
					// Update visibility if changing from/to dockable mode
					if (ParentGroup != null)
						ParentGroup.UpdateVisible (this);
				}
			}
		}
		
		void SetBarDocPosition ()
		{
			// Determine the best position for docking the item
			
			if (Allocation.IsEmpty) {
				int uniqueTrue = -1;
				int uniqueFalse = -1;
				for (int n=0; n<4; n++) {
					bool inMargin = IsNextToMargin ((PositionType) n, false);
					if (inMargin) {
						if (uniqueTrue == -1)
							uniqueTrue = n;
						else
							uniqueTrue = -2;
					} else {
						if (uniqueFalse == -1)
							uniqueFalse = n;
						else
							uniqueFalse = -2;
					}
				}
				
				if (uniqueTrue >= 0) {
					barDocPosition = (PositionType) uniqueTrue;
					autoHideSize = 200;
					return;
				} else if (uniqueFalse >= 0) {
					barDocPosition = (PositionType) uniqueFalse;
					switch (barDocPosition) {
						case PositionType.Left: barDocPosition = PositionType.Right; break;
						case PositionType.Right: barDocPosition = PositionType.Left; break;
						case PositionType.Top: barDocPosition = PositionType.Bottom; break;
						case PositionType.Bottom: barDocPosition = PositionType.Top; break;
					}
					autoHideSize = 200;
					return;
				}
				
				// If the item is in a group, use the dock location of other items
				DockObject current = this;
				do {
					if (EstimateBarDocPosition (current.ParentGroup, current, out barDocPosition, out autoHideSize))
						return;
					current = current.ParentGroup;
				} while (current.ParentGroup != null);
				
				// Can't find a good location. Just guess.
				barDocPosition = PositionType.Bottom;
				autoHideSize = 200;
				return;
			}
			barDocPosition = CalcBarDocPosition ();
		}
		
		bool EstimateBarDocPosition (DockGroup grp, DockObject ignoreChild, out PositionType pos, out int size)
		{
			foreach (DockObject ob in grp.Objects) {
				if (ob == ignoreChild)
					continue;
				if (ob is DockGroup) {
					if (EstimateBarDocPosition ((DockGroup)ob, null, out pos, out size))
						return true;
				} else if (ob is DockGroupItem) {
					DockGroupItem it = (DockGroupItem) ob;
					if (it.status == DockItemStatus.AutoHide) {
						pos = it.barDocPosition;
						size = it.autoHideSize;
						return true;
					}
					if (!it.Allocation.IsEmpty) {
						pos = it.CalcBarDocPosition ();
						size = it.GetAutoHideSize (pos);
						return true;
					}
				}
			}
			pos = PositionType.Bottom;
			size = 0;
			return false;
		}
		
		PositionType CalcBarDocPosition ()
		{
			if (Allocation.Width < Allocation.Height) {
				int mid = Allocation.Left + Allocation.Width / 2;
				if (mid > Frame.Allocation.Left + Frame.Allocation.Width / 2)
					return PositionType.Right;
				else
					return PositionType.Left;
			} else {
				int mid = Allocation.Top + Allocation.Height / 2;
				if (mid > Frame.Allocation.Top + Frame.Allocation.Height / 2)
					return PositionType.Bottom;
				else
					return PositionType.Top;
			}
		}

		internal void SetVisible (bool value)
		{
			if (visibleFlag != value) {
				visibleFlag = value;
				if (visibleFlag)
					item.ShowWidget ();
				else
					item.HideWidget ();
				if (ParentGroup != null)
					ParentGroup.UpdateVisible (this);
			}
		}
		
		internal override void StoreAllocation ()
		{
			base.StoreAllocation ();
			if (Status == DockItemStatus.Floating)
				floatRect = item.FloatingPosition;
			else if (Status == DockItemStatus.AutoHide)
				autoHideSize = item.AutoHideSize;
		}

		internal override void RestoreAllocation ()
		{
			base.RestoreAllocation ();
			item.UpdateVisibleStatus ();
			
			if (Status == DockItemStatus.Floating)
				item.SetFloatMode (floatRect);
			else if (Status == DockItemStatus.AutoHide)
				item.SetAutoHideMode (barDocPosition, GetAutoHideSize (barDocPosition));
			else
				item.ResetMode ();
			
			if (!visibleFlag)
				item.HideWidget ();
		}
		
		int GetAutoHideSize (Gtk.PositionType pos)
		{
			if (autoHideSize != -1)
				return autoHideSize;

			if (pos == PositionType.Left || pos == PositionType.Right)
				return Allocation.Width;
			else
				return Allocation.Height;
		}

		public Gdk.Rectangle FloatRect {
			get {
				return floatRect;
			}
			set {
				floatRect = value;
			}
		}
		
		public override string ToString ()
		{
			return "[DockItem " + Item.Id + "]";
		}
	}
}
