/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Pinta.Gui.Widgets;
using Cairo;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class FragmentEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.Fragment.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Fragment"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Blurs"); }
		}

		public FragmentData Data { get { return EffectData as FragmentData; } }

		public FragmentEffect ()
		{
			EffectData = new FragmentData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		private Gdk.Point[] RecalcPointOffsets (int fragments, double rotationAngle, int distance)
		{
			double pointStep = 2 * Math.PI / (double)fragments;
			double rotationRadians = ((rotationAngle - 90.0) * Math.PI) / 180.0;

			Gdk.Point[] pointOffsets = new Gdk.Point[fragments];

			for (int i = 0; i < fragments; i++) {
				double currentRadians = rotationRadians + (pointStep * i);

				pointOffsets[i] = new Gdk.Point (
				    (int)Math.Round (distance * -Math.Sin (currentRadians), MidpointRounding.AwayFromZero),
				    (int)Math.Round (distance * -Math.Cos (currentRadians), MidpointRounding.AwayFromZero));
			}
			
			return pointOffsets;
		}

		public unsafe override void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			Gdk.Point[] pointOffsets = RecalcPointOffsets (Data.Fragments, Data.Rotation, Data.Distance);

			int poLength = pointOffsets.Length;
			Gdk.Point* pointOffsetsPtr = stackalloc Gdk.Point[poLength];
			
			for (int i = 0; i < poLength; ++i)
				pointOffsetsPtr[i] = pointOffsets[i];

			ColorBgra* samples = stackalloc ColorBgra[poLength];

			// Cache these for a massive performance boost
			int src_width = src.Width;
			int src_height = src.Height;
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;

			foreach (Gdk.Rectangle rect in rois) {
				for (int y = rect.Top; y <= rect.GetBottom (); y++) {
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);

					for (int x = rect.Left; x <= rect.GetRight (); x++) {
						int sampleCount = 0;

						for (int i = 0; i < poLength; ++i) {
							int u = x - pointOffsetsPtr[i].X;
							int v = y - pointOffsetsPtr[i].Y;

							if (u >= 0 && u < src_width && v >= 0 && v < src_height) {
								samples[sampleCount] = src.GetPointUnchecked (src_data_ptr, src_width, u, v);
								++sampleCount;
							}
						}

						*dstPtr = ColorBgra.Blend (samples, sampleCount);
						++dstPtr;
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
