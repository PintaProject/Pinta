// 
// RoundedRectangleTool.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
using Cairo;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Tools
{
	public class RoundedRectangleTool : ShapeTool
	{
		public override string Name {
			get { return Catalog.GetString ("Rounded Rectangle"); }
		}
		public override string Icon {
			get { return "Tools.RoundedRectangle.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString("Left click to draw a shape with the primary color." +
				  "\nLeft click on a shape to add a control point." +
				  "\nLeft click on a control point and drag to move it." +
				  "\nRight click on a control point and drag to change its tension." +
				  "\nHold Shift to snap to angles." +
				  "\nUse arrow keys to move the selected control point." +
				  "\nPress Ctrl + left/right arrows to select control points by order." +
				  "\nPress Delete to delete the selected control point." +
				  "\nPress Space to add a new control point at the mouse position." +
				  "\nHold Ctrl while pressing Space to create the control point at the exact same position." +
				  "\nHold Ctrl while left clicking on a control point to create a new shape at the exact same position." +
				  "\nHold Ctrl while clicking outside of the image bounds to create a new shape starting at the edge." +
				  "\nPress Enter to finalize the shape.");
			}
		}
		public override Gdk.Cursor DefaultCursor {
            get { return new Gdk.Cursor (Gdk.Display.Default, PintaCore.Resources.GetIcon ("Cursor.RoundedRectangle.png"), 9, 18); }
		}
		public override int Priority {
			get { return 43; }
		}

		public override BaseEditEngine.ShapeTypes ShapeType
		{
			get
			{
				return BaseEditEngine.ShapeTypes.RoundedLineSeries;
			}
		}

		public RoundedRectangleTool()
		{
			EditEngine = new RoundedLineEditEngine(this);

			BaseEditEngine.CorrespondingTools.Add(ShapeType, this);
		}
	}
}
