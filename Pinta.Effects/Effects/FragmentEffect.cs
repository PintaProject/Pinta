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

namespace Pinta.Effects
{
	public class FragmentEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.Fragment.png"; }
		}

		public override string Name {
			get { return Translations.GetString ("Fragment"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Translations.GetString ("Blurs"); }
		}

		public FragmentData Data { get { return (FragmentData) EffectData!; } } // NRT - Set in constructor

		public FragmentEffect ()
		{
			EffectData = new FragmentData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		private Core.PointI[] RecalcPointOffsets (int fragments, double rotationAngle, int distance)
		{
			double pointStep = 2 * Math.PI / (double) fragments;
			double rotationRadians = ((rotationAngle - 90.0) * Math.PI) / 180.0;

			var pointOffsets = new Core.PointI[fragments];

			for (int i = 0; i < fragments; i++) {
				double currentRadians = rotationRadians + (pointStep * i);

				pointOffsets[i] = new Core.PointI (
				    (int) Math.Round (distance * -Math.Sin (currentRadians), MidpointRounding.AwayFromZero),
				    (int) Math.Round (distance * -Math.Cos (currentRadians), MidpointRounding.AwayFromZero));
			}

			return pointOffsets;
		}

		public override void Render (ImageSurface src, ImageSurface dst, Core.RectangleI[] rois)
		{
			Core.PointI[] pointOffsets = RecalcPointOffsets (Data.Fragments, Data.Rotation, Data.Distance);

			int poLength = pointOffsets.Length;
			Span<Core.PointI> pointOffsetsPtr = stackalloc Core.PointI[poLength];

			for (int i = 0; i < poLength; ++i)
				pointOffsetsPtr[i] = pointOffsets[i];

			Span<ColorBgra> samples = stackalloc ColorBgra[poLength];

			// Cache these for a massive performance boost
			int src_width = src.Width;
			int src_height = src.Height;
			ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyData ();
			Span<ColorBgra> dst_data = dst.GetData ();

			foreach (Core.RectangleI rect in rois) {
				for (int y = rect.Top; y <= rect.Bottom; y++) {
					var dst_row = dst_data.Slice (y * src_width, src_width);

					for (int x = rect.Left; x <= rect.Right; x++) {
						int sampleCount = 0;

						for (int i = 0; i < poLength; ++i) {
							int u = x - pointOffsetsPtr[i].X;
							int v = y - pointOffsetsPtr[i].Y;

							if (u >= 0 && u < src_width && v >= 0 && v < src_height) {
								samples[sampleCount] = src.GetColorBgra (src_data, src_width, u, v);
								++sampleCount;
							}
						}

						dst_row[x] = ColorBgra.Blend (samples.Slice (0, sampleCount));
					}
				}
			}
		}
		#endregion

		public class FragmentData : EffectData
		{
			[Caption ("Fragments"), MinimumValue (2), MaximumValue (50)]
			public int Fragments = 4;

			[Caption ("Distance"), MinimumValue (0), MaximumValue (100)]
			public int Distance = 8;

			[Caption ("Rotation")]
			public double Rotation = 0;
		}
	}
}
