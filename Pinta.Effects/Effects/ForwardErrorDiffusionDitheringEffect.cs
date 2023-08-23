using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects.Effects;

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

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		var src_data = src.GetReadOnlyPixelData ();
		var original_data = new ColorBgra[src_data.Length];
		src_data.CopyTo (original_data); // Assume this is a writable copy or original
		var dst_data = dest.GetPixelData ();
		foreach (var rect in rois) {
			for (int y = 0; y < rect.Height - Data.DiffusionMatrix.RowsBelow; y++) {
				for (int x = Data.DiffusionMatrix.ColumnsToLeft; x < rect.Width - Data.DiffusionMatrix.ColumnsToRight; x++) {
					var currentIndex = y * src.Width + x;
					var originalPixel = original_data[currentIndex];
					var closestColor = FindClosestPaletteColor (originalPixel);
					dst_data[currentIndex] = closestColor;
					int errorR = originalPixel.R - closestColor.R;
					int errorG = originalPixel.G - closestColor.G;
					int errorB = originalPixel.B - closestColor.B;
					DistributeError (original_data, x, y, errorR, errorG, errorB, src.Width, src.Height);
				}
			}
		}
	}

	private void DistributeError (Span<ColorBgra> original, int x, int y, int errorR, int errorG, int errorB, int sourceWidth, int sourceHeight)
	{
		for (int r = 0; r < Data.DiffusionMatrix.Rows; r++) {
			for (int c = 0; c < Data.DiffusionMatrix.Columns; c++) {
				if (Data.DiffusionMatrix[r, c] is not WeightElement weight)
					continue;
				var thisY = y + r;
				var thisX = x + c - Data.DiffusionMatrix.ColumnsToLeft;
				if (thisX >= sourceWidth)
					continue;
				if (thisY >= sourceHeight)
					continue;
				int idx = (thisY * sourceWidth) + thisX;
				double factor = ((double) weight.Weight) / Data.DiffusionMatrix.TotalWeight;
				original[idx] = AddError (original[idx], factor, errorR, errorG, errorB);
			}
		}
	}

	private static ColorBgra AddError (ColorBgra color, double factor, int errorR, int errorG, int errorB)
	{
		// This function will add the error to the color based on the provided factor
		byte newR = (byte) Math.Clamp (color.R + (int) (factor * errorR), byte.MinValue, byte.MaxValue);
		byte newG = (byte) Math.Clamp (color.G + (int) (factor * errorG), byte.MinValue, byte.MaxValue);
		byte newB = (byte) Math.Clamp (color.B + (int) (factor * errorB), byte.MinValue, byte.MaxValue);
		return ColorBgra.FromBgra (newB, newG, newR, 255);
	}

	private ColorBgra FindClosestPaletteColor (ColorBgra original)
	{
		double minDistance = double.MaxValue;
		ColorBgra closestColor = ColorBgra.FromBgra (0, 0, 0, 1);
		foreach (var paletteColor in Data.Palette) {
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

	public sealed class ForwardErrorDiffusionDitheringData : EffectData
	{
		[Caption ("Diffusion Matrix")]
		public ErrorDiffusionMatrix DiffusionMatrix { get; set; } = DefaultMatrices.FloydSteinberg;

		private ImmutableArray<ColorBgra> palette = DefaultPalettes.OldWindows16;
		[Caption ("Palette")]
		public ImmutableArray<ColorBgra> Palette {
			get => palette;
			set {
				if (palette.IsDefault) throw new ArgumentException ("Palette array has to be valid", nameof (palette));
				palette = value;
			}
		}
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

		private static readonly Lazy<ImmutableArray<ColorBgra>> web_safe;
		private static readonly Lazy<ImmutableArray<ColorBgra>> old_windows_16;

		static DefaultPalettes ()
		{
			web_safe = new (() => EnumerateWebSafeColorCube ().ToImmutableArray ());
			old_windows_16 = new (() => EnumerateOldWindowsColors ().ToImmutableArray ());
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

		private static IEnumerable<ColorBgra> EnumerateWebSafeColorCube ()
		{
			for (byte r = 0; r <= 255; r += 51) {
				for (byte g = 0; g <= 255; g += 51) {
					for (byte b = 0; b <= 255; b += 51) {
						yield return ColorBgra.FromBgr (b, g, r);
					}
				}
			}
		}
	}
}
