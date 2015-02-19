/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Gui.Widgets;
using Pinta.Core;

namespace Pinta.Effects
{
	public class BrightnessContrastEffect : BaseEffect
	{
		private int multiply;
		private int divide;
		private byte[] rgbTable;
		private bool table_calculated;
		
		public override string Icon {
			get { return "Menu.Adjustments.BrightnessAndContrast.png"; }
		}

		public override string Name {
			get { return Mono.Unix.Catalog.GetString ("Brightness / Contrast"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override Gdk.Key AdjustmentMenuKey {
			get { return Gdk.Key.B; }
		}

		public BrightnessContrastData Data { get { return EffectData as BrightnessContrastData; } }
		
		public BrightnessContrastEffect ()
		{
			EffectData = new BrightnessContrastData ();
			EffectData.PropertyChanged += HandleEffectDataPropertyChanged;
		}

		/// <summary>
		/// If any of the effect data was changed, we need to recalculate the rgb table before rendering
		/// </summary>
		void HandleEffectDataPropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			table_calculated = false;
		}
		
		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		public unsafe override void Render (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			if (!table_calculated)
				Calculate ();

			foreach (Gdk.Rectangle rect in rois) {
				for (int y = rect.Top; y <= rect.GetBottom (); y++) {
					ColorBgra* srcRowPtr = src.GetPointAddressUnchecked (rect.Left, y);
					ColorBgra* dstRowPtr = dest.GetPointAddressUnchecked (rect.Left, y);
					ColorBgra* dstRowEndPtr = dstRowPtr + rect.Width;

					if (divide == 0) {
						while (dstRowPtr < dstRowEndPtr) {
							ColorBgra col = *srcRowPtr;
							int i = col.GetIntensityByte ();
							uint c = rgbTable[i];
							dstRowPtr->Bgra = (col.Bgra & 0xff000000) | c | (c << 8) | (c << 16);

							++dstRowPtr;
							++srcRowPtr;
						}
					} else {
						while (dstRowPtr < dstRowEndPtr) {
							ColorBgra col = *srcRowPtr;
							int i = col.GetIntensityByte ();
							int shiftIndex = i * 256;

							col.R = rgbTable[shiftIndex + col.R];
							col.G = rgbTable[shiftIndex + col.G];
							col.B = rgbTable[shiftIndex + col.B];

							*dstRowPtr = col;
							++dstRowPtr;
							++srcRowPtr;
						}
					}
				}
			}
		}

		private void Calculate ()
		{
			if (Data.Contrast < 0) {
				multiply = Data.Contrast + 100;
				divide = 100;
			} else if (Data.Contrast > 0) {
				multiply = 100;
				divide = 100 - Data.Contrast;
			} else {
				multiply = 1;
				divide = 1;
			}

			if (rgbTable == null)
				rgbTable = new byte[65536];

			if (divide == 0) {
				for (int intensity = 0; intensity < 256; intensity++) {
					if (intensity + Data.Brightness < 128)
						rgbTable[intensity] = 0;
					else
						rgbTable[intensity] = 255;
				}
			} else if (divide == 100) {
				for (int intensity = 0; intensity < 256; intensity++) {
					int shift = (intensity - 127) * multiply / divide + 127 - intensity + Data.Brightness;

					for (int col = 0; col < 256; ++col) {
						int index = (intensity * 256) + col;
						rgbTable[index] = Utility.ClampToByte (col + shift);
					}
				}
			} else {
				for (int intensity = 0; intensity < 256; ++intensity) {
					int shift = (intensity - 127 + Data.Brightness) * multiply / divide + 127 - intensity;

					for (int col = 0; col < 256; ++col) {
						int index = (intensity * 256) + col;
						rgbTable[index] = Utility.ClampToByte (col + shift);
					}
				}
			}
			
			table_calculated = true;
		}
		
		public class BrightnessContrastData : EffectData
		{
			private int brightness = 0;
			private int contrast = 0;

			[Caption ("Brightness")]
			public int Brightness {
				get { return brightness; }
				set {
					if (value != brightness) {
						brightness = value;
						FirePropertyChanged ("Brightness");
					}
				}
			}

			[Caption ("Contrast")]
			public int Contrast {
				get { return contrast; }
				set {
					if (value != contrast) {
						contrast = value;
						FirePropertyChanged ("Contrast");
					}
				}
			}

			[Skip]
			public override bool IsDefault {
				get { return Brightness == 0 && Contrast == 0; }
			}
		}
	}
}
