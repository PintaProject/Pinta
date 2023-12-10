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
using Cairo;

namespace Pinta.Core;

public class Layer : ObservableObject
{
	private double opacity;
	private bool hidden;
	private string name;
	private BlendMode blend_mode;

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

		Transform = CairoExtensions.CreateIdentityMatrix ();
	}

	public ImageSurface Surface { get; set; }
	public bool Tiled { get; internal set; }
	public Matrix Transform { get; set; }

	public static readonly string OpacityProperty = "Opacity";
	public static readonly string HiddenProperty = "Hidden";
	public static readonly string NameProperty = "Name";
	public static readonly string BlendModeProperty = "BlendMode";

	public double Opacity {
		get => opacity;
		set { if (opacity != value) SetValue (OpacityProperty, ref opacity, value); }
	}

	public bool Hidden {
		get => hidden;
		set { if (hidden != value) SetValue (HiddenProperty, ref hidden, value); }
	}

	public string Name {
		get => name;
		set { if (name != value) SetValue (NameProperty, ref name, value); }
	}

	public BlendMode BlendMode {
		get => blend_mode;
		set { if (blend_mode != value) SetValue (BlendModeProperty, ref blend_mode, value); }
	}

	public void Clear ()
	{
		Surface.Clear ();
	}

	public void FlipHorizontal ()
	{
		var dest = CairoExtensions.CreateImageSurface (Format.Argb32, Surface.Width, Surface.Height);

		var g = new Cairo.Context (dest);
		g.SetMatrix (CairoExtensions.CreateMatrix (-1, 0, 0, 1, Surface.Width, 0));
		g.SetSourceSurface (Surface, 0, 0);

		g.Paint ();

		Surface = dest;
	}

	public void FlipVertical ()
	{
		var dest = CairoExtensions.CreateImageSurface (Format.Argb32, Surface.Width, Surface.Height);

		var g = new Cairo.Context (dest);
		g.SetMatrix (CairoExtensions.CreateMatrix (1, 0, 0, -1, 0, Surface.Height));
		g.SetSourceSurface (Surface, 0, 0);

		g.Paint ();

		Surface = dest;
	}

	public void Draw (Context ctx)
	{
		Draw (ctx, Surface, Opacity);
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

	public virtual void ApplyTransform (Matrix xform, Size old_size, Size new_size)
	{
		var dest = CairoExtensions.CreateImageSurface (Format.Argb32, new_size.Width, new_size.Height);

		var g = new Context (dest);
		g.Transform (xform);
		g.SetSourceSurface (Surface, 0, 0);
		g.Paint ();

		Surface = dest;
	}

	public static Size RotateDimensions (Size originalSize, double angle)
	{
		double radians = (angle / 180d) * Math.PI;
		double cos = Math.Abs (Math.Cos (radians));
		double sin = Math.Abs (Math.Sin (radians));
		int w = originalSize.Width;
		int h = originalSize.Height;

		return new Size ((int) (w * cos + h * sin), (int) (w * sin + h * cos));
	}

	public virtual void Resize (int width, int height, ResamplingMode resamplingMode)
	{
		ImageSurface dest = CairoExtensions.CreateImageSurface (Format.Argb32, width, height);

		var g = new Context (dest);
		g.Scale ((double) width / (double) Surface.Width, (double) height / (double) Surface.Height);
		g.SetSourceSurface (Surface, 0, 0, resamplingMode);
		g.Paint ();

		Surface = dest;
	}

	public virtual void ResizeCanvas (int width, int height, Anchor anchor)
	{
		ImageSurface dest = CairoExtensions.CreateImageSurface (Format.Argb32, width, height);

		int delta_x = Surface.Width - width;
		int delta_y = Surface.Height - height;
		var anchorPoint = GetAnchorPoint (delta_x, delta_y, anchor);

		var g = new Context (dest);

		g.SetSourceSurface (Surface, anchorPoint.X, anchorPoint.Y);
		g.Paint ();

		Surface = dest;
	}

	private static PointD GetAnchorPoint (int delta_x, int delta_y, Anchor anchor)
	{
		return anchor switch {
			Anchor.NW => new (0, 0),
			Anchor.N => new (-delta_x / 2, 0),
			Anchor.NE => new (-delta_x, 0),
			Anchor.E => new (-delta_x, -delta_y / 2),
			Anchor.SE => new (-delta_x, -delta_y),
			Anchor.S => new (-delta_x / 2, -delta_y),
			Anchor.SW => new (0, -delta_y),
			Anchor.W => new (0, -delta_y / 2),
			Anchor.Center => new (-delta_x / 2, -delta_y / 2),
			_ => throw new InvalidEnumArgumentException (nameof (anchor), (int) anchor, typeof (Anchor)),
		};
	}

	public virtual void Crop (RectangleI rect, Path? selection)
	{
		ImageSurface dest = CairoExtensions.CreateImageSurface (Format.Argb32, rect.Width, rect.Height);

		var g = new Context (dest);
		// Move the selected content to the upper left
		g.Translate (-rect.X, -rect.Y);
		g.Antialias = Antialias.None;

		// Optionally, respect the given path.
		if (selection != null) {
			g.AppendPath (selection);
			g.FillRule = Cairo.FillRule.EvenOdd;
			g.Clip ();
		}

		g.SetSourceSurface (Surface, 0, 0);
		g.Paint ();

		Surface = dest;
	}
}
