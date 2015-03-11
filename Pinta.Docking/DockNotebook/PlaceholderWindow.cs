//
// PlaceholderWindow.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using Gdk;
using Gtk;
using Pinta.Docking;
using System.Collections.Generic;
using System;
using System.Linq;
using Pinta.Docking.Gui;
using MonoDevelop.Components;

namespace Pinta.Docking.DockNotebook
{

	class PlaceholderWindow: Gtk.Window
	{
		uint anim;
		int rx, ry, rw, rh;
		List<DockNotebook> allNotebooks;
		uint timeout;

		DocumentTitleWindow titleWindow;

		static PlaceholderWindow ()
		{
            //IdeApp.Workbench.ActiveDocumentChanged += delegate {
            //    var doc = IdeApp.Workbench.ActiveDocument;
            //    if (doc == null)
            //        return;
            //    var rootWindow = doc.Window.ActiveViewContent.Control.Toplevel as DockWindow;
            //    if (rootWindow == null)
            //        return;
				
            //    rootWindow.Title = DefaultWorkbench.GetTitle (doc.Window);
            //};
		}

		DockNotebookTab frame;
		
		public PlaceholderWindow (DockNotebookTab tab): base (Gtk.WindowType.Toplevel)
		{
			this.frame = tab;
			SkipTaskbarHint = true;
			Decorated = false;
			TypeHint = WindowTypeHint.Utility;
			titleWindow = new DocumentTitleWindow (this, tab);
            //IdeApp.Workbench.LockActiveWindowChangeEvent ();

			titleWindow.FocusInEvent += delegate {
				if (timeout != 0) {
					GLib.Source.Remove (timeout);
					timeout = 0;
				}
			};

			titleWindow.FocusOutEvent += delegate {
				timeout = GLib.Timeout.Add (100, () => {
					titleWindow.Close ();
					return false;
				});
			};

            //var windowStack = IdeApp.CommandService.TopLevelWindowStack.ToArray ();
            allNotebooks = DockNotebook.AllNotebooks.ToList ();
            //allNotebooks.Sort (delegate(DockNotebook x, DockNotebook y) {
            //    var ix = Array.IndexOf (windowStack, (Gtk.Window) x.Toplevel);
            //    var iy = Array.IndexOf (windowStack, (Gtk.Window) y.Toplevel);
            //    if (ix == -1) ix = int.MaxValue;
            //    if (iy == -1) iy = int.MaxValue;
            //    return ix.CompareTo (iy);
            //});
		}

		DockNotebook hoverNotebook;

		bool CanPlaceInHoverNotebook ()
		{
			return !titleWindow.ControlPressed && hoverNotebook != null;
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			Gtk.Application.Invoke (delegate {
				titleWindow.Destroy ();
			});
            //IdeApp.Workbench.UnlockActiveWindowChangeEvent ();
		}

		int curX, curY;

		public void UpdatePosition ()
		{
			MovePosition (curX, curY);
		}

		public void MovePosition (int x, int y)
		{
			this.curX = x;
			this.curY = y;

			ShowPlaceholder (x, y);

			var alloc = titleWindow.Child.SizeRequest ();
			titleWindow.Move (x - alloc.Width / 2, y - alloc.Height / 2);
			titleWindow.Show ();
			titleWindow.Present ();
		}

