using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Pinta.Core;

namespace Pinta.Effects;

public enum PredefinedPalettes
{
	BlackWhite,
	OldMsPaint,
	OldWindows16,
	OldWindows20,
	Rgb3Bit,
	Rgb666,
	Rgb6Bit,
	Rgb12Bit,
}

internal static class PaletteHelper
{
	public static ImmutableArray<ColorBgra> GetPredefined (PredefinedPalettes choice)
	{
		return choice switch {
			PredefinedPalettes.BlackWhite => Predefined.BlackWhite,
			PredefinedPalettes.OldMsPaint => Predefined.OldMsPaint,
			PredefinedPalettes.OldWindows16 => Predefined.OldWindows16,
			PredefinedPalettes.OldWindows20 => Predefined.OldWindows20,
			PredefinedPalettes.Rgb3Bit => Predefined.Rgb3Bit,
			PredefinedPalettes.Rgb666 => Predefined.Rgb666,
			PredefinedPalettes.Rgb6Bit => Predefined.Rgb6Bit,
			PredefinedPalettes.Rgb12Bit => Predefined.Rgb12Bit,
			_ => throw new InvalidEnumArgumentException (nameof (choice), (int) choice, typeof (PredefinedPalettes)),
		};
	}

	public static class Predefined
	{
		public static ImmutableArray<ColorBgra> BlackWhite { get; }
		public static ImmutableArray<ColorBgra> OldMsPaint => old_ms_paint.Value;
		public static ImmutableArray<ColorBgra> OldWindows16 => old_windows_16.Value;
		public static ImmutableArray<ColorBgra> OldWindows20 => old_windows_20.Value;
		public static ImmutableArray<ColorBgra> Rgb3Bit => rgb_3_bit.Value;
		public static ImmutableArray<ColorBgra> Rgb666 => rgb_666.Value;
		public static ImmutableArray<ColorBgra> Rgb6Bit => rgb_6_bit.Value;
		public static ImmutableArray<ColorBgra> Rgb12Bit => rgb_12_bit.Value;

		private static readonly Lazy<ImmutableArray<ColorBgra>> old_windows_16;
		private static readonly Lazy<ImmutableArray<ColorBgra>> old_windows_20;
		private static readonly Lazy<ImmutableArray<ColorBgra>> old_ms_paint;
		private static readonly Lazy<ImmutableArray<ColorBgra>> rgb_3_bit;
		private static readonly Lazy<ImmutableArray<ColorBgra>> rgb_666;
		private static readonly Lazy<ImmutableArray<ColorBgra>> rgb_6_bit;
		private static readonly Lazy<ImmutableArray<ColorBgra>> rgb_12_bit;

		static Predefined ()
		{
			BlackWhite = [ColorBgra.Black, ColorBgra.White];
			old_ms_paint = new (() => EnumerateOldMsPaintColors ().ToImmutableArray ());
			old_windows_16 = new (() => EnumerateOldWindows16Colors ().ToImmutableArray ());
			old_windows_20 = new (() => EnumerateOldWindows20Colors ().ToImmutableArray ());
			rgb_3_bit = new (() => RgbColorCube (levels: 2, factor: 255).ToImmutableArray ()); // https://en.wikipedia.org/wiki/List_of_monochrome_and_RGB_color_formats#3-bit_RGB
			rgb_666 = new (() => RgbColorCube (levels: 6, factor: 51).ToImmutableArray ()); // https://en.wikipedia.org/wiki/List_of_software_palettes#6_level_RGB
			rgb_6_bit = new (() => RgbColorCube (levels: 4, factor: 85).ToImmutableArray ()); // https://en.wikipedia.org/wiki/List_of_monochrome_and_RGB_color_formats#6-bit_RGB
			rgb_12_bit = new (() => RgbColorCube (levels: 16, factor: 17).ToImmutableArray ()); // https://en.wikipedia.org/wiki/List_of_monochrome_and_RGB_color_formats#12-bit_RGB
		}

