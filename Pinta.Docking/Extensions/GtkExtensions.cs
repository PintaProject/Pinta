//
// GtkExtensions.cs
//
// Authors: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (C) 2011 Xamarin Inc.
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
//

using System;
using Cairo;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection.Emit;
using System.Reflection;
using Gtk;

namespace Pinta.Docking
{
    static class GtkExtensions
	{
        internal const string LIBGTK = "libgtk-win32-2.0-0.dll";
        internal const string LIBATK = "libatk-1.0-0.dll";
        internal const string LIBGLIB = "libglib-2.0-0.dll";
        internal const string LIBGDK = "libgdk-win32-2.0-0.dll";
        internal const string LIBGTKGLUE = "gtksharpglue-2";

        static bool oldMacKeyHacks = false;
        static bool supportsHiResIcons = true;

        public static Gdk.Point GetScreenCoordinates (this Gtk.Widget w, Gdk.Point p)
        {
            if (w.ParentWindow == null)
                return Gdk.Point.Zero;
            int x, y;
            w.ParentWindow.GetOrigin (out x, out y);
            var a = w.Allocation;
            x += a.X;
            y += a.Y;
            return new Gdk.Point (x + p.X, y + p.Y);
        }

        /// <summary>
        /// This method can be used to get a reliave Leave event for a widget, which
        /// is not fired if the pointer leaves the widget to enter a child widget.
        /// To ubsubscribe the event, dispose the object returned by the method.
        /// </summary>
        public static IDisposable SubscribeLeaveEvent (this Gtk.Widget w, System.Action leaveHandler)
        {
            return new LeaveEventData (w, leaveHandler);
        }

        public static Gdk.Color ToGdkColor (this Xwt.Drawing.Color color)
        {
            return new Gdk.Color ((byte)(color.Red * 255d), (byte)(color.Green * 255d), (byte)(color.Blue * 255d));
        }
    }

    class LeaveEventData : IDisposable
    {
        public System.Action LeaveHandler;
        public Gtk.Widget RootWidget;
        public bool Inside;

        public LeaveEventData (Gtk.Widget w, System.Action leaveHandler)
        {
            RootWidget = w;
            LeaveHandler = leaveHandler;
            if (w.IsRealized) {
                RootWidget.Unrealized += HandleUnrealized;
                TrackLeaveEvent (w);
            } else
                w.Realized += HandleRealized;
        }

        void HandleRealized (object sender, EventArgs e)
        {
            RootWidget.Realized -= HandleRealized;
            RootWidget.Unrealized += HandleUnrealized;
            TrackLeaveEvent (RootWidget);
        }

        void HandleUnrealized (object sender, EventArgs e)
        {
            RootWidget.Unrealized -= HandleUnrealized;
            UntrackLeaveEvent (RootWidget);
            RootWidget.Realized += HandleRealized;
            if (Inside) {
                Inside = false;
                LeaveHandler ();
            }
        }

        public void Dispose ()
        {
            if (RootWidget.IsRealized) {
                UntrackLeaveEvent (RootWidget);
                RootWidget.Unrealized -= HandleUnrealized;
            } else {
                RootWidget.Realized -= HandleRealized;
            }
        }

        public void TrackLeaveEvent (Gtk.Widget w)
        {
            w.LeaveNotifyEvent += HandleLeaveNotifyEvent;
            w.EnterNotifyEvent += HandleEnterNotifyEvent;
            if (w is Gtk.Container) {
                ((Gtk.Container)w).Added += HandleAdded;
                ((Gtk.Container)w).Removed += HandleRemoved;
                foreach (var c in ((Gtk.Container)w).Children)
                    TrackLeaveEvent (c);
            }
        }

        void UntrackLeaveEvent (Gtk.Widget w)
        {
            w.LeaveNotifyEvent -= HandleLeaveNotifyEvent;
            w.EnterNotifyEvent -= HandleEnterNotifyEvent;
            if (w is Gtk.Container) {
                ((Gtk.Container)w).Added -= HandleAdded;
                ((Gtk.Container)w).Removed -= HandleRemoved;
                foreach (var c in ((Gtk.Container)w).Children)
                    UntrackLeaveEvent (c);
            }
        }

        void HandleRemoved (object o, RemovedArgs args)
        {
            UntrackLeaveEvent (args.Widget);
        }

        void HandleAdded (object o, AddedArgs args)
        {
            TrackLeaveEvent (args.Widget);
        }

        void HandleEnterNotifyEvent (object o, EnterNotifyEventArgs args)
        {
            Inside = true;
        }

        void HandleLeaveNotifyEvent (object o, LeaveNotifyEventArgs args)
        {
            Inside = false;

            // Delay the call to the leave handler since the pointer may be
            // entering a child widget, in which case the event doesn't have to be fired
            Gtk.Application.Invoke (delegate {
                if (!Inside)
                    LeaveHandler ();
            });
        }
    }

}
