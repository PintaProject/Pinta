/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Immutable;

namespace Pinta.Core;

public static class PerlinNoise
{
	// Precalculated rotation matrix coefficients
	private static readonly double rot_11;
	private static readonly double rot_12;
	private static readonly double rot_21;
	private static readonly double rot_22;

	// Adapted to 2-D version in C# from 3-D version in Java from http://mrl.nyu.edu/~perlin/noise/
	// Permutation table for pseudorandom gradient directions
	private static readonly ImmutableArray<int> permutation_table;
	static PerlinNoise ()
	{
#pragma warning disable format
		ReadOnlySpan<byte> permutationTable = [
			151, 160, 137,  91,  90,  15, 131,  13, 201,  95,  96,  53, 194, 233,   7, 225,
			140,  36, 103,  30,  69, 142,   8,  99,  37, 240,  21,  10,  23, 190,   6, 148,
			247, 120, 234,  75,   0,  26, 197,  62,  94, 252, 219, 203, 117,  35,  11,  32,
			 57, 177,  33,  88, 237, 149,  56,  87, 174,  20, 125, 136, 171, 168,  68, 175,
			 74, 165,  71, 134, 139,  48,  27, 166,  77, 146, 158, 231,  83, 111, 229, 122,
			 60, 211, 133, 230, 220, 105,  92,  41,  55,  46, 245,  40, 244, 102, 143,  54,
			 65,  25,  63, 161,   1, 216,  80,  73, 209,  76, 132, 187, 208,  89,  18, 169,
			200, 196, 135, 130, 116, 188, 159,  86, 164, 100, 109, 198, 173, 186,   3,  64,
			 52, 217, 226, 250, 124, 123,   5, 202,  38, 147, 118, 126, 255,  82,  85, 212,
			207, 206,  59, 227,  47,  16,  58,  17, 182, 189,  28,  42, 223, 183, 170, 213,
			119, 248, 152,   2,  44, 154, 163,  70, 221, 153, 101, 155, 167,  43, 172,   9,
			129,  22,  39, 253,  19,  98, 108, 110,  79, 113, 224, 232, 178, 185, 112, 104,
			218, 246,  97, 228, 251,  34, 242, 193, 238, 210, 144,  12, 191, 179, 162, 241,
			 81,  51, 145, 235, 249,  14, 239, 107,  49, 192, 214,  31, 181, 199, 106, 157,
			184,  84, 204, 176, 115, 121,  50,  45, 127,   4, 150, 254, 138, 236, 205,  93,
			222, 114,  67,  29,  24,  72, 243, 141, 128, 195,  78,  66, 215,  61, 156, 180,
		];
#pragma warning restore format
		var permutationsBuilder = ImmutableArray.CreateBuilder<int> (512);
		permutationsBuilder.Count = 512;
		for (int i = 0; i < 256; i++) {
			permutationsBuilder[256 + i] = permutationTable[i];
			permutationsBuilder[i] = permutationTable[i];
		}
		permutation_table = permutationsBuilder.MoveToImmutable ();

		// precalculate a rotation matrix - arbitrary angle...
		DegreesAngle rotationDegrees = new (137.2);
		RadiansAngle rotationRadians = rotationDegrees.ToRadians ();

		rot_11 = Math.Cos (rotationRadians.Radians);
		rot_12 = -Math.Sin (rotationRadians.Radians);
		rot_21 = Math.Sin (rotationRadians.Radians);
		rot_22 = Math.Cos (rotationRadians.Radians);
	}

	internal static double Fade (double t)
		=> t * t * t * (t * (t * 6 - 15) + 10);

	/// <returns>
	/// Dot product of pseudorandom gradient vector and distance vector
	/// </returns>
	private static double Gradient (int hash, double x, double y)
	{
		int h = hash & 15;
		PointD rel = new (
			X: h < 8 ? x : y,
			Y: h < 4 ? y : h == 12 || h == 14 ? x : 0);
		return ((h & 1) == 0 ? rel.X : -rel.X) + ((h & 2) == 0 ? rel.Y : -rel.Y);
	}

