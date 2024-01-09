using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class ForwardErrorDiffusionDitheringEffect : BaseEffect
{
	public override string Name => Translations.GetString ("Color");
	public override bool IsConfigurable => true;
	// TODO: Icon
	public override string EffectMenuCategory => Translations.GetString ("Test"); // TODO:
	public ForwardErrorDiffusionDitheringData Data => (ForwardErrorDiffusionDitheringData) EffectData!; // NRT - Set in constructor

	public override bool IsTileable => false;

	public ForwardErrorDiffusionDitheringEffect ()
	{
		EffectData = new ForwardErrorDiffusionDitheringData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	private sealed record DitheringSettings (
		ErrorDiffusionMatrix diffusionMatrix,
		ImmutableArray<ColorBgra> palette,
		int sourceWidth,
		int sourceHeight);

	private DitheringSettings CreateSettings (ImageSurface src)
		=> new (
			diffusionMatrix: GetPredefinedDiffusionMatrix (Data.DiffusionMatrix),
			palette: GetPredefinedPalette (Data.PaletteChoice),
			sourceWidth: src.Width,
			sourceHeight: src.Height
		);

	protected override void Render (ImageSurface src, ImageSurface dest, RectangleI roi)
	{
		DitheringSettings settings = CreateSettings (src);

		Span<ColorBgra> dst_data = dest.GetPixelData ();

		for (int y = roi.Top; y <= roi.Bottom; y++) {

			for (int x = roi.Left; x <= roi.Right; x++) {

				int currentIndex = y * settings.sourceWidth + x;
				ColorBgra originalPixel = dst_data[currentIndex];
				ColorBgra closestColor = FindClosestPaletteColor (settings.palette, originalPixel);

				dst_data[currentIndex] = closestColor;

				int errorRed = originalPixel.R - closestColor.R;
				int errorGreen = originalPixel.G - closestColor.G;
				int errorBlue = originalPixel.B - closestColor.B;

				for (int r = 0; r < settings.diffusionMatrix.Rows; r++) {

					for (int c = 0; c < settings.diffusionMatrix.Columns; c++) {

						var weight = settings.diffusionMatrix[r, c];

						if (weight <= 0)
							continue;

						PointI thisItem = new (
							X: x + c - settings.diffusionMatrix.ColumnsToLeft,
							Y: y + r
						);

						if (thisItem.X < 0 || thisItem.X >= settings.sourceWidth)
							continue;

						if (thisItem.Y < 0 || thisItem.Y >= settings.sourceHeight)
							continue;

						int idx = (thisItem.Y * settings.sourceWidth) + thisItem.X;

						double factor = ((double) weight) / settings.diffusionMatrix.TotalWeight;

						dst_data[idx] = AddError (dst_data[idx], factor, errorRed, errorGreen, errorBlue);
					}
				}

			}
		}
	}

	private static ColorBgra AddError (ColorBgra color, double factor, int errorRed, int errorGreen, int errorBlue)
		=> ColorBgra.FromBgra (
			b: Utility.ClampToByte (color.B + (int) (factor * errorBlue)),
			g: Utility.ClampToByte (color.G + (int) (factor * errorGreen)),
			r: Utility.ClampToByte (color.R + (int) (factor * errorRed)),
			a: 255
		);

	private static ColorBgra FindClosestPaletteColor (ImmutableArray<ColorBgra> palette, ColorBgra original)
	{
		double minDistance = double.MaxValue;
		ColorBgra closestColor = ColorBgra.FromBgra (0, 0, 0, 1);
		foreach (var paletteColor in palette) {
			double distance = CalculateDistance (original, paletteColor);
			if (distance >= minDistance) continue;
			minDistance = distance;
			closestColor = paletteColor;
		}
		return closestColor;
	}

	private static double CalculateDistance (ColorBgra color1, ColorBgra color2)
	{
		int deltaR = color1.R - color2.R;
		int deltaG = color1.G - color2.G;
		int deltaB = color1.B - color2.B;
		return Math.Sqrt (deltaR * deltaR + deltaG * deltaG + deltaB * deltaB); // Euclidean distance
	}

	public sealed class ErrorDiffusionMatrix
	{
		private readonly int[,] array_2_d;
		public int Columns { get; }
		public int Rows { get; }
		public int TotalWeight { get; }
		public int ColumnsToLeft { get; }
		public int ColumnsToRight { get; }
		public int RowsBelow { get; }
		public int this[int row, int column] => array_2_d[row, column];
		public ErrorDiffusionMatrix (int[,] array2D, int pixelColumn)
		{
			var clone = (int[,]) array2D.Clone ();
			var rows = clone.GetLength (0);
			if (rows <= 0) throw new ArgumentException ("Array has to have a strictly positive number of rows", nameof (array2D));
			var columns = clone.GetLength (1);
			if (columns <= 0) throw new ArgumentException ("Array has to have a strictly positive number of rows", nameof (array2D));
			if (pixelColumn < 0) throw new ArgumentException ("Argument has to refer to a valid column offset", nameof (pixelColumn));
			if (pixelColumn >= columns) throw new ArgumentException ("Argument has to refer to a valid column offset", nameof (pixelColumn));
			if (clone[0, pixelColumn] != 0) throw new ArgumentException ("Target pixel cannot have a nonzero weight");
			var flattened = Flatten2DArray (clone);
			if (flattened.Any (w => w < 0)) throw new ArgumentException ("No negative weights", nameof (array2D));
			if (flattened.Take (pixelColumn).Any (w => w != 0)) throw new ArgumentException ("Pixels previous to target cannot have nonzero weights");
			ColumnsToLeft = pixelColumn;
			ColumnsToRight = columns - 1 - pixelColumn;
			TotalWeight = flattened.Sum ();
			Columns = columns;
			Rows = rows;
			RowsBelow = rows - 1;
			array_2_d = clone;
		}

		private static IEnumerable<T> Flatten2DArray<T> (T[,] array)
		{
			for (int i = 0; i < array.GetLength (0); i++)
				for (int j = 0; j < array.GetLength (1); j++)
					yield return array[i, j];
		}
	}

	public enum PredefinedPalettes
	{
		OldWindows16,
		WebSafe,
		BlackWhite,
		OldMsPaint,
	}

	private static ImmutableArray<ColorBgra> GetPredefinedPalette (PredefinedPalettes choice)
	{
		return choice switch {
			PredefinedPalettes.OldWindows16 => DefaultPalettes.OldWindows16,
			PredefinedPalettes.WebSafe => DefaultPalettes.WebSafe,
			PredefinedPalettes.BlackWhite => DefaultPalettes.BlackWhite,
			PredefinedPalettes.OldMsPaint => DefaultPalettes.OldMsPaint,
			_ => throw new InvalidEnumArgumentException (nameof (choice), (int) choice, typeof (PredefinedPalettes)),
		};
	}

	public enum PredefinedDiffusionMatrices
	{
		Sierra,
		TwoRowSierra,
		SierraLite,
		Burkes,
		Atkinson,
		Stucki,
		JarvisJudiceNinke,
		FloydSteinberg,
		FakeFloydSteinberg,
	}

	private static ErrorDiffusionMatrix GetPredefinedDiffusionMatrix (PredefinedDiffusionMatrices choice)
	{
		return choice switch {
			PredefinedDiffusionMatrices.Sierra => DefaultMatrices.Sierra,
			PredefinedDiffusionMatrices.TwoRowSierra => DefaultMatrices.TwoRowSierra,
			PredefinedDiffusionMatrices.SierraLite => DefaultMatrices.SierraLite,
			PredefinedDiffusionMatrices.Burkes => DefaultMatrices.Burkes,
			PredefinedDiffusionMatrices.Atkinson => DefaultMatrices.Atkinson,
			PredefinedDiffusionMatrices.Stucki => DefaultMatrices.Stucki,
			PredefinedDiffusionMatrices.JarvisJudiceNinke => DefaultMatrices.JarvisJudiceNinke,
			PredefinedDiffusionMatrices.FloydSteinberg => DefaultMatrices.FloydSteinberg,
			PredefinedDiffusionMatrices.FakeFloydSteinberg => DefaultMatrices.FakeFloydSteinberg,
			_ => throw new InvalidEnumArgumentException (nameof (choice), (int) choice, typeof (PredefinedDiffusionMatrices)),
		};
	}

	public sealed class ForwardErrorDiffusionDitheringData : EffectData
	{
		[Caption ("Diffusion Matrix")]
		public PredefinedDiffusionMatrices DiffusionMatrix { get; set; } = PredefinedDiffusionMatrices.FloydSteinberg;

		[Caption ("Palette")]
		public PredefinedPalettes PaletteChoice { get; set; } = PredefinedPalettes.OldWindows16;
	}

	public static class DefaultMatrices
	{
		public static ErrorDiffusionMatrix Sierra { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Sierra, 2);
		public static ErrorDiffusionMatrix TwoRowSierra { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.TwoRowSierra, 2);
		public static ErrorDiffusionMatrix SierraLite { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.SierraLite, 1);
		public static ErrorDiffusionMatrix Burkes { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Burkes, 2);
		public static ErrorDiffusionMatrix Atkinson { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Atkinson, 1);
		public static ErrorDiffusionMatrix Stucki { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Stucki, 2);
		public static ErrorDiffusionMatrix JarvisJudiceNinke { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.JarvisJudiceNinke, 2);
		public static ErrorDiffusionMatrix FloydSteinberg { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.FloydSteinberg, 1);
		public static ErrorDiffusionMatrix FakeFloydSteinberg { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.FakeFloydSteinberg, 0);
	}

	private static class DefaultMatrixArrays
	{
		public static int[,] Sierra { get; } = {
			{ 0, 0, 0, 5, 3, },
			{ 2, 4, 5, 4, 2, },
			{ 0, 2, 3, 2, 0, },
		};

		public static int[,] TwoRowSierra { get; } = {
			{ 0, 0, 0, 4, 3, },
			{ 1, 2, 3, 2, 1, },
		};

		public static int[,] SierraLite { get; } = {
			{ 0, 0, 2, },
			{ 1, 1, 0, },
		};

		public static int[,] Burkes { get; } = {
			{ 0, 0, 0, 8, 4, },
			{ 2, 4, 8, 4, 2, },
		};

		public static int[,] Atkinson { get; } = {
			{ 0, 0, 1, 1, },
			{ 1, 1, 1, 0, },
			{ 0, 1, 0, 0, },
		};

		public static int[,] Stucki { get; } = {
			{ 0, 0, 0, 8, 4, },
			{ 2, 4, 8, 4, 2, },
			{ 1, 2, 4, 2, 1, },
		};

		public static int[,] JarvisJudiceNinke { get; } = {
			{ 0, 0, 0, 7, 5, },
			{ 3, 5, 7, 5, 3, },
			{ 1, 3, 5, 3, 1, },
		};

		public static int[,] FloydSteinberg { get; } = {
			{ 0, 0, 7, },
			{ 3, 5, 1, }
		};

		public static int[,] FakeFloydSteinberg { get; } = {
			{ 0, 3, },
			{ 3, 2, }
		};
	}

	private static class DefaultPalettes
	{
		public static ImmutableArray<ColorBgra> OldWindows16 => old_windows_16.Value;
		public static ImmutableArray<ColorBgra> WebSafe => web_safe.Value;
		public static ImmutableArray<ColorBgra> BlackWhite { get; }
		public static ImmutableArray<ColorBgra> OldMsPaint => old_ms_paint.Value;

		private static readonly Lazy<ImmutableArray<ColorBgra>> web_safe;
		private static readonly Lazy<ImmutableArray<ColorBgra>> old_windows_16;
		private static readonly Lazy<ImmutableArray<ColorBgra>> old_ms_paint;

		static DefaultPalettes ()
		{
			web_safe = new (() => EnumerateWebSafeColorCube ().ToImmutableArray ());
			old_windows_16 = new (() => EnumerateOldWindowsColors ().ToImmutableArray ());
			BlackWhite = ImmutableArray.CreateRange (new[] { ColorBgra.FromBgr (0, 0, 0), ColorBgra.FromBgr (255, 255, 255) });
			old_ms_paint = new (() => EnumerateOldMsPaintColors ().ToImmutableArray ());
		}

		private static IEnumerable<ColorBgra> EnumerateOldWindowsColors ()
		{
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
