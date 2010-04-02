/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;

namespace Pinta.Core
{
	public static class Utility
	{
		internal static bool IsNumber (float x)
		{
			return x >= float.MinValue && x <= float.MaxValue;
		}

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

		public static Cairo.PointD Lerp (Cairo.PointD from, Cairo.PointD to, float frac)
		{
			return new Cairo.PointD (Lerp (from.X, to.X, frac), Lerp (from.Y, to.Y, frac));
		}

		public static void Swap(ref int a, ref int b)
        {
            int t;

            t = a;
            a = b;
            b = t;
        }

		 /// <summary>
        /// Smoothly blends between two colors.
        /// </summary>
        public static ColorBgra Blend(ColorBgra ca, ColorBgra cb, byte cbAlpha)
        {
            uint caA = (uint)Utility.FastScaleByteByByte((byte)(255 - cbAlpha), ca.A);
            uint cbA = (uint)Utility.FastScaleByteByByte(cbAlpha, cb.A);
            uint cbAT = caA + cbA;

            uint r;
            uint g;
            uint b;

            if (cbAT == 0) {
                r = 0;
                g = 0;
                b = 0;
            } else {
                r = ((ca.R * caA) + (cb.R * cbA)) / cbAT;
                g = ((ca.G * caA) + (cb.G * cbA)) / cbAT;
                b = ((ca.B * caA) + (cb.B * cbA)) / cbAT;
            }

            return ColorBgra.FromBgra((byte)b, (byte)g, (byte)r, (byte)cbAT);
        }
	
		/// <summary>
		/// Allows you to find the bounding box for a "region" that is described as an
		/// array of bounding boxes.
		/// </summary>
		/// <param name="rectsF">The "region" you want to find a bounding box for.</param>
		/// <returns>A RectangleF structure that surrounds the Region.</returns>
		public static Gdk.Rectangle GetRegionBounds (Gdk.Rectangle[] rects, int startIndex, int length)
		{
			if (rects.Length == 0) {
				return Gdk.Rectangle.Zero;
			}

			int left = rects[startIndex].Left;
			int top = rects[startIndex].Top;
			int right = rects[startIndex].Right;
			int bottom = rects[startIndex].Bottom;

			for (int i = startIndex + 1; i < startIndex + length; ++i) {
				Gdk.Rectangle rect = rects[i];

				if (rect.Left < left) {
					left = rect.Left;
				}

				if (rect.Top < top) {
					top = rect.Top;
				}

				if (rect.Right > right) {
					right = rect.Right;
				}

				if (rect.Bottom > bottom) {
					bottom = rect.Bottom;
				}
			}

			return Gdk.Rectangle.FromLTRB (left, top, right, bottom);
		}

		public static int ColorDifference (ColorBgra a, ColorBgra b)
		{
			return (int)Math.Ceiling (Math.Sqrt (ColorDifferenceSquared (a, b)));
		}

		public static int ColorDifferenceSquared (ColorBgra a, ColorBgra b)
		{
			int diffSq = 0, tmp;

			tmp = a.R - b.R;
			diffSq += tmp * tmp;
			tmp = a.G - b.G;
			diffSq += tmp * tmp;
			tmp = a.B - b.B;
			diffSq += tmp * tmp;

			return diffSq / 3;
		}

		public static Gdk.Rectangle[] InflateRectangles (Gdk.Rectangle[] rects, int len)
		{
			Gdk.Rectangle[] inflated = new Gdk.Rectangle[rects.Length];

			for (int i = 0; i < rects.Length; ++i)
				inflated[i] = new Gdk.Rectangle(rects[i].X-len, rects[i].Y-len, rects[i].Width + 2 * len, rects[i].Height + 2 * len);

			return inflated;
		}
		
		public static Gdk.Region RectanglesToRegion(Gdk.Rectangle[] rects)
        {
            Gdk.Region reg = Gdk.Region.Rectangle(Gdk.Rectangle.Zero);
            foreach (Gdk.Rectangle r in rects)
                reg.UnionWithRect(r);
            return reg;
        }
		
		public static string GetStaticName (Type type)
		{
			PropertyInfo pi = type.GetProperty ("StaticName", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);
			return (string)pi.GetValue (null, null);
		}

		public static byte FastScaleByteByByte (byte a, byte b)
		{
			int r1 = a * b + 0x80;
			int r2 = ((r1 >> 8) + r1) >> 8;
			return (byte)r2;
		}

		public static Gdk.Point[] GetLinePoints(Gdk.Point first, Gdk.Point second)
        {
            Gdk.Point[] coords = null;

            int x1 = first.X;
            int y1 = first.Y;
            int x2 = second.X;
            int y2 = second.Y;
            int dx = x2 - x1;
            int dy = y2 - y1;
            int dxabs = Math.Abs(dx);
            int dyabs = Math.Abs(dy);
            int px = x1;
            int py = y1;
            int sdx = Math.Sign(dx);
            int sdy = Math.Sign(dy);
            int x = 0;
            int y = 0;

            if (dxabs > dyabs)
            {
                coords = new Gdk.Point[dxabs + 1];

                for (int i = 0; i <= dxabs; i++)
                {
                    y += dyabs;

                    if (y >= dxabs)
                    {
                        y -= dxabs;
                        py += sdy;
                    }

                    coords[i] = new Gdk.Point(px, py);
                    px += sdx;
                }
            }
            else 
                // had to add in this cludge for slopes of 1 ... wasn't drawing half the line
                if (dxabs == dyabs)
            {
                coords = new Gdk.Point[dxabs + 1];

                for (int i = 0; i <= dxabs; i++)
                {
                    coords[i] = new Gdk.Point(px, py);
                    px += sdx;
                    py += sdy;
                }
            }
            else
            {
                coords = new Gdk.Point[dyabs + 1];

                for (int i = 0; i <= dyabs; i++)
                {
                    x += dxabs;

                    if (x >= dyabs)
                    {
                        x -= dyabs;
                        px += sdx;
                    }

                    coords[i] = new Gdk.Point(px, py);
                    py += sdy;
                }
            }

            return coords;
        }

		public static unsafe void GetRgssOffsets (Cairo.PointD* samplesArray, int sampleCount, int quality)
        {
            if (sampleCount < 1)
            {
                throw new ArgumentOutOfRangeException("sampleCount", "sampleCount must be [0, int.MaxValue]");
            }

            if (sampleCount != quality * quality)
            {
                throw new ArgumentOutOfRangeException("sampleCount != (quality * quality)");
            }

            if (sampleCount == 1)
            {
                samplesArray[0] = new Cairo.PointD (0.0, 0.0);
            }
            else
            {
                for (int i = 0; i < sampleCount; ++i)
                {
                    double y = (i + 1d) / (sampleCount + 1d);
                    double x = y * quality;

                    x -= (int)x;

                    samplesArray[i] = new Cairo.PointD (x - 0.5d, y - 0.5d);
                }
            }
        }
	}
}
