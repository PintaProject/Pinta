/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class InkSketchEffect : BaseEffect
{
	private static readonly ImmutableArray<ImmutableArray<int>> conv;
	private const int Size = 5;
	private const int Radius = (Size - 1) / 2;

	private readonly GlowEffect glow_effect;
	private readonly UnaryPixelOps.Desaturate desaturate_op;
	private readonly UserBlendOps.DarkenBlendOp darken_op;

	public override string Icon => Pinta.Resources.Icons.EffectsArtisticInkSketch;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Ink Sketch");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Artistic");

	public InkSketchData Data => (InkSketchData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public InkSketchEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();

		EffectData = new InkSketchData ();

		glow_effect = new GlowEffect (services);
		desaturate_op = new UnaryPixelOps.Desaturate ();
		darken_op = new UserBlendOps.DarkenBlendOp ();
	}

	static InkSketchEffect ()
	{
		conv = [
			[-1, -1, -1, -1, -1],
			[-1, -1, -1, -1, -1],
			[-1, -1, 30, -1, -1],
			[-1, -1, -1, -1, -1],
			[-1, -1, -5, -1, -1],
		];
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		// Glow background 
		glow_effect.Data.Radius = 6;
		glow_effect.Data.Brightness = -(Data.Coloring - 50) * 2;
		glow_effect.Data.Contrast = -(Data.Coloring - 50) * 2;

		glow_effect.Render (src, dest, rois);

		var src_data = src.GetReadOnlyPixelData ();
		int width = src.Width;
		var dst_data = dest.GetPixelData ();

		// Create black outlines by finding the edges of objects 
		foreach (RectangleI roi in rois) {
			for (int y = roi.Top; y <= roi.Bottom; ++y) {

				int top = Math.Max (y - Radius, 0);
				int bottom = Math.Min (y + Radius + 1, dest.Height);

				var dst_row = dst_data.Slice (y * width, width);

				for (int x = roi.Left; x <= roi.Right; ++x) {

					int left = Math.Max (x - Radius, 0);
					int right = Math.Min (x + Radius + 1, dest.Width);

					RectangleI adjustedBounds = RectangleI.FromLTRB (left, top, right, bottom);
					ColorBgra baseRGB = CreateBaseRGBA (src_data, width, x, y, adjustedBounds);
					ColorBgra topLayer = CreateTopLayer (baseRGB);

					// Change Blend Mode to Darken
					ColorBgra originalPixel = dst_row[x];
					dst_row[x] = darken_op.Apply (topLayer, originalPixel);
				}
			}
		}
	}

	private static ColorBgra CreateBaseRGBA (ReadOnlySpan<ColorBgra> src_data, int width, int x, int y, RectangleI adjustedBounds)
	{
		int r = 0;
		int g = 0;
		int b = 0;
		int a = 0;

		for (int v = adjustedBounds.Top; v < adjustedBounds.Bottom; v++) {

			var src_row = src_data.Slice (v * width, width);
			int j = v - y + Radius;

			for (int u = adjustedBounds.Left; u < adjustedBounds.Right; u++) {
				int i1 = u - x + Radius;
				int w = conv[j][i1];

				ColorBgra src_pixel = src_row[u];

				r += src_pixel.R * w;
				g += src_pixel.G * w;
				b += src_pixel.B * w;
				a += src_pixel.A * w;
			}
		}

		return ColorBgra.FromBgra (
			b: Utility.ClampToByte (b),
			g: Utility.ClampToByte (g),
			r: Utility.ClampToByte (r),
			a: Utility.ClampToByte (a)
		);
	}

	private ColorBgra CreateTopLayer (ColorBgra baseRGB)
	{
		// Desaturate 
		ColorBgra topLayer = desaturate_op.Apply (baseRGB);

		// Adjust Brightness and Contrast
		return
			(topLayer.ToStraightAlpha ().R > (Data.InkOutline * 255 / 100))
			? ColorBgra.FromBgra (topLayer.A, topLayer.A, topLayer.A, topLayer.A)
			: ColorBgra.FromBgra (0, 0, 0, topLayer.A);
	}
	#endregion

	public sealed class InkSketchData : EffectData
	{
		[Caption ("Ink Outline"), MinimumValue (0), MaximumValue (99)]
		public int InkOutline { get; set; } = 50;

		[Caption ("Coloring"), MinimumValue (0), MaximumValue (100)]
		public int Coloring { get; set; } = 50;
	}
}
