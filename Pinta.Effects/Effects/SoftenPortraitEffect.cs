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
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class SoftenPortraitEffect : BaseEffect
{
	private readonly GaussianBlurEffect blur_effect;
	private readonly BrightnessContrastEffect bac_adjustment;
	private readonly UnaryPixelOps.Desaturate desaturate_op;
	private readonly UserBlendOps.OverlayBlendOp overlay_op;

	public override string Icon => Pinta.Resources.Icons.EffectsPhotoSoftenPortrait;

	public override string Name => Translations.GetString ("Soften Portrait");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Photo");

	public SoftenPortraitData Data => (SoftenPortraitData) EffectData!;  // NRT - Set in constructor

	public SoftenPortraitEffect ()
	{
		EffectData = new SoftenPortraitData ();

		blur_effect = new GaussianBlurEffect ();
		bac_adjustment = new BrightnessContrastEffect ();
		desaturate_op = new UnaryPixelOps.Desaturate ();
		overlay_op = new UserBlendOps.OverlayBlendOp ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	public override void Render (ImageSurface src, ImageSurface dest, Core.RectangleI[] rois)
	{
		int warmth = Data.Warmth;
		float redAdjust = 1.0f + (warmth / 100.0f);
		float blueAdjust = 1.0f - (warmth / 100.0f);

		this.blur_effect.Render (src, dest, rois);
		this.bac_adjustment.Render (src, dest, rois);

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dest.GetPixelData ();
		int width = dest.Width;

		foreach (var roi in rois) {
			for (int y = roi.Top; y <= roi.Bottom; ++y) {
				var src_row = src_data.Slice (y * width, width);
				var dst_row = dst_data.Slice (y * width, width);

				for (int x = roi.Left; x <= roi.Right; ++x) {
					ColorBgra srcGrey = this.desaturate_op.Apply (src_row[x]);

					srcGrey.R = Utility.ClampToByte ((int) ((float) srcGrey.R * redAdjust));
					srcGrey.B = Utility.ClampToByte ((int) ((float) srcGrey.B * blueAdjust));

					dst_row[x] = this.overlay_op.Apply (srcGrey, dst_row[x]);
				}
			}
		}
	}
}

public sealed class SoftenPortraitData : EffectData
{
	[Caption ("Softness"), MinimumValue (0), MaximumValue (10)]
	public int Softness = 5;

	[Caption ("Lighting"), MinimumValue (-20), MaximumValue (20)]
	public int Lighting = 0;

	[Caption ("Warmth"), MinimumValue (0), MaximumValue (20)]
	public int Warmth = 10;
}
