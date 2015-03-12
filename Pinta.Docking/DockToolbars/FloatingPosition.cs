//
// FloatingPosition.cs
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
using System.Xml.Serialization;

namespace Pinta.Docking.DockToolbars
{
	[XmlType ("floatingPosition")]
	public class FloatingPosition: DockToolbarPosition
	{
		Orientation orientation;
		int x;
		int y;
		
		public FloatingPosition ()
		{
		}
		
		internal FloatingPosition (DockToolbar bar)
		{
			orientation = bar.Orientation;
			bar.FloatingDock.GetPosition (out x, out y);
		}
		
		[XmlAttribute ("x")]
		public int X {
			get { return x; }
			set { x = value; }
		}
		
		[XmlAttribute ("y")]
		public int Y {
			get { return y; }
			set { y = value; }
		}
		
		[XmlAttribute ("orientation")]
		public Orientation Orientation {
			get { return orientation; }
			set { orientation = value; }
		}
		
		internal override void RestorePosition (DockToolbarFrame frame, DockToolbar bar)
		{
			frame.FloatBar (bar, orientation, x, y);
		}
	}
}
