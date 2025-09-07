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
	// If we aren't going to save at least x% from the diff,
	// don't use it and store the whole surface instead
	private const int MINIMUM_SAVINGS_PERCENT = 10;

	private readonly BitArray bitmask;
	private readonly RectangleI bounds;
	private readonly ColorBgra[] pixels;

	private SurfaceDiff (BitArray bitmask, RectangleI bounds, ColorBgra[] pixels)
	{
		this.bitmask = bitmask;
		this.bounds = bounds;
		this.pixels = pixels;
	}

	public static SurfaceDiff? Create (ImageSurface original, ImageSurface updated, bool force = false)
	{
		Size originalSize = original.GetSize ();
		Size updatedSize = updated.GetSize ();

		if (originalSize != updatedSize) {

			// If the surface changed size, only throw an error if the user forced the use of a diff.
			if (force)
				throw new ArgumentException ($"Original and updated surfaces need to be same size.");

			return null;
		}

#if DEBUG_DIFF
		Console.WriteLine ("Original surface size: {0}x{1}", orig_width, orig_height);
		System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch ();
		timer.Start ();
#endif

		// STEP 1 - Find the bounds of the changed pixels.
		RectangleI? diffBounds = null;

		object diffBoundsLock = new ();

		// Split up the work among several threads, each of which processes one row at a time
		// and updates the bounds of the changed pixels it has seen so far. At the end, the
		// results from each thread are merged together to find the overall bounds of the changed
		// pixels.
		Parallel.For (
			0,
			originalSize.Height,
			() => (RectangleI?) null,
			(row, loop, newBounds) => {

				int offset = row * originalSize.Width;

				ReadOnlySpan<ColorBgra> originalRow =
					original
					.GetReadOnlyPixelData ()
					.Slice (offset, originalSize.Width);

				Span<ColorBgra> updatedRow =
					updated
					.GetPixelData ()
					.Slice (offset, originalSize.Width);

				// These variables start in an 'inverted' state
				// (i.e. left > right), but their values are only
				// used if a change in the updated row was detected,
				// at which point these values are updated, and
				// then it becomes left <= right
				int rowLeft = originalSize.Width;
				int rowRight = -1;

				bool changeInRow = false;

				for (int i = 0; i < originalSize.Width; ++i) {
					if (originalRow[i] == updatedRow[i]) continue;
					changeInRow = true;
					rowLeft = Math.Min (rowLeft, i);
					rowRight = Math.Max (rowRight, i);
				}

				if (!changeInRow)
					return newBounds;

				RectangleI newRect = RectangleI.FromLTRB (rowLeft, row, rowRight, row);

				return
					newBounds.HasValue
					? newBounds.Value.Union (newRect)
					: newRect;
			},
			myBounds => {

				if (!myBounds.HasValue)
					return;

				lock (diffBoundsLock) {
					diffBounds =
						(diffBounds.HasValue)
						? diffBounds.Value.Union (myBounds.Value)
						: myBounds;
				}
			}
		);

		// No changes were detected, so the variable was never set
		if (!diffBounds.HasValue)
			return null;
#if DEBUG_DIFF
		Console.WriteLine ("Truncated surface size: {0}x{1}", bounds.Width, bounds.Height);
#endif

		RectangleI finalBounds = diffBounds.Value;

		// STEP 2 - Create a bitarray of whether each pixel in the bounds has changed, and count
		// how many changed pixels we need to store.

		BitArray bitmask = new (finalBounds.Width * finalBounds.Height);

		int index = 0;
		int changeCount = 0;

		int bottom = finalBounds.Bottom;
		int right = finalBounds.Right;
		int boundsX = finalBounds.X;
		int boundsY = finalBounds.Y;

		for (int y = boundsY; y <= bottom; ++y) {

			int offset = y * originalSize.Width;

			ReadOnlySpan<ColorBgra> originalRow =
				original
				.GetReadOnlyPixelData ()
				.Slice (offset, originalSize.Width);

			Span<ColorBgra> updatedRow =
				updated
				.GetPixelData ()
				.Slice (offset, originalSize.Width);

			for (int x = boundsX; x <= right; ++x) {
				bool changed = originalRow[x] != updatedRow[x];
				bitmask[index++] = changed;
				if (changed) {
					changeCount++;
				}
			}
		}

		float savings = 100 - changeCount / (float) (originalSize.Width * originalSize.Height) * 100;
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
		ColorBgra[] pixels = new ColorBgra[changeCount];

		ReadOnlySpan<ColorBgra> originalData = original.GetReadOnlyPixelData ();
		int maskIndex = 0;

		int pixelsIndex = 0;
		for (int y = boundsY; y <= bottom; ++y) {
			int originalIndex = boundsX + y * originalSize.Width;
			for (int x = boundsX; x <= right; ++x) {
				if (bitmask[maskIndex++]) {
					pixels[pixelsIndex++] = originalData[y * originalSize.Width + x];
				}
			}
		}

#if DEBUG_DIFF
		timer.Stop ();
		System.Console.WriteLine ("SurfaceDiff time: " + timer.ElapsedMilliseconds);
#endif

		return new (bitmask, finalBounds, pixels);
	}

	public void Apply (ImageSurface dst)
	{
		ApplyAndSwap (dst, false);
	}

	public void ApplyAndSwap (ImageSurface dst)
	{
		ApplyAndSwap (dst, true);
	}

	public RectangleI GetBounds ()
		=> bounds;

	private void ApplyAndSwap (ImageSurface destination, bool swap)
	{
		destination.Flush ();

		int destinationWidth = destination.Width;
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		int maskIndex = 0;
		int pixelIndex = 0;
		ColorBgra swapPixel;

		for (int y = bounds.Y; y <= bounds.Bottom; y++) {
			int destinationIndex = bounds.X + y * destinationWidth;
			for (int x = bounds.X; x <= bounds.Right; x++) {
				if (bitmask[maskIndex++]) {
					if (swap) {
						swapPixel = destinationData[destinationIndex];
						destinationData[destinationIndex] = pixels[pixelIndex];
						pixels[pixelIndex] = swapPixel;
					} else {
						destinationData[destinationIndex] = pixels[pixelIndex];
					}

					++pixelIndex;
				}

				++destinationIndex;
			}

		}

		destination.MarkDirty ();
	}
}
