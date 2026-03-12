using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Cairo;

namespace Pinta.Core;

public sealed class FarbfeldFormat : IImageExporter
{
	private static readonly Func<uint, uint> adjust_endianness_32;
	private static readonly Func<ushort, ushort> adjust_endianness_16;
	static FarbfeldFormat ()
	{
		bool shouldReverse = BitConverter.IsLittleEndian; // Farbfeld is big-endian
		adjust_endianness_16 = shouldReverse ? BinaryPrimitives.ReverseEndianness : n => n;
		adjust_endianness_32 = shouldReverse ? BinaryPrimitives.ReverseEndianness : n => n;
	}

	private static uint AdjustEndianness (uint value)
		=> adjust_endianness_32 (value);

	private static ushort AdjustEndianness (ushort value)
		=> adjust_endianness_16 (value);

	public void Export (Document document, Gio.File file, Gtk.Window parent)
	{
		using ImageSurface flattenedImage = document.GetFlattenedImage ();
		using GioStream outputStream = new (file.Replace ());
		Export (flattenedImage, outputStream);
	}

	public static void Export (ImageSurface flattenedImage, Stream outputStream)
	{
		uint width = AdjustEndianness (Convert.ToUInt32 (flattenedImage.Width));
		uint height = AdjustEndianness (Convert.ToUInt32 (flattenedImage.Height));
		ReadOnlySpan<ColorBgra> pixels = flattenedImage.GetReadOnlyPixelData ();
		outputStream.Write (ASCIIEncoding.ASCII.GetBytes ("farbfeld"));
		outputStream.Write (BitConverter.GetBytes (width));
		outputStream.Write (BitConverter.GetBytes (height));
		foreach (var pixel in pixels) {
			FarbfeldPixel farbfeldPixel = ToFarbfeldPixel (in pixel);
			outputStream.Write (BitConverter.GetBytes (farbfeldPixel.R));
			outputStream.Write (BitConverter.GetBytes (farbfeldPixel.G));
			outputStream.Write (BitConverter.GetBytes (farbfeldPixel.B));
			outputStream.Write (BitConverter.GetBytes (farbfeldPixel.A));
		}
	}

	private static FarbfeldPixel ToFarbfeldPixel (in ColorBgra pixel)
	{
		if (pixel.A == 0) return new (0, 0, 0, 0);

		// Un-premultiply
		ushort baseR = (ushort) (pixel.R * 65535 / pixel.A);
		ushort baseG = (ushort) (pixel.G * 65535 / pixel.A);
		ushort baseB = (ushort) (pixel.B * 65535 / pixel.A);
		ushort baseA = (ushort) (pixel.A * 256u);

		// Adjust endianness
		ushort adjustedR = AdjustEndianness (baseR);
		ushort adjustedG = AdjustEndianness (baseG);
		ushort adjustedB = AdjustEndianness (baseB);
		ushort adjustedA = AdjustEndianness (baseA);

		return new (
			r: adjustedR,
			g: adjustedG,
			b: adjustedB,
			a: adjustedA);
	}

	private readonly ref struct FarbfeldPixel (
		ushort r,
		ushort g,
		ushort b,
		ushort a)
	{
		public readonly ushort R { get; } = r;
		public readonly ushort G { get; } = g;
		public readonly ushort B { get; } = b;
		public readonly ushort A { get; } = a;
	}
}
