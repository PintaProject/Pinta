//
// DockFrameTopLevel.cs
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

namespace Pinta.Docking
{
	class DockFrameTopLevel: EventBox
	{
		int x, y;
		int width, height;
		bool repositionRequested;
		DockFrame frame;

		public DockFrameTopLevel (DockFrame frame)
		{
			this.frame = frame;
		}
		
		public int X {
			get { return x; }
			set {
				x = value;
				UpdateWindowPos ();
			}
		}
		
		public int Y {
			get { return y; }
			set {
				y = value;
				UpdateWindowPos ();
			}
		}

		public Gdk.Size Size {
			get {
				if (ContainerWindow != null) {
					int w, h;
					ContainerWindow.GetSize (out w, out h);
					return new Gdk.Size (w, h);
				} else {
					return new Gdk.Size (WidthRequest, HeightRequest);
				}
			}
			set {
				width = value.Width;
				height = value.Height;
				if (ContainerWindow != null)
					UpdateWindowPos ();
				else {
					WidthRequest = value.Width;
					HeightRequest = value.Height;
				}
			}
		}

		public int Width {
			get {
				if (ContainerWindow != null) {
					int w, h;
					ContainerWindow.GetSize (out w, out h);
					return w;
				} else
					return WidthRequest;
			}
			set {
				width = value;
				if (ContainerWindow != null)
					UpdateWindowPos ();
				else
					WidthRequest = value;
			}
		}

		public int Height {
			get {
				if (ContainerWindow != null) {
					int w, h;
					ContainerWindow.GetSize (out w, out h);
					return h;
				} else
					return HeightRequest;
			}
			set {
				height = value;
				if (ContainerWindow != null)
					UpdateWindowPos ();
				else
					HeightRequest = value;
			}
		}


		void UpdateWindowPos ()
		{
			if (ContainerWindow != null) {
				if (!repositionRequested && width != 0 && height != 0) {
					repositionRequested = true;
					Application.Invoke (delegate {
						var pos = frame.GetScreenCoordinates (new Gdk.Point (x, y));
                        PlaceWindow (ContainerWindow, pos.X, pos.Y, width, height);
						repositionRequested = false;
					});
				}
			} else if (Parent != null)
				Parent.QueueResize ();
		}
        
        // Pinta TODO: This may need to be overridden for Mac?
        private void PlaceWindow (Gtk.Window window, int x, int y, int width, int height)
        {
            window.Move (x, y);
            window.Resize (width, height);
        }

		internal Gtk.Window ContainerWindow { get; set; }
	}

}
