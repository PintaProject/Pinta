/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzystzof Marecki                                       //
/////////////////////////////////////////////////////////////////////////////////

// This effect was graciously provided by David Issel, aka BoltBait. His original
// copyright and license (MIT License) are reproduced below.

/*
PortraitEffect.cs 
Copyright (c) 2007 David Issel 
Contact Info: BoltBait@hotmail.com http://www.BoltBait.com 

Permission is hereby granted, free of charge, to any person obtaining a copy 
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights 
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
copies of the Software, and to permit persons to whom the Software is 
furnished to do so, subject to the following conditions: 

The above copyright notice and this permission notice shall be included in 
all copies or substantial portions of the Software. 

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
THE SOFTWARE. 
*/
using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class SoftenPortraitEffect : BaseEffect
{
	private readonly GaussianBlurEffect blur_effect;
	private readonly UnaryPixelOps.Desaturate desaturate_op;
	private readonly UserBlendOps.OverlayBlendOp overlay_op;

	public override string Icon => Resources.Icons.EffectsPhotoSoftenPortrait;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Soften Portrait");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Photo");

	public SoftenPortraitData Data => (SoftenPortraitData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public SoftenPortraitEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();

		EffectData = new SoftenPortraitData ();

		blur_effect = new GaussianBlurEffect (services);
		desaturate_op = new UnaryPixelOps.Desaturate ();
		overlay_op = new UserBlendOps.OverlayBlendOp ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private sealed record SoftenPortraitSettings (
		BrightnessContrast.PreRender BrightnessContrast,
		float RedAdjust,
		float BlueAdjust,
		int BlurRadius);

	private SoftenPortraitSettings CreateSettings ()
	{
		SoftenPortraitData data = Data;
		int warmth = data.Warmth;
		int brightness = data.Lighting;
		int contrast = -data.Lighting / 2;
		return new (
			RedAdjust: 1.0f + (warmth / 100.0f),
			BlueAdjust: 1.0f - (warmth / 100.0f),
			BlurRadius: 3 * data.Softness,
			BrightnessContrast: new (brightness, contrast));
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		SoftenPortraitSettings settings = CreateSettings ();

		blur_effect.Data.Radius = settings.BlurRadius;
		blur_effect.Render (source, destination, [roi]);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, source.GetSize ())) {
			ColorBgra blurred = destinationData[pixel.memoryOffset];
			ColorBgra blurredAdjusted = settings.BrightnessContrast.GetPixelColor (blurred);
			ColorBgra grayOriginal = desaturate_op.Apply (sourceData[pixel.memoryOffset]);
			ColorBgra effective = ColorBgra.FromBgra (
				b: Utility.ClampToByte ((int) (grayOriginal.B * settings.BlueAdjust)),
				g: grayOriginal.G,
				r: Utility.ClampToByte ((int) (grayOriginal.R * settings.RedAdjust)),
				a: grayOriginal.A);
			destinationData[pixel.memoryOffset] = overlay_op.Apply (effective, blurredAdjusted);
		}
	}
}

public sealed class SoftenPortraitData : EffectData
{
	[Caption ("Softness")]
	[MinimumValue (0), MaximumValue (10)]
	public int Softness { get; set; } = 5;

	[Caption ("Lighting")]
	[MinimumValue (-20), MaximumValue (20)]
	public int Lighting { get; set; } = 0;

	[Caption ("Warmth")]
	[MinimumValue (0), MaximumValue (20)]
	public int Warmth { get; set; } = 10;
}
