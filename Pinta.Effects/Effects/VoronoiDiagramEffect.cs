using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class VoronoiDiagramEffect : BaseEffect
{
	// TODO: Icon

	public override bool IsTileable => false;

	public override string Name => Translations.GetString ("Voronoi Diagram");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Render");

	public VoronoiDiagramData Data => (VoronoiDiagramData) EffectData!; // NRT - Set in constructor

	public VoronoiDiagramEffect ()
	{
		EffectData = new VoronoiDiagramData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	private sealed record VoronoiSettings (
		int w,
		int h,
		bool showPoints,
		ImmutableArray<PointI> points,
		ImmutableArray<ColorBgra> colors,
		Func<PointI, PointI, double> distanceCalculator);

	private VoronoiSettings CreateSettings (ImageSurface dst, RectangleI roi)
	{
		ColorSorting colorSorting = Data.ColorSorting;

		IEnumerable<PointI> basePoints = CreatePoints (roi, Data.PointCount, Data.PointLocationsSeed);
		ImmutableArray<PointI> points = SortPoints (basePoints, colorSorting).ToImmutableArray ();

		IEnumerable<ColorBgra> baseColors = CreateColors (points.Length, Data.ColorsSeed);
		IEnumerable<ColorBgra> positionSortedColors = SortColors (baseColors, colorSorting);
		IEnumerable<ColorBgra> reversedSortingColors = Data.ReverseColorSorting ? positionSortedColors.Reverse () : positionSortedColors;

		return new (
			w: dst.Width,
			h: dst.Height,
			showPoints: Data.ShowPoints,
			points: points,
			colors: reversedSortingColors.ToImmutableArray (),
			distanceCalculator: GetDistanceCalculator (Data.DistanceCalculationMethod)
		);
	}

	protected override void Render (ImageSurface src, ImageSurface dst, RectangleI roi)
	{
		VoronoiSettings settings = CreateSettings (dst, roi);

		Span<ColorBgra> dst_data = dst.GetPixelData ();

		for (int y = roi.Top; y <= roi.Bottom; y++) {
			var dst_row = dst_data.Slice (y * settings.w, settings.w);
			for (int x = roi.Left; x <= roi.Right; x++) {
				PointI pixelLocation = new (x, y);
				double shortestDistance = double.MaxValue;
				int closestIndex = 0;
				for (var i = 0; i < settings.points.Length; i++) {
					var point = settings.points[i];
					double distance = settings.distanceCalculator (point, pixelLocation);
					if (distance > shortestDistance) continue;
					shortestDistance = distance;
					closestIndex = i;
				}
				dst_row[x] =
					settings.showPoints && shortestDistance == 0
					? ColorBgra.Black
					: settings.colors[closestIndex];
			}
		}
	}

	private static IEnumerable<ColorBgra> SortColors (IEnumerable<ColorBgra> baseColors, ColorSorting colorSorting)
	{
		switch (colorSorting) {
			case ColorSorting.Random:
				return baseColors;
			case ColorSorting.HorizontalBGR:
			case ColorSorting.VerticalBGR:
				return baseColors.OrderBy (p => p.B).ThenBy (p => p.G).ThenBy (p => p.R);
			case ColorSorting.HorizontalBRG:
			case ColorSorting.VerticalBRG:
				return baseColors.OrderBy (p => p.B).ThenBy (p => p.R).ThenBy (p => p.G);
			case ColorSorting.HorizontalGBR:
			case ColorSorting.VerticalGBR:
				return baseColors.OrderBy (p => p.G).ThenBy (p => p.B).ThenBy (p => p.R);
			case ColorSorting.HorizontalGRB:
			case ColorSorting.VerticalGRB:
				return baseColors.OrderBy (p => p.G).ThenBy (p => p.R).ThenBy (p => p.B);
			case ColorSorting.HorizontalRBG:
			case ColorSorting.VerticalRBG:
				return baseColors.OrderBy (p => p.R).ThenBy (p => p.B).ThenBy (p => p.G);
			case ColorSorting.HorizontalRGB:
			case ColorSorting.VerticalRGB:
				return baseColors.OrderBy (p => p.R).ThenBy (p => p.G).ThenBy (p => p.B);
			default:
				throw new InvalidEnumArgumentException (nameof (baseColors), (int) colorSorting, typeof (ColorSorting));
		}
	}

	private static IEnumerable<PointI> SortPoints (IEnumerable<PointI> basePoints, ColorSorting colorSorting)
	{
		switch (colorSorting) {

			case ColorSorting.Random:
				return basePoints;

			case ColorSorting.HorizontalBGR:
			case ColorSorting.HorizontalBRG:
			case ColorSorting.HorizontalGBR:
			case ColorSorting.HorizontalGRB:
			case ColorSorting.HorizontalRBG:
			case ColorSorting.HorizontalRGB:
				return basePoints.OrderBy (p => p.X);

			case ColorSorting.VerticalBGR:
			case ColorSorting.VerticalBRG:
			case ColorSorting.VerticalGBR:
			case ColorSorting.VerticalGRB:
			case ColorSorting.VerticalRBG:
			case ColorSorting.VerticalRGB:
				return basePoints.OrderBy (p => p.Y);

			default:
				throw new InvalidEnumArgumentException (nameof (colorSorting), (int) colorSorting, typeof (ColorSorting));
		}
	}

	private static Func<PointI, PointI, double> GetDistanceCalculator (DistanceCalculationMethod distanceCalculationMethod)
	{
		return distanceCalculationMethod switch {
			DistanceCalculationMethod.Euclidean => Euclidean,
			DistanceCalculationMethod.Manhattan => Manhattan,
			DistanceCalculationMethod.Chebyshev => Chebyshev,
			_ => throw new InvalidEnumArgumentException (nameof (distanceCalculationMethod), (int) distanceCalculationMethod, typeof (DistanceCalculationMethod)),
		};

		static double Euclidean (PointI targetPoint, PointI pixelLocation)
		{
			PointI difference = pixelLocation - targetPoint;
			return difference.Magnitude ();
		}

		static double Manhattan (PointI targetPoint, PointI pixelLocation)
		{
			PointI difference = pixelLocation - targetPoint;
			return Math.Abs (difference.X) + Math.Abs (difference.Y);
		}

		static double Chebyshev (PointI targetPoint, PointI pixelLocation)
		{
			PointI difference = pixelLocation - targetPoint;
			return Math.Max (Math.Abs (difference.X), Math.Abs (difference.Y));
		}
	}

	private static ImmutableHashSet<PointI> CreatePoints (RectangleI roi, int pointCount, RandomSeed pointLocationsSeed)
	{
		int effectivePointCount = Math.Min (pointCount, roi.Width * roi.Height);

		Random randomPositioner = new (pointLocationsSeed.Value);
		var result = ImmutableHashSet.CreateBuilder<PointI> (); // Ensures points' uniqueness

		while (result.Count < effectivePointCount) {

			PointI point = new (
				X: randomPositioner.Next (roi.Left, roi.Right + 1),
				Y: randomPositioner.Next (roi.Top, roi.Bottom + 1)
			);

			result.Add (point);
		}

		return result.ToImmutable ();
	}

	private static IEnumerable<ColorBgra> CreateColors (int colorCount, RandomSeed colorsSeed)
	{
		Random randomColorizer = new (colorsSeed.Value);
		HashSet<ColorBgra> uniquenessTracker = new ();
		while (uniquenessTracker.Count < colorCount) {
			ColorBgra candidateColor = randomColorizer.RandomColorBgra ();
			if (uniquenessTracker.Contains (candidateColor)) continue;
			uniquenessTracker.Add (candidateColor);
			yield return candidateColor;
		}
	}

	public sealed class VoronoiDiagramData : EffectData
	{
		[Caption ("Distance Calculation Method")]
		public DistanceCalculationMethod DistanceCalculationMethod { get; set; } = DistanceCalculationMethod.Euclidean;

		[Caption ("Point Count"), MinimumValue (1), MaximumValue (1024)]
		public int PointCount { get; set; } = 100;

		// Translators: The user can choose whether or not to render the points used in the calculation of a Voronoi diagram
		[Caption ("Show Points")]
		public bool ShowPoints { get; set; } = false;

		[Caption ("Color Sorting")]
		public ColorSorting ColorSorting { get; set; } = ColorSorting.Random;

		// Translators: In this context, "reverse" is a verb, and the user can choose whether or not they want to reverse the color sorting
		[Caption ("Reverse Color Sorting")]
		public bool ReverseColorSorting { get; set; } = false;

		[Caption ("Colors Seed")]
		public RandomSeed ColorsSeed { get; set; } = new (0);

		[Caption ("Point Locations Seed")]
		public RandomSeed PointLocationsSeed { get; set; } = new (0);
	}

	public enum DistanceCalculationMethod
	{
		Euclidean,
		Manhattan,
		Chebyshev,
	}

	public enum ColorSorting
	{
		[Caption ("Random Color Sorting")] Random,



		// Translators: Horizontal color sorting with B, then G as the leading terms
		[Caption ("Horizontal (B, G, R)")]
		HorizontalBGR,

		// Translators: Horizontal color sorting with B, then R as the leading terms
		[Caption ("Horizontal (B, R, G)")]
		HorizontalBRG,

		// Translators: Horizontal color sorting with G, then B as the leading terms
		[Caption ("Horizontal (G, B, R)")]
		HorizontalGBR,

		// Translators: Horizontal color sorting with G, then R as the leading terms
		[Caption ("Horizontal (G, R, B)")]
		HorizontalGRB,

		// Translators: Horizontal color sorting with R, then B as the leading terms
		[Caption ("Horizontal (R, B, G)")]
		HorizontalRBG,

		// Translators: Horizontal color sorting with R, then G as the leading terms
		[Caption ("Horizontal (R, G, B)")]
		HorizontalRGB,



		// Translators: Vertical color sorting with B, then G as the leading terms
		[Caption ("Vertical (B, G, R)")]
		VerticalBGR,

		// Translators: Vertical color sorting with B, then R as the leading terms
		[Caption ("Vertical (B, R, G)")]
		VerticalBRG,

		// Translators: Vertical color sorting with G, then B as the leading terms
		[Caption ("Vertical (G, B, R)")]
		VerticalGBR,

		// Translators: Vertical color sorting with G, then R as the leading terms
		[Caption ("Vertical (G, R, B)")]
		VerticalGRB,

		// Translators: Vertical color sorting with R, then B as the leading terms
		[Caption ("Vertical (R, B, G)")]
		VerticalRBG,

		// Translators: Vertical color sorting with R, then G as the leading terms
		[Caption ("Vertical (R, G, B)")]
		VerticalRGB,
	}
}
