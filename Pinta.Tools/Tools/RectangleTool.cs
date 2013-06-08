// 
// RectangleTool.cs
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
	public class RectangleTool : ShapeTool
	{
		public override string Name {
			get { return Catalog.GetString ("Rectangle"); }
		}
		public override string Icon {
			get { return "Tools.Rectangle.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString ("Click and drag to draw a rectangle (right click for secondary color). Hold Shift to constrain to a square."); }
		}
		public override Gdk.Cursor DefaultCursor {
			get { return new Gdk.Cursor (PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon ("Cursor.Rectangle.png"), 9, 18); }
		}
		public override int Priority{
			get { return 41; }
		}

		protected override Rectangle DrawShape (Rectangle rect, Layer l)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			Rectangle dirty;
			
			using (Context g = new Context (l.Surface)) {
				g.AppendPath (doc.Selection.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				if (FillShape && StrokeShape)
					dirty = g.FillStrokedRectangle (rect, fill_color, outline_color, BrushWidth);
				else if (FillShape)
					dirty = g.FillRectangle (rect, outline_color);
				else
					dirty = g.DrawRectangle (rect, outline_color, BrushWidth);
			}
			
			return dirty;
		}
	}
}
