/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Hanh Pham <hanh.pham@gmx.com>                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public abstract class LocalHistogramEffect : BaseEffect<DBNull>
{
	public override DBNull GetPreRender (ImageSurface src, ImageSurface dst)
		=> DBNull.Value;

	protected static int GetMaxAreaForRadius (int radius)
	{
		int area = 0;
		int cutoff = ((radius * 2 + 1) * (radius * 2 + 1) + 2) / 4;

		for (int v = -radius; v <= radius; ++v) {
			for (int u = -radius; u <= radius; ++u) {
				if (u * u + v * v <= cutoff) {
					++area;
				}
			}
		}

		return area;
	}

	private static void SetToZero (Span<int> data)
	{
		data.Clear ();
	}

	public virtual ColorBgra Apply (in ColorBgra src, int area, Span<int> hb, Span<int> hg, Span<int> hr, Span<int> ha)
	{
		return src;
	}

	//same as Apply, except the histogram is alpha-weighted instead of keeping a separate alpha channel histogram.
	public virtual ColorBgra ApplyWithAlpha (in ColorBgra src, int area, int sum, Span<int> hb, Span<int> hg, Span<int> hr)
	{
		return src;
	}

	public static ColorBgra GetPercentile (int percentile, int area, Span<int> hb, Span<int> hg, Span<int> hr, Span<int> ha)
	{
		int minCount = area * percentile / 100;

		int b = 0;
		int bCount = 0;

		while (b < 255 && hb[b] == 0) {
			++b;
		}

		while (b < 255 && bCount < minCount) {
			bCount += hb[b];
			++b;
		}

		int g = 0;
		int gCount = 0;

		while (g < 255 && hg[g] == 0) {
			++g;
		}

		while (g < 255 && gCount < minCount) {
			gCount += hg[g];
			++g;
		}

		int r = 0;
		int rCount = 0;

		while (r < 255 && hr[r] == 0) {
			++r;
		}

		while (r < 255 && rCount < minCount) {
			rCount += hr[r];
			++r;
		}

		int a = 0;
		int aCount = 0;

		while (a < 255 && ha[a] == 0) {
			++a;
		}

		while (a < 255 && aCount < minCount) {
			aCount += ha[a];
			++a;
		}

		return ColorBgra.FromBgra ((byte) b, (byte) g, (byte) r, (byte) a);
	}

	public void RenderRect (
	    int rad,
	    ImageSurface src,
	    ImageSurface dst,
	    Core.RectangleI rect)
	{
		Size sourceSize = new (
			Width: src.Width,
			Height: src.Height
		);

		int stride = src.Stride / ColorBgra.SizeOf;

		Span<int> leadingEdgeX = stackalloc int[rad + 1];

		// approximately (rad + 0.5)^2
		int cutoff = ((rad * 2 + 1) * (rad * 2 + 1) + 2) / 4;

		for (int v = 0; v <= rad; ++v) {
			for (int u = 0; u <= rad; ++u) {
				if (u * u + v * v <= cutoff) {
					leadingEdgeX[v] = u;
				}
			}
		}

		const int hLength = 256;

		Span<int> hb = stackalloc int[hLength];
		Span<int> hg = stackalloc int[hLength];
		Span<int> hr = stackalloc int[hLength];
		Span<int> ha = stackalloc int[hLength];

		var src_data = src.GetReadOnlyPixelData ();
		var dst_data = dst.GetPixelData ();

		for (int y = rect.Top; y <= rect.Bottom; ++y) {
			SetToZero (hb);
			SetToZero (hg);
			SetToZero (hr);
			SetToZero (ha);

			int area = 0;

			// assert: v + y >= 0
			int top = -Math.Min (rad, y);

			// assert: v + y <= height - 1
			int bottom = Math.Min (rad, sourceSize.Height - 1 - y);

			// assert: u + x >= 0
			int left = -Math.Min (rad, rect.Left);

			// assert: u + x <= width - 1
			int right = Math.Min (rad, sourceSize.Width - 1 - rect.Left);

			for (int v = top; v <= bottom; ++v) {
				ReadOnlySpan<ColorBgra> psamples = src_data[((y + v) * sourceSize.Width + rect.Left + left)..];

				for (int u = left, i = 0; u <= right; ++u, ++i) {
					ColorBgra psamp = psamples[i];
					if ((u * u + v * v) <= cutoff) {
						++area;
						++hb[psamp.B];
						++hg[psamp.G];
						++hr[psamp.R];
						++ha[psamp.A];
					}
				}
			}

			ReadOnlySpan<ColorBgra> ps = src_data.Slice (y * sourceSize.Width, sourceSize.Width);
			Span<ColorBgra> pd = dst_data.Slice (y * sourceSize.Width, sourceSize.Width);

			for (int x = rect.Left; x <= rect.Right; x++) {
				pd[x] = Apply (ps[x], area, hb, hg, hr, ha);

				// assert: u + x >= 0
				left = -Math.Min (rad, x);

				// assert: u + x <= width - 1
				right = Math.Min (rad + 1, sourceSize.Width - 1 - x);

				// Subtract trailing edge top half
				int v = -1;

				while (v >= top) {
					int u = leadingEdgeX[-v];

					if (-u >= left) {
						break;
					}

					--v;
				}

				while (v >= top) {
					int u = leadingEdgeX[-v];
					ColorBgra p = src_data[(y * sourceSize.Width + x) + (v * stride) - u];

					--hb[p.B];
					--hg[p.G];
					--hr[p.R];
					--ha[p.A];
					--area;

					--v;
				}

				// add leading edge top half
				v = -1;
				while (v >= top) {
					int u = leadingEdgeX[-v];

					if (u + 1 <= right) {
						break;
					}

					--v;
				}

				while (v >= top) {
					int u = leadingEdgeX[-v];
					ColorBgra p = src_data[(y * sourceSize.Width + x) + (v * stride) + u + 1];
					++hb[p.B];
					++hg[p.G];
					++hr[p.R];
					++ha[p.A];
					++area;

					--v;
				}

				// Subtract trailing edge bottom half
				v = 0;

				while (v <= bottom) {
					int u = leadingEdgeX[v];

					if (-u >= left) {
						break;
					}

					++v;
				}

				while (v <= bottom) {
					int u = leadingEdgeX[v];
					ColorBgra p = src_data[(y * sourceSize.Width + x) + (v * stride) - u];
					--hb[p.B];
					--hg[p.G];
					--hr[p.R];
					--ha[p.A];
					--area;

					++v;
				}

				// add leading edge bottom half
				v = 0;

				while (v <= bottom) {
					int u = leadingEdgeX[v];

					if (u + 1 <= right) {
						break;
					}

					++v;
				}

				while (v <= bottom) {
					int u = leadingEdgeX[v];
					ColorBgra p = src_data[(y * sourceSize.Width + x) + (v * stride) + u + 1];
					++hb[p.B];
					++hg[p.G];
					++hr[p.R];
					++ha[p.A];
					++area;

					++v;
				}
			}
		}
	}

	//same as RenderRect, except the histogram is alpha-weighted instead of keeping a separate alpha channel histogram.
	public void RenderRectWithAlpha (
	    int rad,
	    ImageSurface src,
	    ImageSurface dst,
	    Core.RectangleI rect)
	{
		Size sourceSize = new (
			Width: src.Width,
			Height: src.Height
		);

		int stride = src.Stride / ColorBgra.SizeOf;

		Span<int> leadingEdgeX = stackalloc int[rad + 1];

		// approximately (rad + 0.5)^2
		int cutoff = ((rad * 2 + 1) * (rad * 2 + 1) + 2) / 4;

		for (int v = 0; v <= rad; ++v) {
			for (int u = 0; u <= rad; ++u) {
				if (u * u + v * v <= cutoff) {
					leadingEdgeX[v] = u;
				}
			}
		}

		const int hLength = 256;

		Span<int> hb = stackalloc int[hLength];
		Span<int> hg = stackalloc int[hLength];
		Span<int> hr = stackalloc int[hLength];

		var src_data = src.GetReadOnlyPixelData ();
		var dst_data = dst.GetPixelData ();

		for (int y = rect.Top; y <= rect.Bottom; y++) {
			SetToZero (hb);
			SetToZero (hg);
			SetToZero (hr);

			int area = 0;
			int sum = 0;

			// assert: v + y >= 0
			int top = -Math.Min (rad, y);

			// assert: v + y <= height - 1
			int bottom = Math.Min (rad, sourceSize.Height - 1 - y);

			// assert: u + x >= 0
			int left = -Math.Min (rad, rect.Left);

			// assert: u + x <= width - 1
			int right = Math.Min (rad, sourceSize.Width - 1 - rect.Left);

			for (int v = top; v <= bottom; ++v) {
				ReadOnlySpan<ColorBgra> psamples = src_data[((y + v) * sourceSize.Width + rect.Left + left)..];

				for (int u = left, i = 0; u <= right; ++u, ++i) {
					ColorBgra psamp = psamples[i];
					if ((u * u + v * v) <= cutoff) {
						++area;
						byte w = psamp.A;
						sum += w;
						hb[psamp.B] += w;
						hg[psamp.G] += w;
						hr[psamp.R] += w;
					}
				}
			}

			ReadOnlySpan<ColorBgra> ps = src_data.Slice (y * sourceSize.Width, sourceSize.Width);
			Span<ColorBgra> pd = dst_data.Slice (y * sourceSize.Width, sourceSize.Width);

			for (int x = rect.Left; x <= rect.Right; x++) {
				pd[x] = ApplyWithAlpha (ps[x], area, sum, hb, hg, hr);

				// assert: u + x >= 0
				left = -Math.Min (rad, x);

				// assert: u + x <= width - 1
				right = Math.Min (rad + 1, sourceSize.Width - 1 - x);

				// Subtract trailing edge top half
				int v = -1;

				while (v >= top) {
					int u = leadingEdgeX[-v];

					if (-u >= left) {
						break;
					}

					--v;
				}

				while (v >= top) {
					int u = leadingEdgeX[-v];
					ColorBgra p = src_data[(y * sourceSize.Width + x) + (v * stride) - u];
					byte w = p.A;

					hb[p.B] -= w;
					hg[p.G] -= w;
					hr[p.R] -= w;
					sum -= w;
					--area;

					--v;
				}

				// add leading edge top half
				v = -1;
				while (v >= top) {
					int u = leadingEdgeX[-v];

					if (u + 1 <= right) {
						break;
					}

					--v;
				}

				while (v >= top) {
					int u = leadingEdgeX[-v];
					ColorBgra p = src_data[(y * sourceSize.Width + x) + (v * stride) + u + 1];
					byte w = p.A;

					hb[p.B] += w;
					hg[p.G] += w;
					hr[p.R] += w;
					sum += w;
					++area;

					--v;
				}

				// Subtract trailing edge bottom half
				v = 0;

				while (v <= bottom) {
					int u = leadingEdgeX[v];

					if (-u >= left) {
						break;
					}

					++v;
				}

				while (v <= bottom) {
					int u = leadingEdgeX[v];
					ColorBgra p = src_data[(y * sourceSize.Width + x) + (v * stride) - u];
					byte w = p.A;

					hb[p.B] -= w;
					hg[p.G] -= w;
					hr[p.R] -= w;
					sum -= w;
					--area;

					++v;
				}

				// add leading edge bottom half
				v = 0;

				while (v <= bottom) {
					int u = leadingEdgeX[v];

					if (u + 1 <= right) {
						break;
					}

					++v;
				}

				while (v <= bottom) {
					int u = leadingEdgeX[v];
					ColorBgra p = src_data[(y * sourceSize.Width + x) + (v * stride) + u + 1];
					byte w = p.A;

					hb[p.B] += w;
					hg[p.G] += w;
					hr[p.R] += w;
					sum += w;
					++area;

					++v;
				}
			}
		}
	}
}
