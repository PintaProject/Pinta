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
using Gdk;
using Pinta.Core;

namespace Pinta.Tools
{
	public class ToolControl
	{
		public ToolControl (MouseHandler moveAction)
		{
			this.action = moveAction;
			Position = new PointD (-5, -5);
		}

		int tolerance = 3;
		MouseHandler action;

		public PointD Position {get; set;}

		public bool Handle (BaseTool tool, Cairo.PointD point)
		{
			if (IsInside (point.X, point.Y)) {
				tool.MouseMoved += action;
				tool.MouseReleased += (x, y, s) => {tool.MouseMoved -= action;};
				//TODO unregister mouse release
				return true;
			}
			return false;
		}

		public bool IsInside (double x, double y)
		{
			return (Math.Abs (x - Position.X) <= tolerance) && (Math.Abs (y - Position.Y) <= tolerance);
		}

		public void Render (Layer layer)
		{
			double scale_factor = (1.0 / PintaCore.Workspace.ActiveWorkspace.Scale);
			using (Context g = new Context (layer.Surface))
				g.FillStrokedRectangle (new Cairo.Rectangle (Position.X - scale_factor * 2, Position.Y - scale_factor * 2, scale_factor * 4, scale_factor * 4),
				                        new Cairo.Color (0, 0, 1, 0.5), new Cairo.Color (0, 0, 1, 0.7), 1);
		}
	}
}