		private static IEnumerable<ColorBgra> EnumerateOldWindows16Colors ()
		{
			// https://en.wikipedia.org/wiki/List_of_software_palettes#Microsoft_Windows_and_IBM_OS/2_default_16-color_palette
			yield return ColorBgra.FromBgr (0, 0, 0); // Black
			yield return ColorBgra.FromBgr (0, 0, 128); // Blue
			yield return ColorBgra.FromBgr (0, 128, 0); // Green
			yield return ColorBgra.FromBgr (0, 128, 128); // Cyan
			yield return ColorBgra.FromBgr (128, 0, 0); // Red
			yield return ColorBgra.FromBgr (128, 0, 128); // Magenta
			yield return ColorBgra.FromBgr (128, 64, 0); // Brown
			yield return ColorBgra.FromBgr (192, 192, 192); // Light Gray
			yield return ColorBgra.FromBgr (128, 128, 128); // Dark Gray
			yield return ColorBgra.FromBgr (0, 0, 255); // Light Blue
			yield return ColorBgra.FromBgr (0, 255, 0); // Light Green
			yield return ColorBgra.FromBgr (0, 255, 255); // Light Cyan
			yield return ColorBgra.FromBgr (255, 0, 0); // Light Red
			yield return ColorBgra.FromBgr (255, 0, 255); // Light Magenta
			yield return ColorBgra.FromBgr (255, 255, 0); // Yellow
			yield return ColorBgra.FromBgr (255, 255, 255); // White
		}

		private static IEnumerable<ColorBgra> EnumerateOldWindows20Colors ()
		{
			// https://en.wikipedia.org/wiki/List_of_software_palettes#Microsoft_Windows_default_20-color_palette
			foreach (var color in EnumerateOldWindows16Colors ())
				yield return color;

			yield return ColorBgra.FromBgr (240, 251, 255); // Cream
			yield return ColorBgra.FromBgr (192, 220, 192); // Money Green
			yield return ColorBgra.FromBgr (240, 202, 166); // Sky Blue
			yield return ColorBgra.FromBgr (164, 160, 160); // Medium Grey
		}

		private static IEnumerable<ColorBgra> EnumerateOldMsPaintColors ()
		{
			// https://wiki.vg-resource.com/Paint

			yield return ColorBgra.FromBgr (0, 0, 0); // Black

			yield return ColorBgra.FromBgr (0, 0, 128); // Maroon
			yield return ColorBgra.FromBgr (0, 128, 0); // Green
			yield return ColorBgra.FromBgr (0, 128, 128); // Olive
			yield return ColorBgra.FromBgr (128, 0, 0); // Navy blue
			yield return ColorBgra.FromBgr (128, 0, 128); // Magenta
			yield return ColorBgra.FromBgr (128, 128, 0); // Teal
			yield return ColorBgra.FromBgr (128, 128, 128); // Dark gray

			yield return ColorBgra.FromBgr (0, 0, 255); // Red
			yield return ColorBgra.FromBgr (0, 255, 0); // Lime green
			yield return ColorBgra.FromBgr (0, 255, 255); // Yellow
			yield return ColorBgra.FromBgr (255, 0, 0); // Blue
			yield return ColorBgra.FromBgr (255, 0, 255); // Light Magenta
			yield return ColorBgra.FromBgr (255, 255, 0); // Cyan
			yield return ColorBgra.FromBgr (255, 255, 255); // White

			yield return ColorBgra.FromBgr (0, 64, 128); // Saddle brown
			yield return ColorBgra.FromBgr (64, 64, 0); // Cyprus (dark teal)
			yield return ColorBgra.FromBgr (64, 128, 128); // Highball (mossy olive)
			yield return ColorBgra.FromBgr (64, 128, 255); // Coral
			yield return ColorBgra.FromBgr (128, 0, 255); // Deep pink
			yield return ColorBgra.FromBgr (128, 64, 0); // Dark cerulean
			yield return ColorBgra.FromBgr (128, 255, 0); // Spring green
			yield return ColorBgra.FromBgr (128, 255, 255); // Light yellow
			yield return ColorBgra.FromBgr (192, 192, 192); // Light gray
			yield return ColorBgra.FromBgr (255, 0, 128); // Electric indigo
			yield return ColorBgra.FromBgr (255, 128, 0); // Dodger blue
			yield return ColorBgra.FromBgr (255, 128, 128); // Light slate blue
			yield return ColorBgra.FromBgr (255, 255, 128); // Electric blue
		}

		private static IEnumerable<ColorBgra> RgbColorCube (ImmutableArray<byte> values)
		{
			for (int r = 0; r < values.Length; r++)
				for (int g = 0; g < values.Length; g++)
					for (int b = 0; b < values.Length; b++)
						yield return ColorBgra.FromBgr (
							b: values[b],
							g: values[g],
							r: values[r]);
		}

		private static IEnumerable<ColorBgra> RgbColorCube (IEnumerable<byte> valueSequence)
			=> RgbColorCube (valueSequence.ToImmutableArray ());

		private static IEnumerable<ColorBgra> RgbColorCube (byte levels, byte factor)
			=> RgbColorCube (Enumerable.Range (0, levels).Select (i => Utility.ClampToByte (i * factor)));
	}
}
