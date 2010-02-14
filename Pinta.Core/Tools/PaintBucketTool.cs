// 
// PaintBucketTool.cs
//  
// Author:
//       dufoli <>
// 
// Copyright (c) 2010 dufoli
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
using System.Collections.Generic;

namespace Pinta.Core
{

	public class PaintBucketTool : BaseTool
	{
		public override string Name {
			get { return "Paint Bucket"; }
		}
		public override string Icon {
			get { return "Tools.PaintBucket.png"; }
		}
		/*
		public override string StatusBarText {
			get { return "Fill stencil by color."; }
		}
		public override bool Enabled {
			get { return true; }
		}
		
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			if (!PintaCore.Workspace.PointInCanvas (point)) 
				return;
			
			Path path = PintaCore.Layers.SelectionPath;
			//TODO check if point is in selection
			
			if (args.Event.Button == 1)
				color = PintaCore.Palette.PrimaryColor.ToUint();
			else if (args.Event.Button == 3)
				color = PintaCore.Palette.SecondaryColor.ToUint();
			
			//if ((args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) //continuous
			    FillStencilFromPoint (point);
            //else
            //    FillStencilByColor ();
				
		}

		void FillStencilByColor ()
		{
			ImageSurface surf = PintaCore.Layers.CurrentLayer.Surface;

			using (Context g = new Context (surf)) {
				g.FillPreserve();
			}
			
			throw new System.NotImplementedException ();
		}


		unsafe void FillStencilFromPoint (PointD point)
		{
			ImageSurface surf = PintaCore.Layers.CurrentLayer.Surface;
			checkedPoints = new List<Point> ();
			
			ColorBgra* ptr = surf.GetPointAddress ((int)point.X, (int)point.Y);
			
			oldColor = ptr->Bgra;
			using (Context g = new Context (surf)) {
				CheckPoint (g, (int)point.X, (int)point.Y);
			}
			PintaCore.Workspace.Invalidate();
		}
		
		private List<Point> checkedPoints;
		uint oldColor;
		uint color;
		
		unsafe void CheckPoint (Context g, int x, int y)
		{
			if (checkedPoints.Contains(new Point(x, y)))
				return;
			
			if (!PintaCore.Workspace.PointInCanvas(new PointD(x, y)))
				return;
			
			checkedPoints.Add (new Point (x, y));
			
			ImageSurface surf = PintaCore.Layers.CurrentLayer.Surface;
			
			ColorBgra* ptr = surf.GetPointAddress (x, y);
			if (ptr->Bgra == oldColor)
			{
				ptr->Bgra = color;
				//TODO check if outside of selectionPath
				
				CheckPoint (g, x - 1, y);
				CheckPoint (g, x + 1, y);
				CheckPoint (g, x, y - 1);
				CheckPoint (g, x, y + 1);
			}
		}
		*/
	}
}
