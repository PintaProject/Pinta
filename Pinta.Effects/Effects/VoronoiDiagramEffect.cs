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

	protected override void Render (ImageSurface src, ImageSurface dst, RectangleI roi)
	{
		int w = dst.Width;
		int h = dst.Height;

		ColorSorting colorSorting = Data.ColorSorting;

		ImmutableArray<PointI> points =
			SortPoints (
				CreatePoints (
					roi,
					Data.PointCount,
					Data.PointLocationsSeed),
				colorSorting
			)
			.ToImmutableArray ();

		ImmutableArray<ColorBgra> colors =
			SortColors (
				CreateColors (
					points.Length,
					Data.ColorsSeed),
				colorSorting
			)
			.ToImmutableArray ();

		Func<PointI, PointI, double> distanceCalculator = GetDistanceCalculator (Data.DistanceCalculationMethod);

		Span<ColorBgra> dst_data = dst.GetPixelData ();

		for (int y = roi.Top; y <= roi.Bottom; y++) {
			var dst_row = dst_data.Slice (y * w, w);
			for (int x = roi.Left; x <= roi.Right; x++) {
				PointI pixelLocation = new (x, y);
				double shortestDistance = double.MaxValue;
				int closestIndex = 0;
				for (var i = 0; i < points.Length; i++) {
					var point = points[i];
					double distance = distanceCalculator (point, pixelLocation);
					if (distance > shortestDistance) continue;
					shortestDistance = distance;
					closestIndex = i;
				}
				dst_row[x] = colors[closestIndex];
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

		[Caption ("Color Sorting")]
		public ColorSorting ColorSorting { get; set; } = ColorSorting.Random;

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
		Random,

		HorizontalBGR,
		HorizontalBRG,
		HorizontalGBR,
		HorizontalGRB,
		HorizontalRBG,
		HorizontalRGB,

		VerticalBGR,
		VerticalBRG,
		VerticalGBR,
		VerticalGRB,
		VerticalRBG,
		VerticalRGB,
	}
}
