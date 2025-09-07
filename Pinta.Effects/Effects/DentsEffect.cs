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
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private readonly IChromeService chrome;
	private readonly ILivePreview live_preview;
	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;
	public DentsEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		live_preview = services.GetService<ILivePreview> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new DentsData ();
	}

	private readonly record struct DentsSettings (
		double EffectiveDetail,
		double NormalizedRoughness,
		double RefractionScale,
		double ScaleR,
		byte Seed,
		double Theta);

	private DentsSettings CreateSettings (Warp.Settings warpSettings, ImageSurface source)
	{
		DentsData data = Data;
		double scaleR = 400.0 / warpSettings.defaultRadius / data.Scale;
		double roughness = data.Roughness;
		double detail = 1.0 + (roughness / 10.0);
		double maxDetail = Math.Floor (Math.Log (scaleR) / Math.Log (0.5)); // We don't want the perlin noise frequency components exceeding the nyquist limit, so we will limit 'detail' appropriately
		return new (
			EffectiveDetail: (detail > maxDetail && maxDetail >= 1.0) ? maxDetail : detail,
			NormalizedRoughness: roughness / 100.0,
			RefractionScale: data.Refraction / 100.0 / scaleR,
			ScaleR: scaleR,
			Seed: Utility.ClampToByte (data.Seed.Value),
			Theta: RadiansAngle.FullTurn * data.Tension / 10.0);
	}

	// Algorithm code ported from PDN

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		Warp.Settings warpSettings = Warp.CreateSettings (
			Data,
			live_preview.RenderBounds,
			palette);

		DentsSettings dentsSettings = CreateSettings (warpSettings, source);

		Span<ColorBgra> destinationData = destination.GetPixelData ();
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, source.GetSize ()))
			destinationData[pixel.memoryOffset] = Warp.GetPixelColor (
				warpSettings,
				InverseTransform,
				source,
				sourceData[pixel.memoryOffset],
				pixel);

		PointD InverseTransform (Warp.Settings settings, PointD data)
		{
			PointD scaled = data.Scaled (dentsSettings.ScaleR);
			double noise = PerlinNoise.Compute (scaled, dentsSettings.EffectiveDetail, dentsSettings.NormalizedRoughness, dentsSettings.Seed);
			RadiansAngle bumpAngle = new (dentsSettings.Theta * noise);
			return new (
				X: data.X + (dentsSettings.RefractionScale * Math.Sin (-bumpAngle.Radians)),
				Y: data.Y + (dentsSettings.RefractionScale * Math.Cos (bumpAngle.Radians)));
		}
	}
}

public sealed class DentsData : EffectData, Warp.IEffectData
{
	[Caption ("Scale")]
	[MinimumValue (1), MaximumValue (200), IncrementValue (1)]
	public double Scale { get; set; } = 25;

	[Caption ("Refraction")]
	[MinimumValue (0), MaximumValue (200), IncrementValue (1)]
	public double Refraction { get; set; } = 50;

	[Caption ("Roughness")]
	[MinimumValue (0), MaximumValue (100), IncrementValue (1)]
	public double Roughness { get; set; } = 10;

	[Caption ("Turbulence")]
	[MinimumValue (0), MaximumValue (100), IncrementValue (1)]
	public double Tension { get; set; } = 10;

	[Caption ("Random Noise Seed")]
	[MinimumValue (0), MaximumValue (255)]
	public RandomSeed Seed { get; set; } = new (0);

	[Caption ("Quality")]
	[MinimumValue (1), MaximumValue (5)]
	public int Quality { get; set; } = 2;

	[Caption ("Center Offset")]
	public CenterOffset<double> CenterOffset { get; set; }

	[Caption ("Edge Behavior")]
	public EdgeBehavior EdgeBehavior { get; set; } = EdgeBehavior.Wrap;
}
