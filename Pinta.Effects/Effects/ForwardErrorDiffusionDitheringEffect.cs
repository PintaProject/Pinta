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
	public override string Name => Translations.GetString ("Dithering");
	public override bool IsConfigurable => true;
	// TODO: Icon
	public override string EffectMenuCategory => Translations.GetString ("Test"); // TODO:
	public ForwardErrorDiffusionDitheringData Data => (ForwardErrorDiffusionDitheringData) EffectData!; // NRT - Set in constructor

	public ForwardErrorDiffusionDitheringEffect ()
	{
		EffectData = new ForwardErrorDiffusionDitheringData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		//var src_data = src.GetReadOnlyPixelData ();
		var dst_data = dest.GetPixelData ();
		foreach (var rect in rois) {
			for (int y = rect.Top; y <= rect.Bottom; y++) {
				for (int x = rect.Left; x <= rect.Right; x++) {
					var currentIndex = y * src.Width + x;
					var originalPixel = dst_data[currentIndex];
					var closestColor = FindClosestPaletteColor (originalPixel);
					dst_data[currentIndex] = closestColor;
					int errorRed = originalPixel.R - closestColor.R;
					int errorGreen = originalPixel.G - closestColor.G;
					int errorBlue = originalPixel.B - closestColor.B;
					DistributeError (dst_data, x, y, errorRed, errorGreen, errorBlue, src.Width, src.Height);
				}
			}
		}
	}

	private void DistributeError (Span<ColorBgra> original, int x, int y, int errorRed, int errorGreen, int errorBlue, int sourceWidth, int sourceHeight)
	{
		var diffusionMatrix = GetPredefinedDiffusionMatrix (Data.DiffusionMatrix);
		for (int r = 0; r < diffusionMatrix.Rows; r++) {
			for (int c = 0; c < diffusionMatrix.Columns; c++) {
				if (diffusionMatrix[r, c] is not WeightElement weight)
					continue;
				var this_y = y + r;
				var this_x = x + c - diffusionMatrix.ColumnsToLeft;
				if (this_x < 0)
					continue;
				if (this_y < 0)
					continue;
				if (this_x >= sourceWidth)
					continue;
				if (this_y >= sourceHeight)
					continue;
				int idx = (this_y * sourceWidth) + this_x;
				double factor = ((double) weight.Weight) / diffusionMatrix.TotalWeight;
				original[idx] = AddError (original[idx], factor, errorRed, errorGreen, errorBlue);
			}
		}
	}

	private static ColorBgra AddError (ColorBgra color, double factor, int errorRed, int errorGreen, int errorBlue)
	{
		// This function will add the error to the color based on the provided factor
		byte newR = Utility.ClampToByte (color.R + (int) (factor * errorRed));
		byte newG = Utility.ClampToByte (color.G + (int) (factor * errorGreen));
		byte newB = Utility.ClampToByte (color.B + (int) (factor * errorBlue));
		return ColorBgra.FromBgra (newB, newG, newR, 255);
	}

	private ColorBgra FindClosestPaletteColor (ColorBgra original)
	{
		double minDistance = double.MaxValue;
		ColorBgra closestColor = ColorBgra.FromBgra (0, 0, 0, 1);
		var palette = GetPredefinedPalette (Data.Palette);
		foreach (var paletteColor in palette) {
			double distance = CalculateDistance (original, paletteColor);
			if (distance < minDistance) {
				minDistance = distance;
				closestColor = paletteColor;
			}
		}
		return closestColor;
	}

	private static double CalculateDistance (ColorBgra color1, ColorBgra color2)
	{
		int deltaR = color1.R - color2.R;
		int deltaG = color1.G - color2.G;
		int deltaB = color1.B - color2.B;

		// Euclidean distance
		return Math.Sqrt (deltaR * deltaR + deltaG * deltaG + deltaB * deltaB);
	}

	public sealed class ErrorDiffusionMatrix
	{
		private readonly DiffusionMatrixElement[,] array_2_d;
		public int Columns { get; }
		public int Rows { get; }
		public int TotalWeight { get; }
		public int ColumnsToLeft { get; }
		public int ColumnsToRight { get; }
		public int RowsBelow { get; }
		public DiffusionMatrixElement this[int row, int column] => array_2_d[row, column];
		public ErrorDiffusionMatrix (DiffusionMatrixElement[,] array2D)
		{
			var clone = (DiffusionMatrixElement[,]) array2D.Clone ();
			var firstRow = ReadRow (clone, 0);
			var firstRowTarget = firstRow.OfType<TargetPixelElement> ().Count ();
			if (firstRowTarget != 1) throw new ArgumentException ($"First row has to contain exactly one element of type {nameof (TargetPixelElement)}");
			var flattened = Flatten2DArray (clone);
			var targetPixels = flattened.OfType<TargetPixelElement> ().Count ();
			if (targetPixels != 1) throw new ArgumentException ($"Array has to contain exactly one element of type {nameof (TargetPixelElement)}");
			var targetPixelOffset = FirstIndexOfPixel (clone);
			var columns = clone.GetLength (1);
			var rows = clone.GetLength (0);
			ColumnsToLeft = targetPixelOffset;
			ColumnsToRight = columns - 1 - targetPixelOffset;
			TotalWeight = flattened.OfType<WeightElement> ().Select (w => w.Weight).Sum ();
			Columns = columns;
			Rows = rows;
			RowsBelow = rows - 1;
			array_2_d = clone;
		}

		private static int FirstIndexOfPixel (DiffusionMatrixElement[,] array2D)
		{
			var columns = array2D.GetLength (1);
			for (int i = 0; i < columns; i++) {
				if (array2D[0, i].ElementType == DiffusionElementType.TargetPixel)
					return i;
			}
			throw new ArgumentException ($"No item of type {nameof (TargetPixelElement)} found in first row");
		}

		private static IEnumerable<T> ReadRow<T> (T[,] array, int row)
		{
			var rows = array.GetLength (0);
			if (row >= rows) throw new ArgumentOutOfRangeException (nameof (row));
			var columns = array.GetLength (1);
			for (int i = 0; i < columns; i++) {
				yield return array[row, i];
			}
		}

		private static IEnumerable<T> Flatten2DArray<T> (T[,] array)
		{
			for (int i = 0; i < array.GetLength (0); i++) {
				for (int j = 0; j < array.GetLength (1); j++) {
					yield return array[i, j];
				}
			}
		}
	}

	public enum DiffusionElementType
	{
		TargetPixel,
		Ignore,
		Weight,
	}

	public abstract class DiffusionMatrixElement
	{
		public abstract DiffusionElementType ElementType { get; }
	}

	public sealed class TargetPixelElement : DiffusionMatrixElement
	{
		public static TargetPixelElement Instance { get; } = new TargetPixelElement ();
		public override DiffusionElementType ElementType => DiffusionElementType.TargetPixel;
		private TargetPixelElement () { }
	}

	public sealed class IgnoreElement : DiffusionMatrixElement
	{
		public static IgnoreElement Instance { get; } = new IgnoreElement ();
		public override DiffusionElementType ElementType => DiffusionElementType.Ignore;
		private IgnoreElement () { }
	}

	public sealed class WeightElement : DiffusionMatrixElement
	{
		public override DiffusionElementType ElementType => DiffusionElementType.Weight;
		public int Weight { get; }

		public WeightElement (int weight)
		{
			if (weight <= 0) throw new ArgumentOutOfRangeException (nameof (weight), "Weight must be positive");
			Weight = weight;
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
		switch (choice) {
			case PredefinedPalettes.OldWindows16:
				return DefaultPalettes.OldWindows16;
			case PredefinedPalettes.WebSafe:
				return DefaultPalettes.WebSafe;
			case PredefinedPalettes.BlackWhite:
				return DefaultPalettes.BlackWhite;
			case PredefinedPalettes.OldMsPaint:
				return DefaultPalettes.OldMsPaint;
			default:
				throw new InvalidEnumArgumentException (nameof (choice), (int) choice, typeof (PredefinedPalettes));
		}
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
		switch (choice) {
			case PredefinedDiffusionMatrices.Sierra:
				return DefaultMatrices.Sierra;
			case PredefinedDiffusionMatrices.TwoRowSierra:
				return DefaultMatrices.TwoRowSierra;
			case PredefinedDiffusionMatrices.SierraLite:
				return DefaultMatrices.SierraLite;
			case PredefinedDiffusionMatrices.Burkes:
				return DefaultMatrices.Burkes;
			case PredefinedDiffusionMatrices.Atkinson:
				return DefaultMatrices.Atkinson;
			case PredefinedDiffusionMatrices.Stucki:
				return DefaultMatrices.Stucki;
			case PredefinedDiffusionMatrices.JarvisJudiceNinke:
				return DefaultMatrices.JarvisJudiceNinke;
			case PredefinedDiffusionMatrices.FloydSteinberg:
				return DefaultMatrices.FloydSteinberg;
			case PredefinedDiffusionMatrices.FakeFloydSteinberg:
				return DefaultMatrices.FakeFloydSteinberg;
			default:
				throw new InvalidEnumArgumentException (nameof (choice), (int) choice, typeof (PredefinedDiffusionMatrices));
		}
	}

	public sealed class ForwardErrorDiffusionDitheringData : EffectData
	{
		[Caption ("Diffusion Matrix")]
		public PredefinedDiffusionMatrices DiffusionMatrix { get; set; } = PredefinedDiffusionMatrices.FloydSteinberg;

		[Caption ("Palette")]
		public PredefinedPalettes Palette { get; set; } = PredefinedPalettes.OldWindows16;
	}

	public static class DefaultMatrices
	{
		public static ErrorDiffusionMatrix Sierra { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Sierra);
		public static ErrorDiffusionMatrix TwoRowSierra { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.TwoRowSierra);
		public static ErrorDiffusionMatrix SierraLite { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.SierraLite);
		public static ErrorDiffusionMatrix Burkes { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Burkes);
		public static ErrorDiffusionMatrix Atkinson { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Atkinson);
		public static ErrorDiffusionMatrix Stucki { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Stucki);
		public static ErrorDiffusionMatrix JarvisJudiceNinke { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.JarvisJudiceNinke);
		public static ErrorDiffusionMatrix FloydSteinberg { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.FloydSteinberg);
		public static ErrorDiffusionMatrix FakeFloydSteinberg { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.FakeFloydSteinberg);
	}

	private static class DefaultMatrixArrays
	{
		public static DiffusionMatrixElement[,] Sierra { get; } = {
		    { IgnoreElement.Instance, IgnoreElement.Instance, TargetPixelElement.Instance, new WeightElement(5), new WeightElement(3), },
		    { new WeightElement(2), new WeightElement(4), new WeightElement(5), new WeightElement(4), new WeightElement(2), },
		    { IgnoreElement.Instance, new WeightElement(2), new WeightElement(3), new WeightElement(2), IgnoreElement.Instance, },
		};

		public static DiffusionMatrixElement[,] TwoRowSierra { get; } = {
		    { IgnoreElement.Instance, IgnoreElement.Instance, TargetPixelElement.Instance, new WeightElement(4), new WeightElement(3), },
		    { new WeightElement(1), new WeightElement(2), new WeightElement(3), new WeightElement(2), new WeightElement(1), },
		};

		public static DiffusionMatrixElement[,] SierraLite { get; } = {
		    { IgnoreElement.Instance, TargetPixelElement.Instance, new WeightElement(2), },
		    { new WeightElement(1), new WeightElement(1), IgnoreElement.Instance, },
		};

		public static DiffusionMatrixElement[,] Burkes { get; } = {
		    { IgnoreElement.Instance, IgnoreElement.Instance, TargetPixelElement.Instance, new WeightElement(8), new WeightElement(4), },
		    { new WeightElement(2), new WeightElement(4), new WeightElement(8), new WeightElement(4), new WeightElement(2), },
		};

		public static DiffusionMatrixElement[,] Atkinson { get; } = {
		    { IgnoreElement.Instance, TargetPixelElement.Instance, new WeightElement(1), new WeightElement(1), },
		    { new WeightElement(1), new WeightElement(1), new WeightElement(1), IgnoreElement.Instance, },
		    { IgnoreElement.Instance, new WeightElement(1), IgnoreElement.Instance, IgnoreElement.Instance, },
		};

		public static DiffusionMatrixElement[,] Stucki { get; } = {
		    { IgnoreElement.Instance, IgnoreElement.Instance, TargetPixelElement.Instance, new WeightElement(8), new WeightElement(4), },
		    { new WeightElement(2), new WeightElement(4), new WeightElement(8), new WeightElement(4), new WeightElement(2), },
		    { new WeightElement(1), new WeightElement(2), new WeightElement(4), new WeightElement(2), new WeightElement(1), },
		};

		public static DiffusionMatrixElement[,] JarvisJudiceNinke { get; } = {
		    { IgnoreElement.Instance, IgnoreElement.Instance, TargetPixelElement.Instance, new WeightElement(7), new WeightElement(5), },
		    { new WeightElement(3), new WeightElement(5), new WeightElement(7), new WeightElement(5), new WeightElement(3), },
		    { new WeightElement(1), new WeightElement(3), new WeightElement(5), new WeightElement(3), new WeightElement(1), },
		};

		public static DiffusionMatrixElement[,] FloydSteinberg { get; } = {
		    { IgnoreElement.Instance, TargetPixelElement.Instance, new WeightElement(7), },
		    { new WeightElement(3), new WeightElement(5), new WeightElement(1), }
		};

		public static DiffusionMatrixElement[,] FakeFloydSteinberg { get; } = {
		    { TargetPixelElement.Instance, new WeightElement(3), },
		    { new WeightElement(3), new WeightElement(2), }
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
			// https://wiki.vg-resource.com/
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
			for (short r = 0; r <= 255; r += 51) {
				for (short g = 0; g <= 255; g += 51) {
					for (short b = 0; b <= 255; b += 51) {
						yield return ColorBgra.FromBgr ((byte) b, (byte) g, (byte) r);
					}
				}
			}
		}
	}
}
