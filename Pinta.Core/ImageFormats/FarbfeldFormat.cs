using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Cairo;

namespace Pinta.Core;

public sealed class FarbfeldFormat : IImageExporter, IImageImporter
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

	private static readonly string farbfeld_signature = "farbfeld";
	private static readonly byte[] farbfeld_signature_bytes = Encoding.ASCII.GetBytes (farbfeld_signature);

	public static void Export (ImageSurface flattenedImage, Stream outputStream)
	{
		uint width = AdjustEndianness (Convert.ToUInt32 (flattenedImage.Width));
		uint height = AdjustEndianness (Convert.ToUInt32 (flattenedImage.Height));
		ReadOnlySpan<ColorBgra> pixels = flattenedImage.GetReadOnlyPixelData ();
		outputStream.Write (farbfeld_signature_bytes);
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

	public Document Import (Gio.File file)
	{
		using GioStream stream = new (file.Read (cancellable: null));
		using BinaryReader reader = new (stream);

		Span<byte> signatureBytes = stackalloc byte[8];

		int readSignatureBytes = reader.Read (signatureBytes);
		if (readSignatureBytes != 8) throw new FormatException ("Unexpected end of file while reading signature");
		string signatureString = ASCIIEncoding.ASCII.GetString (signatureBytes);
		if (signatureString != farbfeld_signature) throw new FormatException ($"Signature is not correct. It should be '{farbfeld_signature}'");

		Span<byte> widthBytes = stackalloc byte[4];
		Span<byte> heightBytes = stackalloc byte[4];

		int readWidthBytes = reader.Read (widthBytes);
		int readHeightBytes = reader.Read (heightBytes);

		if (readWidthBytes != 4) throw new FormatException ("Unexpected end of file while reading width");
		if (readHeightBytes != 4) throw new FormatException ("Unexpected end of file while reading height");

		int width = (int) AdjustEndianness (BitConverter.ToUInt32 (widthBytes));
		int height = (int) AdjustEndianness (BitConverter.ToUInt32 (heightBytes));

		Size imageSize = new (width, height);

		Document newDocument = new (
			PintaCore.Actions,
			PintaCore.Tools,
			PintaCore.Workspace,
			imageSize,
			file,
			"ff");
		Layer layer = newDocument.Layers.AddNewLayer (file.GetDisplayName ());
		Span<ColorBgra> pixels = layer.Surface.GetPixelData ();

		layer.Surface.Flush ();

		int pixelCount = checked(width * height);

		Span<byte> rBytes = stackalloc byte[2];
		Span<byte> gBytes = stackalloc byte[2];
		Span<byte> bBytes = stackalloc byte[2];
		Span<byte> aBytes = stackalloc byte[2];

		for (int i = 0; i < pixelCount; i++) {

			int readR = reader.Read (rBytes);
			int readG = reader.Read (gBytes);
			int readB = reader.Read (bBytes);
			int readA = reader.Read (aBytes);

			if (readR != 2 || readG != 2 || readB != 2 || readA != 2)
				throw new FormatException ("Unexpected end of file");

			FarbfeldPixel farbfeldPixel = new (
				r: BitConverter.ToUInt16 (rBytes),
				g: BitConverter.ToUInt16 (gBytes),
				b: BitConverter.ToUInt16 (bBytes),
				a: BitConverter.ToUInt16 (aBytes));

			pixels[i] = ToColorBgra (in farbfeldPixel);
		}

		layer.Surface.MarkDirty ();

		return newDocument;
	}

	private static ColorBgra ToColorBgra (in FarbfeldPixel pixel)
	{
		ushort r16 = AdjustEndianness (pixel.R);
		ushort g16 = AdjustEndianness (pixel.G);
		ushort b16 = AdjustEndianness (pixel.B);
		ushort a16 = AdjustEndianness (pixel.A);

		byte a8 = (byte) (a16 / 256);

		if (a8 == 0) return ColorBgra.Transparent;

		return ColorBgra.FromBgra (
			b: (byte) ((b16 * a8 + 32767) / 65535),
			g: (byte) ((g16 * a8 + 32767) / 65535),
			r: (byte) ((r16 * a8 + 32767) / 65535),
			a: a8);
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