		public void ShowPlaceholder (int x, int y)
		{
			hoverNotebook = null;

			// TODO: Handle z-ordering of floating windows.
			int ox = 0, oy = 0;
			foreach (var notebook in allNotebooks) {
				if (notebook.GdkWindow == null)
					continue;

				int ox2, oy2;
				notebook.ParentWindow.GetOrigin (out ox2, out oy2);
				var alloc = notebook.Allocation;
				ox2 += alloc.X;
				ox2 += alloc.Y;
				if (ox2 <= x && x <= ox2 + alloc.Width && oy2 <= y && y <= oy2 + alloc.Height) {
					hoverNotebook = notebook;
					TransientFor = (Gtk.Window) hoverNotebook.Toplevel;
					ox = ox2;
					oy = oy2;
					break;
				}
			}

			if (CanPlaceInHoverNotebook ()) {
				var container = hoverNotebook.Container;
				var alloc = hoverNotebook.Allocation;
				var targetTabCount = hoverNotebook.TabCount;
				var overTabStrip = y <= oy + hoverNotebook.BarHeight;

				if (hoverNotebook.Tabs.Contains (frame))
					targetTabCount--; // Current is going to be removed, so it doesn't count

				if (targetTabCount > 0 && x <= ox + alloc.Width / 3 && !overTabStrip) {
					if (container.AllowLeftInsert) {
						Relocate (
							ox,
							oy,
							alloc.Width / 2,
							alloc.Height,
							false
						);
						placementDelegate = delegate(DockNotebook arg1, DockNotebookTab tab, Rectangle allocation2, int x2, int y2) {
							var window = (SdiWorkspaceWindow)tab.Content;
							container.InsertLeft (window);
							window.SelectWindow ();
						};
						return;
					}
				}

				if (targetTabCount > 0 && x >= ox + alloc.Width - alloc.Width / 3 && !overTabStrip) {
					if (container.AllowRightInsert) {
						Relocate (
							ox + alloc.Width / 2,
							oy,
							alloc.Width / 2,
							alloc.Height,
							false
						);
						placementDelegate = delegate(DockNotebook arg1, DockNotebookTab tab, Rectangle allocation2, int x2, int y2) {
							var window = (SdiWorkspaceWindow)tab.Content;
							container.InsertRight (window);
							window.SelectWindow ();
						};
						return;
					}
				}

				Relocate (
					ox, 
					oy, 
					alloc.Width, 
					alloc.Height, 
					false
				); 
				if (!hoverNotebook.Tabs.Contains (frame))
					placementDelegate = PlaceInHoverNotebook;
				else
					placementDelegate = null;
				return;
			}

			Hide ();
			placementDelegate = PlaceInFloatingFrame;
			titleWindow.SetDectorated (true);
		}

		protected override bool OnFocusInEvent (EventFocus evt)
		{
			if (timeout != 0) {
				GLib.Source.Remove (timeout);
				timeout = 0;
			}

			return base.OnFocusInEvent (evt);
		}

		protected override bool OnFocusOutEvent (EventFocus evt)
		{
			timeout = GLib.Timeout.Add (100, () => {
				titleWindow.Close ();
				return false;
			});

			return base.OnFocusOutEvent (evt);
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			GdkWindow.Opacity = 0.4;
		}
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			int w, h;
			GetSize (out w, out h);

			using (var ctx = CairoHelper.Create (evnt.Window)) {
				ctx.SetSourceColor (new Cairo.Color (0.17, 0.55, 0.79));
				ctx.Rectangle (Allocation.ToCairoRect ());
				ctx.Fill ();
			}
			return true;
		}

		public void Relocate (int x, int y, int w, int h, bool animate)
		{
			if (!Visible || x != rx || y != ry || w != rw || h != rh) {
				Hide ();
				if (w != rw || h != rh)
					Resize (w, h);
				Move (x, y);
				ShowAll ();
				titleWindow.SetDectorated (false);

				rx = x; ry = y; rw = w; rh = h;

				if (anim != 0) {
					GLib.Source.Remove (anim);
					anim = 0;
				}
				if (animate && w < 150 && h < 150) {
					const int sa = 7;
					Move (rx-sa, ry-sa);
					Resize (rw+sa*2, rh+sa*2);
					anim = GLib.Timeout.Add (10, RunAnimation);
				}
			}
		}

		bool RunAnimation ()
		{
			int cx, cy, ch, cw;
			GetSize (out cw, out ch);
			GetPosition	(out cx, out cy);

			if (cx != rx) {
				cx++; cy++;
				ch-=2; cw-=2;
				Move (cx, cy);
				Resize (cw, ch);
				return true;
			}
			anim = 0;
			return false;
		}

		public DockDelegate DockDelegate { get; private set; }
		public Rectangle DockRect { get; private set; }

		public void SetDockInfo (DockDelegate dockDelegate, Rectangle rect)
		{
			DockDelegate = dockDelegate;
			DockRect = rect;
		}

		static void PlaceInFloatingFrame (DockNotebook notebook, DockNotebookTab tab, Rectangle allocation, int ox, int oy)
		{
			var newWindow = new DockWindow ();
			var newNotebook = newWindow.Container.GetFirstNotebook ();
			var newTab = newNotebook.AddTab ();

			var workspaceWindow = (SdiWorkspaceWindow)tab.Content;
			newTab.Content = workspaceWindow;
			// JONTODO
            //newWindow.Title = DefaultWorkbench.GetTitle (workspaceWindow);

			workspaceWindow.SetDockNotebook (newNotebook, newTab);
			newWindow.Move (ox - w / 2, oy - h / 2);
			newWindow.Resize (w, h);
			newWindow.ShowAll ();
			DockNotebook.ActiveNotebook = newNotebook;
		}

