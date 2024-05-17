/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Hanh Pham <hanh.pham@gmx.com>                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class UnfocusEffect : LocalHistogramEffect
{
	private int radius;

	public override string Icon
		=> Pinta.Resources.Icons.EffectsBlursUnfocus;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Unfocus");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Blurs");

	public UnfocusData Data
		=> (UnfocusData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public UnfocusEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();

		EffectData = new UnfocusData ();
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN
	public override void Render (
		DBNull preRender,
		ImageSurface src,
		ImageSurface dest,
		ReadOnlySpan<RectangleI> rois)
	{
		radius = Data.Radius;
		foreach (var rect in rois)
			RenderRectWithAlpha (radius, src, dest, rect);
	}

	public override ColorBgra ApplyWithAlpha (in ColorBgra src, int area, int sum, Span<int> hb, Span<int> hg, Span<int> hr)
	{
		//each slot of the histogram can contain up to area * 255. This will overflow an int when area > 32k
		if (area < 32768) {
			int b = 0;
			int g = 0;
			int r = 0;

			for (int i = 1; i < 256; ++i) {
				b += i * hb[i];
				g += i * hg[i];
				r += i * hr[i];
			}

			int alpha = sum / area;
			int div = area * 255;

			return ColorBgra.FromBgraClamped (b / div, g / div, r / div, alpha);
		} else {
			//use a long if an int will overflow.
			long b = 0;
			long g = 0;
			long r = 0;

			for (long i = 1; i < 256; ++i) {
				b += i * hb[(int) i];
				g += i * hg[(int) i];
				r += i * hr[(int) i];
			}

			int alpha = sum / area;
			int div = area * 255;

			return ColorBgra.FromBgraClamped (b / div, g / div, r / div, alpha);
		}
	}
	#endregion

	public sealed class UnfocusData : EffectData
	{
		[Caption ("Radius"), MinimumValue (1), MaximumValue (200)]
		public int Radius { get; set; } = 4;

		[Skip]
		public override bool IsDefault => Radius == 0;
	}
}
