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
	public class ZoomBlurEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.ZoomBlur.png"; }
		}

		public override string Name {
			get { return Translations.GetString ("Zoom Blur"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Translations.GetString ("Blurs"); }
		}

		public ZoomBlurData Data { get { return (ZoomBlurData) EffectData!; } } // NRT - Set in constructor

		public ZoomBlurEffect ()
		{
			EffectData = new ZoomBlurData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public override void Render (ImageSurface src, ImageSurface dst, Core.Rectangle[] rois)
		{
			if (Data.Amount == 0) {
				// Copy src to dest
				return;
			}

			int src_width = src.Width;
			ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyData ();
			int dst_width = dst.Width;
			Span<ColorBgra> dst_data = dst.GetData ();
			Core.Rectangle src_bounds = src.GetBounds ();

			long w = dst.Width;
			long h = dst.Height;
			long fox = (long) (dst.Width * Data.Offset.X * 32768.0);
			long foy = (long) (dst.Height * Data.Offset.Y * 32768.0);
			long fcx = fox + (w << 15);
			long fcy = foy + (h << 15);
			long fz = Data.Amount;

			const int n = 64;

			foreach (Core.Rectangle rect in rois) {
				for (int y = rect.Top; y <= rect.Bottom; ++y) {
					var src_row = src_data.Slice (y * src_width, src_width);
					var dst_row = dst_data.Slice (y * dst_width, dst_width);

					for (int x = rect.Left; x <= rect.Right; ++x) {
						long fx = (x << 16) - fcx;
						long fy = (y << 16) - fcy;

						int sr = 0;
						int sg = 0;
						int sb = 0;
						int sa = 0;
						int sc = 0;

						ref readonly ColorBgra src_pixel = ref src_row[x];
						sr += src_pixel.R * src_pixel.A;
						sg += src_pixel.G * src_pixel.A;
						sb += src_pixel.B * src_pixel.A;
						sa += src_pixel.A;
						++sc;

						for (int i = 0; i < n; ++i) {
							fx -= ((fx >> 4) * fz) >> 10;
							fy -= ((fy >> 4) * fz) >> 10;

							int u = (int) (fx + fcx + 32768 >> 16);
							int v = (int) (fy + fcy + 32768 >> 16);

							if (src_bounds.Contains (u, v)) {
								ref readonly ColorBgra src_pixel_2 = ref src.GetColorBgra (src_data, src_width, u, v);

								sr += src_pixel_2.R * src_pixel_2.A;
								sg += src_pixel_2.G * src_pixel_2.A;
								sb += src_pixel_2.B * src_pixel_2.A;
								sa += src_pixel_2.A;
								++sc;
							}
						}

						ref ColorBgra dst_pixel = ref dst_row[x];

						if (sa != 0) {
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

		public class ZoomBlurData : EffectData
		{
			[Caption ("Amount"), MinimumValue (0), MaximumValue (100)]
			public int Amount = 10;

			[Caption ("Offset")]
			public Core.Point Offset = new (0, 0);

			[Skip]
			public override bool IsDefault { get { return Amount == 0; } }
		}
	}
}
