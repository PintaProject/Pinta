/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Immutable;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class MotionBlurEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsBlursMotionBlur;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Motion Blur");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Blurs");

	public MotionBlurData Data => (MotionBlurData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public MotionBlurEffect (IServiceManager services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new MotionBlurData ();
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN

	private sealed record MotionBlurSettings (
		Size canvasSize,
		ImmutableArray<PointD> points);

	private MotionBlurSettings CreateSettings (ImageSurface src)
	{
		PointD start = new (0, 0);
		double theta = (double) (Data.Angle.Degrees + 180) * 2 * Math.PI / 360.0;
		double alpha = Data.Distance;
		PointD end = new ((float) alpha * Math.Cos (theta), (float) (-alpha * Math.Sin (theta)));

		if (Data.Centered) {
			start = new (-end.X / 2.0f, -end.Y / 2.0f);
			end = new (end.X / 2.0f, end.Y / 2.0f);
		}

		int numberOfPoints = (1 + Data.Distance) * 3 / 2;
		var points = ImmutableArray.CreateBuilder<PointD> (numberOfPoints);
		points.Count = numberOfPoints;
		if (numberOfPoints == 1) {
			points[0] = new PointD (0, 0);
		} else {
			for (int i = 0; i < numberOfPoints; ++i) {
				float frac = i / (float) (numberOfPoints - 1);
				points[i] = Utility.Lerp (start, end, frac);
			}
		}

		return new (
			canvasSize: src.GetSize (),
			points: points.MoveToImmutable ());
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		MotionBlurSettings settings = CreateSettings (src);

		Span<ColorBgra> samples = stackalloc ColorBgra[settings.points.Length];

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();

		foreach (var rect in rois) {
			foreach (var pixel in Utility.GeneratePixelOffsets (rect, settings.canvasSize)) {
				int sampleCount = 0;
				for (int j = 0; j < settings.points.Length; ++j) {
					PointD pt = new (settings.points[j].X + pixel.coordinates.X, settings.points[j].Y + pixel.coordinates.Y);
					if (pt.X < 0 || pt.Y < 0 || pt.X > (settings.canvasSize.Width - 1) || pt.Y > (settings.canvasSize.Height - 1))
						continue;
					samples[sampleCount] = src.GetBilinearSample (src_data, settings.canvasSize.Width, settings.canvasSize.Height, (float) pt.X, (float) pt.Y);
					++sampleCount;
				}
				dst_data[pixel.memoryOffset] = ColorBgra.Blend (samples[..sampleCount]);
			}
		}
	}
	#endregion

	public sealed class MotionBlurData : EffectData
	{
		[Skip]
		public override bool IsDefault => Distance == 0;

		[Caption ("Angle")]
		public DegreesAngle Angle { get; set; } = new (25);

		[Caption ("Distance"), MinimumValue (1), MaximumValue (200)]
		public int Distance { get; set; } = 10;

		[Caption ("Centered")]
		public bool Centered { get; set; } = true;
	}
}
