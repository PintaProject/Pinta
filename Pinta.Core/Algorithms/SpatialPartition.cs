using System;
using System.Collections.Immutable;
using System.ComponentModel;

namespace Pinta.Core;

public static class SpatialPartition
{
	public static Func<PointD, PointD, double> GetDistanceCalculator (DistanceMetric distanceCalculationMethod)
	{
		return distanceCalculationMethod switch {
			DistanceMetric.Euclidean => Euclidean,
			DistanceMetric.Manhattan => Manhattan,
			DistanceMetric.Chebyshev => Chebyshev,
			_ => throw new InvalidEnumArgumentException (
				nameof (distanceCalculationMethod),
				(int) distanceCalculationMethod,
				typeof (DistanceMetric)),
		};

		static double Euclidean (PointD targetPoint, PointD pixelLocation)
		{
			PointD difference = pixelLocation - targetPoint;
			return difference.Magnitude ();
		}

		static double Manhattan (PointD targetPoint, PointD pixelLocation)
		{
			PointD difference = pixelLocation - targetPoint;
			return Math.Abs (difference.X) + Math.Abs (difference.Y);
		}

		static double Chebyshev (PointD targetPoint, PointD pixelLocation)
		{
			PointD difference = pixelLocation - targetPoint;
			return Math.Max (Math.Abs (difference.X), Math.Abs (difference.Y));
		}
	}

	public static ImmutableHashSet<PointI> CreateCellControlPoints (RectangleI roi, int pointCount, RandomSeed pointLocationsSeed)
	{
		if (pointCount > roi.Width * roi.Height)
			throw new ArgumentException ($"Requested more control points via {nameof (pointCount)} than pixels in {nameof (roi)}");

		Random randomPositioner = new (pointLocationsSeed.Value);
		var result = ImmutableHashSet.CreateBuilder<PointI> (); // Ensures points' uniqueness

		while (result.Count < pointCount) {

			PointI point = new (
				X: randomPositioner.Next (roi.Left, roi.Right + 1),
				Y: randomPositioner.Next (roi.Top, roi.Bottom + 1)
			);

			result.Add (point);
		}

		return result.ToImmutable ();
	}
}
