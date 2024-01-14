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

		ImmutableArray<PointI> points =
			CreatePoints (roi, Data.PointCount, Data.PointLocationsSeed)
			.OrderBy (p => p.X)
			.ToImmutableArray ();

		ImmutableArray<ColorBgra> colors = CreateColors (points.Length, Data.ColorsSeed).ToImmutableArray ();

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

	private static ImmutableHashSet<ColorBgra> CreateColors (int colorCount, RandomSeed colorsSeed)
	{
		Random randomColorizer = new (colorsSeed.Value);
		var result = ImmutableHashSet.CreateBuilder<ColorBgra> (); // Ensures points' uniqueness

		while (result.Count < colorCount)
			result.Add (randomColorizer.RandomColorBgra ());

		return result.ToImmutable ();
	}

	public sealed class VoronoiDiagramData : EffectData
	{
		[Caption ("Distance Calculation Method")]
		public DistanceCalculationMethod DistanceCalculationMethod { get; set; } = DistanceCalculationMethod.Euclidean;

		[Caption ("Point Count"), MinimumValue (1), MaximumValue (1024)]
		public int PointCount { get; set; } = 100;

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
}
