// 
// SurfaceDiff.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2012 Jonathan Pobst
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

//#define DEBUG_DIFF

using System;
using System.Collections;
using System.Threading.Tasks;
using Cairo;

namespace Pinta.Core;

public sealed class SurfaceDiff
{
	private struct DiffBounds
	{
		public int left;
		public int right;
		public int top;
		public int bottom;

		public DiffBounds (int width, int height)
		{
			left = width + 1;
			right = -1;
			top = height + 1;
			bottom = -1;
		}

		private DiffBounds (int newLeft, int newRight, int newTop, int newBottom)
		{
			left = newLeft;
			right = newRight;
			top = newTop;
			bottom = newBottom;
		}

		public readonly DiffBounds AsMerged (DiffBounds other)
		{
			var newLeft = Math.Min (left, other.left);
			var newRight = Math.Max (right, other.right);
			var newTop = Math.Min (top, other.top);
			var newBottom = Math.Max (bottom, other.bottom);
			return new (newLeft, newRight, newTop, newBottom);
		}
	}

	// If we aren't going to save at least x% from the diff,
	// don't use it and store the whole surface instead
	private const int MINIMUM_SAVINGS_PERCENT = 10;

	private readonly BitArray bitmask;
	private RectangleI bounds;
	private readonly ColorBgra[] pixels;

	#region Constructors
	private SurfaceDiff (BitArray bitmask, RectangleI bounds, ColorBgra[] pixels)
	{
		this.bitmask = bitmask;
		this.bounds = bounds;
		this.pixels = pixels;
	}

	public static SurfaceDiff? Create (ImageSurface original, ImageSurface updated_surf, bool force = false)
	{
		if (original.Width != updated_surf.Width || original.Height != updated_surf.Height) {
			// If the surface changed size, only throw an error if the user forced the use of a diff.
			if (force) {
				throw new InvalidOperationException ("SurfaceDiff requires surfaces to be same size.");
			} else {
				return null;
			}
		}

		// Cache some pinvokes
		var orig_width = original.Width;
		var orig_height = original.Height;

#if DEBUG_DIFF
		Console.WriteLine ("Original surface size: {0}x{1}", orig_width, orig_height);
		System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch ();
		timer.Start ();
#endif

		// STEP 1 - Find the bounds of the changed pixels.
		DiffBounds diff_bounds = new DiffBounds (orig_width, orig_height);
		object diff_bounds_lock = new ();

		// Split up the work among several threads, each of which processes one row at a time
		// and updates the bounds of the changed pixels it has seen so far. At the end, the
		// results from each thread are merged together to find the overall bounds of the changed
		// pixels.
		Parallel.For<DiffBounds> (0, orig_height, () => new DiffBounds (orig_width, orig_height),
			     (row, loop, my_bounds) => {
				     var offset = row * orig_width;
				     var orig_row = original.GetReadOnlyPixelData ().Slice (offset, orig_width);
				     var updated_row = updated_surf.GetPixelData ().Slice (offset, orig_width);

				     bool change_in_row = false;

				     for (int i = 0; i < orig_width; ++i) {
					     if (orig_row[i] != updated_row[i]) {
						     change_in_row = true;
						     my_bounds.left = System.Math.Min (my_bounds.left, i);
						     my_bounds.right = System.Math.Max (my_bounds.right, i);
					     }
				     }

				     if (change_in_row) {
					     my_bounds.top = System.Math.Min (my_bounds.top, row);
					     my_bounds.bottom = System.Math.Max (my_bounds.bottom, row);
				     }

				     return my_bounds;

			     }, (my_bounds) => {
				     lock (diff_bounds_lock) {
					     diff_bounds = diff_bounds.AsMerged (my_bounds);
				     }
				     return;
			     });

		var bounds = new RectangleI (diff_bounds.left, diff_bounds.top,
					     diff_bounds.right - diff_bounds.left + 1, diff_bounds.bottom - diff_bounds.top + 1);

#if DEBUG_DIFF
		Console.WriteLine ("Truncated surface size: {0}x{1}", bounds.Width, bounds.Height);
#endif

		// STEP 2 - Create a bitarray of whether each pixel in the bounds has changed, and count
		// how many changed pixels we need to store.
		var bitmask = new BitArray (bounds.Width * bounds.Height);
		int index = 0;
		int num_changed = 0;

		int bottom = bounds.Bottom;
		int right = bounds.Right;
		int bounds_x = bounds.X;
		int bounds_y = bounds.Y;

		for (int y = bounds_y; y <= bottom; ++y) {
			var offset = y * orig_width;
			var orig_row = original.GetReadOnlyPixelData ().Slice (offset, orig_width);
			var updated_row = updated_surf.GetPixelData ().Slice (offset, orig_width);

			for (int x = bounds_x; x <= right; ++x) {
				bool changed = orig_row[x] != updated_row[x];
				bitmask[index++] = changed;
				if (changed) {
					num_changed++;
				}
			}
		}

		var savings = 100 - (float) num_changed / (float) (orig_width * orig_height) * 100;
#if DEBUG_DIFF
		Console.WriteLine ("Compressed bitmask: {0}/{1} = {2}%", num_changed, orig_height * orig_width, 100 - savings);
#endif

		if (!force && savings < MINIMUM_SAVINGS_PERCENT) {
#if DEBUG_DIFF
			Console.WriteLine ("Savings too small, returning null");
#endif
			return null;
		}

		// Store the old pixels.
		var pixels = new ColorBgra[num_changed];
		var orig_data = original.GetPixelData ();
		int mask_index = 0;

		int pixels_idx = 0;
		for (int y = bounds_y; y <= bottom; ++y) {
			int orig_idx = bounds_x + y * orig_width;

			for (int x = bounds_x; x <= right; ++x) {
				if (bitmask[mask_index++]) {
					pixels[pixels_idx++] = orig_data[y * orig_width + x];
				}
			}
		}

#if DEBUG_DIFF
		timer.Stop ();
		System.Console.WriteLine ("SurfaceDiff time: " + timer.ElapsedMilliseconds);
#endif

		return new SurfaceDiff (bitmask, bounds, pixels);
	}
	#endregion

	#region Public Methods
	public void Apply (ImageSurface dst)
	{
		ApplyAndSwap (dst, false);
	}

	public void ApplyAndSwap (ImageSurface dst)
	{
		ApplyAndSwap (dst, true);
	}

	public RectangleI GetBounds ()
	{
		return bounds;
	}
	#endregion

	#region Private Methods
	private void ApplyAndSwap (ImageSurface dst, bool swap)
	{
		dst.Flush ();

		var dest_width = dst.Width;
		var dst_data = dst.GetPixelData ();
		var mask_index = 0;
		int pixel_idx = 0;
		ColorBgra swap_pixel;

		for (int y = bounds.Y; y <= bounds.Bottom; y++) {
			int dst_idx = bounds.X + y * dest_width;
			for (int x = bounds.X; x <= bounds.Right; x++) {
				if (bitmask[mask_index++]) {
					if (swap) {
						swap_pixel = dst_data[dst_idx];
						dst_data[dst_idx] = pixels[pixel_idx];
						pixels[pixel_idx] = swap_pixel;
					} else {
						dst_data[dst_idx] = pixels[pixel_idx];
					}

					++pixel_idx;
				}

				++dst_idx;
			}

		}

		dst.MarkDirty ();
	}
	#endregion
}
