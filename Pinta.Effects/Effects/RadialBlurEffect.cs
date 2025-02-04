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
	private readonly IWorkspaceService workspace;
	public RadialBlurEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new RadialBlurData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN

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
		int fsr);

	private RadialBlurSettings CreateSettings (ImageSurface source)
	{
		var offset = Data.Offset;
		int quality = Data.Quality;
		Size sourceSize = source.GetSize ();
		int n = quality * quality * (30 + quality * quality);
		int fr = ((int) (Data.Angle.Degrees * Math.PI * 65536.0 / 181.0));
		return new (
			canvasSize: sourceSize,
			fcx: (sourceSize.Width << 15) + (int) (offset.Horizontal * (sourceSize.Width << 15)),
			fcy: (sourceSize.Height << 15) + (int) (offset.Vertical * (sourceSize.Height << 15)),
			n: n,
			fsr: fr / n);
	}

	public override void Render (ImageSurface source, ImageSurface destination, ReadOnlySpan<RectangleI> rois)
	{
		if (Data.Angle.Degrees == 0) // Copy src to dest
			return;

		RadialBlurSettings settings = CreateSettings (source);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		foreach (RectangleI rect in rois)
			foreach (var pixel in Tiling.GeneratePixelOffsets (rect, settings.canvasSize))
				destinationData[pixel.memoryOffset] = GetFinalPixelColor (
					settings,
					sourceData,
					pixel);
	}

	private static ColorBgra GetFinalPixelColor (
		RadialBlurSettings settings,
		ReadOnlySpan<ColorBgra> sourceData,
		PixelOffset pixel)
	{
		ColorBgra sourcePixel = sourceData[pixel.memoryOffset];

		PointI f = new (
			X: (pixel.coordinates.X << 16) - settings.fcx,
			Y: (pixel.coordinates.Y << 16) - settings.fcy);

		int sr = sourcePixel.R;
		int sg = sourcePixel.G;
		int sb = sourcePixel.B;
		int sa = sourcePixel.A;
		int sc = 1;

		PointI o1 = f;
		PointI o2 = f;

		for (int i = 0; i < settings.n; ++i) {

			o1 = Rotated (o1, settings.fsr);
			o2 = Rotated (o2, -settings.fsr);

			PointI p1 = new (
				X: o1.X + settings.fcx + 32768 >> 16,
				Y: o1.Y + settings.fcy + 32768 >> 16);

			if (p1.X > 0 && p1.Y > 0 && p1.X < settings.canvasSize.Width && p1.Y < settings.canvasSize.Height) {

				ColorBgra sample = sourceData[p1.Y * settings.canvasSize.Width + p1.X];

				sr += sample.R;
				sg += sample.G;
				sb += sample.B;
				sa += sample.A;

				++sc;
			}

			PointI p2 = new (
				X: o2.X + settings.fcx + 32768 >> 16,
				Y: o2.Y + settings.fcy + 32768 >> 16);

			if (p2.X > 0 && p2.Y > 0 && p2.X < settings.canvasSize.Width && p2.Y < settings.canvasSize.Height) {

				ColorBgra sample = sourceData[p2.Y * settings.canvasSize.Width + p2.X];

				sr += sample.R;
				sg += sample.G;
				sb += sample.B;
				sa += sample.A;

				++sc;
			}
		}

		return
			(sa > 0)
			? ColorBgra.FromBgra (
				b: Utility.ClampToByte (sb / sc),
				g: Utility.ClampToByte (sg / sc),
				r: Utility.ClampToByte (sr / sc),
				a: Utility.ClampToByte (sa / sc))
			: ColorBgra.FromUInt32 (0);
	}

	public sealed class RadialBlurData : EffectData
	{
		[Caption ("Angle")]
		public DegreesAngle Angle { get; set; } = new (2);

		[Caption ("Offset")]
		public CenterOffset<double> Offset { get; set; } = new (0, 0);

		[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
		[Hint ("Use low quality for previews, small images, and small angles.  Use high quality for final quality, large images, and large angles.")]
		public int Quality { get; set; } = 2;

		[Skip]
		public override bool IsDefault => Angle.Degrees == 0;
	}
}
