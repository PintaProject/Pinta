/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Pinta.Core;

public sealed class SplineInterpolator
{
	private readonly SortedList<double, double> points = [];
	private ImmutableArray<double> y2;

	public int Count => points.Count;

	public void Add (double x, double y)
	{
		points[x] = y;
		y2 = default;
	}

	public void Clear ()
	{
		points.Clear ();
	}

	// Interpolate() and PreCompute() are adapted from:
	// NUMERICAL RECIPES IN C: THE ART OF SCIENTIFIC COMPUTING
	// ISBN 0-521-43108-5, page 113, section 3.3.

	public double Interpolate (double x)
	{
		if (y2.IsDefault)
			y2 = PreCompute ();

		IList<double> xa = points.Keys;
		IList<double> ya = points.Values;

		int n = ya.Count;
		int klo = 0;     // We will find the right place in the table by means of
		int khi = n - 1; // bisection. This is optimal if sequential calls to this

		while (khi - klo > 1) {
			// routine are at random values of x. If sequential calls
			int k = (khi + klo) >> 1;// are in order, and closely spaced, one would do better

			if (xa[k] > x) {
				khi = k; // to store previous values of klo and khi and test if
			} else {
				klo = k;
			}
		}

		double h = xa[khi] - xa[klo];
		double a = (xa[khi] - x) / h;
		double b = (x - xa[klo]) / h;

		// Cubic spline polynomial is now evaluated.
		return a * ya[klo] + b * ya[khi] +
		    ((a * a * a - a) * y2[klo] + (b * b * b - b) * y2[khi]) * (h * h) / 6.0; // NRT - y2 is set above by PreCompute ()
	}

	private ImmutableArray<double> PreCompute ()
	{
		int n = points.Count;
		double[] u = new double[n];
		IList<double> xa = points.Keys;
		IList<double> ya = points.Values;

		var resultingY2 = ImmutableArray.CreateBuilder<double> (n);
		resultingY2.Count = n;

		u[0] = 0;
		resultingY2[0] = 0;

		for (int i = 1; i < n - 1; ++i) {
			// This is the decomposition loop of the tridiagonal algorithm. 
			// y2 and u are used for temporary storage of the decomposed factors.
			double wx = xa[i + 1] - xa[i - 1];
			double sig = (xa[i] - xa[i - 1]) / wx;
			double p = sig * resultingY2[i - 1] + 2.0;

			resultingY2[i] = (sig - 1.0) / p;

			double ddydx =
			    (ya[i + 1] - ya[i]) / (xa[i + 1] - xa[i]) -
			    (ya[i] - ya[i - 1]) / (xa[i] - xa[i - 1]);

			u[i] = (6.0 * ddydx / wx - sig * u[i - 1]) / p;
		}

		resultingY2[n - 1] = 0;

		// This is the backsubstitution loop of the tridiagonal algorithm
		for (int i = n - 2; i >= 0; --i) {
			resultingY2[i] = resultingY2[i] * resultingY2[i + 1] + u[i];
		}

		return resultingY2.MoveToImmutable ();
	}
}

