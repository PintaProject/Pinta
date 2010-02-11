// 
// ZoomTool.cs
//  
// Author:
//       dufoli <${AuthorEmail}>
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

namespace Pinta.Core
{


	public class ZoomTool : ShapeTool
	{
		/*private Cursor cursorZoomIn;
        private Cursor cursorZoomOut;
        private Cursor cursorZoom;
        private Cursor cursorZoomPan;
        */
		private uint mouseDown;
		//private bool moveOffsetMode;
		//private PointD downPt;
        //private PointD lastPt;
		//private Rectangle rect = new Rectangle(0,0,0,0);
		
		public override string Name {
			get { return "Zoom"; }
		}
		public override string Icon {
			get { return "Tools.Zoom.png"; }
		}
		public override string StatusBarText {
			get { return "Click left to zoom in. Click right to zoom out. Click and drag to zoom in selection."; }
		}
		public override bool Enabled {
			get { return true; }
		}
		
		// We don't want the ShapeTool's toolbar
		protected override void BuildToolBar (Gtk.Toolbar tb)
		{
		}
		
		protected override Rectangle DrawShape (Rectangle r, Layer l)
		{
			Path path = PintaCore.Layers.SelectionPath;
			
			using (Context g = new Context (l.Surface))
				PintaCore.Layers.SelectionPath = g.CreateRectanglePath (r);
			
			(path as IDisposable).Dispose ();
			
			// Add some padding for invalidation
			return new Rectangle (r.X, r.Y, r.Width + 2, r.Height + 2);
		}
		
		//TODO change cursor
		
		public ZoomTool () : base ()
		{
			this.mouseDown = 0;
		}
		
		
        /*protected override void OnActivate()
        {
            base.OnActivate();
            
            this.outline = new Selection();
            this.outlineRenderer = new SelectionRenderer(this.RendererList, this.outline, this.PintaCore.Workspace);
            this.outlineRenderer.InvertedTinting = true;
            this.outlineRenderer.TintColor = Color.FromArgb(128, 255, 255, 255);
            this.outlineRenderer.ResetOutlineWhiteOpacity();
            this.RendererList.Add(this.outlineRenderer, true);
        }
        */

        protected override void OnDeactivated ()
        {
        	base.OnDeactivated();
        }

        protected override void OnMouseDown(Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
        {
			shape_origin = point;
            
            switch (args.Event.Button) 
            {
                case 1://left
                    //Cursor = cursorZoomIn;
                    break;

                case 2://midle
                    //Cursor = cursorZoomPan;
                    break;

                case 3://right
                    //Cursor = cursorZoomOut;
                    break;
            }

            mouseDown = args.Event.Button;
            //OnMouseMove(null, args, point);

        }
		
		protected override void OnMouseMove(object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
        {
			base.OnMouseMove (o, args, point);


            if ((mouseDown == 1 && 
                 Math.Sqrt(Math.Pow(point.X - shape_origin.X, 2)+Math.Pow(point.Y - shape_origin.Y, 2)) > 10))  // if they've moved the mouse more than 10 pixels since they clicked
            {
                is_drawing = true;
            } 
            else if (mouseDown == 2)
            {
                PointD lastScrollPosition = PintaCore.Workspace.CenterPosition;
                lastScrollPosition.X += (point.X - shape_origin.X) * PintaCore.Workspace.Scale;
                lastScrollPosition.Y += (point.Y - shape_origin.Y) * PintaCore.Workspace.Scale;
                PintaCore.Workspace.CenterPosition = lastScrollPosition;
            }

            //lastPt = point;
        }

        protected override void OnMouseUp(Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
        {
			double x = point.X;
			double y = point.Y;

			// If the user didn't move the mouse, they want to deselect
			int tolerance = 10;

			
            //OnMouseMove(e);
            bool resetMouseDown = true;

            //Cursor = cursorZoom;

            if (mouseDown == 1 || mouseDown == 3) //left or right
            {
                Rectangle zoomTo = PointsToRectangle(shape_origin, point, false);

                if (args.Event.Button == 1) //left
                {
                    if (Math.Abs (shape_origin.X - x) <= tolerance && Math.Abs (shape_origin.Y - y) <= tolerance) 
                    {
						PintaCore.Workspace.RecenterView(point);
						PintaCore.Workspace.ZoomIn();
                    } 
                    else
                    {
						PintaCore.Workspace.ZoomToRectangle(zoomTo);
                    }
                }
                else
                {
					PintaCore.Workspace.RecenterView(point);
					PintaCore.Workspace.ZoomOut();
                }

                //this.outline.Reset();
            }

            if (resetMouseDown)
            {
                mouseDown = 0;
            }
			
			is_drawing = false;
        }

        /*private void UpdateDrawnRect() 
        {
            if (!rect.IsEmpty)
            {
                this.outline.PerformChanging();
                this.outline.Reset();
                this.outline.SetContinuation(rect, CombineMode.Replace);
                this.outlineRenderer.ResetOutlineWhiteOpacity();
                this.outline.CommitContinuation();
                this.outline.PerformChanged();
                Update();
            }
        }*/
	}
}
