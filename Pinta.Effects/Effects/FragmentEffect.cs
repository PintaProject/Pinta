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
	private readonly IWorkspaceService workspace;
	public FragmentEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new FragmentData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

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

	protected override void Render (
		ImageSurface source,
		ImageSurface destination,
		RectangleI roi)
	{
		FragmentSettings settings = CreateSettings (source);
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = destination.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.sourceSize))
			dst_data[pixel.memoryOffset] = GetFinalPixelColor (
				settings,
				source,
				sourceData,
				pixel);
	}

	private sealed record FragmentSettings (
		Size sourceSize,
		ImmutableArray<PointI> pointOffsets);
	private FragmentSettings CreateSettings (ImageSurface source)
	{
		return new (
			sourceSize: source.GetSize (),
			pointOffsets: RecalcPointOffsets (
				Data.Fragments,
				Data.Rotation.ToRadians (),
				Data.Distance)
		);
	}

	private static ColorBgra GetFinalPixelColor (
		FragmentSettings settings,
		ImageSurface source,
		ReadOnlySpan<ColorBgra> src_data,
		PixelOffset pixel)
	{
		Span<ColorBgra> samples = stackalloc ColorBgra[settings.pointOffsets.Length];

		int sampleCount = 0;

		for (int i = 0; i < settings.pointOffsets.Length; ++i) {

			PointI relative = new (
				X: pixel.coordinates.X - settings.pointOffsets[i].X,
				Y: pixel.coordinates.Y - settings.pointOffsets[i].Y);

			if (relative.X < 0 || relative.X >= settings.sourceSize.Width || relative.Y < 0 || relative.Y >= settings.sourceSize.Height)
				continue;

			samples[sampleCount] = source.GetColorBgra (
				src_data,
				settings.sourceSize.Width,
				relative);

			++sampleCount;
		}

		return ColorBgra.Blend (samples[..sampleCount]);
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
