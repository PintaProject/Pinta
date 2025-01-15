using System;

namespace Pinta.Core;

public sealed class Julia
{
	private readonly double max_squared;
	private readonly double log2_max;
	public Julia (double maxSquared)
	{
		if (maxSquared <= 0) throw new ArgumentOutOfRangeException (nameof (maxSquared));
		max_squared = maxSquared;
		log2_max = Math.Log (maxSquared);
	}

	public double Compute (PointD jLoc, double r, double i)
	{
		double c = 0;
		while (c < 256 && Utility.MagnitudeSquared (jLoc) < max_squared) {
			jLoc = GetNextLocation (jLoc, r, i);
			++c;
		}
		return c - (2 - 2 * log2_max / Math.Log (Utility.MagnitudeSquared (jLoc)));
	}

	private static PointD GetNextLocation (PointD jLoc, double r, double i)
	{
		double t = jLoc.X;
		double x = (jLoc.X * jLoc.X) - (jLoc.Y * jLoc.Y) + r;
		double y = (2 * t * jLoc.Y) + i;
		return new (x, y);
	}
}
