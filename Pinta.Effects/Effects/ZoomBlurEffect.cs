/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class ZoomBlurEffect : BaseEffect
{
	public override string Icon => Resources.Icons.EffectsBlursZoomBlur;

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

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	// Algorithm Code Ported From PDN

	private sealed record ZoomBlurSettings (
		RectangleI sourceBounds,
		Size size,
		long fc_x,
		long fc_y,
		long fz);

	private ZoomBlurSettings CreateSettings (ImageSurface source)
	{
		CenterOffset<double> offset = Data.Offset;
		Size size = source.GetSize ();
		long w = source.Width;
		long h = source.Height;
		long fo_x = (long) (w * offset.Horizontal * 32768.0);
		long fo_y = (long) (h * offset.Vertical * 32768.0);
		return new (
			sourceBounds: source.GetBounds (),
			size: size,
			fc_x: fo_x + (w << 15),
			fc_y: fo_y + (h << 15),
			fz: Data.Amount);
	}

	protected override void Render (
		ImageSurface source,
		ImageSurface destination,
		RectangleI roi)
	{
		if (Data.Amount == 0)
			return; // Copy src to dest

		ZoomBlurSettings settings = CreateSettings (source);
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in Utility.GeneratePixelOffsets (roi, settings.size))
			destinationData[pixel.memoryOffset] = GetFinalPixelColor (
				source,
				settings,
				sourceData,
				pixel);
	}

	private static ColorBgra GetFinalPixelColor (
		ImageSurface src,
		ZoomBlurSettings settings,
		ReadOnlySpan<ColorBgra> sourceData,
		PixelOffset pixel)
	{
		const int N = 64;

		long fx = (pixel.coordinates.X << 16) - settings.fc_x;
		long fy = (pixel.coordinates.Y << 16) - settings.fc_y;

		int sr = 0;
		int sg = 0;
		int sb = 0;
		int sa = 0;
		int sc = 0;

		ColorBgra src_pixel = sourceData[pixel.memoryOffset];
		sr += src_pixel.R * src_pixel.A;
		sg += src_pixel.G * src_pixel.A;
		sb += src_pixel.B * src_pixel.A;
		sa += src_pixel.A;
		++sc;

		for (int i = 0; i < N; ++i) {

			fx -= ((fx >> 4) * settings.fz) >> 10;
			fy -= ((fy >> 4) * settings.fz) >> 10;

			PointI transformed = new (
				X: (int) (fx + settings.fc_x + 32768 >> 16),
				Y: (int) (fy + settings.fc_y + 32768 >> 16));

			if (settings.sourceBounds.Contains (transformed)) {

				ColorBgra src_pixel_2 = src.GetColorBgra (
					sourceData,
					settings.size.Width,
					transformed);

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

	public sealed class ZoomBlurData : EffectData
	{
		[Caption ("Amount"), MinimumValue (0), MaximumValue (100)]
		public int Amount { get; set; } = 10;

		[Caption ("Offset")]
		public CenterOffset<double> Offset { get; set; } = new (0, 0);

		[Skip]
		public override bool IsDefault => Amount == 0;
	}
}
