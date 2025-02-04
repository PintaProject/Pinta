/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Lehonti Ramos                                           //
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
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class VignetteEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsPhotoVignette;

	public override bool IsTileable
		=> true;

	public override string Name
		// Translators: The vignette effect darkens the outer edges of an image, which fade into an unchanged circular area in the center (or at some other point chosen by the user), similar to what is seen during the closing scene in old cartoons
		=> Translations.GetString ("Vignette");

	public override string EffectMenuCategory
		=> Translations.GetString ("Photo");

	public override bool IsConfigurable
		=> true;

	public VignetteData Data
		=> (VignetteData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public VignetteEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new VignetteData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private sealed record VignetteSettings (
		Size canvasSize,
		double radiusR,
		double amount,
		double amount1,
		PointI centerOffset);

	private VignetteSettings CreateSettings (ImageSurface src)
	{
		Size canvasSize = src.GetSize ();
		double r1 = Math.Max (canvasSize.Width, canvasSize.Height) * 0.5d;
		double r2 = r1 * Convert.ToDouble (Data.RadiusPercentage) / 100d;
		double amount = Data.Amount;
		return new (
			canvasSize: canvasSize,
			radiusR: Math.PI / (8 * (r2 * r2)),
			amount: amount,
			amount1: 1d - amount,
			centerOffset: Data.Offset);
	}

	// Algorithm code ported from PDN
	protected override void Render (
		ImageSurface source,
		ImageSurface destination,
		RectangleI roi)
	{
		VignetteSettings settings = CreateSettings (source);
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.canvasSize))
			destinationData[pixel.memoryOffset] = GetFinalPixelColor (settings, sourceData, pixel);
	}

	private static ColorBgra GetFinalPixelColor (
		VignetteSettings settings,
		ReadOnlySpan<ColorBgra> sourceData,
		PixelOffset pixel)
	{
		double iy = pixel.coordinates.Y - settings.centerOffset.Y;
		double iy2 = iy * iy;
		double ix = pixel.coordinates.X - settings.centerOffset.X;
		double d = (iy2 + (ix * ix)) * settings.radiusR;
		double factor = Math.Cos (d);
		ColorBgra originalColor = sourceData[pixel.memoryOffset];
		if (factor <= 0 || d > Math.PI) {
			return ColorBgra.FromBgra (
				r: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (originalColor.R) * settings.amount1))),
				g: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (originalColor.G) * settings.amount1))),
				b: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (originalColor.B) * settings.amount1))),
				a: originalColor.A);
		} else {
			double factor2 = factor * factor;
			double factor4 = factor2 * factor2;
			double effectiveFactor = settings.amount1 + (settings.amount * factor4);
			return ColorBgra.FromBgra (
				r: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (originalColor.R) * effectiveFactor))),
				g: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (originalColor.G) * effectiveFactor))),
				b: (byte) (0.5 + (255 * SrgbUtility.ToSrgbClamped (SrgbUtility.ToLinear (originalColor.B) * effectiveFactor))),
				a: originalColor.A);
		}
	}
}

public sealed class VignetteData : EffectData
{
	[Caption ("Offset")]
	public PointI Offset { get; set; }

	// Translators: This refers to how big the radius is as a percentage of the image's dimensions
	[Caption ("Radius Percentage")]
	[MinimumValue (10), MaximumValue (400)]
	public int RadiusPercentage { get; set; } = 50;

	[MinimumValue (0), MaximumValue (1)]
	[Caption ("Strength")]
	public double Amount { get; set; } = 1;
}
