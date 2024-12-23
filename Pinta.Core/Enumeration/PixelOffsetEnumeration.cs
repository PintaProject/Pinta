using System;
using System.Collections;
using System.Collections.Generic;

namespace Pinta.Core;

/// <summary>
/// Contains the information of a ROI and its related canvas
/// such that an <see cref="IEnumerator{T}"/> of the positions
/// and memory offsets of the contained pixels can be created.
/// </summary>
/// <remarks>
/// This is instantiated by Pinta's core utility methods.
/// It is not intended to be instantiated directly.
/// </remarks>
public readonly struct PixelOffsetEnumerable : IEnumerable<PixelOffset>
{
	private readonly RectangleI roi_bounds;
	private readonly Size canvas_size;
	internal PixelOffsetEnumerable (in RectangleI roiBounds, in Size canvasSize)
	{
		if (roiBounds.Left < 0 || roiBounds.Right >= canvasSize.Width || roiBounds.Top < 0 || roiBounds.Bottom >= canvasSize.Height)
			throw new ArgumentException ($"Rectangle is out of size bounds");
		roi_bounds = roiBounds;
		canvas_size = canvasSize;
	}

	public PixelOffsetEnumerator GetEnumerator ()
		=> new (roi_bounds, canvas_size);

	IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
	IEnumerator<PixelOffset> IEnumerable<PixelOffset>.GetEnumerator () => GetEnumerator ();
}

/// <summary>
/// Iterates over all the positions and memory offsets
/// of the pixels in a ROI (taking its related canvas into account).
/// </summary>
/// <remarks>
/// Instantiated indirectly by <see cref="PixelOffsetEnumerable"/>.
/// It is not intended to be instantiated directly.
/// </remarks>
public struct PixelOffsetEnumerator : IEnumerator<PixelOffset>
{
	private readonly Size canvas_size;
	private readonly int left;
	private readonly int right;
	private readonly int top;
	private readonly int bottom;
	private int x;
	private int y;
	private int row_offset;
	private bool has_more_rows;
	internal PixelOffsetEnumerator (in RectangleI roiBounds, in Size canvasSize)
	{
		int l = roiBounds.Left;
		int r = roiBounds.Right;
		int t = roiBounds.Top;
		int b = roiBounds.Bottom;

		// --- Read-only
		canvas_size = canvasSize;
		left = l;
		right = r;
		top = t;
		bottom = b;

		// --- Mutable
		x = l - 1; // Initialize to just before the first x
		y = t;
		row_offset = t * canvasSize.Width;
		has_more_rows = t <= b;
	}

	public readonly PixelOffset Current => new (
		coordinates: new PointI (x, y),
		memoryOffset: row_offset + x);

	public bool MoveNext ()
	{
		if (!has_more_rows) return false;
		x++;
		if (x <= right) return true;
		x = left;
		y++;
		row_offset += canvas_size.Width;
		if (y <= bottom) return true;
		has_more_rows = false;
		return false;
	}

	public void Reset ()
	{
		x = left - 1; // Initialize to just before the first x
		y = top;
		row_offset = top * canvas_size.Width;
		has_more_rows = top <= bottom;
	}

	public void Dispose () { }

	readonly object IEnumerator.Current => Current;
}
