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

public sealed class RadialBlurEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsBlursRadialBlur;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Radial Blur");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Blurs");

	public RadialBlurData Data => (RadialBlurData) EffectData!;  // NRT - Set in constructor

	public RadialBlurEffect ()
	{
		EffectData = new RadialBlurData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
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

	private sealed record RadialBlurSettings (
		int w,
		int h,
		int src_w,
		int fcx,
		int fcy,
		int n,
		int fr);

	private RadialBlurSettings CreateSettings (ImageSurface src, ImageSurface dst)
	{
		var offset = Data.Offset;
		int w = dst.Width;
		int h = dst.Height;
		int quality = Data.Quality;
		return new (
			w: w,
			h: h,
			src_w: src.Width,
			fcx: (w << 15) + (int) (offset.X * (w << 15)),
			fcy: (h << 15) + (int) (offset.Y * (h << 15)),
			n: quality * quality * (30 + quality * quality),
			fr: (int) (Data.Angle.Degrees * Math.PI * 65536.0 / 181.0)
		);
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		if (Data.Angle.Degrees == 0) // Copy src to dest
			return;

		RadialBlurSettings settings = CreateSettings (src, dst);

		var dst_data = dst.GetPixelData ();
		var src_data = src.GetReadOnlyPixelData ();

		foreach (Core.RectangleI rect in rois) {

			for (int y = rect.Top; y <= rect.Bottom; ++y) {

				var dst_row = dst_data.Slice (y * settings.w, settings.w);
				var src_row = src_data.Slice (y * settings.src_w, settings.src_w);

				for (int x = rect.Left; x <= rect.Right; ++x) {

					ColorBgra src_pixel = src_row[x];

					int fx = (x << 16) - settings.fcx;
					int fy = (y << 16) - settings.fcy;

					int fsr = settings.fr / settings.n;

					int sr = src_pixel.R * src_pixel.A;
					int sg = src_pixel.G * src_pixel.A;
					int sb = src_pixel.B * src_pixel.A;
					int sa = src_pixel.A;
					int sc = 1;

					int ox1 = fx;
					int ox2 = fx;
					int oy1 = fy;
					int oy2 = fy;

					for (int i = 0; i < settings.n; ++i) {
						Rotate (ref ox1, ref oy1, fsr);
						Rotate (ref ox2, ref oy2, -fsr);

						int u1 = ox1 + settings.fcx + 32768 >> 16;
						int v1 = oy1 + settings.fcy + 32768 >> 16;

						if (u1 > 0 && v1 > 0 && u1 < settings.w && v1 < settings.h) {
							ColorBgra sample = src_data[v1 * settings.src_w + u1];

							sr += sample.R * sample.A;
							sg += sample.G * sample.A;
							sb += sample.B * sample.A;
							sa += sample.A;
							++sc;
						}

						int u2 = ox2 + settings.fcx + 32768 >> 16;
						int v2 = oy2 + settings.fcy + 32768 >> 16;

						if (u2 > 0 && v2 > 0 && u2 < settings.w && v2 < settings.h) {
							ColorBgra sample = src_data[v2 * settings.src_w + u2];

							sr += sample.R * sample.A;
							sg += sample.G * sample.A;
							sb += sample.B * sample.A;
							sa += sample.A;
							++sc;
						}
					}

					dst_row[x] = GetFinalPixelColor (sr, sg, sb, sa, sc);

					static ColorBgra GetFinalPixelColor (
						int sr,
						int sg,
						int sb,
						int sa,
						int sc)
					{
						if (sa > 0) {
							return ColorBgra.FromBgra (
								b: Utility.ClampToByte (sb / sa),
								g: Utility.ClampToByte (sg / sa),
								r: Utility.ClampToByte (sr / sa),
								a: Utility.ClampToByte (sa / sc)
							);
						} else {
							return ColorBgra.FromUInt32 (0);
						}
					}
				}
			}
		}
	}

	#endregion

	public sealed class RadialBlurData : EffectData
	{
		[Caption ("Angle")]
		public DegreesAngle Angle { get; set; } = new (2);

		[Caption ("Offset")]
		public PointD Offset { get; set; } = new (0, 0);

		[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
		[Hint ("Use low quality for previews, small images, and small angles.  Use high quality for final quality, large images, and large angles.")]
		public int Quality { get; set; } = 2;

		[Skip]
		public override bool IsDefault => Angle.Degrees == 0;
	}
}
