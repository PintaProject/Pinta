using System;
using System.Collections;

namespace Pinta.Core
{
	// Represent a two dimensional matrix of bits used to store a
	// true/false value for each pixel in an image.
	public class BitMask
	{
		private readonly BitArray array;

		public BitMask (int width, int height)
		{
			Width = width;
			Height = height;

			array = new BitArray (width * height);
		}

		public bool this[Cairo.Point pt] {
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

		public void Set (Gdk.Rectangle rect, bool newValue)
		{
			for (var y = rect.Y; y <= rect.GetBottom (); ++y) {
				for (var x = rect.X; x <= rect.GetRight (); ++x) {
					Set (x, y, newValue);
				}
			}
		}

		private int GetIndex (int x, int y) => (y * Width) + x;
	}
}
