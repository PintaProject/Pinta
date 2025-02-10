using System;

namespace Pinta.Core;

public sealed class Mandelbrot
{
	private readonly double max_squared;
	private readonly double inv_log_max;
	private readonly int max_iterations;
	public Mandelbrot (int maxIterations, double maxSquared)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero (maxIterations);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual (maxSquared, 1);
		max_iterations = maxIterations;
		max_squared = maxSquared;
		inv_log_max = 1.0 / Math.Log (maxSquared);
	}

	public double Compute (double r, double i, int factor)
	{
		int c = 0;
		PointD p = new (0, 0);
		while ((c * factor) < max_iterations && Utility.MagnitudeSquared (p) < max_squared) {
			p = NextLocation (p, r, i);
			++c;
		}
		return c - Math.Log (Utility.MagnitudeSquared (p)) * inv_log_max;
	}

	private static PointD NextLocation (PointD p, double r, double i)
	{
		double t = p.X;
		double x = p.X * p.X - p.Y * p.Y + r;
		double y = 2 * t * p.Y + i;
		return new (x, y);
	}
}
