//
// DockToolbarFrame.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using Gtk;
using Gdk;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using Pinta.Docking;

namespace Pinta.Docking.DockToolbars
{
	public class DockToolbarFrame: EventBox
	{
		DockToolbarPanel[] panels;
		DockToolbarPanel targetPanel;
		VBox vbox;
		VBox contentBox;
		DockToolbar dragBar;
		int xDragDif, yDragDif;
		List<DockToolbar> bars = new List<DockToolbar> ();
		
		Dictionary<string,DockToolbarStatus[]> layouts = new Dictionary<string,DockToolbarStatus[]> ();
		string currentLayout = "";
		
		Cursor handCursor = new Cursor (CursorType.Fleur);
		
		public DockToolbarFrame ()
		{
			vbox = new VBox ();
			Add (vbox);
			
			DockToolbarPanel topPanel = new DockToolbarPanel (this, Placement.Top);
			DockToolbarPanel bottomPanel = new DockToolbarPanel (this, Placement.Bottom);
			DockToolbarPanel leftPanel = new DockToolbarPanel (this, Placement.Left);
			DockToolbarPanel rightPanel = new DockToolbarPanel (this, Placement.Right);

			panels = new DockToolbarPanel [4];
			panels [(int)Placement.Top] = topPanel;
			panels [(int)Placement.Bottom] = bottomPanel;
			panels [(int)Placement.Left] = leftPanel;
			panels [(int)Placement.Right] = rightPanel;
		
			vbox.PackStart (topPanel, false, false, 0);
			
			HBox hbox = new HBox ();
			contentBox = new VBox ();

			hbox.PackStart (leftPanel, false, false, 0);
			hbox.PackStart (contentBox, true, true, 0);
			hbox.PackStart (rightPanel, false, false, 0);
			
			vbox.PackStart (hbox, true, true, 0);
			vbox.PackStart (bottomPanel, false, false, 0);
			
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask; 
			ShowAll ();
		}
		
		public void AddContent (Widget w)
		{
			contentBox.PackStart (w, true, true, 0);
		}
		
		public void RemoveContent (Widget w)
		{
			contentBox.Remove (w);
		}
		
		public string CurrentLayout {
			get { return currentLayout; }
			set {
				if (value != currentLayout) {
					SaveCurrentLayout ();
					RestoreLayout (value);
				}
			}
		}
		
		public DockToolbarFrameStatus GetStatus ()
		{
			SaveCurrentLayout ();
			DockToolbarFrameStatus col = new DockToolbarFrameStatus ();
			col.Version = DockToolbarFrameStatus.CurrentVersion;
			
			foreach (var layout in layouts) {
				col.Status.Add (new DockToolbarFrameLayout () {
					Id = layout.Key,
					Bars = layout.Value,
				});
			}
			return col;
		}
		
		public void SetStatus (DockToolbarFrameStatus status)
		{
			layouts.Clear ();
			if (status != null && status.Status != null) {
				foreach (DockToolbarFrameLayout c in status.Status) {
					if (status.Version < 2) {
						// Convert from old to new toolbar id
						foreach (DockToolbarStatus ts in c.Bars)
							ts.BarId = ConvertToolbarId (ts.BarId);
					}
					layouts [c.Id] = c.Bars;
				}
			}
			RestoreLayout ("");
		}
		
		public void SaveStatus (XmlWriter writer)
		{
			XmlSerializer ser = new XmlSerializer (typeof(DockToolbarFrameStatus));
			ser.Serialize (writer, GetStatus ());
		}
		
		public void LoadStatus (XmlReader reader)
		{
			layouts.Clear ();
			XmlSerializer ser = new XmlSerializer (typeof(DockToolbarFrameStatus));
			DockToolbarFrameStatus col = (DockToolbarFrameStatus) ser.Deserialize (reader);
			SetStatus (col);
		} 
		
		string ConvertToolbarId (string id)
		{
			// Old MD versions include the display name of the toolbar in the id.
			// New MD versions use a different id composition.
			// This method translates the id from the old to the new format (when possible)
			
			int i = id.LastIndexOf ('/');
			if (i == -1)
				return id;
			
			string baseId = id.Substring (0, i + 1);
			
			foreach (DockToolbar t in bars) {
				if (t.Id == id)
					return id;
				if (baseId + t.Title == id)
					return t.Id;
			}
			return id;
		}
		
		public IDockToolbar AddBar (DockToolbar bar)
		{
			return AddBar (bar, Placement.Top, true);
		}
		
		public IDockToolbar AddBar (DockToolbar bar, Placement defaultPanel, bool defaultVisible)
		{
			bar.SetParentFrame (this);
			bars.Add (bar);
			
			DockToolbarPosition pos = new DockedPosition (defaultPanel);
			DockToolbarStatus s = new DockToolbarStatus (bar.Id, defaultVisible, pos);
			bar.DefaultStatus = s;
			bar.Status = s;
			
			return bar;
		}
		
		public void RemoveToolbar(DockToolbar bar)
		{
			IDockToolbar db = (IDockToolbar)bar;
			db.Visible = false;
			bar.Destroy();
			bars.Remove(bar);
		}

		public void ClearToolbars ()
		{
			foreach (DockToolbar bar in bars) {
				IDockToolbar db = (IDockToolbar) bar;
				db.Visible = false;
				bar.Destroy ();
			}
			bars.Clear ();
		}
		
		public IDockToolbar GetBar (string id)
		{
			foreach (DockToolbar bar in bars)
				if (bar.Id == id) return bar;
			return null;
		}
		
