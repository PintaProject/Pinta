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
using System.ComponentModel;
using System.Collections.Specialized;
using Cairo;
using Gdk;

namespace Pinta.Core
{
	public class Layer : ObservableObject
	{	
		private double opacity;
		private bool hidden;
		private string name;
		private BlendMode blend_mode;
		private Matrix transform = new Matrix();

		public Layer () : this (null)
		{
		}
		
		public Layer (ImageSurface surface) : this (surface, false, 1f, "")
		{
		}
		
		public Layer (ImageSurface surface, bool hidden, double opacity, string name)
		{
			Surface = surface;

			this.hidden = hidden;
			this.opacity = opacity;
			this.name = name;			
			this.blend_mode = BlendMode.Normal;
		}
		
		public ImageSurface Surface { get; set; }
		public bool Tiled { get; set; }	
		public Matrix Transform { get { return transform; } }
		
		public static readonly string OpacityProperty = "Opacity";
		public static readonly string HiddenProperty = "Hidden";
		public static readonly string NameProperty = "Name";		
		public static readonly string BlendModeProperty = "BlendMode";

		public double Opacity {
			get { return opacity; }
			set { if (opacity != value) SetValue (OpacityProperty, ref opacity, value); }
		}
		
		public bool Hidden {
			get { return hidden; }
			set { if (hidden != value) SetValue (HiddenProperty, ref hidden, value); }
		}
		
		public string Name {
			get { return name; }
			set { if (name != value) SetValue (NameProperty, ref name, value); }
		}				
			
		public BlendMode BlendMode {
			get { return blend_mode; }
			set { if (blend_mode != value) SetValue (BlendModeProperty, ref blend_mode, value); }
		}				
		
		public void Clear ()
		{
			Surface.Clear ();
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

		public void Draw(Context ctx)
		{
			Draw(ctx, Surface, Opacity);
		}

		public void Draw (Context ctx, ImageSurface surface, double opacity, bool transform = true)
		{
			ctx.Save ();

			if (transform)
				ctx.Transform (Transform);

			ctx.BlendSurface (surface, BlendMode, opacity);

			ctx.Restore ();
		}

		public void DrawWithOperator (Context ctx, ImageSurface surface, Operator op, double opacity = 1.0, bool transform = true)
		{
			ctx.Save ();

			if (transform)
				ctx.Transform (Transform);
			ctx.Operator = op;
			ctx.SetSourceSurface (surface, 0, 0);
			if (opacity >= 1.0)
				ctx.Paint ();
			else 
				ctx.PaintWithAlpha (opacity);
			ctx.Restore ();
		}
		
		public virtual void ApplyTransform (Matrix xform, Size new_size)
		{
			var old_size = PintaCore.Workspace.ImageSize;
			var dest = new ImageSurface (Format.ARGB32, new_size.Width, new_size.Height);
			using (var g = new Context (dest))
			{
				g.Transform (xform);
				g.SetSource (Surface);
				g.Paint ();
			}
			
			Surface old = Surface;
			Surface = dest;
			old.Dispose ();
		}

		public static Gdk.Size RotateDimensions (Gdk.Size originalSize, double angle)
		{
			double radians = (angle / 180d) * Math.PI;
			double cos = Math.Abs (Math.Cos (radians));
			double sin = Math.Abs (Math.Sin (radians));
			int w = originalSize.Width;
			int h = originalSize.Height;

			return new Gdk.Size ((int)(w * cos + h * sin), (int)(w * sin + h * cos));
		}
		
		public unsafe void HueSaturation (int hueDelta, int satDelta, int lightness)
		{
			ImageSurface dest = Surface.Clone ();
			ColorBgra* dstPtr = (ColorBgra*)dest.DataPtr;
			
			int len = Surface.Data.Length / 4;
			
			// map the range [0,100] -> [0,100] and the range [101,200] -> [103,400]
			if (satDelta > 100) 
				satDelta = ((satDelta - 100) * 3) + 100;
			
			UnaryPixelOp op;
			
			if (hueDelta == 0 && satDelta == 100 && lightness == 0)
				op = new UnaryPixelOps.Identity ();
			else
				op = new UnaryPixelOps.HueSaturationLightness (hueDelta, satDelta, lightness);
			
			op.Apply (dstPtr, len);
			
			using (Context g = new Context (Surface)) {
				g.AppendPath (PintaCore.Workspace.ActiveDocument.Selection.SelectionPath);
				g.FillRule = Cairo.FillRule.EvenOdd;
				g.Clip ();

				g.SetSource (dest);
				g.Paint ();
			}

			(dest as IDisposable).Dispose ();
		}

		public virtual void Resize (int width, int height)
		{
			ImageSurface dest = new ImageSurface (Format.Argb32, width, height);
			Pixbuf pb = Surface.ToPixbuf();
			Pixbuf pbScaled = pb.ScaleSimple (width, height, InterpType.Bilinear);

			using (Context g = new Context (dest)) {
				CairoHelper.SetSourcePixbuf (g, pbScaled, 0, 0);
				g.Paint ();
			}

			(Surface as IDisposable).Dispose ();
			(pb as IDisposable).Dispose ();
			(pbScaled as IDisposable).Dispose ();
			Surface = dest;
		}

		public virtual void ResizeCanvas (int width, int height, Anchor anchor)
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

		public virtual void Crop (Gdk.Rectangle rect, Path selection)
		{
			ImageSurface dest = new ImageSurface (Format.Argb32, rect.Width, rect.Height);

			using (Context g = new Context (dest)) {
				// Move the selected content to the upper left
				g.Translate (-rect.X, -rect.Y);
				g.Antialias = Antialias.None;

				// Optionally, respect the given path.
				if (selection != null)
				{
					g.AppendPath (selection);
					g.FillRule = Cairo.FillRule.EvenOdd;
					g.Clip ();
				}

				g.SetSource (Surface);
				g.Paint ();
			}

			(Surface as IDisposable).Dispose ();
			Surface = dest;
		}
	}
}
