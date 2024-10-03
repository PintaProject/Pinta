/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Hanh Pham <hanh.pham@gmx.com>                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class MedianEffect : LocalHistogramEffect
{
	private int radius;
	private int percentile;

	public override string Icon => Pinta.Resources.Icons.EffectsNoiseMedian;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Median");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Noise");

	public MedianData Data => (MedianData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public MedianEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new MedianData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		radius = Data.Radius;
		percentile = Data.Percentile;

		foreach (RectangleI rect in rois)
			RenderRect (radius, src, dest, rect);
	}

	public override ColorBgra Apply (in ColorBgra src, int area, Span<int> hb, Span<int> hg, Span<int> hr, Span<int> ha)
		=> GetPercentile (percentile, area, hb, hg, hr, ha);

	#endregion

	public sealed class MedianData : EffectData
	{
		[Caption ("Radius"), MinimumValue (1), MaximumValue (200)]
		public int Radius { get; set; } = 10;

		[Caption ("Percentile"), MinimumValue (0), MaximumValue (100)]
		public int Percentile { get; set; } = 50;

		[Skip]
		public override bool IsDefault => Radius == 0;
	}
}
