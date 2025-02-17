/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;

namespace Pinta.Core;

public sealed class SplineInterpolator<TNumber> where TNumber : IFloatingPoint<TNumber>
{
	private readonly SortedList<TNumber, TNumber> points = [];
	private ImmutableArray<TNumber> y2;

	public int Count => points.Count;

	public void Add (TNumber x, TNumber y)
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

	public TNumber Interpolate (TNumber x)
	{
		if (y2.IsDefault)
			y2 = PreCompute ();

		IList<TNumber> xa = points.Keys;
		IList<TNumber> ya = points.Values;

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

		TNumber h = xa[khi] - xa[klo];
		TNumber a = (xa[khi] - x) / h;
		TNumber b = (x - xa[klo]) / h;

		// Cubic spline polynomial is now evaluated.
		return a * ya[klo] + b * ya[khi] +
		    ((a * a * a - a) * y2[klo] + (b * b * b - b) * y2[khi]) * (h * h) / TNumber.CreateChecked (6); // NRT - y2 is set above by PreCompute ()
	}

	private ImmutableArray<TNumber> PreCompute ()
	{
		int n = points.Count;
		TNumber[] u = new TNumber[n];
		IList<TNumber> xa = points.Keys;
		IList<TNumber> ya = points.Values;

		var resultingY2 = ImmutableArray.CreateBuilder<TNumber> (n);
		resultingY2.Count = n;

		u[0] = TNumber.Zero;
		resultingY2[0] = TNumber.Zero;

		for (int i = 1; i < n - 1; ++i) {
			// This is the decomposition loop of the tridiagonal algorithm. 
			// y2 and u are used for temporary storage of the decomposed factors.
			TNumber wx = xa[i + 1] - xa[i - 1];
			TNumber sig = (xa[i] - xa[i - 1]) / wx;
			TNumber p = sig * resultingY2[i - 1] + TNumber.CreateChecked (2);

			resultingY2[i] = (sig - TNumber.One) / p;

			TNumber ddydx =
			    (ya[i + 1] - ya[i]) / (xa[i + 1] - xa[i]) -
			    (ya[i] - ya[i - 1]) / (xa[i] - xa[i - 1]);

			u[i] = (TNumber.CreateChecked (6) * ddydx / wx - sig * u[i - 1]) / p;
		}

		resultingY2[n - 1] = TNumber.Zero;

		// This is the backsubstitution loop of the tridiagonal algorithm
		for (int i = n - 2; i >= 0; --i) {
			resultingY2[i] = resultingY2[i] * resultingY2[i + 1] + u[i];
		}

		return resultingY2.MoveToImmutable ();
	}
}