		const int w = 640;
		const int h = 480;

		void PlaceInHoverNotebook (DockNotebook notebook, DockNotebookTab tab, Rectangle allocation, int ox, int oy)
		{
			var window = (SdiWorkspaceWindow)tab.Content;
			var newTab = hoverNotebook.AddTab (window); 
			window.SetDockNotebook (hoverNotebook, newTab); 
			window.SelectWindow ();
		}

		Action<DockNotebook, DockNotebookTab, Rectangle, int, int> placementDelegate;

		public void PlaceWindow (DockNotebook notebook)
		{
			try {
                //IdeApp.Workbench.LockActiveWindowChangeEvent ();
				var allocation = Allocation;
				Destroy ();

				if (placementDelegate != null) {
					var tab = notebook.CurrentTab;
					notebook.RemoveTab (tab.Index, true); 
					placementDelegate (notebook, tab, allocation, curX, curY);
				} else {
					((SdiWorkspaceWindow)frame.Content).SelectWindow ();
				}
			} finally {
                //IdeApp.Workbench.UnlockActiveWindowChangeEvent ();
			}
		}
	}


	class DocumentTitleWindow: Gtk.Window
	{
		int controlKeyMask;
		PlaceholderWindow placeholder;
		HBox titleBox;

		public DocumentTitleWindow (PlaceholderWindow placeholder, DockNotebookTab draggedItem): base (Gtk.WindowType.Toplevel)
		{
			this.placeholder = placeholder;

			SkipTaskbarHint = true;
			Decorated = false;

			//TransientFor = parent;
			TypeHint = WindowTypeHint.Utility;

			VBox mainBox = new VBox ();
			mainBox.Spacing = 3;

			titleBox = new HBox (false, 3);
			if (draggedItem.Icon != null) {
				var img = new ImageView (draggedItem.Icon);
				titleBox.PackStart (img, false, false, 0);
			}
			Gtk.Label la = new Label ();
			la.Markup = draggedItem.Text;
			titleBox.PackStart (la, false, false, 0);

			mainBox.PackStart (titleBox, false, false, 0);

            var wi = RenderWidget (draggedItem.Content);
            if (wi != null) {
                wi = wi.WithBoxSize (200);
                mainBox.PackStart (new ImageView (wi), false, false, 0);
            }

			CustomFrame f = new CustomFrame ();
			f.SetPadding (2, 2, 2, 2);
			f.SetMargins (1, 1, 1, 1);
			f.Add (mainBox);

			Add (f);
			mainBox.CanFocus = true;
			Child.ShowAll ();
		}

		public void SetDectorated (bool decorated)
		{
		//	Decorated = decorated;
		//	titleBox.Visible = !decorated;
		}

		public bool ControlPressed {
			get { return controlKeyMask != 0; }
		}

		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape)
				Close ();
			if (evnt.Key == Gdk.Key.Control_L)
				controlKeyMask |= 1;
			if (evnt.Key == Gdk.Key.Control_R)
				controlKeyMask |= 2;
			placeholder.UpdatePosition ();

			return base.OnKeyPressEvent (evnt);
		}


		protected override bool OnKeyReleaseEvent (EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Control_L)
				controlKeyMask &= ~1;
			if (evnt.Key == Gdk.Key.Control_R)
				controlKeyMask &= ~2;
			placeholder.UpdatePosition ();

			return base.OnKeyReleaseEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			Close ();
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			Close ();
			return base.OnLeaveNotifyEvent (evnt);
		}

        Gdk.Pixbuf RenderWidget (Widget w)
        {
            Gdk.Window win = w.GdkWindow;

            if (win != null && win.IsViewable)
                return Gdk.Pixbuf.FromDrawable (win, Colormap.System, w.Allocation.X, w.Allocation.Y, 0, 0, w.Allocation.Width, w.Allocation.Height);
            else
                return null;
        }

		public void Close ()
		{
			Application.Invoke (delegate {
				placeholder.Destroy ();
			});
		}
	}
}
