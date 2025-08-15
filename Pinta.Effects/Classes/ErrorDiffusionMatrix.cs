using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Pinta.Core;

namespace Pinta.Effects;

public enum PredefinedDiffusionMatrices
{
	// Translators: Image dithering matrix named after Frankie Sierra
	[Caption ("Sierra")]
	Sierra,

	// Translators: Image dithering matrix named after Frankie Sierra
	[Caption ("Two-Row Sierra")]
	TwoRowSierra,

	// Translators: Image dithering matrix named after Frankie Sierra
	[Caption ("Sierra Lite")]
	SierraLite,

	// Translators: Image dithering matrix named after Daniel Burkes
	[Caption ("Burkes")]
	Burkes,

	// Translators: Image dithering matrix named after Bill Atkinson
	[Caption ("Atkinson")]
	Atkinson,

	// Translators: Image dithering matrix named after Peter Stucki
	[Caption ("Stucki")]
	Stucki,

	// Translators: Image dithering matrix named after J. F. Jarvis, C. N. Judice, and W. H. Ninke
	[Caption ("Jarvis-Judice-Ninke")]
	JarvisJudiceNinke,

	// Translators: Image dithering matrix named after Robert W. Floyd and Louis Steinberg
	[Caption ("Floyd-Steinberg")]
	FloydSteinberg,

	// Translators: Image dithering matrix named after Robert W. Floyd and Louis Steinberg. Some software may use it and call it Floyd-Steinberg, but it's not the actual Floyd-Steinberg matrix
	[Caption ("Floyd-Steinberg Lite")]
	FalseFloydSteinberg,
}

/// <summary>
/// Represents the matrix that is used by the dithering algorithm
/// in order to propagate the error (defined as the difference
/// between a pixel's color and the color in a certain palette that is
/// closest to it) forward.
/// </summary>
internal sealed class ErrorDiffusionMatrix
{
	public static ErrorDiffusionMatrix GetPredefined (PredefinedDiffusionMatrices choice)
	{
		return choice switch {
			PredefinedDiffusionMatrices.Sierra => Predefined.Sierra,
			PredefinedDiffusionMatrices.TwoRowSierra => Predefined.TwoRowSierra,
			PredefinedDiffusionMatrices.SierraLite => Predefined.SierraLite,
			PredefinedDiffusionMatrices.Burkes => Predefined.Burkes,
			PredefinedDiffusionMatrices.Atkinson => Predefined.Atkinson,
			PredefinedDiffusionMatrices.Stucki => Predefined.Stucki,
			PredefinedDiffusionMatrices.JarvisJudiceNinke => Predefined.JarvisJudiceNinke,
			PredefinedDiffusionMatrices.FloydSteinberg => Predefined.FloydSteinberg,
			PredefinedDiffusionMatrices.FalseFloydSteinberg => Predefined.FakeFloydSteinberg,
			_ => throw new InvalidEnumArgumentException (nameof (choice), (int) choice, typeof (PredefinedDiffusionMatrices)),
		};
	}

	public static class Predefined
	{
		public static ErrorDiffusionMatrix Sierra { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Sierra, 2);
		public static ErrorDiffusionMatrix TwoRowSierra { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.TwoRowSierra, 2);
		public static ErrorDiffusionMatrix SierraLite { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.SierraLite, 1);
		public static ErrorDiffusionMatrix Burkes { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Burkes, 2);
		public static ErrorDiffusionMatrix Atkinson { get; } = new ErrorDiffusionMatrix (DefaultMatrixArrays.Atkinson, 1, weightReductionFactor: 1.0 / 8.0);
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

	private readonly int[,] array_2_d;
	public int Columns { get; }
	public int Rows { get; }
	public double WeightReductionFactor { get; }
	public int ColumnsToLeft { get; }
	public int ColumnsToRight { get; }
	public int RowsBelow { get; }
	public int this[int row, int column] => array_2_d[row, column];
	public ErrorDiffusionMatrix (int[,] array2D, int pixelColumn, double? weightReductionFactor = null)
	{
		var clone = (int[,]) array2D.Clone ();
		int rows = clone.GetLength (0);
		if (rows <= 0) throw new ArgumentException ("Array has to have a strictly positive number of rows", nameof (array2D));
		int columns = clone.GetLength (1);
		if (columns <= 0) throw new ArgumentException ("Array has to have a strictly positive number of rows", nameof (array2D));
		if (pixelColumn < 0) throw new ArgumentException ("Argument has to refer to a valid column offset", nameof (pixelColumn));
		if (pixelColumn >= columns) throw new ArgumentException ("Argument has to refer to a valid column offset", nameof (pixelColumn));
		if (clone[0, pixelColumn] != 0) throw new ArgumentException ("Target pixel cannot have a nonzero weight");
		var flattened = Flatten2DArray (clone);
		if (flattened.Any (w => w < 0)) throw new ArgumentException ("No negative weights", nameof (array2D));
		if (flattened.Take (pixelColumn).Any (w => w != 0)) throw new ArgumentException ("Pixels previous to target cannot have nonzero weights");
		ColumnsToLeft = pixelColumn;
		ColumnsToRight = columns - 1 - pixelColumn;
		WeightReductionFactor = weightReductionFactor ?? (1.0 / flattened.Sum ());
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
