/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class GaussianBlurEffect : BaseEffect
{
	public override string Icon => Resources.Icons.EffectsBlursGaussianBlur;

	public sealed override bool IsTileable => false;

	public override string Name => Translations.GetString ("Gaussian Blur");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Blurs");

	public GaussianBlurData Data => (GaussianBlurData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	private readonly ISystemService system;

	public GaussianBlurEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		system = services.GetService<ISystemService> ();
		EffectData = new GaussianBlurData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	public static ImmutableArray<int> CreateGaussianBlurRow (int amount)
	{
		int size = 1 + (amount * 2);
		var weights = ImmutableArray.CreateBuilder<int> (size);
		weights.Count = size;

		for (int i = 0; i <= amount; ++i) {
			// 1 + aa - aa + 2ai - ii
			weights[i] = 16 * (i + 1);
			weights[size - i - 1] = weights[i];
		}

		return weights.MoveToImmutable ();
	}

	// --- Separable two-pass Gaussian blur ---
	//
	// The 2D Gaussian kernel is separable: it can be decomposed into two
	// sequential 1D convolutions (horizontal then vertical). This reduces
	// the per-pixel work from O(kernel²) to O(2·kernel), giving a large
	// speedup for bigger radii.
	//
	// Pass 1 (horizontal): For each pixel, convolve the source row with
	//   the 1D kernel and store the weighted sums in an intermediate buffer.
	// Pass 2 (vertical):   For each pixel, convolve the intermediate
	//   column with the 1D kernel and write the final result.
	//
	// Alpha handling: the source is premultiplied. We blur the premultiplied
	// B/G/R channels and the alpha channel separately. The final straight-
	// alpha color is recovered as (255 · blurred_premul_c / blurred_alpha),
	// and the output is converted back to premultiplied format.
	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		if (Data.Radius == 0)
			return; // Copy src to dest

		int r = Data.Radius;
		ImmutableArray<int> weights = CreateGaussianBlurRow (r);
		int wlen = weights.Length;
		int width = src.Width;
		int height = src.Height;
		int threads = system.RenderThreads;

		// --- Pass 1: Horizontal convolution (parallelized by row) ---
		int size = width * height;
		int[] h_b = new int[size];
		int[] h_g = new int[size];
		int[] h_r = new int[size];
		int[] h_a = new int[size];

		// Precompute horizontal weight sums (depends only on x position)
		long[] h_weight_sums = new long[width];
		for (int x = 0; x < width; ++x) {
			long sum = 0;
			int wx_start = Math.Max (0, r - x);
			int wx_end = Math.Min (wlen, width - x + r);
			for (int wx = wx_start; wx < wx_end; ++wx)
				sum += weights[wx];
			h_weight_sums[x] = sum;
		}

		Parallel.For (0, height,
			new ParallelOptions { MaxDegreeOfParallelism = threads },
			y => {
				ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
				int row_offset = y * width;

				for (int x = 0; x < width; ++x) {
					int s_b = 0, s_g = 0, s_r = 0, s_a = 0;

					int wx_start = Math.Max (0, r - x);
					int wx_end = Math.Min (wlen, width - x + r);

					for (int wx = wx_start; wx < wx_end; ++wx) {
						int src_x = x + wx - r;
						ColorBgra c = src_data[row_offset + src_x];
						int w = weights[wx];

						s_b += w * c.B;
						s_g += w * c.G;
						s_r += w * c.R;
						s_a += w * c.A;
					}

					int idx = row_offset + x;
					h_b[idx] = s_b;
					h_g[idx] = s_g;
					h_r[idx] = s_r;
					h_a[idx] = s_a;
				}
			});

		// --- Pass 2: Vertical convolution (parallelized by row) ---
		Parallel.For (0, height,
			new ParallelOptions { MaxDegreeOfParallelism = threads },
			y => {
				Span<ColorBgra> dst_data = dest.GetPixelData ();
				RenderVerticalRow (dst_data, y, width, height, r, weights, wlen, h_b, h_g, h_r, h_a, h_weight_sums);
			});
	}

	private static void RenderVerticalRow (
		Span<ColorBgra> dst_data,
		int y,
		int width, int height, int r,
		ImmutableArray<int> weights, int wlen,
		int[] h_b, int[] h_g, int[] h_r, int[] h_a,
		long[] h_weight_sums)
	{
		// Determine valid vertical kernel range for this row
		int wy_start = Math.Max (0, r - y);
		int wy_end = Math.Min (wlen, height - y + r);

		long v_weight_sum = 0;
		for (int wy = wy_start; wy < wy_end; ++wy)
			v_weight_sum += weights[wy];

		// Rent accumulators from the pool to avoid per-row heap allocation
		long[] rent_b = ArrayPool<long>.Shared.Rent (width);
		long[] rent_g = ArrayPool<long>.Shared.Rent (width);
		long[] rent_r = ArrayPool<long>.Shared.Rent (width);
		long[] rent_a = ArrayPool<long>.Shared.Rent (width);

		try {
			Span<long> sum_b = rent_b.AsSpan (0, width);
			Span<long> sum_g = rent_g.AsSpan (0, width);
			Span<long> sum_r = rent_r.AsSpan (0, width);
			Span<long> sum_a = rent_a.AsSpan (0, width);

			sum_b.Clear ();
			sum_g.Clear ();
			sum_r.Clear ();
			sum_a.Clear ();

			// Accumulate weighted intermediate rows (vertical convolution)
			for (int wy = wy_start; wy < wy_end; ++wy) {
				int src_y = y + wy - r;
				int w = weights[wy];
				int row_offset = src_y * width;

				AccumulateRow (sum_b, h_b, row_offset, width, w);
				AccumulateRow (sum_g, h_g, row_offset, width, w);
				AccumulateRow (sum_r, h_r, row_offset, width, w);
				AccumulateRow (sum_a, h_a, row_offset, width, w);
			}

			// Write output pixels
			var dst_row = dst_data.Slice (y * width, width);

			for (int x = 0; x < width; ++x) {
				long total_weight = h_weight_sums[x] * v_weight_sum;

				if (total_weight == 0 || sum_a[x] == 0) {
					dst_row[x] = ColorBgra.Zero;
				} else {
					byte alpha = (byte) (sum_a[x] / total_weight);
					byte blue = (byte) (sum_b[x] * 255 / sum_a[x]);
					byte green = (byte) (sum_g[x] * 255 / sum_a[x]);
					byte red = (byte) (sum_r[x] * 255 / sum_a[x]);
					dst_row[x] = ColorBgra.FromBgra (blue, green, red, alpha).ToPremultipliedAlpha ();
				}
			}
		} finally {
			ArrayPool<long>.Shared.Return (rent_b);
			ArrayPool<long>.Shared.Return (rent_g);
			ArrayPool<long>.Shared.Return (rent_r);
			ArrayPool<long>.Shared.Return (rent_a);
		}
	}

	/// <summary>
	/// Adds weight × source[offset..offset+length] into the accumulator span, using
	/// SIMD (Vector256) when available.
	/// </summary>
	[MethodImpl (MethodImplOptions.AggressiveInlining)]
	private static void AccumulateRow (Span<long> accumulator, int[] source, int source_offset, int length, int weight)
	{
		ref int src_ref = ref source[source_offset];
		ref long acc_ref = ref MemoryMarshal.GetReference (accumulator);
		int i = 0;

		if (Vector256.IsHardwareAccelerated && length >= Vector256<int>.Count) {
			Vector256<long> w_vec = Vector256.Create ((long) weight);

			for (; i <= length - Vector256<int>.Count; i += Vector256<int>.Count) {
				Vector256<int> src_vec = Vector256.LoadUnsafe (ref src_ref, (nuint) i);
				(Vector256<long> lo, Vector256<long> hi) = Vector256.Widen (src_vec);

				Vector256<long> acc_lo = Vector256.LoadUnsafe (ref acc_ref, (nuint) i);
				Vector256<long> acc_hi = Vector256.LoadUnsafe (ref acc_ref, (nuint) (i + Vector256<long>.Count));

				acc_lo += lo * w_vec;
				acc_hi += hi * w_vec;

				acc_lo.StoreUnsafe (ref acc_ref, (nuint) i);
				acc_hi.StoreUnsafe (ref acc_ref, (nuint) (i + Vector256<long>.Count));
			}
		}

		// Scalar tail
		for (; i < length; ++i)
			Unsafe.Add (ref acc_ref, i) += (long) weight * Unsafe.Add (ref src_ref, i);
	}

	public sealed class GaussianBlurData : EffectData
	{
		[Caption ("Radius")]
		[MinimumValue (0), MaximumValue (200)]
		public int Radius { get; set; } = 2;

		[Skip]
		public override bool IsDefault => Radius == 0;
	}
}
