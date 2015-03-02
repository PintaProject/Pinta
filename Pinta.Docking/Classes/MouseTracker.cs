//
// MouseTracker.cs
//
// Author:
//       Jason Smith <jason.smith@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using Gtk;

namespace MonoDevelop.Components
{
	public class MouseTracker
	{
		public bool Hovered { get; private set; }
		public Gdk.Point MousePosition { get; private set; }
		public bool TrackMotion { 
			get { return trackMotion; }
			set { 
				if (value == trackMotion)
					return;
				trackMotion = value; 
				OnTrackMotionChanged (); 
			} 
		}

		public event EventHandler MouseMoved;
		public event EventHandler HoveredChanged;

		bool trackMotion;
		Gtk.Widget owner;

		public MouseTracker (Gtk.Widget owner)
		{
			this.owner = owner;
			Hovered = false;
			MousePosition = new Gdk.Point(0, 0);

			owner.Events = owner.Events | Gdk.EventMask.PointerMotionMask;

			owner.MotionNotifyEvent += (object o, MotionNotifyEventArgs args) => {
				MousePosition = new Gdk.Point ((int)args.Event.X, (int)args.Event.Y);
				if (MouseMoved != null)
					MouseMoved (this, EventArgs.Empty);
			};

			owner.EnterNotifyEvent += (o, args) => {
				Hovered = true;
				if (HoveredChanged != null)
					HoveredChanged (this, EventArgs.Empty);
			};

			owner.LeaveNotifyEvent += (o, args) => {
				Hovered = false;
				if (HoveredChanged != null)
					HoveredChanged (this, EventArgs.Empty);
			};
		}

		void OnTrackMotionChanged ()
		{
			if (TrackMotion)
				owner.Events = owner.Events | Gdk.EventMask.PointerMotionMask;
			else
				owner.Events = owner.Events & ~Gdk.EventMask.PointerMotionMask;

		}
	}
}

