//
// DockItem.cs
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
using Mono.Unix;

namespace MonoDevelop.Components.Docking
{
	public class DockItem
	{
		Widget content;
		DockItemContainer widget;
		string defaultLocation;
		bool defaultVisible = true;
		DockItemStatus defaultStatus = DockItemStatus.Dockable;
		string id;
		DockFrame frame;
		int defaultWidth = -1;
		int defaultHeight = -1;
		string label;
		Gdk.Pixbuf icon;
		bool expand;
		bool drawFrame = true;
		DockItemBehavior behavior;
		Gtk.Window floatingWindow;
		DockBarItem dockBarItem;
		bool lastVisibleStatus;
		bool lastContentVisibleStatus;
		bool gettingContent;
		bool isPositionMarker;
		bool stickyVisible;
		IDockItemLabelProvider dockLabelProvider;
		DockItemToolbar toolbarTop;
		DockItemToolbar toolbarBottom;
		DockItemToolbar toolbarLeft;
		DockItemToolbar toolbarRight;
		
		public event EventHandler VisibleChanged;
		public event EventHandler ContentVisibleChanged;
		public event EventHandler ContentRequired;
		
		internal DockItem (DockFrame frame, string id)
		{
			this.frame = frame;
			this.id = id;
		}
		
		internal DockItem (DockFrame frame, Widget w, string id)
		{
			this.frame = frame;
			this.id = id;
			content = w;
		}
		
		public string Id {
			get { return id; }
		}

		internal bool StickyVisible {
			get { return stickyVisible; }
			set { stickyVisible = value; }
		}
		
		public string Label {
			get { return label ?? string.Empty; }
			set {
				label = value; 
				if (widget != null)
					widget.Label = label;
				frame.UpdateTitle (this);
				if (floatingWindow != null)
					floatingWindow.Title = GetWindowTitle ();
			}
		}

		public bool Visible {
			get {
				return frame.GetVisible (this); 
			}
			set {
				stickyVisible = value;
				frame.SetVisible (this, value);
				UpdateVisibleStatus ();
			}
		}
		
		public bool VisibleInLayout (string layout)
		{
			return frame.GetVisible (this, layout); 
		}
		
		public DockItemStatus Status {
			get {
				return frame.GetStatus (this); 
			}
			set {
				frame.SetStatus (this, value);
			}
		}
		
		public IDockItemLabelProvider DockLabelProvider {
			get { return this.dockLabelProvider; }
			set { this.dockLabelProvider = value; }
		}
		
		internal DockItemContainer Widget {
			get {
				if (widget == null) {
					widget = new DockItemContainer (frame, this);
					widget.Visible = false; // Required to ensure that the Shown event is fired
					widget.Label = label;
					widget.Shown += SetupContent;
				}
				return widget;
			}
		}
		
		void SetupContent (object ob, EventArgs args)
		{
			widget.Shown -= SetupContent;
			
			if (ContentRequired != null) {
				gettingContent = true;
				try {
					ContentRequired (this, EventArgs.Empty);
				} finally {
					gettingContent = false;
				}
			}

			widget.UpdateContent ();
			widget.Shown += delegate {
				UpdateContentVisibleStatus ();
			};
			widget.Hidden += delegate {
				UpdateContentVisibleStatus ();
			};
			widget.ParentSet += delegate {
				UpdateContentVisibleStatus ();
			};
			UpdateContentVisibleStatus ();
		}
		
		public Widget Content {
			get {
				return content;
			}
			set {
				content = value;
				if (!gettingContent && widget != null)
					widget.UpdateContent ();
			}
		}
		
		public DockItemToolbar GetToolbar (PositionType position)
		{
			switch (position) {
				case PositionType.Top:
					if (toolbarTop == null)
						toolbarTop = new DockItemToolbar (this, PositionType.Top);
					return toolbarTop;
				case PositionType.Bottom:
					if (toolbarBottom == null)
						toolbarBottom = new DockItemToolbar (this, PositionType.Bottom);
					return toolbarBottom;
				case PositionType.Left:
					if (toolbarLeft == null)
						toolbarLeft = new DockItemToolbar (this, PositionType.Left);
					return toolbarLeft;
				case PositionType.Right:
					if (toolbarRight == null)
						toolbarRight = new DockItemToolbar (this, PositionType.Right);
					return toolbarRight;
				default: throw new ArgumentException ();
			}
		}
		
