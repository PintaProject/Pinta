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
using Cairo;

namespace Pinta.Core
{
	public class SurfaceDiff
	{
		// If we aren't going to save at least x% from the diff,
		// don't use it and store the whole surface instead
		private const int MINIMUM_SAVINGS_PERCENT = 10;

		private BitArray bitmask;
		private Gdk.Rectangle bounds;
		private ColorBgra[] pixels;

		#region Constructors
		private SurfaceDiff (BitArray bitmask, Gdk.Rectangle bounds, ColorBgra[] pixels)
		{
			this.bitmask = bitmask;
			this.bounds = bounds;
			this.pixels = pixels;
		}

		public static unsafe SurfaceDiff Create (ImageSurface original, ImageSurface updated, bool force = false)
		{
			if (original.Width != updated.Width || original.Height != updated.Height) {
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
#endif

			// STEP 1 - Create a bitarray of whether each pixel is changed (true for changed)
			var bitmask = new BitArray (orig_width * orig_height);

			var hit_first_change = false;
			var first_change = -1;
			var last_change = -1;

			var orig_ptr = (int*)original.DataPtr;
			var updated_ptr = (int*)updated.DataPtr;

			for (int i = 0; i < bitmask.Length; i++) {
				var changed = *(orig_ptr++) == *(updated_ptr++) ? false : true;
				bitmask.Set (i, changed);

				if (!hit_first_change) {
					if (changed) {
						first_change = i;
						hit_first_change = true;
					}
				}

				if (changed)
					last_change = i;
			}
			
			// STEP 2 - Figure out the bounds of the changed pixels
			var first_row = first_change / orig_width;
			var last_row = last_change / orig_width + 1;

			// We have to loop through the bitmask to find the first and last column
			first_change = -1;
			last_change = -1;
			hit_first_change = false;

			for (int x = 0; x < orig_width; x++)
				for (int y = 0; y < orig_height; y++) {
					var changed = bitmask[x + (y * orig_width)];

					if (!hit_first_change) {
						if (changed) {
							first_change = (x * orig_height) + y;
							hit_first_change = true;
						}
					}

					if (changed)
						last_change = (x * orig_height) + y;
				}

			var first_col = first_change / orig_height;
			var last_col = last_change / orig_height + 1;

			var bounds = new Gdk.Rectangle (first_col, first_row, last_col - first_col, last_row - first_row);

#if DEBUG_DIFF
			Console.WriteLine ("Truncated surface size: {0}x{1}", bounds.Width, bounds.Height);
#endif

			// If truncating doesn't save us at least x%, don't bother
			if (100 - (float)(bounds.Width * bounds.Height) / (float)(orig_width * orig_height) * 100 < MINIMUM_SAVINGS_PERCENT) {
				bounds = new Gdk.Rectangle (0, 0, orig_width, orig_height);

#if DEBUG_DIFF
				Console.WriteLine ("Truncating not worth it, skipping.");
#endif
			}

			// STEP 3 - Truncate our bitarray to the bounding rectangle
			if (bounds.Width != orig_width || bounds.Height != orig_height) {
				var new_bitmask = new BitArray (bounds.Width * bounds.Height);

				int index = 0;

				for (int y = bounds.Y; y <= bounds.GetBottom (); y++)
					for (int x = bounds.X; x <= bounds.GetRight (); x++)
						new_bitmask[index++] = bitmask[y * orig_width + x];

				bitmask = new_bitmask;
			}
			

			// STEP 4 - Count how many changed pixels we need to store
			var length = 0;

			for (int i = 0; i < bitmask.Length; i++)
				if (bitmask[i])
					length++;

			var savings = 100 - (float)length / (float)(orig_width * orig_height) * 100;
#if DEBUG_DIFF
			Console.WriteLine ("Compressed bitmask: {0}/{1} = {2}%", length, orig_height * orig_width, 100 - savings);
#endif

			if (!force && savings < MINIMUM_SAVINGS_PERCENT) {
#if DEBUG_DIFF
				Console.WriteLine ("Savings too small, returning null");
#endif
				return null;
			}

			// Store the old pixels
			var pixels = new ColorBgra[length];
			var new_ptr = (ColorBgra*)original.DataPtr;

			var mask_index = 0;

			fixed (ColorBgra* fixed_ptr = pixels) {
				var pixel_ptr = fixed_ptr;
				new_ptr += bounds.X + bounds.Y * orig_width;

				for (int y = bounds.Y; y <= bounds.GetBottom (); y++) {
					for (int x = bounds.X; x <= bounds.GetRight (); x++) {
						if (bitmask[mask_index++])
							*pixel_ptr++ = *new_ptr;

						new_ptr++;
					}

					new_ptr += orig_width - bounds.Width;
				}
			}

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

		public Gdk.Rectangle GetBounds ()
		{
			return bounds;
		}
		#endregion

		#region Private Methods
		private unsafe void ApplyAndSwap (ImageSurface dst, bool swap)
		{
			dst.Flush ();

			var dest_width = dst.Width;
			var dst_ptr = (ColorBgra*)dst.DataPtr;
			var mask_index = 0;
			ColorBgra swap_pixel;

			fixed (ColorBgra* fixed_ptr = pixels) {
				var pixel_ptr = fixed_ptr;
				dst_ptr += bounds.X + bounds.Y * dest_width;

				for (int y = bounds.Y; y <= bounds.GetBottom (); y++) {
					for (int x = bounds.X; x <= bounds.GetRight (); x++) {
						if (bitmask[mask_index++])
							if (swap) {
								swap_pixel = *dst_ptr;
								*dst_ptr = *pixel_ptr;
								*pixel_ptr++ = swap_pixel;
							} else {
								*dst_ptr = *pixel_ptr++;
							}

						dst_ptr++;
					}

					dst_ptr += dest_width - bounds.Width;
				}
			}
			
			dst.MarkDirty ();
		}
		#endregion
	}
}
