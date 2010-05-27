//
// AutoHideBox.cs
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
using Gtk;
using Gdk;

namespace MonoDevelop.Components.Docking
{
	class AutoHideBox: DockFrameTopLevel
	{
		static Gdk.Cursor resizeCursorW = new Gdk.Cursor (Gdk.CursorType.SbHDoubleArrow);
		static Gdk.Cursor resizeCursorH = new Gdk.Cursor (Gdk.CursorType.SbVDoubleArrow);
		
		bool resizing;
		int resizePos;
		int origSize;
		int origPos;
		bool horiz;
		bool startPos;
		DockFrame frame;
		bool animating;
		int targetSize;
		int targetPos;
		ScrollableContainer scrollable;
		Gtk.PositionType position;
		bool disposed;
		bool insideGrip;
		
		const int gripSize = 8;
		
		public AutoHideBox (DockFrame frame, DockItem item, Gtk.PositionType pos, int size)
		{
			this.position = pos;
			this.frame = frame;
			this.targetSize = size;
			horiz = pos == PositionType.Left || pos == PositionType.Right;
			startPos = pos == PositionType.Top || pos == PositionType.Left;
			Events = Events | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
			
			Box fr;
			CustomFrame cframe = new CustomFrame ();
			switch (pos) {
				case PositionType.Left: cframe.SetMargins (1, 1, 0, 1); break;
				case PositionType.Right: cframe.SetMargins (1, 1, 1, 0); break;
				case PositionType.Top: cframe.SetMargins (0, 1, 1, 1); break;
				case PositionType.Bottom: cframe.SetMargins (1, 0, 1, 1); break;
			}
			EventBox sepBox = new EventBox ();
			cframe.Add (sepBox);
			
			if (horiz) {
				fr = new HBox ();
				sepBox.Realized += delegate { sepBox.GdkWindow.Cursor = resizeCursorW; };
				sepBox.WidthRequest = gripSize;
			} else {
				fr = new VBox ();
				sepBox.Realized += delegate { sepBox.GdkWindow.Cursor = resizeCursorH; };
				sepBox.HeightRequest = gripSize;
			}
			
			sepBox.Events = EventMask.AllEventsMask;
			
			if (pos == PositionType.Left || pos == PositionType.Top)
				fr.PackEnd (cframe, false, false, 0);
			else
				fr.PackStart (cframe, false, false, 0);

			Add (fr);
			ShowAll ();
			Hide ();
			
			scrollable = new ScrollableContainer ();
			scrollable.ScrollMode = false;
			scrollable.Show ();

			item.Widget.Show ();
			scrollable.Add (item.Widget);
			fr.PackStart (scrollable, true, true, 0);
			
			sepBox.ButtonPressEvent += OnSizeButtonPress;
			sepBox.ButtonReleaseEvent += OnSizeButtonRelease;
			sepBox.MotionNotifyEvent += OnSizeMotion;
			sepBox.ExposeEvent += OnGripExpose;
			sepBox.EnterNotifyEvent += delegate { insideGrip = true; sepBox.QueueDraw (); };
			sepBox.LeaveNotifyEvent += delegate { insideGrip = false; sepBox.QueueDraw (); };
		}
		
		public bool Disposed {
			get { return disposed; }
			set { disposed = value; }
		}
		
		public void AnimateShow ()
		{
			animating = true;
			scrollable.ScrollMode = true;
			scrollable.SetSize (position, targetSize);
			
			switch (position) {
			case PositionType.Left:
				WidthRequest = 0;
				break;
			case PositionType.Right:
				targetPos = X = X + WidthRequest;
				WidthRequest = 0;
				break;
			case PositionType.Top:
				HeightRequest = 0;
				break;
			case PositionType.Bottom:
				targetPos = Y = Y + HeightRequest;
				HeightRequest = 0;
				break;
			}
			Show ();
			GLib.Timeout.Add (10, RunAnimateShow);
		}
		
		protected override void OnShown ()
		{
			base.OnShown ();
		}

		
		public void AnimateHide ()
		{
			animating = true;
			scrollable.ScrollMode = true;
			scrollable.SetSize (position, targetSize);
			GLib.Timeout.Add (10, RunAnimateHide);
		}
		
		bool RunAnimateShow ()
		{
			if (!animating)
				return false;

			switch (position) {
			case PositionType.Left:
				WidthRequest += 1 + (targetSize - WidthRequest) / 3;
				if (WidthRequest < targetSize)
					return true;
				break;
			case PositionType.Right:
				WidthRequest += 1 + (targetSize - WidthRequest) / 3;
				X = targetPos - WidthRequest;
				if (WidthRequest < targetSize)
					return true;
				break;
			case PositionType.Top:
				HeightRequest += 1 + (targetSize - HeightRequest) / 3;
				if (HeightRequest < targetSize)
					return true;
				break;
			case PositionType.Bottom:
				HeightRequest += 1 + (targetSize - HeightRequest) / 3;
				Y = targetPos - HeightRequest;
				if (HeightRequest < targetSize)
					return true;
				break;
			}
			
			scrollable.ScrollMode = false;
			if (horiz)
				WidthRequest = targetSize;
			else
				HeightRequest = targetSize;
			animating = false;
			return false;
		}
		
