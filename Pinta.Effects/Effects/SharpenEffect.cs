/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class SharpenEffect : LocalHistogramEffect
{
	public override string Icon
		=> Pinta.Resources.Icons.EffectsPhotoSharpen;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Sharpen");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Photo");

	public SharpenData Data
		=> (SharpenData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public SharpenEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();

		EffectData = new SharpenData ();
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	public override void Render (
		DBNull preRender,
		ImageSurface src,
		ImageSurface dest,
		ReadOnlySpan<RectangleI> rois)
	{
		foreach (var rect in rois)
			RenderRect (Data.Amount, src, dest, rect);
	}

	public override ColorBgra Apply (in ColorBgra src, int area, Span<int> hb, Span<int> hg, Span<int> hr, Span<int> ha)
	{
		ColorBgra median = GetPercentile (50, area, hb, hg, hr, ha);
		return ColorBgra.Lerp (src, median, -0.5f);
	}
}

public sealed class SharpenData : EffectData
{
	[Caption ("Amount"), MinimumValue (1), MaximumValue (20)]
	public int Amount { get; set; } = 2;
}

