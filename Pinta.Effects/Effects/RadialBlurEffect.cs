/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public class RadialBlurEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.RadialBlur.png"; }
		}

		public override string Name {
			get { return Translations.GetString ("Radial Blur"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Translations.GetString ("Blurs"); }
		}

		public RadialBlurData Data { get { return (RadialBlurData) EffectData!; } } // NRT - Set in constructor

		public RadialBlurEffect ()
		{
			EffectData = new RadialBlurData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		private static void Rotate (ref int fx, ref int fy, int fr)
		{
			int cx = fx;
			int cy = fy;

			//sin(x) ~~ x
			//cos(x)~~ 1 - x^2/2
			fx = cx - ((cy >> 8) * fr >> 8) - ((cx >> 14) * (fr * fr >> 11) >> 8);
			fy = cy + ((cx >> 8) * fr >> 8) - ((cy >> 14) * (fr * fr >> 11) >> 8);
		}

		public override void Render (ImageSurface src, ImageSurface dst, Core.RectangleI[] rois)
		{
			if (Data.Angle == 0) {
				// Copy src to dest
				return;
			}

			int w = dst.Width;
			int h = dst.Height;
			int src_w = src.Width;
			int fcx = (w << 15) + (int) (Data.Offset.X * (w << 15));
			int fcy = (h << 15) + (int) (Data.Offset.Y * (h << 15));

			int n = (Data.Quality * Data.Quality) * (30 + Data.Quality * Data.Quality);

			int fr = (int) (Data.Angle * Math.PI * 65536.0 / 181.0);

			var dst_data = dst.GetData ();
			var src_data = src.GetReadOnlyData ();

			foreach (Core.RectangleI rect in rois) {
				for (int y = rect.Top; y <= rect.Bottom; ++y) {
					var dst_row = dst_data.Slice (y * w, w);
					var src_row = src_data.Slice (y * src_w, src_w);

					for (int x = rect.Left; x <= rect.Right; ++x) {
						ref ColorBgra dst_pixel = ref dst_row[x];
						ref readonly ColorBgra src_pixel = ref src_row[x];

						int fx = (x << 16) - fcx;
						int fy = (y << 16) - fcy;

						int fsr = fr / n;

						int sr = 0;
						int sg = 0;
						int sb = 0;
						int sa = 0;
						int sc = 0;

						sr += src_pixel.R * src_pixel.A;
						sg += src_pixel.G * src_pixel.A;
						sb += src_pixel.B * src_pixel.A;
						sa += src_pixel.A;
						++sc;

						int ox1 = fx;
						int ox2 = fx;
						int oy1 = fy;
						int oy2 = fy;

						for (int i = 0; i < n; ++i) {
							Rotate (ref ox1, ref oy1, fsr);
							Rotate (ref ox2, ref oy2, -fsr);

							int u1 = ox1 + fcx + 32768 >> 16;
							int v1 = oy1 + fcy + 32768 >> 16;

							if (u1 > 0 && v1 > 0 && u1 < w && v1 < h) {
								ref readonly ColorBgra sample = ref src_data[v1 * src_w + u1];

								sr += sample.R * sample.A;
								sg += sample.G * sample.A;
								sb += sample.B * sample.A;
								sa += sample.A;
								++sc;
							}

							int u2 = ox2 + fcx + 32768 >> 16;
							int v2 = oy2 + fcy + 32768 >> 16;

							if (u2 > 0 && v2 > 0 && u2 < w && v2 < h) {
								ref readonly ColorBgra sample = ref src_data[v2 * src_w + u2];

								sr += sample.R * sample.A;
								sg += sample.G * sample.A;
								sb += sample.B * sample.A;
								sa += sample.A;
								++sc;
							}
						}

						if (sa > 0) {
							dst_pixel = ColorBgra.FromBgra (
							    Utility.ClampToByte (sb / sa),
							    Utility.ClampToByte (sg / sa),
							    Utility.ClampToByte (sr / sa),
							    Utility.ClampToByte (sa / sc));
						} else {
							dst_pixel.Bgra = 0;
						}
					}
				}
			}
		}
		#endregion

		public class RadialBlurData : EffectData
		{
			[Caption ("Angle")]
			public Double Angle = 2;

			[Caption ("Offset")]
			public PointD Offset = new PointD (0, 0);

			[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
			[Hint ("Use low quality for previews, small images, and small angles.  Use high quality for final quality, large images, and large angles.")]
			public int Quality = 2;

			[Skip]
			public override bool IsDefault { get { return Angle == 0; } }
		}
	}
}
