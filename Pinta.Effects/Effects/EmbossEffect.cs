/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class EmbossEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsStylizeEmboss;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Emboss");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Stylize");

	public EmbossData Data => (EmbossData) EffectData!;

	private readonly IChromeService chrome;

	public EmbossEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new EmbossData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN

	private sealed record EmbossSettings (
		double[,] weights,
		int srcWidth,
		int srcHeight);

	private EmbossSettings CreateSettings (ImageSurface src)
		=> new (
			weights: ComputeWeights (Data.Angle.ToRadians ()),
			srcWidth: src.Width,
			srcHeight: src.Height);

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		EmbossSettings settings = CreateSettings (src);
		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		foreach (var rect in rois) {
			for (int y = rect.Top; y <= rect.Bottom; ++y) { // Loop through lines in rectangle
				int fyStart = (y == 0) ? 1 : 0;
				int fyEnd = (y == settings.srcHeight - 1) ? 2 : 3;
				Span<ColorBgra> dst_row = dst_data.Slice (y * settings.srcWidth, settings.srcWidth);
				for (int x = rect.Left; x <= rect.Right; ++x) { // Loop through points in line
					int fxStart = (x == 0) ? 1 : 0;
					int fxEnd = (x == settings.srcWidth - 1) ? 2 : 3;
					double sum = 0.0;
					for (int fy = fyStart; fy < fyEnd; ++fy) {
						for (int fx = fxStart; fx < fxEnd; ++fx) {
							double weight = settings.weights[fy, fx];
							PointI pixelPosition = new (
								X: x - 1 + fx,
								Y: y - 1 + fy);
							ColorBgra c = src.GetColorBgra (src_data, settings.srcWidth, pixelPosition);
							sum += weight * c.GetIntensityByte (); // Mutation
						}
					}
					byte iSum = Utility.ClampToByte (((int) sum) + 128);
					dst_row[x] = ColorBgra.FromBgra (iSum, iSum, iSum, 255); // Pixel
				}
			}
		}
	}


	private static double[,] ComputeWeights (RadiansAngle angle)
	{
		// angle delta for each weight
		const double ANGLE_DELTA = Math.PI / 4.0;

		// for r = 0 this builds an emboss filter pointing straight left
		double[,] weights = new double[3, 3];

		weights[0, 0] = Math.Cos (angle.Radians + ANGLE_DELTA);
		weights[0, 1] = Math.Cos (angle.Radians + 2.0 * ANGLE_DELTA);
		weights[0, 2] = Math.Cos (angle.Radians + 3.0 * ANGLE_DELTA);

		weights[1, 0] = Math.Cos (angle.Radians);
		weights[1, 1] = 0;
		weights[1, 2] = Math.Cos (angle.Radians + 4.0 * ANGLE_DELTA);

		weights[2, 0] = Math.Cos (angle.Radians - ANGLE_DELTA);
		weights[2, 1] = Math.Cos (angle.Radians - 2.0 * ANGLE_DELTA);
		weights[2, 2] = Math.Cos (angle.Radians - 3.0 * ANGLE_DELTA);

		return weights;
	}

	#endregion


	public sealed class EmbossData : EffectData
	{
		[Caption ("Angle")]
		public DegreesAngle Angle { get; set; } = new (0);
	}
}
