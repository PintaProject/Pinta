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

public sealed class RadialBlurEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsBlursRadialBlur;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Radial Blur");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Blurs");

	public RadialBlurData Data => (RadialBlurData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public RadialBlurEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();

		EffectData = new RadialBlurData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN

	private static PointI Rotated (PointI f, int fr)
	{
		//sin(x) ~~ x
		//cos(x)~~ 1 - x^2/2
		return new (
			X: f.X - ((f.Y >> 8) * fr >> 8) - ((f.X >> 14) * (fr * fr >> 11) >> 8),
			Y: f.Y + ((f.X >> 8) * fr >> 8) - ((f.Y >> 14) * (fr * fr >> 11) >> 8)
		);
	}

	private sealed record RadialBlurSettings (
		Size canvasSize,
		int fcx,
		int fcy,
		int n,
		int fr);

	private RadialBlurSettings CreateSettings (ImageSurface source)
	{
		var offset = Data.Offset;
		int quality = Data.Quality;
		Size sourceSize = source.GetSize ();
		return new (
			canvasSize: sourceSize,
			fcx: (sourceSize.Width << 15) + (int) (offset.X * (sourceSize.Width << 15)),
			fcy: (sourceSize.Height << 15) + (int) (offset.Y * (sourceSize.Height << 15)),
			n: quality * quality * (30 + quality * quality),
			fr: (int) (Data.Angle.Degrees * Math.PI * 65536.0 / 181.0)
		);
	}

	public override void Render (ImageSurface source, ImageSurface destination, ReadOnlySpan<RectangleI> rois)
	{
		if (Data.Angle.Degrees == 0) // Copy src to dest
			return;

		RadialBlurSettings settings = CreateSettings (source);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		foreach (RectangleI rect in rois) {

			for (int y = rect.Top; y <= rect.Bottom; ++y) {

				ReadOnlySpan<ColorBgra> sourceRow = sourceData.Slice (y * settings.canvasSize.Width, settings.canvasSize.Width);
				Span<ColorBgra> destinationRow = destinationData.Slice (y * settings.canvasSize.Width, settings.canvasSize.Width);

				for (int x = rect.Left; x <= rect.Right; ++x)
					destinationRow[x] = GetFinalPixelColor (settings, sourceData, sourceRow, x, y);
			}
		}
	}

	private static ColorBgra GetFinalPixelColor (
		RadialBlurSettings settings,
		ReadOnlySpan<ColorBgra> sourceData,
		ReadOnlySpan<ColorBgra> sourceRow,
		int x,
		int y)
	{
		ColorBgra src_pixel = sourceRow[x];

		PointI f = new (
			X: (x << 16) - settings.fcx,
			Y: (y << 16) - settings.fcy);

		int fsr = settings.fr / settings.n;

		int sr = src_pixel.R * src_pixel.A;
		int sg = src_pixel.G * src_pixel.A;
		int sb = src_pixel.B * src_pixel.A;
		int sa = src_pixel.A;
		int sc = 1;

		PointI o1 = f;
		PointI o2 = f;

		for (int i = 0; i < settings.n; ++i) {

			o1 = Rotated (o1, fsr);
			o2 = Rotated (o2, -fsr);

			PointI p1 = new (
				X: o1.X + settings.fcx + 32768 >> 16,
				Y: o1.Y + settings.fcy + 32768 >> 16);

			if (p1.X > 0 && p1.Y > 0 && p1.X < settings.canvasSize.Width && p1.Y < settings.canvasSize.Height) {

				ColorBgra sample = sourceData[p1.Y * settings.canvasSize.Width + p1.X];

				sr += sample.R * sample.A;
				sg += sample.G * sample.A;
				sb += sample.B * sample.A;
				sa += sample.A;

				++sc;
			}

			PointI p2 = new (
				X: o2.X + settings.fcx + 32768 >> 16,
				Y: o2.Y + settings.fcy + 32768 >> 16);

			if (p2.X > 0 && p2.Y > 0 && p2.X < settings.canvasSize.Width && p2.Y < settings.canvasSize.Height) {

				ColorBgra sample = sourceData[p2.Y * settings.canvasSize.Width + p2.X];

				sr += sample.R * sample.A;
				sg += sample.G * sample.A;
				sb += sample.B * sample.A;
				sa += sample.A;

				++sc;
			}
		}

		return
			(sa > 0)
			? ColorBgra.FromBgra (
				b: Utility.ClampToByte (sb / sa),
				g: Utility.ClampToByte (sg / sa),
				r: Utility.ClampToByte (sr / sa),
				a: Utility.ClampToByte (sa / sc))
			: ColorBgra.FromUInt32 (0);
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
