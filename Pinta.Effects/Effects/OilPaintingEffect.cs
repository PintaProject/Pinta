/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Gui.Widgets;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class OilPaintingEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Artistic.OilPainting.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Oil Painting"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Artistic"); }
		}

		public OilPaintingData Data { get { return EffectData as OilPaintingData; } }

		public OilPaintingEffect ()
		{
			EffectData = new OilPaintingData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void Render (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			int width = src.Width;
			int height = src.Height;

			int arrayLens = 1 + Data.Coarseness;

			int localStoreSize = arrayLens * 5 * sizeof (int);

			byte* localStore = stackalloc byte[localStoreSize];
			byte* p = localStore;

			int* intensityCount = (int*)p;
			p += arrayLens * sizeof (int);

			uint* avgRed = (uint*)p;
			p += arrayLens * sizeof (uint);

			uint* avgGreen = (uint*)p;
			p += arrayLens * sizeof (uint);

			uint* avgBlue = (uint*)p;
			p += arrayLens * sizeof (uint);

			uint* avgAlpha = (uint*)p;
			p += arrayLens * sizeof (uint);

			byte maxIntensity = (byte)Data.Coarseness;

			foreach (Gdk.Rectangle rect in rois) {

				int rectTop = rect.Top;
				int rectBottom = rect.GetBottom ();
				int rectLeft = rect.Left;
				int rectRight = rect.GetRight ();

				ColorBgra* dst_dataptr = (ColorBgra*)dest.DataPtr;
				int dst_width = dest.Width;
				ColorBgra* src_dataptr = (ColorBgra*)src.DataPtr;
				int src_width = src.Width;
				
				for (int y = rectTop; y <= rectBottom; ++y) {
					ColorBgra* dstPtr = dest.GetPointAddressUnchecked (dst_dataptr, dst_width, rect.Left, y);

					int top = y - Data.BrushSize;
					int bottom = y + Data.BrushSize + 1;

					if (top < 0) {
						top = 0;
					}

					if (bottom > height) {
						bottom = height;
					}

					for (int x = rectLeft; x <= rectRight; ++x) {
						SetToZero (localStore, (ulong)localStoreSize);

						int left = x - Data.BrushSize;
						int right = x + Data.BrushSize + 1;

						if (left < 0) {
							left = 0;
						}

						if (right > width) {
							right = width;
						}

						int numInt = 0;

						for (int j = top; j < bottom; ++j) {
							ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_dataptr, src_width, left, j);

							for (int i = left; i < right; ++i) {
								byte intensity = Utility.FastScaleByteByByte (srcPtr->GetIntensityByte (), maxIntensity);

								++intensityCount[intensity];
								++numInt;

								avgRed[intensity] += srcPtr->R;
								avgGreen[intensity] += srcPtr->G;
								avgBlue[intensity] += srcPtr->B;
								avgAlpha[intensity] += srcPtr->A;

								++srcPtr;
							}
						}

						byte chosenIntensity = 0;
						int maxInstance = 0;

						for (int i = 0; i <= maxIntensity; ++i) {
							if (intensityCount[i] > maxInstance) {
								chosenIntensity = (byte)i;
								maxInstance = intensityCount[i];
							}
						}

						// TODO: correct handling of alpha values?

						byte R = (byte)(avgRed[chosenIntensity] / maxInstance);
						byte G = (byte)(avgGreen[chosenIntensity] / maxInstance);
						byte B = (byte)(avgBlue[chosenIntensity] / maxInstance);
						byte A = (byte)(avgAlpha[chosenIntensity] / maxInstance);

						*dstPtr = ColorBgra.FromBgra (B, G, R, A);
						++dstPtr;
					}
				}
			}
		}
		
		// This is slow, and gets called a lot
		private unsafe static void SetToZero (byte* dst, ulong length)
		{
			int* ptr = (int*)dst;
			
			for (ulong i = 0; i < length / 4; i++) {
				*ptr = 0;
				ptr++;
			}
		}
		#endregion

		public class OilPaintingData : EffectData
		{
			[Caption ("Brush Size"), MinimumValue (1), MaximumValue (8)]
			public int BrushSize = 3;

			[Caption ("Coarseness"), MinimumValue (3), MaximumValue (255)]
			public int Coarseness = 50;
		}
	}
}
