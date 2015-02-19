/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Pinta.Gui.Widgets;
using Cairo;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class MotionBlurEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.MotionBlur.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Motion Blur"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Blurs"); }
		}

		public MotionBlurData Data { get { return EffectData as MotionBlurData; } }

		public MotionBlurEffect ()
		{
			EffectData = new MotionBlurData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			PointD start = new PointD (0, 0);
			double theta = ((double)(Data.Angle + 180) * 2 * Math.PI) / 360.0;
			double alpha = (double)Data.Distance;
			PointD end = new PointD ((float)alpha * Math.Cos (theta), (float)(-alpha * Math.Sin (theta)));

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
					float frac = (float)i / (float)(points.Length - 1);
					points[i] = Utility.Lerp (start, end, frac);
				}
			}

			ColorBgra* samples = stackalloc ColorBgra[points.Length];

			ColorBgra* src_dataptr = (ColorBgra*)src.DataPtr;
			int src_width = src.Width;
			int src_height = src.Height;

			foreach (Gdk.Rectangle rect in rois) {

				for (int y = rect.Top; y <= rect.GetBottom (); ++y) {
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);

					for (int x = rect.Left; x <= rect.GetRight (); ++x) {
						int sampleCount = 0;

						for (int j = 0; j < points.Length; ++j) {
							PointD pt = new PointD (points[j].X + (float)x, points[j].Y + (float)y);

							if (pt.X >= 0 && pt.Y >= 0 && pt.X <= (src_width - 1) && pt.Y <= (src_height - 1)) {
								samples[sampleCount] = src.GetBilinearSample (src_dataptr, src_width, src_height, (float)pt.X, (float)pt.Y);
								++sampleCount;
							}
						}

						*dstPtr = ColorBgra.Blend (samples, sampleCount);
						++dstPtr;
					}
				}
			}
		}
		#endregion

		public class MotionBlurData : EffectData
		{
			[Skip]
			public override bool IsDefault { get { return Distance == 0; } }

			[Caption ("Angle")]
			public double Angle = 25;

			[Caption ("Distance"), MinimumValue (1), MaximumValue (200)]
			public int Distance = 10;

			[Caption ("Centered")]
			public bool Centered = true;
		}
	}
}
