//
// PaintBrush.cs
//  
// Author:
//       Aaron Bockover <abockover@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using Gtk;

using Pinta.Core;
using Pinta.Tools;

namespace Pinta.Tools.Brushes
{
	public abstract class PaintBrush : BasePaintBrush
	{
		private static Random random = new Random ();
		public Random Random {
			get { return random; }
		}

		public virtual double StrokeAlphaMultiplier {
			get { return 1; }
		}

		public PaintBrushTool Tool { get; set; }

		protected Cairo.Context G {
			get { return Tool.Drawable; }
		}

		protected Color StrokeColor { get { return Tool.StrokeColor; } }
		protected ImageSurface Surface { get { return Tool.Surface; } }

		public virtual void DoMouseUp ()
		{
			OnMouseUp ();
		}

		public virtual void DoMouseDown ()
		{
			OnMouseDown ();
		}

		public virtual Gdk.Rectangle DoMouseMove (int x, int y, int lastX, int lastY)
		{
			return OnMouseMove (x, y, lastX, lastY);
		}

		protected virtual void OnMouseUp ()
		{
		}

		protected virtual void OnMouseDown ()
		{
		}

		protected virtual Gdk.Rectangle OnMouseMove (int x, int y, int lastX, int lastY)
		{
			return Gdk.Rectangle.Zero;
		}
	}
}

