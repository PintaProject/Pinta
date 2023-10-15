using System;
using System.Collections;

namespace Pinta.Core;

/// <summary>
/// Represents a two-dimensional matrix of bits used to store a
/// true/false value for each pixel in an image.
/// </summary>
public sealed class BitMask : ICloneable
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

	private BitMask (BitMask source)
	{
		Width = source.Width;
		Height = source.Height;
		array = (BitArray) source.array.Clone ();
	}

	object ICloneable.Clone () => Clone ();

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

	public void InvertAll ()
	{
		for (int i = 0; i < array.Length; i++)
			array[i] = !array[i];
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

	public override int GetHashCode () => Width.GetHashCode () ^ Height.GetHashCode ();
}
