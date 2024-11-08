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
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class FragmentEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsBlursFragment;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Fragment");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Blurs");

	public FragmentData Data
		=> (FragmentData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public FragmentEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new FragmentData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	// Algorithm Code Ported From PDN

	private static ImmutableArray<PointI> RecalcPointOffsets (
		int fragments,
		RadiansAngle rotation,
		int distance)
	{
		RadiansAngle pointStep = new (RadiansAngle.MAX_RADIANS / fragments);
		RadiansAngle adjustedRotation = rotation - new RadiansAngle (RadiansAngle.MAX_RADIANS / 4);

		var pointOffsets = ImmutableArray.CreateBuilder<PointI> (fragments);
		pointOffsets.Count = fragments;
		for (int i = 0; i < fragments; i++) {
			RadiansAngle currentAngle = new (adjustedRotation.Radians + (pointStep.Radians * i));
			pointOffsets[i] = new PointI (
				X: (int) Math.Round (distance * -Math.Sin (currentAngle.Radians), MidpointRounding.AwayFromZero),
				Y: (int) Math.Round (distance * -Math.Cos (currentAngle.Radians), MidpointRounding.AwayFromZero));
		}

		return pointOffsets.MoveToImmutable ();
	}

	public override void Render (
		ImageSurface source,
		ImageSurface destination,
		ReadOnlySpan<RectangleI> rois)
	{
		var pointOffsets = RecalcPointOffsets (
			Data.Fragments,
			Data.Rotation.ToRadians (),
			Data.Distance);

		Span<ColorBgra> samples = stackalloc ColorBgra[pointOffsets.Length];

		Size sourceSize = source.GetSize ();

		ReadOnlySpan<ColorBgra> src_data = source.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = destination.GetPixelData ();

		foreach (RectangleI rect in rois) {

			foreach (var pixel in Utility.GeneratePixelOffsets (rect, sourceSize)) {

				int sampleCount = 0;

				for (int i = 0; i < pointOffsets.Length; ++i) {

					PointI relative = new (
						X: pixel.coordinates.X - pointOffsets[i].X,
						Y: pixel.coordinates.Y - pointOffsets[i].Y);

					if (relative.X < 0 || relative.X >= sourceSize.Width || relative.Y < 0 || relative.Y >= sourceSize.Height)
						continue;

					samples[sampleCount] = source.GetColorBgra (
						src_data,
						sourceSize.Width,
						relative);

					++sampleCount;
				}

				dst_data[pixel.memoryOffset] = ColorBgra.Blend (samples[..sampleCount]);
			}
		}
	}

	public sealed class FragmentData : EffectData
	{
		[Caption ("Fragments"), MinimumValue (2), MaximumValue (50)]
		public int Fragments { get; set; } = 4;

		[Caption ("Distance"), MinimumValue (0), MaximumValue (100)]
		public int Distance { get; set; } = 8;

		[Caption ("Rotation")]
		public DegreesAngle Rotation { get; set; } = new (0);
	}
}
