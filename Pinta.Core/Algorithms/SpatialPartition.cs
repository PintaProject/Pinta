using System;
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
}
