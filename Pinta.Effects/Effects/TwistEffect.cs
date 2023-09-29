/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class TwistEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsDistortTwist;

	public override string Name => Translations.GetString ("Twist");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Distort");

	public TwistData Data => (TwistData) EffectData!;  // NRT - Set in constructor

	public TwistEffect ()
	{
		EffectData = new TwistData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		MaterializedSettings settings = CreateSettings (dst);

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();

		foreach (var rect in rois) {
			for (int y = rect.Top; y <= rect.Bottom; y++) {
				float j = y - settings.halfHeight;
				var rowOffset = y * src.Width;
				var src_row = src_data.Slice (rowOffset, src.Width);
				var dst_row = dst_data.Slice (rowOffset, src.Width);
				for (int x = rect.Left; x <= rect.Right; x++) {
					float i = x - settings.halfWidth;
					if (i * i + j * j > (settings.maxrad + 1) * (settings.maxrad + 1))
						dst_row[x] = src_row[x];
					else
						dst_row[x] = GetFinalPixelColor (src, settings, src_data, j, i);
				}
			}
		}
	}

	private sealed record MaterializedSettings (float halfWidth, float halfHeight, float maxrad, float twist, IReadOnlyList<PointD> antialiasPoints);

	private static ColorBgra GetFinalPixelColor (ImageSurface src, MaterializedSettings settings, ReadOnlySpan<ColorBgra> src_data, float j, float i)
	{
		int b = 0;
		int g = 0;
		int r = 0;
		int a = 0;
		int antialiasSamples = settings.antialiasPoints.Count;
		for (int p = 0; p < antialiasSamples; ++p) {
			float u = i + (float) settings.antialiasPoints[p].X;
			float v = j + (float) settings.antialiasPoints[p].Y;
			double rad = Math.Sqrt (u * u + v * v);
			double preTheta = Math.Atan2 (v, u);
			double preT = 1 - rad / settings.maxrad;
			double t = (preT < 0) ? 0 : (preT * preT * preT);
			double theta = preTheta + ((t * settings.twist) / 100);
			var sampleX = (int) (settings.halfWidth + (float) (rad * Math.Cos (theta)));
			var sampleY = (int) (settings.halfHeight + (float) (rad * Math.Sin (theta)));
			ref readonly ColorBgra sample = ref src.GetColorBgra (src_data, src.Width, sampleX, sampleY);
			b += sample.B;
			g += sample.G;
			r += sample.R;
			a += sample.A;
		}
		return ColorBgra.FromBgra (
			(byte) (b / antialiasSamples),
			(byte) (g / antialiasSamples),
			(byte) (r / antialiasSamples),
			(byte) (a / antialiasSamples));
	}

	private MaterializedSettings CreateSettings (ImageSurface dst)
	{
		float preliminaryTwist = Data.Amount;

		float hw = dst.Width / 2.0f;
		float hh = dst.Height / 2.0f;
		return new (
			halfWidth: hw,
			halfHeight: hh,
			maxrad: Math.Min (hw, hh),
			twist: preliminaryTwist * preliminaryTwist * Math.Sign (preliminaryTwist),
			antialiasPoints: InitializeAntialiasPoints (Data.Antialias)
		);
	}

	private static PointD[] InitializeAntialiasPoints (int aaLevel)
	{
		int aaSamples = aaLevel * aaLevel + 1;
		PointD[] aaPoints = new PointD[aaSamples];
		for (int i = 0; i < aaSamples; ++i) {
			var prePtX = ((i * aaLevel) / (float) aaSamples);
			var ptX = prePtX - ((int) prePtX);
			var ptY = i / (float) aaSamples;
			aaPoints[i] = new (ptX, ptY);
		}
		return aaPoints;
	}

	#endregion

	public sealed class TwistData : EffectData
	{
		[Caption ("Amount"), MinimumValue (-100), MaximumValue (100)]
		public int Amount { get; set; } = 45;

		[Caption ("Antialias"), MinimumValue (0), MaximumValue (5)]
		public int Antialias { get; set; } = 2;
	}
}