		bool RunAnimateHide ()
		{
			if (!animating)
				return false;

			switch (position) {
			case PositionType.Left: {
				int ns = WidthRequest - 1 - WidthRequest / 3;
				if (ns > 0) {
					WidthRequest = ns;
					return true;
				}
				break;
			}
			case PositionType.Right: {
				int ns = WidthRequest - 1 - WidthRequest / 3;
				if (ns > 0) {
					WidthRequest = ns;
					X = targetPos - ns;
					return true;
				}
				break;
			}
			case PositionType.Top: {
				int ns = HeightRequest - 1 - HeightRequest / 3;
				if (ns > 0) {
					HeightRequest = ns;
					return true;
				}
				break;
			}
			case PositionType.Bottom: {
				int ns = HeightRequest - 1 - HeightRequest / 3;
				if (ns > 0) {
					HeightRequest = ns;
					Y = targetPos - ns;
					return true;
				}
				break;
			}
			}

			Hide ();
			animating = false;
			return false;
		}
		
		protected override void OnHidden ()
		{
			base.OnHidden ();
			animating = false;
		}

		
		public int Size {
			get {
				return horiz ? WidthRequest : HeightRequest;
			}
		}
		
		void OnSizeButtonPress (object ob, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Button == 1 && !animating) {
				int n;
				if (horiz) {
					Toplevel.GetPointer (out resizePos, out n);
					origSize = WidthRequest;
					if (!startPos) {
						origPos = X + origSize;
					}
				} else {
					Toplevel.GetPointer (out n, out resizePos);
					origSize = HeightRequest;
					if (!startPos) {
						origPos = Y + origSize;
					}
				}
				resizing = true;
			}
		}
		
		void OnSizeButtonRelease (object ob, Gtk.ButtonReleaseEventArgs args)
		{
			resizing = false;
		}
		
		void OnSizeMotion (object ob, Gtk.MotionNotifyEventArgs args)
		{
			if (resizing) {
				int newPos, n;
				if (horiz) {
					Toplevel.GetPointer (out newPos, out n);
					int diff = startPos ? (newPos - resizePos) : (resizePos - newPos);
					int newSize = origSize + diff;
					if (newSize < Child.SizeRequest ().Width)
						newSize = Child.SizeRequest ().Width;
					if (!startPos) {
						X = origPos - newSize;
					}
					WidthRequest = newSize;
				} else {
					Toplevel.GetPointer (out n, out newPos);
					int diff = startPos ? (newPos - resizePos) : (resizePos - newPos);
					int newSize = origSize + diff;
					if (newSize < Child.SizeRequest ().Height)
						newSize = Child.SizeRequest ().Height;
					if (!startPos) {
						Y = origPos - newSize;
					}
					HeightRequest = newSize;
				}
				frame.QueueResize ();
			}
		}
		
		void OnGripExpose (object ob, Gtk.ExposeEventArgs args)
		{
			EventBox w = (EventBox) ob;
			Gdk.Rectangle handleRect = w.Allocation;
//			w.GdkWindow.DrawRectangle (w.Style.DarkGC (StateType.Normal), true, handleRect);
			handleRect.X = handleRect.Y = 0;
			
/*			switch (position) {
			case PositionType.Top:
				handleRect.Height -= 4; handleRect.Y += 1;
				Gtk.Style.PaintHline (w.Style, w.GdkWindow, StateType.Normal, args.Event.Area, w, "", 0, w.Allocation.Width, gripSize - 2);
				break;
			case PositionType.Bottom:
				handleRect.Height -= 4; handleRect.Y += 3;
				Gtk.Style.PaintHline (w.Style, w.GdkWindow, StateType.Normal, args.Event.Area, w, "", 0, w.Allocation.Width, 0);
				break;
			case PositionType.Left:
				handleRect.Width -= 4; handleRect.X += 1;
				Gtk.Style.PaintVline (w.Style, w.GdkWindow, StateType.Normal, args.Event.Area, w, "", 0, w.Allocation.Height, gripSize - 2);
				break;
			case PositionType.Right:
				handleRect.Width -= 4; handleRect.X += 3;
				Gtk.Style.PaintVline (w.Style, w.GdkWindow, StateType.Normal, args.Event.Area, w, "", 0, w.Allocation.Height, 0);
				break;
			}*/
			
			Orientation or = horiz ? Orientation.Vertical : Orientation.Horizontal;
			StateType s = insideGrip ? StateType.Prelight : StateType.Normal;
			Gtk.Style.PaintHandle (w.Style, w.GdkWindow, s, ShadowType.None, args.Event.Area, w, "paned", handleRect.Left, handleRect.Top, handleRect.Width, handleRect.Height, or);
		}
	}
	
	class ScrollableContainer: EventBox
	{
		PositionType expandPos;
		bool scrollMode;
		int targetSize;
		
		public bool ScrollMode {
			get {
				return scrollMode;
			}
			set {
				scrollMode = value;
				QueueResize ();
			}
		}
		
		public void SetSize (PositionType expandPosition, int targetSize)
		{
			this.expandPos = expandPosition;
			this.targetSize = targetSize;
			QueueResize ();
		}
		
		protected override void OnSizeRequested (ref Requisition req)
		{
			base.OnSizeRequested (ref req);
			if (scrollMode || Child == null) {
				req.Width = 0;
				req.Height = 0;
			}
			else
				req = Child.SizeRequest ();
		}

		protected override void OnSizeAllocated (Rectangle alloc)
		{
			if (scrollMode && Child != null) {
				switch (expandPos) {
				case PositionType.Bottom:
					alloc = new Rectangle (alloc.X, alloc.Y, alloc.Width, targetSize);
					break;
				case PositionType.Top:
					alloc = new Rectangle (alloc.X, alloc.Y - targetSize + alloc.Height, alloc.Width, targetSize);
					break;
				case PositionType.Right:
					alloc = new Rectangle (alloc.X, alloc.Y, targetSize, alloc.Height);
					break;
				case PositionType.Left:
					alloc = new Rectangle (alloc.X - targetSize + alloc.Width, alloc.Y, targetSize, alloc.Height);
					break;
				}
			}
			base.OnSizeAllocated (alloc);
		}
	}

}
