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

	public Layer (ImageSurface surface)
		: this (surface, false, 1f, "")
	{ }

	public Layer (
		ImageSurface surface,
		bool hidden,
		double opacity,
		string name)
	{
		Surface = surface;

		this.hidden = hidden;
		this.opacity = opacity;
		this.name = name;
		blend_mode = BlendMode.Normal;

		Transform = CairoExtensions.CreateIdentityMatrix ();
	}

	public ImageSurface Surface { get; set; }
	public Matrix Transform { get; set; }

	public static string OpacityProperty { get; } = nameof (Opacity);
	public static string HiddenProperty { get; } = nameof (Hidden);
	public static string NameProperty { get; } = nameof (Name);
	public static string BlendModeProperty { get; } = nameof (BlendMode);

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
		ImageSurface dest = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			Surface.Width,
			Surface.Height);

		Context g = new (dest);

		g.SetMatrix (CairoExtensions.CreateMatrix (-1, 0, 0, 1, Surface.Width, 0));
		g.SetSourceSurface (Surface, 0, 0);

		g.Paint ();

		Surface = dest;
	}

	public void FlipVertical ()
	{
		ImageSurface dest = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			Surface.Width,
			Surface.Height);

		Context g = new (dest);

		g.SetMatrix (CairoExtensions.CreateMatrix (1, 0, 0, -1, 0, Surface.Height));
		g.SetSourceSurface (Surface, 0, 0);

		g.Paint ();

		Surface = dest;
	}

	public void Draw (Context ctx)
	{
		Draw (ctx, Surface, Opacity);
	}

	public void Draw (
		Context ctx,
		ImageSurface surface,
		double opacity,
		bool transform = true)
	{
		ctx.Save ();

		if (transform)
			ctx.Transform (Transform);

		ctx.BlendSurface (surface, BlendMode, opacity);

		ctx.Restore ();
	}

	public void DrawWithOperator (Context ctx, Operator op, double opacity = 1.0, bool transform = true)
	{
		DrawWithOperator (ctx, Surface, op, opacity, transform);
	}

	public void DrawWithOperator (
		Context ctx,
		ImageSurface surface,
		Operator op,
		double opacity = 1.0,
		bool transform = true)
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

	public virtual void ApplyTransform (
		Matrix xform,
		Size old_size,
		Size new_size)
	{
		ImageSurface dest = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			new_size.Width,
			new_size.Height);

		Context g = new (dest);

		g.Transform (xform);
		g.SetSourceSurface (Surface, 0, 0);

		g.Paint ();

		Surface = dest;
	}

	public static Size RotateDimensions (Size originalSize, DegreesAngle angle)
	{
		RadiansAngle radians = angle.ToRadians ();

		double cos = Math.Abs (Math.Cos (radians.Radians));
		double sin = Math.Abs (Math.Sin (radians.Radians));

		int w = originalSize.Width;
		int h = originalSize.Height;

		return new (
			(int) (w * cos + h * sin),
			(int) (w * sin + h * cos));
	}

	public virtual void Resize (Size newSize, ResamplingMode resamplingMode)
	{
		ImageSurface dest = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			newSize.Width,
			newSize.Height);

		Context g = new (dest);

		g.Scale (newSize.Width / (double) Surface.Width, newSize.Height / (double) Surface.Height);
		g.SetSourceSurface (Surface, resamplingMode);

		g.Paint ();

		Surface = dest;
	}

	public virtual void ResizeCanvas (Size newSize, Anchor anchor)
	{
		ImageSurface dest = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			newSize.Width,
			newSize.Height);

		PointI delta = new (
			X: Surface.Width - newSize.Width,
			Y: Surface.Height - newSize.Height);

		PointD anchorPoint = GetAnchorPoint (delta, anchor);

		Context g = new (dest);

		g.SetSourceSurface (Surface, anchorPoint.X, anchorPoint.Y);
		g.Paint ();

		Surface = dest;
	}

	private static PointD GetAnchorPoint (PointI delta, Anchor anchor)
		=> anchor switch {
			Anchor.NW => new (0, 0),
			Anchor.N => new (-delta.X / 2, 0),
			Anchor.NE => new (-delta.X, 0),
			Anchor.E => new (-delta.X, -delta.Y / 2),
			Anchor.SE => new (-delta.X, -delta.Y),
			Anchor.S => new (-delta.X / 2, -delta.Y),
			Anchor.SW => new (0, -delta.Y),
			Anchor.W => new (0, -delta.Y / 2),
			Anchor.Center => new (-delta.X / 2, -delta.Y / 2),
			_ => throw new InvalidEnumArgumentException (nameof (anchor), (int) anchor, typeof (Anchor)),
		};

	public virtual void Crop (RectangleI rect, Path? selection)
	{
		ImageSurface dest = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			rect.Width,
			rect.Height);

		Context g = new (dest);

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
