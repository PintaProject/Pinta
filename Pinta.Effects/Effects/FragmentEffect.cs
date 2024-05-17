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

public sealed class FragmentEffect : BaseEffect<DBNull>
{
	public override string Icon
		=> Pinta.Resources.Icons.EffectsBlursFragment;

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

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN

	private static ImmutableArray<PointI> RecalcPointOffsets (int fragments, double rotationAngle, int distance)
	{
		double pointStep = 2 * Math.PI / fragments;
		double rotationRadians = ((rotationAngle - 90.0) * Math.PI) / 180.0;

		var pointOffsets = ImmutableArray.CreateBuilder<PointI> (fragments);
		pointOffsets.Count = fragments;

		for (int i = 0; i < fragments; i++) {
			double currentRadians = rotationRadians + (pointStep * i);

			pointOffsets[i] = new PointI (
				X: (int) Math.Round (distance * -Math.Sin (currentRadians), MidpointRounding.AwayFromZero),
				Y: (int) Math.Round (distance * -Math.Cos (currentRadians), MidpointRounding.AwayFromZero)
			);
		}

		return pointOffsets.MoveToImmutable ();
	}

	public override DBNull GetPreRender (ImageSurface src, ImageSurface dst)
		=> DBNull.Value;

	public override void Render (
		DBNull preRender,
		ImageSurface src,
		ImageSurface dst,
		ReadOnlySpan<RectangleI> rois)
	{
		var pointOffsets = RecalcPointOffsets (Data.Fragments, Data.Rotation.Degrees, Data.Distance);

		int poLength = pointOffsets.Length;
		Span<PointI> pointOffsetsPtr = stackalloc PointI[poLength];

		for (int i = 0; i < poLength; ++i)
			pointOffsetsPtr[i] = pointOffsets[i];

		Span<ColorBgra> samples = stackalloc ColorBgra[poLength];

		// Cache these for a massive performance boost
		int src_width = src.Width;
		int src_height = src.Height;
		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();

		foreach (RectangleI rect in rois) {
			for (int y = rect.Top; y <= rect.Bottom; y++) {
				var dst_row = dst_data.Slice (y * src_width, src_width);

				for (int x = rect.Left; x <= rect.Right; x++) {

					int sampleCount = 0;

					for (int i = 0; i < poLength; ++i) {

						int u = x - pointOffsetsPtr[i].X;
						int v = y - pointOffsetsPtr[i].Y;

						if (u < 0 || u >= src_width || v < 0 || v >= src_height)
							continue;

						samples[sampleCount] = src.GetColorBgra (src_data, src_width, new (u, v));
						++sampleCount;
					}

					dst_row[x] = ColorBgra.Blend (samples[..sampleCount]);
				}
			}
		}
	}

	#endregion

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
