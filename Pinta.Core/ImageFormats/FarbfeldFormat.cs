using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using Cairo;

namespace Pinta.Core;

public sealed class FarbfeldFormat : IImageExporter, IImageImporter
{
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
		uint width = Convert.ToUInt32 (flattenedImage.Width);
		uint height = Convert.ToUInt32 (flattenedImage.Height);
		ReadOnlySpan<ColorBgra> pixels = flattenedImage.GetReadOnlyPixelData ();
		outputStream.Write (farbfeld_signature_bytes);
		Span<byte> widthBytes = stackalloc byte[4];
		Span<byte> heightBytes = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32BigEndian (widthBytes, width);
		BinaryPrimitives.WriteUInt32BigEndian (heightBytes, height);
		outputStream.Write (widthBytes);
		outputStream.Write (heightBytes);
		Span<byte> bytesR = stackalloc byte[2];
		Span<byte> bytesG = stackalloc byte[2];
		Span<byte> bytesB = stackalloc byte[2];
		Span<byte> bytesA = stackalloc byte[2];
		foreach (var pixel in pixels) {
			FarbfeldPixel farbfeldPixel = ToFarbfeldPixel (in pixel);
			BinaryPrimitives.WriteUInt16BigEndian (bytesR, farbfeldPixel.R);
			BinaryPrimitives.WriteUInt16BigEndian (bytesG, farbfeldPixel.G);
			BinaryPrimitives.WriteUInt16BigEndian (bytesB, farbfeldPixel.B);
			BinaryPrimitives.WriteUInt16BigEndian (bytesA, farbfeldPixel.A);
			outputStream.Write (bytesR);
			outputStream.Write (bytesG);
			outputStream.Write (bytesB);
			outputStream.Write (bytesA);
		}
	}

	private static FarbfeldPixel ToFarbfeldPixel (in ColorBgra pixel)
	{
		if (pixel.A == 0) return new (0, 0, 0, 0);

		// Un-premultiply
		ushort r = (ushort) (pixel.R * 65535 / pixel.A);
		ushort g = (ushort) (pixel.G * 65535 / pixel.A);
		ushort b = (ushort) (pixel.B * 65535 / pixel.A);
		ushort a = (ushort) (pixel.A * 256u);

		return new (
			r: r,
			g: g,
			b: b,
			a: a);
	}

	public Document Import (Gio.File file)
	{
		using GioStream stream = new (file.Read (cancellable: null));

		FarbfeldReader reader = new (stream);

		reader.ReadSignature ();
		Size imageSize = reader.ReadSize ();

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

		int pixelCount = checked(imageSize.Width * imageSize.Height);

		for (int i = 0; i < pixelCount; i++) {
			FarbfeldPixel farbfeldPixel = reader.ReadPixel ();
			pixels[i] = ToColorBgra (in farbfeldPixel);
		}

		layer.Surface.MarkDirty ();

		return newDocument;
	}

	private readonly ref struct FarbfeldReader (Stream stream)
	{
		public void ReadSignature ()
		{
			Span<byte> signatureBytes = stackalloc byte[8];
			stream.ReadExactly (signatureBytes);
			string signatureString = ASCIIEncoding.ASCII.GetString (signatureBytes);
			if (signatureString != farbfeld_signature)
				throw new FormatException ($"Signature is not correct. It should be '{farbfeld_signature}'");
		}

		public Size ReadSize ()
		{
			Span<byte> widthBytes = stackalloc byte[4];
			Span<byte> heightBytes = stackalloc byte[4];
			stream.ReadExactly (widthBytes);
			stream.ReadExactly (heightBytes);
			int width = (int) BinaryPrimitives.ReadUInt32BigEndian (widthBytes);
			int height = (int) BinaryPrimitives.ReadUInt32BigEndian (heightBytes);
			return new (width, height);
		}

		public FarbfeldPixel ReadPixel ()
		{
			Span<byte> rBytes = stackalloc byte[2];
			Span<byte> gBytes = stackalloc byte[2];
			Span<byte> bBytes = stackalloc byte[2];
			Span<byte> aBytes = stackalloc byte[2];
			stream.ReadExactly (rBytes);
			stream.ReadExactly (gBytes);
			stream.ReadExactly (bBytes);
			stream.ReadExactly (aBytes);
			return new (
				r: BinaryPrimitives.ReadUInt16BigEndian (rBytes),
				g: BinaryPrimitives.ReadUInt16BigEndian (gBytes),
				b: BinaryPrimitives.ReadUInt16BigEndian (bBytes),
				a: BinaryPrimitives.ReadUInt16BigEndian (aBytes));
		}
	}

	private static ColorBgra ToColorBgra (in FarbfeldPixel pixel)
	{
		byte a8 = (byte) (pixel.A / 256);

		if (a8 == 0) return ColorBgra.Transparent;

		return ColorBgra.FromBgra (
			b: (byte) ((pixel.B * a8 + 32767) / 65535),
			g: (byte) ((pixel.G * a8 + 32767) / 65535),
			r: (byte) ((pixel.R * a8 + 32767) / 65535),
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
