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
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public class OilPaintingEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Artistic.OilPainting.png"; }
		}

		public override string Name {
			get { return Translations.GetString ("Oil Painting"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Translations.GetString ("Artistic"); }
		}

		public OilPaintingData Data { get { return (OilPaintingData) EffectData!; } } // NRT - Set in constructor

		public OilPaintingEffect ()
		{
			EffectData = new OilPaintingData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public override void Render (ImageSurface src, ImageSurface dest, Core.Rectangle[] rois)
		{
			int width = src.Width;
			int height = src.Height;

			int arrayLens = 1 + Data.Coarseness;
			Span<int> intensityCount = stackalloc int[arrayLens];
			Span<uint> avgRed = stackalloc uint[arrayLens];
			Span<uint> avgGreen = stackalloc uint[arrayLens];
			Span<uint> avgBlue = stackalloc uint[arrayLens];
			Span<uint> avgAlpha = stackalloc uint[arrayLens];

			byte maxIntensity = (byte) Data.Coarseness;

			ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyData ();
			Span<ColorBgra> dst_data = dest.GetData ();

			foreach (Core.Rectangle rect in rois) {

				int rectTop = rect.Top;
				int rectBottom = rect.Bottom;
				int rectLeft = rect.Left;
				int rectRight = rect.Right;

				for (int y = rectTop; y <= rectBottom; ++y) {
					var dst_row = dst_data.Slice (y * width, width);

					int top = y - Data.BrushSize;
					int bottom = y + Data.BrushSize + 1;

					if (top < 0) {
						top = 0;
					}

					if (bottom > height) {
						bottom = height;
					}

					for (int x = rectLeft; x <= rectRight; ++x) {
						intensityCount.Clear ();
						avgRed.Clear ();
						avgGreen.Clear ();
						avgBlue.Clear ();
						avgAlpha.Clear ();

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
							var src_row = src_data.Slice (j * width, width);

							for (int i = left; i < right; ++i) {
								ref readonly ColorBgra src_pixel = ref src_row[i];
								byte intensity = Utility.FastScaleByteByByte (src_pixel.GetIntensityByte (), maxIntensity);

								++intensityCount[intensity];
								++numInt;

								avgRed[intensity] += src_pixel.R;
								avgGreen[intensity] += src_pixel.G;
								avgBlue[intensity] += src_pixel.B;
								avgAlpha[intensity] += src_pixel.A;
							}
						}

						byte chosenIntensity = 0;
						int maxInstance = 0;

						for (int i = 0; i <= maxIntensity; ++i) {
							if (intensityCount[i] > maxInstance) {
								chosenIntensity = (byte) i;
								maxInstance = intensityCount[i];
							}
						}

						// TODO: correct handling of alpha values?

						byte R = (byte) (avgRed[chosenIntensity] / maxInstance);
						byte G = (byte) (avgGreen[chosenIntensity] / maxInstance);
						byte B = (byte) (avgBlue[chosenIntensity] / maxInstance);
						byte A = (byte) (avgAlpha[chosenIntensity] / maxInstance);

						dst_row[x] = ColorBgra.FromBgra (B, G, R, A);
					}
				}
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
