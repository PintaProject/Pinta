/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core
{
	public static class Utility
	{
		public static double Clamp (double x, double min, double max)
		{
			if (x < min) {
				return min;
			} else if (x > max) {
				return max;
			} else {
				return x;
			}
		}

		public static float Clamp (float x, float min, float max)
		{
			if (x < min) {
				return min;
			} else if (x > max) {
				return max;
			} else {
				return x;
			}
		}

		public static int Clamp (int x, int min, int max)
		{
			if (x < min) {
				return min;
			} else if (x > max) {
				return max;
			} else {
				return x;
			}
		}

		public static byte ClampToByte (double x)
		{
			if (x > 255) {
				return 255;
			} else if (x < 0) {
				return 0;
			} else {
				return (byte)x;
			}
		}

		public static byte ClampToByte (float x)
		{
			if (x > 255) {
				return 255;
			} else if (x < 0) {
				return 0;
			} else {
				return (byte)x;
			}
		}

		public static byte ClampToByte (int x)
		{
			if (x > 255) {
				return 255;
			} else if (x < 0) {
				return 0;
			} else {
				return (byte)x;
			}
		}

		public static float Lerp (float from, float to, float frac)
		{
			return (from + frac * (to - from));
		}

		public static double Lerp (double from, double to, double frac)
		{
			return (from + frac * (to - from));
		}
	}
}