		internal bool HasWidget {
			get { return widget != null; }
		}
		
		public string DefaultLocation {
			get { return defaultLocation; }
			set { defaultLocation = value; }
		}

		public bool DefaultVisible {
			get { return defaultVisible; }
			set { defaultVisible = value; }
		}

		public DockItemStatus DefaultStatus {
			get { return defaultStatus; }
			set { defaultStatus = value; }
		}

		public int DefaultWidth {
			get {
				return defaultWidth;
			}
			set {
				defaultWidth = value;
			}
		}

		public int DefaultHeight {
			get {
				return defaultHeight;
			}
			set {
				defaultHeight = value;
			}
		}

		public Gdk.Pixbuf Icon {
			get {
				return icon;
			}
			set {
				icon = value;
			}
		}

		public DockItemBehavior Behavior {
			get {
				return behavior;
			}
			set {
				behavior = value;
				if (widget != null)
					widget.UpdateBehavior ();
			}
		}

		public bool Expand {
			get {
				return expand;
			}
			set {
				expand = value;
			}
		}

		public bool DrawFrame {
			get {
				return drawFrame;
			}
			set {
				drawFrame = value;
			}
		}
		
		public void Present (bool giveFocus)
		{
			if (dockBarItem != null)
				dockBarItem.Present (Status == DockItemStatus.AutoHide || giveFocus);
			else
				frame.Present (this, Status == DockItemStatus.AutoHide || giveFocus);
		}

		public bool ContentVisible {
			get {
				if (widget == null)
					return false;
				return widget.Parent != null && widget.Visible;
			}
		}
		
		public void SetDockLocation (string location)
		{
			frame.SetDockLocation (this, location);
		}

		internal void SetFocus ()
		{
			SetFocus (Content);
		}
		
		internal static void SetFocus (Widget w)
		{
			w.ChildFocus (DirectionType.Down);

			Window win = w.Toplevel as Gtk.Window;
			if (win == null)
				return;

			// Make sure focus is not given to internal children
			if (win.Focus != null) {
				Container c = win.Focus.Parent as Container;
				if (c.Children.Length == 0)
					win.Focus = c;
			}
		}
		
		internal void UpdateVisibleStatus ()
		{
			bool vis = frame.GetVisible (this);
			if (vis != lastVisibleStatus) {
				lastVisibleStatus = vis;
				if (VisibleChanged != null)
					VisibleChanged (this, EventArgs.Empty);
			}
			UpdateContentVisibleStatus ();
		}
		
		internal void UpdateContentVisibleStatus ()
		{
			bool vis = ContentVisible;
			if (vis != lastContentVisibleStatus) {
				lastContentVisibleStatus = vis;
				if (ContentVisibleChanged != null)
					ContentVisibleChanged (this, EventArgs.Empty);
			}
		}
		
		internal void ShowWidget ()
		{
			if (floatingWindow != null)
				floatingWindow.Show ();
			if (dockBarItem != null)
				dockBarItem.Show ();
			Widget.Show ();
		}
		
		internal void HideWidget ()
		{
			if (floatingWindow != null)
				floatingWindow.Hide ();
			else if (dockBarItem != null)
				dockBarItem.Hide ();
			else if (widget != null)
				widget.Hide ();
		}
		
		internal void SetFloatMode (Gdk.Rectangle rect)
		{
			ResetBarUndockMode ();
			if (floatingWindow == null) {
				if (Widget.Parent != null)
					Widget.Unparent ();
				floatingWindow = new Window (GetWindowTitle ());
				floatingWindow.TransientFor = frame.Toplevel as Gtk.Window;
				floatingWindow.TypeHint = Gdk.WindowTypeHint.Utility;
				floatingWindow.Add (Widget);
				floatingWindow.DeleteEvent += delegate (object o, DeleteEventArgs a) {
					if (behavior == DockItemBehavior.CantClose)
						Status = DockItemStatus.Dockable;
					else
						Visible = false;
					a.RetVal = true;
				};
			}
			floatingWindow.Move (rect.X, rect.Y);
			floatingWindow.Resize (rect.Width, rect.Height);
			floatingWindow.Show ();
			Widget.UpdateBehavior ();
			Widget.Show ();
		}
		
