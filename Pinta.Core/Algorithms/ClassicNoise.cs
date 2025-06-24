// Copyright (c) 2025 Jerry Huxtable
//
// MIT License: http://www.opensource.org/licenses/mit-license.php
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// Ported to Pinta by Martin del Rio

using System;

namespace Pinta.Core;

public sealed class ClassicNoise
{
	private const int GRADIENT_SIZE = 256;

	private readonly int[] permutation = new int[GRADIENT_SIZE * 2];
	private readonly double[] gradient_x = new double[GRADIENT_SIZE];
	private readonly double[] gradient_y = new double[GRADIENT_SIZE];
	private readonly double[] gradient_z = new double[GRADIENT_SIZE];

	// The constructor now takes a seed to generate a unique, repeatable noise pattern.
	public ClassicNoise (int seed)
	{
		var randomGen = new Random (seed);
		int[] p = new int[GRADIENT_SIZE];

		for (int i = 0; i < GRADIENT_SIZE; i++)
			p[i] = i;

		for (int i = 0; i < GRADIENT_SIZE; i++) {
			int j = randomGen.Next (GRADIENT_SIZE);
			(p[i], p[j]) = (p[j], p[i]);
		}

		for (int i = 0; i < GRADIENT_SIZE; i++) {
			permutation[i] = permutation[i + GRADIENT_SIZE] = p[i];
			double invLen;
			do {
				gradient_x[i] = randomGen.NextDouble () * 2.0 - 1.0;
				gradient_y[i] = randomGen.NextDouble () * 2.0 - 1.0;
				gradient_z[i] = randomGen.NextDouble () * 2.0 - 1.0;
				invLen = gradient_x[i] * gradient_x[i] + gradient_y[i] * gradient_y[i] + gradient_z[i] * gradient_z[i];
			} while (invLen == 0);
			invLen = 1.0 / Math.Sqrt (invLen);
			gradient_x[i] *= invLen;
			gradient_y[i] *= invLen;
			gradient_z[i] *= invLen;
		}
	}
	private double Grad (int hash, double x, double y, double z)
	{
		int h = hash & (GRADIENT_SIZE - 1);
		return gradient_x[h] * x + gradient_y[h] * y + gradient_z[h] * z;
	}

	public double Noise3 (double x, double y, double z)
	{
		int X = (int) Math.Floor (x) & (GRADIENT_SIZE - 1);
		int Y = (int) Math.Floor (y) & (GRADIENT_SIZE - 1);
		int Z = (int) Math.Floor (z) & (GRADIENT_SIZE - 1);

		x -= Math.Floor (x);
		y -= Math.Floor (y);
		z -= Math.Floor (z);

		double u = PerlinNoise.Fade (x);
		double v = PerlinNoise.Fade (y);
		double w = PerlinNoise.Fade (z);

		int A = permutation[X] + Y;
		int AA = permutation[A] + Z;
		int AB = permutation[A + 1] + Z;
		int B = permutation[X + 1] + Y;
		int BA = permutation[B] + Z;
		int BB = permutation[B + 1] + Z;

		return Mathematics.Lerp (
			Mathematics.Lerp (
				Mathematics.Lerp (
					Grad (permutation[AA], x, y, z),
					Grad (permutation[BA], x - 1, y, z),
					u),
				Mathematics.Lerp (
					Grad (
						permutation[AB],
						x,
						y - 1,
						z),
					Grad (
						permutation[BB],
						x - 1,
						y - 1,
						z),
					u),
				v),
			Mathematics.Lerp (
				Mathematics.Lerp (
					Grad (
						permutation[AA + 1],
						x,
						y,
						z - 1),
					Grad (
						permutation[BA + 1],
						x - 1,
						y,
						z - 1),
					u),
				Mathematics.Lerp (
					Grad (
						permutation[AB + 1],
						x,
						y - 1,
						z - 1),
					Grad (
						permutation[BB + 1],
						x - 1,
						y - 1,
						z - 1),
					u),
				v),
			w);
	}
}
