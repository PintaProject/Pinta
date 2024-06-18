/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

// Copyright (c) 2007,2008 Ed Harvey 
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
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class VignetteEffect : BaseEffect
{
	// TODO: Icon
	public override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Vignette");

	public override string EffectMenuCategory
		=> Translations.GetString ("Photo");

	public override bool IsConfigurable
		=> true;

	public VignetteData Data
		=> (VignetteData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	public VignetteEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new VignetteData ();
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	// Algorithm code ported from PDN
	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		Size canvasSize = src.GetSize ();
		double effectiveRadius = Math.Max (canvasSize.Width, canvasSize.Height) * 0.5d;
		effectiveRadius *= Data.Radius;
		effectiveRadius *= effectiveRadius;
		double radiusR = Math.PI / (8 * effectiveRadius);
		double amount = Data.Amount;
		double amount1 = 1d - amount;
		PointI centerOffset = Data.CenterOffset;
		ReadOnlySpan<ColorBgra> src_data = src.GetPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		foreach (RectangleI roi in rois) {
			foreach (var pixel in Utility.GeneratePixelOffsets (roi, canvasSize)) {
				double iy2 = pixel.coordinates.Y - centerOffset.Y;
				iy2 *= iy2;
				double ix = pixel.coordinates.X - centerOffset.X;
				double d = (iy2 + (ix * ix)) * radiusR;
				double factor = Math.Cos (d);
				ColorBgra src_color = src_data[pixel.memoryOffset];
				if (factor <= 0 || d > Math.PI) {
					dst_data[pixel.memoryOffset] = ColorBgra.FromBgra (
						r: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (src_color.R) * amount1))),
						g: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (src_color.G) * amount1))),
						b: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (src_color.B) * amount1))),
						a: src_color.A);
				} else {
					factor *= factor;
					factor *= factor;
					factor = amount1 + (amount * factor);
					dst_data[pixel.memoryOffset] = ColorBgra.FromBgra (
						r: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (src_color.R) * factor))),
						g: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (src_color.G) * factor))),
						b: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (src_color.B) * factor))),
						a: src_color.A);
				}
			}
		}
	}
}

public sealed class VignetteData : EffectData
{
	[Caption ("Center Offset")]
	public PointI CenterOffset { get; set; }

	[MinimumValue (0), MaximumValue (4)] // TODO: In Paint.NET, the minimum is 0.1. Make it possible to constrain fractionally
	[Caption ("Radius")]
	public double Radius { get; set; } = 0.5;

	[MinimumValue (0), MaximumValue (1)]
	[Caption ("Strength")]
	public double Amount { get; set; } = 1;
}
