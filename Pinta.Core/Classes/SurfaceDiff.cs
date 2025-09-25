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

		// STEP 1 - Find the bounds of the changed pixels.
		RectangleI? differenceBounds = CalculateDifferenceBounds (original, updated, originalSize);

		// No changes were detected, so the variable was never set
		if (!differenceBounds.HasValue)
			return null;

		RectangleI finalBounds = differenceBounds.Value;

		// STEP 2 - Create a bitarray of whether each pixel in the bounds has changed, and count
		// how many changed pixels we need to store.

		(int changeCount, BitArray bitmask) = CalculateChanges (original, updated, finalBounds, originalSize);
		float savings = 100 - changeCount / (float) (originalSize.Width * originalSize.Height) * 100;
		if (!force && savings < MINIMUM_SAVINGS_PERCENT)
			return null;

		// Store the old pixels.
		ColorBgra[] pixels = new ColorBgra[changeCount];

		ReadOnlySpan<ColorBgra> originalData = original.GetReadOnlyPixelData ();
		int maskIndex = 0;
		int bottom = finalBounds.Bottom;
		int right = finalBounds.Right;
		int pixelsIndex = 0;
		for (int y = finalBounds.Y; y <= bottom; ++y) {
			int originalIndex = finalBounds.X + y * originalSize.Width;
			for (int x = finalBounds.X; x <= right; ++x) {
				if (!bitmask[maskIndex++]) continue;
				pixels[pixelsIndex++] = originalData[y * originalSize.Width + x];
			}
		}

		return new (bitmask, finalBounds, pixels);
	}

	private static (int ChangeCount, BitArray BitMask) CalculateChanges (
		ImageSurface original,
		ImageSurface updated,
		RectangleI bounds,
		Size size)
	{
		BitArray bitmask = new (bounds.Width * bounds.Height);
		int result = 0;

		int index = 0;

		int bottom = bounds.Bottom;
		int right = bounds.Right;
		int boundsX = bounds.X;
		int boundsY = bounds.Y;

		for (int y = boundsY; y <= bottom; ++y) {

			int offset = y * size.Width;

			ReadOnlySpan<ColorBgra> originalRow =
				original
				.GetReadOnlyPixelData ()
				.Slice (offset, size.Width);

			Span<ColorBgra> updatedRow =
				updated
				.GetPixelData ()
				.Slice (offset, size.Width);

			for (int x = boundsX; x <= right; ++x) {
				bool changed = originalRow[x] != updatedRow[x];
				bitmask[index++] = changed;
				if (!changed) continue;
				result++;
			}
		}

		return (result, bitmask);
	}

	private static RectangleI? CalculateDifferenceBounds (ImageSurface original, ImageSurface updated, Size size)
	{
		RectangleI? diffBounds = null;

		object diffBoundsLock = new ();

		// Split up the work among several threads, each of which processes one row at a time
		// and updates the bounds of the changed pixels it has seen so far. At the end, the
		// results from each thread are merged together to find the overall bounds of the changed
		// pixels.
		Parallel.For (
			0,
			size.Height,
			() => (RectangleI?) null,
			(row, loop, newBounds) => {

				int offset = row * size.Width;

				ReadOnlySpan<ColorBgra> originalRow =
					original
					.GetReadOnlyPixelData ()
					.Slice (offset, size.Width);

				Span<ColorBgra> updatedRow =
					updated
					.GetPixelData ()
					.Slice (offset, size.Width);

				// These variables start in an 'inverted' state
				// (i.e. left > right), but their values are only
				// used if a change in the updated row was detected,
				// at which point these values are updated, and
				// then it becomes left <= right
				int rowLeft = size.Width;
				int rowRight = -1;

				bool changeInRow = false;

				for (int i = 0; i < size.Width; ++i) {
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

		return diffBounds;
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
