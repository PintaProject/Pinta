/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public class MotionBlurEffect : BaseEffect
	{
		public override string Icon => Pinta.Resources.Icons.EffectsBlursMotionBlur;

		public override string Name => Translations.GetString ("Motion Blur");

		public override bool IsConfigurable => true;

		public override string EffectMenuCategory => Translations.GetString ("Blurs");

		public MotionBlurData Data => (MotionBlurData) EffectData!;  // NRT - Set in constructor

		public MotionBlurEffect ()
		{
			EffectData = new MotionBlurData ();
		}

		public override void LaunchConfiguration ()
		{
			EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public override void Render (ImageSurface src, ImageSurface dst, Core.RectangleI[] rois)
		{
			PointD start = new PointD (0, 0);
			double theta = ((double) (Data.Angle + 180) * 2 * Math.PI) / 360.0;
			double alpha = (double) Data.Distance;
			PointD end = new PointD ((float) alpha * Math.Cos (theta), (float) (-alpha * Math.Sin (theta)));

			if (Data.Centered) {
				start.X = -end.X / 2.0f;
				start.Y = -end.Y / 2.0f;

				end.X /= 2.0f;
				end.Y /= 2.0f;
			}

			PointD[] points = new PointD[((1 + Data.Distance) * 3) / 2];

			if (points.Length == 1) {
				points[0] = new PointD (0, 0);
			} else {
				for (int i = 0; i < points.Length; ++i) {
					float frac = (float) i / (float) (points.Length - 1);
					points[i] = Utility.Lerp (start, end, frac);
				}
			}

			Span<ColorBgra> samples = stackalloc ColorBgra[points.Length];

			ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
			Span<ColorBgra> dst_data = dst.GetPixelData ();
			int src_width = src.Width;
			int src_height = src.Height;

			foreach (var rect in rois) {

				for (int y = rect.Top; y <= rect.Bottom; ++y) {
					var dst_row = dst_data.Slice (y * src_width, src_width);

					for (int x = rect.Left; x <= rect.Right; ++x) {
						int sampleCount = 0;

						for (int j = 0; j < points.Length; ++j) {
							PointD pt = new PointD (points[j].X + (float) x, points[j].Y + (float) y);

							if (pt.X >= 0 && pt.Y >= 0 && pt.X <= (src_width - 1) && pt.Y <= (src_height - 1)) {
								samples[sampleCount] = src.GetBilinearSample (src_data, src_width, src_height, (float) pt.X, (float) pt.Y);
								++sampleCount;
							}
						}

						dst_row[x] = ColorBgra.Blend (samples.Slice (0, sampleCount));
					}
				}
			}
		}
		#endregion

		public class MotionBlurData : EffectData
		{
			[Skip]
			public override bool IsDefault => Distance == 0;

			[Caption ("Angle")]
			public double Angle = 25;

			[Caption ("Distance"), MinimumValue (1), MaximumValue (200)]
			public int Distance = 10;

			[Caption ("Centered")]
			public bool Centered = true;
		}
	}
}
