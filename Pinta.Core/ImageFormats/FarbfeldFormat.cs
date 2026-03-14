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
		using BufferedStream bufferedStream = new (outputStream);
		Export (flattenedImage, bufferedStream);
	}

	private static readonly string farbfeld_signature = "farbfeld";
	private static readonly byte[] farbfeld_signature_bytes = Encoding.ASCII.GetBytes (farbfeld_signature);

	public static void Export (ImageSurface flattenedImage, Stream outputStream)
	{
		FarbfeldWriter writer = new (outputStream);
		writer.WriteSignature ();
		writer.WriteSize (flattenedImage.GetSize ());
		ReadOnlySpan<ColorBgra> pixels = flattenedImage.GetReadOnlyPixelData ();
		foreach (var pixel in pixels)
			writer.WritePixel (in pixel);
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
		layer.Surface.Flush ();
		Span<ColorBgra> pixels = layer.Surface.GetPixelData ();
		int pixelCount = checked(imageSize.Width * imageSize.Height);
		for (int i = 0; i < pixelCount; i++) pixels[i] = reader.ReadPixel ();
		layer.Surface.MarkDirty ();
		return newDocument;
	}

	private static FarbfeldPixel ToFarbfeldPixel (in ColorBgra pixel)
	{
		if (pixel.A == 0) return new (0, 0, 0, 0);

		// Un-premultiply
		ushort r = (ushort) Math.Min ((pixel.R * 65535 + pixel.A / 2) / pixel.A, 65535);
		ushort g = (ushort) Math.Min ((pixel.G * 65535 + pixel.A / 2) / pixel.A, 65535);
		ushort b = (ushort) Math.Min ((pixel.B * 65535 + pixel.A / 2) / pixel.A, 65535);
		ushort a = (ushort) (pixel.A * 257u); // 255 * 257 = 65535, so this maps 0-255 to 0-65535

		return new (
			r: r,
			g: g,
			b: b,
			a: a);
	}

	private static ColorBgra ToColorBgra (in FarbfeldPixel pixel)
	{
		byte a8 = (byte) (pixel.A / 256);
		if (a8 == 0) return ColorBgra.Transparent;
		byte b8 = (byte) ((pixel.B + 128) / 257);
		byte g8 = (byte) ((pixel.G + 128) / 257);
		byte r8 = (byte) ((pixel.R + 128) / 257);
		return ColorBgra.FromBgra (
			b: (byte) ((b8 * a8 + 127) / 255),
			g: (byte) ((g8 * a8 + 127) / 255),
			r: (byte) ((r8 * a8 + 127) / 255),
			a: a8);
	}

	private readonly ref struct FarbfeldWriter (Stream outputStream)
	{
		public void WriteSignature ()
		{
			outputStream.Write (farbfeld_signature_bytes);
		}

		public void WriteSize (Size size)
		{
			uint width = Convert.ToUInt32 (size.Width);
			uint height = Convert.ToUInt32 (size.Height);
			Span<byte> widthBytes = stackalloc byte[4];
			Span<byte> heightBytes = stackalloc byte[4];
			BinaryPrimitives.WriteUInt32BigEndian (widthBytes, width);
			BinaryPrimitives.WriteUInt32BigEndian (heightBytes, height);
			outputStream.Write (widthBytes);
			outputStream.Write (heightBytes);
		}

		public void WritePixel (in ColorBgra pixel)
		{
			FarbfeldPixel farbfeldPixel = ToFarbfeldPixel (in pixel);
			WriteFarbfeldPixel (in farbfeldPixel);
		}

		private void WriteFarbfeldPixel (in FarbfeldPixel pixel)
		{
			Span<byte> rBytes = stackalloc byte[2];
			Span<byte> gBytes = stackalloc byte[2];
			Span<byte> bBytes = stackalloc byte[2];
			Span<byte> aBytes = stackalloc byte[2];
			BinaryPrimitives.WriteUInt16BigEndian (rBytes, pixel.R);
			BinaryPrimitives.WriteUInt16BigEndian (gBytes, pixel.G);
			BinaryPrimitives.WriteUInt16BigEndian (bBytes, pixel.B);
			BinaryPrimitives.WriteUInt16BigEndian (aBytes, pixel.A);
			outputStream.Write (rBytes);
			outputStream.Write (gBytes);
			outputStream.Write (bBytes);
			outputStream.Write (aBytes);
		}
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
			int width = checked((int) BinaryPrimitives.ReadUInt32BigEndian (widthBytes));
			int height = checked((int) BinaryPrimitives.ReadUInt32BigEndian (heightBytes));
			return new (width, height);
		}

		public ColorBgra ReadPixel ()
		{
			FarbfeldPixel farbfeldPixel = ReadFarbfeldPixel ();
			return ToColorBgra (in farbfeldPixel);
		}

		private FarbfeldPixel ReadFarbfeldPixel ()
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