	/// <returns>
	/// Perlin noise value at specified grid coordinates and offsets
	/// </returns>
	public static double Compute (byte gridX, byte gridY, PointD offset, byte seed)
	{
		PointD faded = new (
			X: Fade (offset.X),
			Y: Fade (offset.Y));

		int a = permutation_table[gridX + seed] + gridY;
		int aa = permutation_table[a];
		int ab = permutation_table[a + 1];
		int b = permutation_table[gridX + 1 + seed] + gridY;
		int ba = permutation_table[b];
		int bb = permutation_table[b + 1];

		double gradAA = Gradient (permutation_table[aa], offset.X, offset.Y);
		double gradBA = Gradient (permutation_table[ba], offset.X - 1, offset.Y);

		double edge1 = Mathematics.Lerp (gradAA, gradBA, faded.X);

		double gradAB = Gradient (permutation_table[ab], offset.X, offset.Y - 1);
		double gradBB = Gradient (permutation_table[bb], offset.X - 1, offset.Y - 1);

		double edge2 = Mathematics.Lerp (gradAB, gradBB, faded.X);

		return Mathematics.Lerp (edge1, edge2, faded.Y);
	}

	/// <summary>
	/// Computes fractal Perlin noise over multiple octaves
	/// </summary>
	public static double Compute (PointD pos, double detail, double roughness, byte seed)
	{
		double total = 0.0;

		double frequency = 1;
		double amplitude = 1;

		double partialOctaveFactor = detail;
		int octaves = (int) Math.Ceiling (detail);

		for (int i = 0; i < octaves; i++) {

			// reduces correlation between octaves.
			PointD rotated = new (
				X: (pos.X * rot_11) + (pos.Y * rot_12),
				Y: (pos.X * rot_21) + (pos.Y * rot_22));

			double preliminaryNoise = amplitude * Noise (rotated.Scaled (frequency), seed);
			double noise =
				(partialOctaveFactor < 1)
				? preliminaryNoise * partialOctaveFactor // if this is the last 'partial' octave, reduce its contribution accordingly.
				: preliminaryNoise;

			total += noise;
			amplitude *= roughness; // scale amplitude for next octave.

			if (amplitude < 0.001)
				break; // if the contribution is going to be negligible, don't bother with higher octaves.

			// setup for next octave
			frequency += frequency;
			partialOctaveFactor -= 1.0;

			// offset the coordinates by prime numbers, with prime difference.
			// reduces correlation between octaves.
			pos = new PointD (
				X: rotated.X + 499,
				Y: rotated.Y + 506);
		}

		return total;
	}

	private static double Noise (PointD pos, byte seed)
	{
		PointD f = new (
			X: Math.Floor (pos.X),
			Y: Math.Floor (pos.Y));

		PointI gridPos = new (
			X: (int) f.X & 255,
			Y: (int) f.Y & 255);

		PointD offset = pos - f;

		PointD faded = new (
			X: Fade (offset.X),
			Y: Fade (offset.Y));

		int a = permutation_table[gridPos.X + seed] + gridPos.Y;
		int aa = permutation_table[a];
		int ab = permutation_table[a + 1];
		int b = permutation_table[gridPos.X + 1 + seed] + gridPos.Y;
		int ba = permutation_table[b];
		int bb = permutation_table[b + 1];

		double gradAA = Gradient (permutation_table[aa], offset.X, offset.Y);
		double gradBA = Gradient (permutation_table[ba], offset.X - 1, offset.Y);

		double edge1 = Mathematics.Lerp (gradAA, gradBA, faded.X);

		double gradAB = Gradient (permutation_table[ab], offset.X, offset.Y - 1);
		double gradBB = Gradient (permutation_table[bb], offset.X - 1, offset.Y - 1);

		double edge2 = Mathematics.Lerp (gradAB, gradBB, faded.X);

		double lerped = Mathematics.Lerp (edge1, edge2, faded.Y);

		return lerped;
	}
}
