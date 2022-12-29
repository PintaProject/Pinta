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
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public class BrightnessContrastEffect : BaseEffect
	{
		private int multiply;
		private int divide;
		private byte[]? rgbTable;
		private bool table_calculated;

		public override string Icon {
			get { return "Menu.Adjustments.BrightnessAndContrast.png"; }
		}

		public override string Name {
			get { return Translations.GetString ("Brightness / Contrast"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string AdjustmentMenuKey {
			get { return "B"; }
		}

		public BrightnessContrastData Data { get { return (BrightnessContrastData) EffectData!; } } // NRT - Set in constructor

		public BrightnessContrastEffect ()
		{
			EffectData = new BrightnessContrastData ();
			EffectData.PropertyChanged += HandleEffectDataPropertyChanged;
		}

		/// <summary>
		/// If any of the effect data was changed, we need to recalculate the rgb table before rendering
		/// </summary>
		void HandleEffectDataPropertyChanged (object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			table_calculated = false;
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		public override void Render (ImageSurface src, ImageSurface dest, Core.Rectangle[] rois)
		{
			if (!table_calculated)
				Calculate ();

			var src_data = src.GetReadOnlyData ();
			var dst_data = dest.GetData ();
			int width = src.Width;

			foreach (Core.Rectangle rect in rois) {
				for (int y = rect.Top; y <= rect.Bottom; y++) {
					var src_row = src_data.Slice (y * width + rect.Left, rect.Width);
					var dst_row = dst_data.Slice (y * width + rect.Left, rect.Width);

					if (divide == 0) {
						for (int i = 0; i < src_row.Length; ++i) {
							ref readonly ColorBgra col = ref src_row[i];
							uint c = rgbTable![col.GetIntensityByte ()]; // NRT - Set in Calculate
							dst_row[i].Bgra = (col.Bgra & 0xff000000) | c | (c << 8) | (c << 16);
						}
					} else {
						for (int i = 0; i < src_row.Length; ++i) {
							ColorBgra col = src_row[i];
							int intensity = col.GetIntensityByte ();
							int shiftIndex = intensity * 256;

							col.R = rgbTable![shiftIndex + col.R];
							col.G = rgbTable[shiftIndex + col.G];
							col.B = rgbTable[shiftIndex + col.B];

							dst_row[i] = col;
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
