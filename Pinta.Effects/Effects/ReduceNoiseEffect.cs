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

namespace Pinta.Effects;

public sealed class ReduceNoiseEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsNoiseReduceNoise;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Reduce Noise");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Noise");

	public ReduceNoiseData Data
		=> (ReduceNoiseData) EffectData!; // NRT - Set in constructor

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public ReduceNoiseEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new ReduceNoiseData ();
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		int radius = Data.Radius;
		double strength = -0.2 * Data.Strength;

		LocalHistogram.RenderRect (Apply, radius, source, destination, roi);

		// --- Methods ---

		ColorBgra Apply (ColorBgra color, int area, Span<int> hb, Span<int> hg, Span<int> hr, Span<int> ha)
		{
			ColorBgra straightColor = color.ToStraightAlpha ();
			ColorBgra normalized =
				ColorBgra.FromBgra (
					GetPercentileChannel (hb, area, straightColor.B),
					GetPercentileChannel (hg, area, straightColor.G),
					GetPercentileChannel (hr, area, straightColor.R),
					straightColor.A)
				.ToPremultipliedAlpha ();
			double lerp = strength * (1 - 0.75 * color.GetIntensity ());
			return ColorBgra.Lerp (color, normalized, lerp);
		}
	}

	private static byte GetPercentileChannel (Span<int> hc, int area, byte straightChannel)
	{
		int cc = 0;
		for (int i = 0; i < straightChannel; i++)
			cc += hc[i];
		cc = cc * 255 / area;
		return (byte) cc;
	}

	public sealed class ReduceNoiseData : EffectData
	{
		[Caption ("Radius")]
		[MinimumValue (1), MaximumValue (200)]
		public int Radius { get; set; } = 6;

		[Caption ("Strength")]
		[MinimumValue (0), MaximumValue (1)]
		[IncrementValue (0.01), DigitsValue (2),]
		public double Strength { get; set; } = 0.4;
	}
}
