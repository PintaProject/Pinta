//
// DockedPosition.cs
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
using System.Xml.Serialization;

namespace Pinta.Docking.DockToolbars
{
	[XmlType ("dockedPosition")]
	public class DockedPosition: DockToolbarPosition
	{
		Placement placement;
		int dockOffset;
		int dockRow;
		
		public DockedPosition ()
		{
		}
		
		internal DockedPosition (DockToolbar bar)
		{
			dockOffset = bar.AnchorOffset;
			dockRow = bar.DockRow;
			placement = ((DockToolbarPanel)bar.Parent).Placement;
		}
		
		internal DockedPosition (Placement placement)
		{
			this.placement = placement;
			dockRow = -1;
		}
		
		[XmlAttribute ("offset")]
		public int DockOffset {
			get { return dockOffset; }
			set { dockOffset = value; }
		}
		
		[XmlAttribute ("row")]
		public int DockRow {
			get { return dockRow; }
			set { dockRow = value; }
		}
		
		[XmlAttribute ("placement")]
		public Placement Placement {
			get { return placement; }
			set { placement = value; }
		}
		
		internal override void RestorePosition (DockToolbarFrame frame, DockToolbar bar)
		{
			frame.DockToolbar (bar, placement, dockOffset, dockRow);
		}
	}
}
