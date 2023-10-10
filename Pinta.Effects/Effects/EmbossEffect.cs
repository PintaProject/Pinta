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

public sealed class EmbossEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsStylizeEmboss;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Emboss");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Stylize");

	public EmbossData Data => (EmbossData) EffectData!;

	public EmbossEffect ()
	{
		EffectData = new EmbossData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		double[,] weights = ComputeWeights ();

		var srcWidth = src.Width;
		var srcHeight = src.Height;

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();

		foreach (var rect in rois) {
			// loop through each line of target rectangle
			for (int y = rect.Top; y <= rect.Bottom; ++y) {
				int fyStart = 0;
				int fyEnd = 3;

				if (y == 0)
					fyStart = 1;

				if (y == srcHeight - 1)
					fyEnd = 2;

				// loop through each point in the line 
				var dst_row = dst_data.Slice (y * srcWidth, srcWidth);

				for (int x = rect.Left; x <= rect.Right; ++x) {
					int fxStart = 0;
					int fxEnd = 3;

					if (x == 0)
						fxStart = 1;

					if (x == srcWidth - 1)
						fxEnd = 2;

					// loop through each weight
					double sum = 0.0;

					for (int fy = fyStart; fy < fyEnd; ++fy) {
						for (int fx = fxStart; fx < fxEnd; ++fx) {
							double weight = weights[fy, fx];
							ref readonly ColorBgra c = ref src.GetColorBgra (src_data, srcWidth, x - 1 + fx, y - 1 + fy);
							double intensity = (double) c.GetIntensityByte ();
							sum += weight * intensity;
						}
					}

					int iSum = (int) sum;
					iSum += 128;

					if (iSum > 255)
						iSum = 255;

					if (iSum < 0)
						iSum = 0;

					dst_row[x] = ColorBgra.FromBgra ((byte) iSum, (byte) iSum, (byte) iSum, 255);
				}
			}
		}
	}


	public double[,] ComputeWeights ()
	{
		// adjust and convert angle to radians
		double r = (double) Data.Angle * 2.0 * Math.PI / 360.0;

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
		public double Angle { get; set; } = 0;
	}
}
