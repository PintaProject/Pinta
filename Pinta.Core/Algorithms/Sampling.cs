using System.Collections.Immutable;

namespace Pinta.Core;

public static class Sampling
{
	/// <returns>
	/// Offsets, from top left corner of points,
	/// where samples should be taken.
	/// </returns>
	public static ImmutableArray<PointD> CreateSamplingLocations (int quality)
	{
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
}
