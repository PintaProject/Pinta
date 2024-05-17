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

public sealed class ZoomBlurEffect : BaseEffect<ZoomBlurEffect.ZoomBlurSettings>
{
	public override string Icon => Pinta.Resources.Icons.EffectsBlursZoomBlur;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Zoom Blur");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Blurs");

	public ZoomBlurData Data => (ZoomBlurData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public ZoomBlurEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new ZoomBlurData ();
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN

	public sealed record ZoomBlurSettings (
		int src_width,
		int dst_width,
		long fcx,
		long fcy,
		long fz);

	public override ZoomBlurSettings GetPreRender (ImageSurface src, ImageSurface dst)
	{
		PointI offset = Data.Offset;

		long w = dst.Width;
		long h = dst.Height;
		long fox = (long) (w * offset.X * 32768.0);
		long foy = (long) (h * offset.Y * 32768.0);

		return new (
			src_width: src.Width,
			dst_width: dst.Width,
			fcx: fox + (w << 15),
			fcy: foy + (h << 15),
			fz: Data.Amount
		);
	}

	public override void Render (
		ZoomBlurSettings settings,
		ImageSurface src,
		ImageSurface dst,
		ReadOnlySpan<RectangleI> rois)
	{
		if (Data.Amount == 0)
			return; // Copy src to dest

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		RectangleI src_bounds = src.GetBounds ();

		foreach (var rect in rois) {
			for (int y = rect.Top; y <= rect.Bottom; ++y) {
				var src_row = src_data.Slice (y * settings.src_width, settings.src_width);
				var dst_row = dst_data.Slice (y * settings.dst_width, settings.dst_width);

				for (int x = rect.Left; x <= rect.Right; ++x)
					dst_row[x] = GetFinalPixelColor (src, settings, src_data, src_bounds, y, src_row, x);
			}
		}
	}

	private static ColorBgra GetFinalPixelColor (ImageSurface src, ZoomBlurSettings settings, ReadOnlySpan<ColorBgra> src_data, RectangleI src_bounds, int y, ReadOnlySpan<ColorBgra> src_row, int x)
	{
		const int n = 64;

		long fx = (x << 16) - settings.fcx;
		long fy = (y << 16) - settings.fcy;

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

			fx -= ((fx >> 4) * settings.fz) >> 10;
			fy -= ((fy >> 4) * settings.fz) >> 10;

			int u = (int) (fx + settings.fcx + 32768 >> 16);
			int v = (int) (fy + settings.fcy + 32768 >> 16);

			if (src_bounds.Contains (u, v)) {
				ColorBgra src_pixel_2 = src.GetColorBgra (src_data, settings.src_width, new (u, v));

				sr += src_pixel_2.R * src_pixel_2.A;
				sg += src_pixel_2.G * src_pixel_2.A;
				sb += src_pixel_2.B * src_pixel_2.A;
				sa += src_pixel_2.A;
				++sc;
			}
		}

		return
			(sa != 0)
			? ColorBgra.FromBgra (
				b: Utility.ClampToByte (sb / sa),
				g: Utility.ClampToByte (sg / sa),
				r: Utility.ClampToByte (sr / sa),
				a: Utility.ClampToByte (sa / sc))
			: ColorBgra.FromUInt32 (0);
	}

	#endregion

	public sealed class ZoomBlurData : EffectData
	{
		[Caption ("Amount"), MinimumValue (0), MaximumValue (100)]
		public int Amount { get; set; } = 10;

		[Caption ("Offset")]
		public PointI Offset { get; set; } = new (0, 0);

		[Skip]
		public override bool IsDefault => Amount == 0;
	}
}