		public ICollection<DockToolbar> Toolbars {
			get { return bars; }
		}
		
		public void ResetToolbarPositions ()
		{
			foreach (DockToolbarPanel panel in panels)
				panel.ResetBarPositions (false);
		}
		
		void SaveCurrentLayout ()
		{
			DockToolbarStatus[] status = SaveStatus ();
			layouts [currentLayout] = status;
		}
		
		void RestoreLayout (string layout)
		{
			DockToolbarStatus[] status;
			layouts.TryGetValue (layout, out status);
			RestoreStatus (status);
			currentLayout = layout;
		}
		
		internal void DeleteLayout (string layout)
		{
			if (layouts.ContainsKey (layout))
				layouts.Remove (layout);
		}
		
		DockToolbarStatus[] SaveStatus ()
		{
			DockToolbarStatus[] status = new DockToolbarStatus [bars.Count];
			for (int n=0; n<bars.Count; n++) {
				DockToolbar bar = (DockToolbar) bars [n];
				status [n] = bar.Status;
			}
			return status;
		}
		
		void RestoreStatus (DockToolbarStatus[] status)
		{
			foreach (IDockToolbar b in bars)
				b.Visible = false;
			
			if (status == null) {
				foreach (DockToolbar bar in bars)
					bar.Status = bar.DefaultStatus;
			} else {
				foreach (DockToolbarStatus s in status) {
					DockToolbar bar = (DockToolbar) GetBar (s.BarId);
					if (bar != null)
						bar.Status = s;
				}
			}
		}
		
		internal int DockMargin {
			get { return 7; }
		}
		
		internal void DockToolbar (DockToolbar bar, Placement placement, int offset, int row)
		{
			DockToolbarPanel p = GetPanel (placement);
			if (row != -1)
				p.AddDockToolbar (bar, offset, row);
			else
				p.AddDockToolbar (bar);
		}
		
		internal void FloatBar (DockToolbar bar, Orientation orientation, int x, int y)
		{
			FloatingDock fdock = new FloatingDock (this);
			fdock.Move (x, y);
			bar.ResetSize ();
			fdock.Attach (bar);
			bar.Orientation = orientation;
		}
		
		internal Gtk.Window TopWindow {
			get {
				Widget w = Parent;
				while (w != null && !(w is Gtk.Window))
					w = w.Parent;
				return (Gtk.Window) w;
			}
		}
		
		DockToolbarPanel GetPanel (Placement o)
		{
			return panels [(int)o];
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion e)
		{
			if (dragBar != null) {
				int sx,sy;
				this.GdkWindow.GetOrigin (out sx, out sy);
				int rx = (int)e.XRoot - sx;
				int ry = (int)e.YRoot - sy;
				
				if (dragBar.Floating) {
					bool foundPanel = false;
					dragBar.FloatingDock.Move ((int)e.XRoot + xDragDif, (int)e.YRoot + yDragDif);
					Rectangle barRect = new Rectangle (rx + xDragDif, ry + yDragDif, dragBar.Allocation.Width, dragBar.DefaultHeight);
					foreach (DockToolbarPanel p in panels) {
						if (p.Allocation.IntersectsWith (barRect)) {
							if (targetPanel != null && targetPanel != p)
								targetPanel.EndDragBar (dragBar);
							p.Reposition (dragBar, rx, ry, xDragDif, yDragDif);
							targetPanel = p;
							foundPanel = true;
							break;
						}
					}
					if (!foundPanel && targetPanel != null)
						targetPanel.EndDragBar (dragBar);
				} else {
					DockToolbarPanel panel = (DockToolbarPanel) dragBar.Parent;
					panel.Reposition (dragBar, rx, ry, xDragDif, yDragDif);
				}
			}
			return base.OnMotionNotifyEvent (e);
		}
		
		internal void StartDragBar (DockToolbar bar, int x, int y, uint time)
		{
			dragBar = bar;
			xDragDif = -x;
			yDragDif = -y;
			Pointer.Grab (this.GdkWindow, false, EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask, null, handCursor, time);
			if (!bar.Floating) {
				DockToolbarPanel panel = (DockToolbarPanel) dragBar.Parent;
				panel.StartDragBar (bar);
			}
		}
		
		internal void EndDragBar (DockToolbar bar, uint time)
		{
			Pointer.Ungrab (time);
			if (targetPanel != null) {
				targetPanel.DropDragBar (bar);
				targetPanel.EndDragBar (bar);
			}
			dragBar = null;
		}
		
		protected override bool OnButtonReleaseEvent (EventButton e)
		{
			if (dragBar != null)
				EndDragBar (dragBar, e.Time);

			return base.OnButtonReleaseEvent (e);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			if (e.TriggersContextMenu ()) {
				int sx,sy;
				this.GdkWindow.GetOrigin (out sx, out sy);
				int rx = (int)e.XRoot - sx;
				int ry = (int)e.YRoot - sy;
				
				foreach (DockToolbarPanel p in panels) {
					if (p.Allocation.Contains (rx, ry))
						OnPanelClick (e, p.Placement);
				}
			}
			return base.OnButtonPressEvent (e);
		}
		
		protected virtual void OnPanelClick (Gdk.EventButton e, Placement placement)
		{
		}
		
		protected override void OnDestroyed ()
		{
			if (handCursor != null) {
				handCursor.Dispose ();
				handCursor = null;
			}
			base.OnDestroyed ();
		}
	}
}
