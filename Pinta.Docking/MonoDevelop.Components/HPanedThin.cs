//
// HPanedThin.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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
using System;
using Pinta.Docking.Gui;
using System.Collections.Generic;
using Pinta.Docking;

namespace MonoDevelop.Components
{
	class HPanedThin: Gtk.HPaned
	{
		static HashSet<int> stylesParsed = new HashSet<int> ();

		CustomPanedHandle handle;

		public HPanedThin ()
		{
			GtkWorkarounds.FixContainerLeak (this);
			handle = new CustomPanedHandle (this);
			handle.Parent = this;
		}

		public int GrabAreaSize {
			get { return handle.GrabAreaSize; }
			set {
				handle.GrabAreaSize = value;
				QueueResize ();
			}
		}

		public Gtk.Widget HandleWidget {
			get { return handle.HandleWidget; }
			set { handle.HandleWidget = value; }
		}

		internal static void InitStyle (Gtk.Paned paned, int size)
		{
			string id = "MonoDevelop.ThinPanedHandle.s" + size;
			if (stylesParsed.Add (size)) {
				Gtk.Rc.ParseString ("style \"" + id + "\" {\n GtkPaned::handle-size = " + size + "\n }\n");
				Gtk.Rc.ParseString ("widget \"*." + id + "\" style  \"" + id + "\"\n");
			}
			paned.Name = id;
		}

		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			base.ForAll (include_internals, callback);
			if (handle != null)
				callback (handle);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			base.OnExposeEvent (evnt);

			if (Child1 != null && Child1.Visible && Child2 != null && Child2.Visible) {
				var gc = new Gdk.GC (evnt.Window);
				gc.RgbFgColor = Styles.ThinSplitterColor;
				var x = Child1.Allocation.X + Child1.Allocation.Width;
				evnt.Window.DrawLine (gc, x, Allocation.Y, x, Allocation.Y + Allocation.Height);
				gc.Dispose ();
			}

			return true;
		}
	}

	class CustomPanedHandle: Gtk.EventBox
	{
		static Gdk.Cursor resizeCursorW = new Gdk.Cursor (Gdk.CursorType.SbHDoubleArrow);
		static Gdk.Cursor resizeCursorH = new Gdk.Cursor (Gdk.CursorType.SbVDoubleArrow);
		internal const int HandleGrabWidth = 4;

		Gtk.Paned parent;
		bool horizontal;
		bool dragging;
		int initialPos;
		int initialPanedPos;

		public CustomPanedHandle (Gtk.Paned parent)
		{
			this.parent = parent;
			this.horizontal = parent is HPanedThin;
			GrabAreaSize = HandleGrabWidth;
			Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask;

			parent.SizeRequested += delegate {
				SizeRequest ();
			};
			parent.SizeAllocated += HandleSizeAllocated;
			HandleWidget = null;
		}

		void HandleSizeAllocated (object o, Gtk.SizeAllocatedArgs args)
		{
			if (parent.Child1 != null && parent.Child1.Visible && parent.Child2 != null && parent.Child2.Visible) {
				Show ();
				int centerSize = Child == null ? GrabAreaSize / 2 : 0;
				if (horizontal)
					SizeAllocate (new Gdk.Rectangle (parent.Child1.Allocation.X + parent.Child1.Allocation.Width - centerSize, args.Allocation.Y, GrabAreaSize, args.Allocation.Height));
				else
					SizeAllocate (new Gdk.Rectangle (args.Allocation.X, parent.Child1.Allocation.Y + parent.Child1.Allocation.Height - centerSize, args.Allocation.Width, GrabAreaSize));
			} else
				Hide ();
		}

		public int GrabAreaSize {
			get {
				if (horizontal)
					return SizeRequest ().Width;
				else
					return SizeRequest ().Height;
			}
			set {
				if (horizontal)
					WidthRequest = value;
				else
					HeightRequest = value;
			}
		}

		public Gtk.Widget HandleWidget {
			get { return Child; }
			set {
				if (Child != null) {
					Remove (Child);
				}
				if (value != null) {
					Add (value);
					value.Show ();
					VisibleWindow = true;
					WidthRequest = HeightRequest = -1;
					HPanedThin.InitStyle (parent, GrabAreaSize);
				} else {
					VisibleWindow = false;
					if (horizontal)
						WidthRequest = 1;
					else
						HeightRequest = 1;
					HPanedThin.InitStyle (parent, 1);
				}
			}
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			if (horizontal)
				GdkWindow.Cursor = resizeCursorW;
			else
				GdkWindow.Cursor = resizeCursorH;
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			GdkWindow.Cursor = null;
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (horizontal)
				initialPos = (int) evnt.XRoot;
			else
				initialPos = (int) evnt.YRoot;
			initialPanedPos = parent.Position;
			dragging = true;
			return true;
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			dragging = false;
			return true;
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			if (dragging) {
				if (horizontal) {
					int newpos = initialPanedPos + ((int) evnt.XRoot - initialPos);
					parent.Position = newpos >= 10 ? newpos : 10;
				}
				else {
					int newpos = initialPanedPos + ((int) evnt.YRoot - initialPos);
					parent.Position = newpos >= 10 ? newpos : 10;
				}
			}
			return base.OnMotionNotifyEvent (evnt);
		}
	}
}