		internal void ResetFloatMode ()
		{
			if (floatingWindow != null) {
				floatingWindow.Remove (Widget);
				floatingWindow.Destroy ();
				floatingWindow = null;
				widget.UpdateBehavior ();
			}
		}
		
		internal Gdk.Rectangle FloatingPosition {
			get {
				if (floatingWindow != null) {
					int x,y,w,h;
					floatingWindow.GetPosition (out x, out y);
					floatingWindow.GetSize (out w, out h);
					return new Gdk.Rectangle (x,y,w,h);
				}
				else
					return Gdk.Rectangle.Zero;
			}
		}
		
		internal void ResetMode ()
		{
			ResetFloatMode ();
			ResetBarUndockMode ();
		}
		
		internal void SetAutoHideMode (Gtk.PositionType pos, int size)
		{
			ResetMode ();
			if (widget != null) {
				widget.Hide (); // Avoids size allocation warning
				widget.Unparent ();
			}
			dockBarItem = frame.BarDock (pos, this, size);
			if (widget != null)
				widget.UpdateBehavior ();
		}
		
		void ResetBarUndockMode ()
		{
			if (dockBarItem != null) {
				dockBarItem.Close ();
				dockBarItem = null;
				if (widget != null)
					widget.UpdateBehavior ();
			}
		}
		
		internal int AutoHideSize {
			get {
				if (dockBarItem != null)
					return dockBarItem.Size;
				else
					return -1;
			}
		}

		internal bool IsPositionMarker {
			get {
				return isPositionMarker;
			}
			set {
				isPositionMarker = value;
			}
		}
		
		string GetWindowTitle ()
		{
			if (Label.IndexOf ('<') == -1)
				return Label;
			try {
				XmlDocument doc = new XmlDocument ();
				doc.LoadXml ("<a>" + Label + "</a>");
				return doc.InnerText;
			} catch {
				return label;
			}
		}
		
		internal void ShowDockPopupMenu (uint time)
		{
			Menu menu = new Menu ();
			
			// Hide menuitem
			if ((Behavior & DockItemBehavior.CantClose) == 0) {
				MenuItem mitem = new MenuItem (Catalog.GetString("Hide"));
				mitem.Activated += delegate { Visible = false; };
				menu.Append (mitem);
			}

			CheckMenuItem citem;
			
			// Dockable menuitem
			citem = new CheckMenuItem (Catalog.GetString("Dockable"));
			citem.Active = Status == DockItemStatus.Dockable;
			citem.DrawAsRadio = true;
			citem.Toggled += delegate { Status = DockItemStatus.Dockable; };
			menu.Append (citem);

			// Floating menuitem
			if ((Behavior & DockItemBehavior.NeverFloating) == 0) {
				citem = new CheckMenuItem (Catalog.GetString("Floating"));
				citem.Active = Status == DockItemStatus.Floating;
				citem.DrawAsRadio = true;
				citem.Toggled += delegate { Status = DockItemStatus.Floating; };
				menu.Append (citem);
			}

			// Auto Hide menuitem
			if ((Behavior & DockItemBehavior.CantAutoHide) == 0) {
				citem = new CheckMenuItem (Catalog.GetString("Auto Hide"));
				citem.Active = Status == DockItemStatus.AutoHide;
				citem.DrawAsRadio = true;
				citem.Toggled += delegate { Status = DockItemStatus.AutoHide; };
				menu.Append (citem);
			}

			menu.ShowAll ();
			menu.Popup (null, null, null, 3, time);
		}
	}
	
	public interface IDockItemLabelProvider
	{
		Gtk.Widget CreateLabel (Orientation orientation);
	}
}
