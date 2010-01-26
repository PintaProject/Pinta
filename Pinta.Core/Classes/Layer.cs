// 
// Layer.cs
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

namespace Pinta.Core
{
	public class Layer
	{
		public ImageSurface Surface { get; set; }
		public double Opacity { get; set; }
		public bool Hidden { get; set; }
		public string Name { get; set; }
		public bool Tiled { get; set; }
		public PointD Offset { get; set; }
		
		public Layer () : this (null)
		{
		}
		
		public Layer (ImageSurface surface) : this (surface, false, 1f, "")
		{
		}
		
		public Layer (ImageSurface surface, bool hidden, double opacity, string name)
		{
			Surface = surface;
			Hidden = hidden;
			Opacity = opacity;
			Name = name;
			Offset = new PointD (0, 0);
		}	
		
		public void Clear ()
		{
			using (Context g = new Context (Surface)) {
				g.Operator = Operator.Clear;
				g.Paint ();
			}
		}
		
		public void FlipHorizontal ()
		{
			Layer dest = PintaCore.Layers.CreateLayer ();
			
			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.Matrix = new Matrix (-1, 0, 0, 1, Surface.Width, 0);
				g.SetSource (Surface);
				
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest.Surface;
			(old as IDisposable).Dispose ();
		}
		
		public void FlipVertical ()
		{
			Layer dest = PintaCore.Layers.CreateLayer ();
			
			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.Matrix = new Matrix (1, 0, 0, -1, 0, Surface.Height);
				g.SetSource (Surface);
				
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest.Surface;
			(old as IDisposable).Dispose ();
		}
		
		public void Rotate180 ()
		{
			Layer dest = PintaCore.Layers.CreateLayer ();
			
			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.Matrix = new Matrix (-1, 0, 0, -1, Surface.Width, Surface.Height);
				g.SetSource (Surface);
				
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest.Surface;
			(old as IDisposable).Dispose ();
		}
		
		public void Rotate90CW ()
		{
			double w = PintaCore.Workspace.ImageSize.X;
			double h = PintaCore.Workspace.ImageSize.Y;
			
			Layer dest = PintaCore.Layers.CreateLayer ("", (int)h, (int)w);
			
			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.Translate (h / 2, w / 2);
				g.Rotate (Math.PI / 2);
				g.Translate (-w / 2, -h / 2);
				g.SetSource (Surface);
				
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest.Surface;
			(old as IDisposable).Dispose ();
		}
		
		public void Rotate90CCW ()
		{
			double w = PintaCore.Workspace.ImageSize.X;
			double h = PintaCore.Workspace.ImageSize.Y;
			
			Layer dest = PintaCore.Layers.CreateLayer ("", (int)h, (int)w);
			
			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.Translate (h / 2, w / 2);
				g.Rotate (Math.PI / -2);
				g.Translate (-w / 2, -h / 2);
				g.SetSource (Surface);
				
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest.Surface;
			(old as IDisposable).Dispose ();
		}
		
