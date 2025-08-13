using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;

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

	public static IEnumerable<PointD> CreateCellControlPoints (
		RectangleI roi,
		int pointCount,
		RandomSeed pointLocationsSeed,
		PointArrangement pointArrangement)
	{
		ArgumentOutOfRangeException.ThrowIfNegative (pointCount);

		if (pointCount > roi.Width * roi.Height)
			throw new ArgumentException ($"Requested more control points via {nameof (pointCount)} than pixels in {nameof (roi)}");

		if (pointCount == 0)
			return [];

		return pointArrangement switch {
			PointArrangement.Random => CreateRandomPoints (roi, pointCount, pointLocationsSeed),
			PointArrangement.Circle => CreateCirclePoints (roi, pointCount),
			PointArrangement.Phyllotaxis => CreateFibonacciSpiralPoints (roi, pointCount),
			_ => throw new InvalidEnumArgumentException (nameof (pointArrangement), (int) pointArrangement, typeof (PointArrangement)),
		};
	}

	private static readonly PointD centering_offset = new (0.5, 0.5);
	private static PointD[] CreateRandomPoints (RectangleI roi, int pointCount, RandomSeed pointLocationsSeed)
	{
		Random randomPositioner = new (pointLocationsSeed.Value);

		var uniquenessTracker = ImmutableHashSet.CreateBuilder<PointI> ();

		while (uniquenessTracker.Count < pointCount) {

			PointI point = new (
				X: randomPositioner.Next (roi.Left, roi.Right + 1),
				Y: randomPositioner.Next (roi.Top, roi.Bottom + 1));

			uniquenessTracker.Add (point);
		}

		return [.. uniquenessTracker.Select (p => p.ToDouble () + centering_offset)];
	}

	private static PointD[] CreateCirclePoints (RectangleI roi, int pointCount)
	{
		PointD center = ComputeCenter (roi);
		double radius = Math.Min (roi.Width, roi.Height) / 2.0;
		double anglePortion = RadiansAngle.FullTurn / pointCount;
		PointD[] points = new PointD[pointCount];
		for (int i = 0; i < pointCount; i++) {
			double angle = anglePortion * i;
			points[i] = new PointD (
				X: center.X + radius * Math.Cos (angle),
				Y: center.Y + radius * Math.Sin (angle));
		}
		return points;
	}

	private static readonly RadiansAngle golden_angle = new (Math.PI * (3.0 - Math.Sqrt (5.0)));
	private static PointD[] CreateFibonacciSpiralPoints (RectangleI roi, int pointCount)
	{
		PointD center = ComputeCenter (roi);
		double maxRadius = Math.Min (roi.Width, roi.Height) / 2.0;
		double inverseCount = 1.0 / pointCount;
		PointD[] points = new PointD[pointCount];
		for (int i = 0; i < pointCount; i++) {
			double radius = Math.Sqrt (i * inverseCount) * maxRadius;
			double angle = i * golden_angle.Radians;
			points[i] = new PointD (
				X: center.X + radius * Math.Cos (angle),
				Y: center.Y + radius * Math.Sin (angle));
		}
		return points;
	}

	private static PointD ComputeCenter (RectangleI roi)
	{
		return new (
			X: Mathematics.Lerp (roi.Left, roi.Right, 0.5),
			Y: Mathematics.Lerp (roi.Top, roi.Bottom, 0.5));
	}
}

public enum PointArrangement
{
	Random,
	Circle,
	Phyllotaxis,
}
