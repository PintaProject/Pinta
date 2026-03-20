using System;
using System.IO;
using System.Text;
using Cairo;
using Gtk;

namespace Pinta.Core;

public sealed class NetpbmPortablePixmap : IImageExporter, IImageImporter
{
	private const string MAGIC_SEQUENCE = "P3"; // Magic sequence for text-based portable pixmap format

	public Document Import (Gio.File file)
	{
		using GioStream stream = new GioStream (file.Read (cancellable: null));
		using StreamReader reader = new (stream, Encoding.ASCII);

		PpmTokenReader tokenizer = new (reader);

		string magic = tokenizer.ReadToken ();
		if (magic != MAGIC_SEQUENCE)
			throw new FormatException ($"Expected '{MAGIC_SEQUENCE}' magic sequence, got '{magic}'");

		int width = tokenizer.ReadPositiveInt ("Image width");
		int height = tokenizer.ReadPositiveInt ("Image height");
		int maxValue = tokenizer.ReadPositiveInt ("Max color value");

		Size imageSize = new (width, height);

		Document newDocument = new (
			PintaCore.Actions,
			PintaCore.Tools,
			PintaCore.Workspace,
			imageSize,
			file,
			"ppm");

		Layer layer = newDocument.Layers.AddNewLayer (file.GetDisplayName ());
		Span<ColorBgra> pixelData = layer.Surface.GetPixelData ();

		layer.Surface.Flush ();

		int pixelCount = width * height;
		if (maxValue == 255) {
			for (int i = 0; i < pixelCount; i++)
				pixelData[i] = FastReadPixel (ref tokenizer);
		} else {
			for (int i = 0; i < pixelCount; i++)
				pixelData[i] = ReadPixel (ref tokenizer, maxValue);
		}

		layer.Surface.MarkDirty ();

		return newDocument;

		static ColorBgra FastReadPixel (ref PpmTokenReader tokens)
		{
			int r = tokens.ReadColorComponent (255);
			int g = tokens.ReadColorComponent (255);
			int b = tokens.ReadColorComponent (255);
			return ColorBgra.FromBgra (
				b: (byte) b,
				g: (byte) g,
				r: (byte) r,
				a: byte.MaxValue);
		}

		static ColorBgra ReadPixel (ref PpmTokenReader tokens, int max)
		{
			int r = tokens.ReadColorComponent (max);
			int g = tokens.ReadColorComponent (max);
			int b = tokens.ReadColorComponent (max);
			return ColorBgra.FromBgra (
				b: ScaleToByteRange (b, max),
				g: ScaleToByteRange (g, max),
				r: ScaleToByteRange (r, max),
				a: byte.MaxValue);
		}
	}

	private static byte ScaleToByteRange (int value, int maxValue)
	{
		int rounded = (int) (value * 255.0 / maxValue);
		int clamped = Math.Clamp (rounded, byte.MinValue, byte.MaxValue);
		return (byte) clamped;
	}

	public void Export (ImageSurface flattenedImage, Stream outputStream)
	{
		using StreamWriter writer = new (outputStream, Encoding.ASCII) {
			NewLine = "\n", // Same output, including line endings, on every platform
		};

		Size imageSize = flattenedImage.GetSize ();
		ReadOnlySpan<ColorBgra> pixelData = flattenedImage.GetReadOnlyPixelData ();

		writer.WriteLine (MAGIC_SEQUENCE);
		writer.WriteLine ($"{imageSize.Width} {imageSize.Height}");
		writer.WriteLine (byte.MaxValue);

		for (int row = 0; row < imageSize.Height; row++) {
			int rowStart = row * imageSize.Width;
			int rowEnd = rowStart + imageSize.Width;
			for (int index = rowStart; index < rowEnd; index++) {
				ColorBgra color = pixelData[index];
				WritePixel (color);
				if (index != rowEnd - 1)
					writer.Write ("   ");
			}
			writer.WriteLine ();
		}

		void WritePixel (ColorBgra color)
		{
			string r = RepresentColorComponent (color.R);
			string g = RepresentColorComponent (color.G);
			string b = RepresentColorComponent (color.B);
			writer.Write ($"{r} {g} {b}");
		}

		static string RepresentColorComponent (byte component)
			=> component.ToString ().PadLeft (3, ' ');
	}

	public void Export (Document document, Gio.File file, Window parent)
	{
		using ImageSurface flattenedImage = document.GetFlattenedImage ();
		using GioStream outputStream = new (file.Replace ());
		Export (flattenedImage, outputStream);
	}

	private readonly ref struct PpmTokenReader
	{
		private readonly TextReader reader;
		internal PpmTokenReader (TextReader reader)
		{
			this.reader = reader;
		}

		public string ReadToken ()
		{
			SkipWhitespaceAndComments ();
			StringBuilder token = new ();
			int c;
			while ((c = reader.Read ()) != -1) {
				char ch = (char) c;
				if (char.IsWhiteSpace (ch)) break;
				if (ch == '#') { // Comment
					reader.ReadLine (); // Skip rest of line
					break;
				}
				token.Append (ch);
			}
			return
				token.Length > 0
				? token.ToString ()
				: throw new FormatException ("Unexpected end of data");
		}

		public int ReadPositiveInt (string fieldName)
		{
			int value = ReadInt ();
			return
				value > 0
				? value
				: throw new FormatException ($"Invalid {fieldName}: {value}");
		}

		public int ReadColorComponent (int maxValue)
		{
			int value = ReadInt ();
			return
				value >= 0 && value <= maxValue
				? value
				: throw new FormatException ($"Color component {value} outside range 0..{maxValue}");
		}

		private int ReadInt ()
		{
			SkipWhitespaceAndComments ();
			int value = 0;
			bool found = false;
			int c;
			while ((c = reader.Peek ()) is >= '0' and <= '9') {
				reader.Read ();
				value = value * 10 + (c - '0');
				found = true;
			}
			return
				found
				? value
				: throw new FormatException ("Expected integer value");
		}

		private void SkipWhitespaceAndComments ()
		{
			while (reader.Peek () is int c and not -1) {
				char ch = (char) c;
				if (char.IsWhiteSpace (ch)) {
					reader.Read ();
					continue;
				}
				if (ch == '#') { // Comment
					reader.ReadLine (); // Skip rest of line
					continue;
				}
				break;
			}
		}
	}
}
