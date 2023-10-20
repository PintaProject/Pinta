using System;
using System.Collections;

namespace Pinta.Core;

/// <summary>
/// Represents a two-dimensional matrix of bits used to store a
/// true/false value for each pixel in an image.
/// </summary>
public sealed class BitMask
{
	private readonly BitArray array;

	public BitMask (int width, int height)
	{
		if (width < 0)
			throw new ArgumentOutOfRangeException (nameof (width));
		if (height < 0)
			throw new ArgumentOutOfRangeException (nameof (height));

		Width = width;
		Height = height;

		array = new BitArray (width * height);
	}

	private BitMask (BitMask other)
	{
		Width = other.Width;
		Height = other.Height;
		array = (BitArray) other.array.Clone ();
	}

	public BitMask Clone () => new (this);

	public bool this[PointI pt] {
		get => this[pt.X, pt.Y];
		set => this[pt.X, pt.Y] = value;
	}

	public bool this[int x, int y] {
		get => Get (x, y);
		set => Set (x, y, value);
	}

	public int Width { get; }

	public int Height { get; }

	public bool IsEmpty => array.Length == 0;

	public void Clear (bool newValue) => array.SetAll (newValue);

	public bool Get (int x, int y) => array.Get (GetIndex (x, y));

	public void Invert (int x, int y)
	{
		var index = GetIndex (x, y);
		array[index] = !array[index];
	}

	public void Invert (Scanline scan)
	{
		var x = scan.X;
		while (x < scan.X + scan.Length) {
			Invert (x, scan.Y);
			++x;
		}
	}

	public void Set (int x, int y, bool newValue) => array[GetIndex (x, y)] = newValue;

	public void Set (RectangleI rect, bool newValue)
	{
		for (var y = rect.Y; y <= rect.Bottom; ++y)
			for (var x = rect.X; x <= rect.Right; ++x)
				Set (x, y, newValue);
	}

	private int GetIndex (int x, int y)
	{
		if (x < 0)
			throw new ArgumentOutOfRangeException (nameof (x));
		if (y < 0)
			throw new ArgumentOutOfRangeException (nameof (y));
		return (y * Width) + x;
	}

	/// <summary>
	/// Mirrors the bits in the BitMask along the X axis,
	/// such that the bits that used to be on the left side
	/// are now on the right side and vice versa.
	/// </summary>
	public void FlipHorizontal ()
	{
		int flippableWidth = Width / 2;
		for (int h = 0; h < Height; h++) {
			for (int w = 0; w < flippableWidth; w++) {
				int rightIndex = Width - 1 - w;
				bool originalLeft = this[w, h];
				bool originalRight = this[rightIndex, h];
				this[w, h] = originalRight;
				this[rightIndex, h] = originalLeft;
			}
		}
	}

	/// <summary>
	/// Mirrors the bits in the BitMask along the Y axis,
	/// such that the bits that used to be on the top side
	/// are now on the bottom side and vice versa.
	/// </summary>
	public void FlipVertical ()
	{
		int flippableHeight = Height / 2;
		for (int h = 0; h < flippableHeight; h++) {
			for (int w = 0; w < Width; w++) {
				int bottomIndex = Height - 1 - h;
				bool originalTop = this[w, h];
				bool originalBottom = this[w, bottomIndex];
				this[w, h] = originalBottom;
				this[w, bottomIndex] = originalTop;
			}
		}
	}

	public override bool Equals (object? obj)
	{
		if (obj is not BitMask other) return false;
		if (Width != other.Width) return false;
		if (Height != other.Height) return false;
		for (int i = 0; i < array.Length; i++) {
			if (array[i] != other.array[i])
				return false;
		}
		return true;
	}

	/// <summary>
	/// Inverts all bits in the bitmask
	/// </summary>
	public void Not () => array.Not ();

	/// <summary>
	/// Performs bit-by-bit AND in the area where
	/// this and <paramref name="other"/> overlap
	/// </summary>
	public void And (BitMask other)
	{
		if (Width == 0 || Height == 0) return;

		if (Width == other.Width && Height == other.Height) {
			array.And (other.array);
			return;
		}

		RectangleI overlap = GetOverlap (other, PointI.Zero);
		if (overlap.IsEmpty) return;
		int right = overlap.Right;
		int bottom = overlap.Bottom;
		for (int x = overlap.Left; x <= right; x++)
			for (int y = overlap.Top; y <= bottom; y++)
				this[x, y] = this[x, y] && other[x, y];
	}

	/// <summary>
	/// Performs bit-by-bit OR in the area where
	/// this and <paramref name="other"/> overlap
	/// </summary>
	public void Or (BitMask other)
	{
		if (Width == 0 || Height == 0) return;

		if (Width == other.Width && Height == other.Height) {
			array.Or (other.array);
			return;
		}

		RectangleI overlap = GetOverlap (other, PointI.Zero);
		if (overlap.IsEmpty) return;
		int right = overlap.Right;
		int bottom = overlap.Bottom;
		for (int x = overlap.Left; x <= right; x++)
			for (int y = overlap.Top; y <= bottom; y++)
				this[x, y] = this[x, y] || other[x, y];
	}

	/// <summary>
	/// Performs bit-by-bit XOR in the area where
	/// this and <paramref name="other"/> overlap
	/// </summary>
	public void Xor (BitMask other)
	{
		if (Width == 0 || Height == 0) return;

		if (Width == other.Width && Height == other.Height) {
			array.Xor (other.array);
			return;
		}

		RectangleI overlap = GetOverlap (other, PointI.Zero);
		if (overlap.IsEmpty) return;
		int right = overlap.Right;
		int bottom = overlap.Bottom;
		for (int x = overlap.Left; x <= right; x++)
			for (int y = overlap.Top; y <= bottom; y++)
				this[x, y] = this[x, y] ^ other[x, y];
	}

	/// <param name="bounds"></param>
	/// <returns>
	/// A new bitmask with the width and height specified in
	/// <paramref name="bounds"/>, and the bits set to the values
	/// in the area demarcated by it
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public BitMask CloneArea (RectangleI bounds)
	{
		if (bounds.X < 0 || bounds.Y < 0 || bounds.Right >= Width || bounds.Bottom >= Height)
			throw new ArgumentOutOfRangeException (nameof (bounds), "Chosen area out of bounds");

		BitMask submask = new (bounds.Width, bounds.Height);

		int right = bounds.Right;
		int bottom = bounds.Bottom;
		for (int y = bounds.Top; y <= bottom; y++) {
			for (int x = bounds.Left; x <= right; x++) {
				submask[x - bounds.Left, y - bounds.Top] = this[x, y];
			}
		}

		return submask;
	}

	private RectangleI GetOverlap (BitMask other, PointI offset)
	{
		if (Width == 0 || Height == 0) return RectangleI.Zero;

		RectangleI thisRect = new (
			point: PointI.Zero,
			width: Width,
			height: Height
		);

		RectangleI otherRect = new (
			point: offset,
			width: other.Width,
			height: other.Height
		);

		return thisRect.Intersect (otherRect);
	}

	public override int GetHashCode () => Width.GetHashCode () ^ Height.GetHashCode ();
}
