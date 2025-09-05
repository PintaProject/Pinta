using System;
using System.Collections.ObjectModel;
using Pinta.Core;

namespace Pinta.Effects;

internal static class BrightnessContrast
{
	public sealed class PreRender
	{
		public int Multiply { get; }
		public int Divide { get; }
		public ReadOnlyCollection<byte> RGBTable { get; }
		public PreRender (int brightness, int contrast)
		{
			(int multiply, int divide) = contrast switch {
				< 0 => (contrast + 100, 100),
				> 0 => (100, 100 - contrast),
				_ => (1, 1),
			};

			(Multiply, Divide) = (multiply, divide);
			RGBTable = Array.AsReadOnly (CalculateTable (brightness, multiply, divide));
		}

		private static byte[] CalculateTable (int brightness, int multiply, int divide)
		{
			byte[] result = new byte[65536];

			if (divide == 0) {
				for (int intensity = 0; intensity < 256; intensity++) {
					if (intensity + brightness < 128)
						result[intensity] = 0;
					else
						result[intensity] = 255;
				}
			} else if (divide == 100) {
				for (int intensity = 0; intensity < 256; intensity++) {
					int shift = (intensity - 127) * multiply / divide + 127 - intensity + brightness;

					for (int col = 0; col < 256; ++col) {
						int index = (intensity * 256) + col;
						result[index] = Utility.ClampToByte (col + shift);
					}
				}
			} else {
				for (int intensity = 0; intensity < 256; ++intensity) {
					int shift = (intensity - 127 + brightness) * multiply / divide + 127 - intensity;

					for (int col = 0; col < 256; ++col) {
						int index = (intensity * 256) + col;
						result[index] = Utility.ClampToByte (col + shift);
					}
				}
			}

			return result;
		}
	}

	public static ColorBgra Apply (this PreRender preRender, in ColorBgra originalColor)
	{
		int intensity = originalColor.GetIntensityByte ();

		if (preRender.Divide == 0) {
			uint c = preRender.RGBTable[intensity];
			return ColorBgra.FromUInt32 ((originalColor.BGRA & 0xff000000) | c | (c << 8) | (c << 16));
		}

		int shiftIndex = intensity * 256;
		return ColorBgra.FromBgra (
			b: preRender.RGBTable[shiftIndex + originalColor.B],
			g: preRender.RGBTable[shiftIndex + originalColor.G],
			r: preRender.RGBTable[shiftIndex + originalColor.R],
			a: originalColor.A);
	}
}
