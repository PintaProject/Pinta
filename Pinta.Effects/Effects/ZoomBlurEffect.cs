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

namespace Pinta.Effects;

public sealed class ZoomBlurEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsBlursZoomBlur;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Zoom Blur");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Blurs");

	public ZoomBlurData Data => (ZoomBlurData) EffectData!;  // NRT - Set in constructor

	public ZoomBlurEffect ()
	{
		EffectData = new ZoomBlurData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		if (Data.Amount == 0) {
			// Copy src to dest
			return;
		}

		int src_width = src.Width;
		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		int dst_width = dst.Width;
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		Core.RectangleI src_bounds = src.GetBounds ();

		long w = dst.Width;
		long h = dst.Height;
		long fox = (long) (dst.Width * Data.Offset.X * 32768.0);
		long foy = (long) (dst.Height * Data.Offset.Y * 32768.0);
		long fcx = fox + (w << 15);
		long fcy = foy + (h << 15);
		long fz = Data.Amount;

		const int n = 64;

		foreach (var rect in rois) {
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

					ColorBgra src_pixel = src_row[x];
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
							ColorBgra src_pixel_2 = src.GetColorBgra (src_data, src_width, new (u, v));

							sr += src_pixel_2.R * src_pixel_2.A;
							sg += src_pixel_2.G * src_pixel_2.A;
							sb += src_pixel_2.B * src_pixel_2.A;
							sa += src_pixel_2.A;
							++sc;
						}
					}

					if (sa != 0) {
						dst_row[x] = ColorBgra.FromBgra (
							b: Utility.ClampToByte (sb / sa),
							g: Utility.ClampToByte (sg / sa),
							r: Utility.ClampToByte (sr / sa),
							a: Utility.ClampToByte (sa / sc)
						);
					} else {
						dst_row[x].Bgra = 0;
					}
				}
			}
		}
	}
	#endregion

	public sealed class ZoomBlurData : EffectData
	{
		[Caption ("Amount"), MinimumValue (0), MaximumValue (100)]
		public int Amount { get; set; } = 10;

		[Caption ("Offset")]
		public Core.PointI Offset { get; set; } = new (0, 0);

		[Skip]
		public override bool IsDefault => Amount == 0;
	}
}
