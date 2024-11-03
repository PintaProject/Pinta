/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Lehonti Ramos                                           //
/////////////////////////////////////////////////////////////////////////////////

// Copyright (C) 2006-2008 Ed Harvey
//
// MIT License: http://www.opensource.org/licenses/mit-license.php
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
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class DentsEffect : BaseEffect
{
	public sealed override string Icon
		=> Resources.Icons.EffectsDistortDents;

	public sealed override bool IsTileable
		=> true;

	public sealed override string Name
		// Translators: This refers to an image distortion that creates small, random warps or distortions in the image, like tiny dents, bumps, or waves
		=> Translations.GetString ("Dents");

	public sealed override string EffectMenuCategory
		=> Translations.GetString ("Distort");

	public DentsData Data
		=> (DentsData) EffectData!; // NRT - Set in constructor

	public sealed override bool IsConfigurable
		=> true;

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	private readonly IChromeService chrome;
	private readonly LivePreviewManager live_preview;
	private readonly IPaletteService palette;
	public DentsEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		live_preview = services.GetService<LivePreviewManager> ();
		palette = services.GetService<IPaletteService> ();
		EffectData = new DentsData ();
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		Warp.Settings settings = Warp.CreateSettings (
			Data,
			live_preview.RenderBounds,
			palette);

		Span<ColorBgra> dst_data = dst.GetPixelData ();
		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		foreach (RectangleI rect in rois) {
			foreach (var pixel in Utility.GeneratePixelOffsets (rect, src.GetSize ())) {
				dst_data[pixel.memoryOffset] = Warp.GetPixelColor (
					settings,
					InverseTransform,
					src,
					src_data[pixel.memoryOffset],
					pixel);
			}
		}
	}

	// Algorithm code ported from PDN
	public Warp.TransformData InverseTransform (
		Warp.Settings settings,
		Warp.TransformData data)
	{
		double scale = Data.Scale;

		double refraction = Data.Refraction;
		double detail1 = Data.Roughness;
		double detail2 = detail1;
		double roughness = detail2;

		double turbulence = Data.Tension;

		byte seed = Utility.ClampToByte (Data.Seed.Value);

		double scaleR = 400.0 / settings.defaultRadius / scale;
		double refractionScale = refraction / 100.0 / scaleR;
		double theta = Math.PI * 2.0 * turbulence / 10.0;
		double effectiveRoughness = roughness / 100.0;

		double detail3 = 1.0 + (detail2 / 10.0);

		// We don't want the perlin noise frequency components exceeding the nyquist limit, so we will limit 'detail' appropriately
		double maxDetail = Math.Floor (Math.Log (scaleR) / Math.Log (0.5));

		double effectiveDetail = (detail3 > maxDetail && maxDetail >= 1.0) ? maxDetail : detail3;

		double x = data.X;
		double y = data.Y;

		double ix = x * scaleR;
		double iy = y * scaleR;

		double bumpAngle = theta * PerlinNoise.Compute (ix, iy, effectiveDetail, effectiveRoughness, seed);

		return new (
			X: data.X + (refractionScale * Math.Sin (-bumpAngle)),
			Y: data.Y + (refractionScale * Math.Cos (bumpAngle)));
	}
}

public sealed class DentsData : EffectData, IWarpData
{
	[MinimumValue (1), MaximumValue (200)]
	public double Scale { get; set; } = 25;

	[MinimumValue (0), MaximumValue (200)]
	public double Refraction { get; set; } = 50;

	[MinimumValue (0), MaximumValue (100)]
	public double Roughness { get; set; } = 10;

	[Caption ("Turbulence")]
	[MinimumValue (0), MaximumValue (100)]
	public double Tension { get; set; } = 10;

	[MinimumValue (0), MaximumValue (255)]
	public RandomSeed Seed { get; set; } = new (0);

	[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
	public int Quality { get; set; } = 2;

	[Caption ("Center Offset")]
	public PointD CenterOffset { get; set; }

	public WarpEdgeBehavior EdgeBehavior { get; set; } = WarpEdgeBehavior.Wrap;
}
