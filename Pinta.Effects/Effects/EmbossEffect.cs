/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class EmbossEffect : BaseEffect<EmbossEffect.EmbossSettings>
{
	public override string Icon
		=> Pinta.Resources.Icons.EffectsStylizeEmboss;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Emboss");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Stylize");

	public EmbossData Data
		=> (EmbossData) EffectData!;

	private readonly IChromeService chrome;

	public EmbossEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new EmbossData ();
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN

	public sealed record EmbossSettings (
		double[,] weights,
		int srcWidth,
		int srcHeight);

	public override EmbossSettings GetPreRender (ImageSurface src, ImageSurface dst)
		=> new (
			weights: ComputeWeights (Data.Angle.Degrees),
			srcWidth: src.Width,
			srcHeight: src.Height
		);

	public override void Render (
		EmbossSettings settings,
		ImageSurface src,
		ImageSurface dst,
		ReadOnlySpan<RectangleI> rois)
	{
		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();

		foreach (var rect in rois) {
			// loop through each line of target rectangle
			for (int y = rect.Top; y <= rect.Bottom; ++y) {
				int fyStart = 0;
				int fyEnd = 3;

				if (y == 0)
					fyStart = 1;

				if (y == settings.srcHeight - 1)
					fyEnd = 2;

				// loop through each point in the line 
				var dst_row = dst_data.Slice (y * settings.srcWidth, settings.srcWidth);

				for (int x = rect.Left; x <= rect.Right; ++x) {
					int fxStart = 0;
					int fxEnd = 3;

					if (x == 0)
						fxStart = 1;

					if (x == settings.srcWidth - 1)
						fxEnd = 2;

					// loop through each weight
					double sum = 0.0;

					for (int fy = fyStart; fy < fyEnd; ++fy) {
						for (int fx = fxStart; fx < fxEnd; ++fx) {

							double weight = settings.weights[fy, fx];

							PointI pixelPosition = new (
								X: x - 1 + fx,
								Y: y - 1 + fy
							);

							ColorBgra c = src.GetColorBgra (src_data, settings.srcWidth, pixelPosition);

							double intensity = c.GetIntensityByte ();

							sum += weight * intensity;
						}
					}

					byte iSum = Utility.ClampToByte (((int) sum) + 128);

					dst_row[x] = ColorBgra.FromBgra (iSum, iSum, iSum, 255);
				}
			}
		}
	}


	private static double[,] ComputeWeights (double degrees)
	{
		// adjust and convert angle to radians
		double r = degrees * 2.0 * Math.PI / 360.0;

		// angle delta for each weight
		double dr = Math.PI / 4.0;

		// for r = 0 this builds an emboss filter pointing straight left
		double[,] weights = new double[3, 3];

		weights[0, 0] = Math.Cos (r + dr);
		weights[0, 1] = Math.Cos (r + 2.0 * dr);
		weights[0, 2] = Math.Cos (r + 3.0 * dr);

		weights[1, 0] = Math.Cos (r);
		weights[1, 1] = 0;
		weights[1, 2] = Math.Cos (r + 4.0 * dr);

		weights[2, 0] = Math.Cos (r - dr);
		weights[2, 1] = Math.Cos (r - 2.0 * dr);
		weights[2, 2] = Math.Cos (r - 3.0 * dr);

		return weights;
	}

	#endregion


	public sealed class EmbossData : EffectData
	{
		[Caption ("Angle")]
		public DegreesAngle Angle { get; set; } = new (0);
	}
}