		public unsafe void Sepia ()
		{
			Desaturate ();
			
			UnaryPixelOp op = new UnaryPixelOps.Level(
				ColorBgra.Black, 
				ColorBgra.White,
				new float[] { 1.2f, 1.0f, 0.8f },
				ColorBgra.Black,
				ColorBgra.White);

			ImageSurface dest = Surface.Clone ();

			ColorBgra* dstPtr = (ColorBgra*)dest.DataPtr;
			int len = Surface.Data.Length / 4;
			
			op.Apply (dstPtr, len);

			using (Context g = new Context (Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.SetSource (dest);
				g.Paint ();
			}

			(dest as IDisposable).Dispose ();
		}
		
		public unsafe void Invert ()
		{
			ImageSurface dest = Surface.Clone ();

			ColorBgra* dstPtr = (ColorBgra*)dest.DataPtr;
			int len = Surface.Data.Length / 4;
			
			for (int i = 0; i < len; i++) {
				if (dstPtr->A != 0)
				*dstPtr = (ColorBgra.FromBgra((byte)(255 - dstPtr->B), (byte)(255 - dstPtr->G), (byte)(255 - dstPtr->R), dstPtr->A));
				dstPtr++;
			}

			using (Context g = new Context (Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.SetSource (dest);
				g.Paint ();
			}

			(dest as IDisposable).Dispose ();
		}
		
		public unsafe void Desaturate ()
		{
			ImageSurface dest = Surface.Clone ();

			ColorBgra* dstPtr = (ColorBgra*)dest.DataPtr;
			int len = Surface.Data.Length / 4;
			
			for (int i = 0; i < len; i++) {
				byte ib = dstPtr->GetIntensityByte();

				dstPtr->R = ib;
				dstPtr->G = ib;
				dstPtr->B = ib;
				dstPtr++;
			}

			using (Context g = new Context (Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.SetSource (dest);
				g.Paint ();
			}
			
			(dest as IDisposable).Dispose ();
		}
		
		public unsafe void AutoLevel ()
		{
			ImageSurface dest = Surface.Clone ();
			ColorBgra* dstPtr = (ColorBgra*)dest.DataPtr;
			ColorBgra* srcPtr = (ColorBgra*)Surface.DataPtr;
			
			int len = Surface.Data.Length / 4;

			UnaryPixelOps.Level levels = null;
		        
			HistogramRgb histogram = new HistogramRgb();
			histogram.UpdateHistogram (Surface, new Rectangle (0, 0, Surface.Width, Surface.Height));
			levels = histogram.MakeLevelsAuto();

			if (levels.isValid)
				levels.Apply (dstPtr, srcPtr, len);
				
			using (Context g = new Context (Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.SetSource (dest);
				g.Paint ();
			}

			(dest as IDisposable).Dispose ();
		}

		public void Resize (int width, int height)
		{
			ImageSurface dest = new ImageSurface (Format.Argb32, width, height);

			using (Context g = new Context (dest)) {
				g.Scale ((double)width / (double)Surface.Width, (double)height / (double)Surface.Height);
				g.SetSourceSurface (Surface, 0, 0);
				g.Paint ();
			}

			(Surface as IDisposable).Dispose ();
			Surface = dest;
		}

		public void ResizeCanvas (int width, int height, Anchor anchor)
		{
			ImageSurface dest = new ImageSurface (Format.Argb32, width, height);

			int delta_x = Surface.Width - width;
			int delta_y = Surface.Height - height;
			
			using (Context g = new Context (dest)) {
				switch (anchor) {
					case Anchor.NW:
						g.SetSourceSurface (Surface, 0, 0);
						break;
					case Anchor.N:
						g.SetSourceSurface (Surface, -delta_x / 2, 0);
						break;
					case Anchor.NE:
						g.SetSourceSurface (Surface, -delta_x, 0);
						break;
					case Anchor.E:
						g.SetSourceSurface (Surface, -delta_x, -delta_y / 2);
						break;
					case Anchor.SE:
						g.SetSourceSurface (Surface, -delta_x, -delta_y);
						break;
					case Anchor.S:
						g.SetSourceSurface (Surface, -delta_x / 2, -delta_y);
						break;
					case Anchor.SW:
						g.SetSourceSurface (Surface, 0, -delta_y);
						break;
					case Anchor.W:
						g.SetSourceSurface (Surface, 0, -delta_y / 2);
						break;
					case Anchor.Center:
						g.SetSourceSurface (Surface, -delta_x / 2, -delta_y / 2);
						break;
				}
				
				g.Paint ();
			}

			(Surface as IDisposable).Dispose ();
			Surface = dest;
		}

		public void Crop (Rectangle rect)
		{
			ImageSurface dest = new ImageSurface (Format.Argb32, (int)rect.Width, (int)rect.Height);

			using (Context g = new Context (dest)) {
				g.SetSourceSurface (Surface, -(int)rect.X, -(int)rect.Y);
				g.Paint ();
			}

			(Surface as IDisposable).Dispose ();
			Surface = dest;
		}
	}
}
