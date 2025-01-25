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

public sealed class ReduceNoiseEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsNoiseReduceNoise;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Reduce Noise");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Noise");

	public ReduceNoiseData Data => (ReduceNoiseData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public ReduceNoiseEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();

		EffectData = new ReduceNoiseData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	// Algorithm Code Ported From PDN
	private static ColorBgra GetPercentileOfColor (ColorBgra color, int area, Span<int> hb, Span<int> hg, Span<int> hr, Span<int> ha)
	{
		int rc = 0;
		int gc = 0;
		int bc = 0;

		ColorBgra straight_color = color.ToStraightAlpha ();

		for (int i = 0; i < straight_color.R; ++i)
			rc += hr[i];

		for (int i = 0; i < straight_color.G; ++i)
			gc += hg[i];

		for (int i = 0; i < straight_color.B; ++i)
			bc += hb[i];

		rc = (rc * 255) / area;
		gc = (gc * 255) / area;
		bc = (bc * 255) / area;

		return ColorBgra.FromBgra ((byte) bc, (byte) gc, (byte) rc, straight_color.A).ToPremultipliedAlpha ();
	}

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		int radius = Data.Radius;
		double strength = -0.2 * Data.Strength;

		foreach (var rect in rois)
			LocalHistogram.RenderRect (Apply, radius, src, dest, rect);

		// === Methods ===

		ColorBgra Apply (ColorBgra color, int area, Span<int> hb, Span<int> hg, Span<int> hr, Span<int> ha)
		{
			ColorBgra normalized = GetPercentileOfColor (color, area, hb, hg, hr, ha);
			double lerp = strength * (1 - 0.75 * color.GetIntensity ());
			return ColorBgra.Lerp (color, normalized, lerp);
		}
	}

	public sealed class ReduceNoiseData : EffectData
	{
		[Caption ("Radius"), MinimumValue (1), MaximumValue (200)]
		public int Radius { get; set; } = 6;

		[Caption ("Strength"), MinimumValue (0), IncrementValue (0.01), DigitsValue (2), MaximumValue (1)]
		public double Strength { get; set; } = 0.4;
	}
}
