// 
// ToolControl.cs
//  
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
// 
// Copyright (c) 2011 Olivier Dufour
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

namespace Pinta.Tools
{
	public class ToolControl
	{
		public ToolControl (Gdk.CursorType cursor, MouseHandler moveAction)
		{
			this.action = moveAction;
			Position = new PointD (-5, -5);
		    Cursor = cursor;
		}

        private const int Size = 6;
		private const int Tolerance = 10;
		public static readonly Color FillColor = new Color (0, 0, 1, 0.5);
		public static readonly Color StrokeColor = new Color (0, 0, 1, 0.7);
		private readonly MouseHandler action;

		public PointD Position {get; set;}
        public Gdk.CursorType Cursor { get; private set; }

		public bool IsInside (PointD point)
		{
			return (Math.Abs (point.X - Position.X) <= Tolerance) && (Math.Abs (point.Y - Position.Y) <= Tolerance);
		}

        public void HandleMouseMove (double x, double y, Gdk.ModifierType state)
        {
            action (x, y, state);
        }

        public void Render (Context g)
        {
            var rect = GetHandleRect ();
            g.FillStrokedRectangle (rect, FillColor, StrokeColor, 1);
        }

        /// <summary>
        /// Erase the handle that was drawn in a previous call to Render ().
        /// </summary>
        public void Clear (Context g)
        {
            g.Save ();

            var rect = GetHandleRect ().Inflate (2, 2);
            using (var path = g.CreateRectanglePath (rect))
                g.AppendPath (path);
            g.Operator = Operator.Clear;
            g.Fill ();

            g.Restore ();
        }

	    private Rectangle GetHandleRect ()
	    {
	        var scale_factor = (1.0/PintaCore.Workspace.ActiveWorkspace.Scale);
	        return new Cairo.Rectangle (Position.X - scale_factor*Size/2,
	            Position.Y - scale_factor*Size/2,
	            scale_factor*Size, scale_factor*Size);
	    }
    }
}

