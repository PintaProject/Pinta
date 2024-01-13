using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
	WebSafe,
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
			PredefinedPalettes.WebSafe => Predefined.WebSafe,
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
		public static ImmutableArray<ColorBgra> WebSafe => web_safe.Value;

		private static readonly Lazy<ImmutableArray<ColorBgra>> old_windows_16;
		private static readonly Lazy<ImmutableArray<ColorBgra>> old_windows_20;
		private static readonly Lazy<ImmutableArray<ColorBgra>> old_ms_paint;
		private static readonly Lazy<ImmutableArray<ColorBgra>> rgb_3_bit;
		private static readonly Lazy<ImmutableArray<ColorBgra>> rgb_666;
		private static readonly Lazy<ImmutableArray<ColorBgra>> web_safe;

		static Predefined ()
		{
			BlackWhite = ImmutableArray.CreateRange (new[] { ColorBgra.FromBgr (0, 0, 0), ColorBgra.FromBgr (255, 255, 255) });
			old_ms_paint = new (() => EnumerateOldMsPaintColors ().ToImmutableArray ());
			old_windows_16 = new (() => EnumerateOldWindows16Colors ().ToImmutableArray ());
			old_windows_20 = new (() => EnumerateOldWindows20Colors ().ToImmutableArray ());
			rgb_3_bit = new (() => EnumerateRgb3Bit ().ToImmutableArray ());
			rgb_666 = new (() => EnumerateRgb666 ().ToImmutableArray ());
			web_safe = new (() => EnumerateWebSafeColorCube ().ToImmutableArray ());
		}

		private static IEnumerable<ColorBgra> EnumerateRgb666 ()
		{
			// https://en.wikipedia.org/wiki/List_of_software_palettes#6_level_RGB
			const byte factor = 51;
			for (byte r = 0; r < 6; r++)
				for (byte g = 0; g < 6; g++)
					for (byte b = 0; b < 6; b++)
						yield return ColorBgra.FromBgr (
							b: (byte) (b * factor),
							g: (byte) (g * factor),
							r: (byte) (r * factor));
		}

		private static IEnumerable<ColorBgra> EnumerateRgb3Bit ()
		{
			// https://en.wikipedia.org/wiki/List_of_monochrome_and_RGB_color_formats#3-bit_RGB
			for (byte i = 0; i < 8; i++) {
				bool r = (i & 1) != 0;
				bool g = (i & 2) != 0;
				bool b = (i & 4) != 0;
				yield return ColorBgra.FromBgr (
					b: b ? byte.MaxValue : byte.MinValue,
					g: g ? byte.MaxValue : byte.MinValue,
					r: r ? byte.MaxValue : byte.MinValue);
			}
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
			yield return ColorBgra.FromBgr (255, 255, 255); // White
			yield return ColorBgra.FromBgr (128, 128, 128); // Dark gray
			yield return ColorBgra.FromBgr (192, 192, 192); // Light gray
			yield return ColorBgra.FromBgr (0, 0, 255); // Red
			yield return ColorBgra.FromBgr (0, 0, 128); // Maroon
			yield return ColorBgra.FromBgr (0, 255, 255); // Yellow
			yield return ColorBgra.FromBgr (0, 128, 128); // Olive
			yield return ColorBgra.FromBgr (0, 255, 0); // Lime green
			yield return ColorBgra.FromBgr (0, 128, 0); // Green
			yield return ColorBgra.FromBgr (255, 255, 0); // Cyan
			yield return ColorBgra.FromBgr (128, 128, 0); // Teal
			yield return ColorBgra.FromBgr (255, 0, 0); // Blue
			yield return ColorBgra.FromBgr (128, 0, 0); // Navy blue
			yield return ColorBgra.FromBgr (255, 0, 255); // Light Magenta
			yield return ColorBgra.FromBgr (128, 0, 128); // Magenta
			yield return ColorBgra.FromBgr (128, 255, 255); // Light yellow
			yield return ColorBgra.FromBgr (64, 128, 128); // Highball (mossy olive)
			yield return ColorBgra.FromBgr (128, 255, 0); // Spring green
			yield return ColorBgra.FromBgr (64, 64, 0); // Cyprus (dark teal)
			yield return ColorBgra.FromBgr (255, 255, 128); // Electric blue
			yield return ColorBgra.FromBgr (255, 128, 0); // Dodger blue
			yield return ColorBgra.FromBgr (255, 128, 128); // Light slate blue
			yield return ColorBgra.FromBgr (128, 64, 0); // Dark cerulean
			yield return ColorBgra.FromBgr (128, 0, 255); // Deep pink
			yield return ColorBgra.FromBgr (255, 0, 128); // Electric indigo
			yield return ColorBgra.FromBgr (64, 128, 255); // Coral
			yield return ColorBgra.FromBgr (0, 64, 128); // Saddle brown
		}

		private static IEnumerable<ColorBgra> EnumerateWebSafeColorCube ()
		{
			for (short r = 0; r <= 255; r += 51)
				for (short g = 0; g <= 255; g += 51)
					for (short b = 0; b <= 255; b += 51)
						yield return ColorBgra.FromBgr ((byte) b, (byte) g, (byte) r);
		}


	}
}
