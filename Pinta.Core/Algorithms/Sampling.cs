using System;
using System.Collections.Immutable;

namespace Pinta.Core;

public static class Sampling
{
	/// <returns>
	/// Offsets, from top left corner of points,
	/// where samples should be taken.
	/// </returns>
	public static ImmutableArray<PointD> CreateSamplingOffsets (int quality)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan (quality, 1);

		var builder = ImmutableArray.CreateBuilder<PointD> ();
		builder.Capacity = quality * quality;
		double sectionSize = 1.0 / quality;
		double initial = sectionSize / 2;

		for (int h = 0; h < quality; h++) {
			for (int v = 0; v < quality; v++) {
				double hOffset = sectionSize * h;
				double vOffset = sectionSize * v;
				PointD currentPoint = new (
					X: initial + hOffset,
					Y: initial + vOffset);
				builder.Add (currentPoint);
			}
		}

		return builder.MoveToImmutable ();
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
