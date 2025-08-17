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
	private readonly IWorkspaceService workspace;
	public ZoomBlurEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new ZoomBlurData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN

	private sealed record ZoomBlurSettings (
		RectangleI sourceBounds,
		Size size,
		long fcX,
		long fcY,
		long fZ);

	private ZoomBlurSettings CreateSettings (ImageSurface source)
	{
		ZoomBlurData data = Data;
		CenterOffset<double> offset = data.Offset;
		Size size = source.GetSize ();
		long w = size.Width;
		long h = size.Height;
		long foX = (long) (w * offset.Horizontal * 32768.0);
		long foY = (long) (h * offset.Vertical * 32768.0);
		return new (
			sourceBounds: source.GetBounds (),
			size: size,
			fcX: foX + (w << 15),
			fcY: foY + (h << 15),
			fZ: data.Amount);
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		if (Data.Amount == 0)
			return; // Copy src to dest

		ZoomBlurSettings settings = CreateSettings (source);
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.size))
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

		long fx = (pixel.coordinates.X << 16) - settings.fcX;
		long fy = (pixel.coordinates.Y << 16) - settings.fcY;

		ColorBgra sourcePixel = sourceData[pixel.memoryOffset];

		int sr = sourcePixel.R;
		int sg = sourcePixel.G;
		int sb = sourcePixel.B;
		int sa = sourcePixel.A;

		int sc = 1;

		for (int i = 0; i < N; ++i) {

			fx -= ((fx >> 4) * settings.fZ) >> 10;
			fy -= ((fy >> 4) * settings.fZ) >> 10;

			PointI transformed = new (
				X: (int) (fx + settings.fcX + 32768 >> 16),
				Y: (int) (fy + settings.fcY + 32768 >> 16));

			if (!settings.sourceBounds.Contains (transformed))
				continue;

			ColorBgra src_pixel_2 = src.GetColorBgra (
				sourceData,
				settings.size.Width,
				transformed);

			sr += src_pixel_2.R;
			sg += src_pixel_2.G;
			sb += src_pixel_2.B;
			sa += src_pixel_2.A;

			++sc;
		}

		return
			(sa != 0)
			? ColorBgra.FromBgra (
				b: Utility.ClampToByte (sb / sc),
				g: Utility.ClampToByte (sg / sc),
				r: Utility.ClampToByte (sr / sc),
				a: Utility.ClampToByte (sa / sc))
			: ColorBgra.Transparent;
	}

	public sealed class ZoomBlurData : EffectData
	{
		[Caption ("Amount")]
		[MinimumValue (0), MaximumValue (100)]
		public int Amount { get; set; } = 10;

		[Caption ("Offset")]
		public CenterOffset<double> Offset { get; set; } = new (0, 0);

		[Skip]
		public override bool IsDefault => Amount == 0;
	}
}
