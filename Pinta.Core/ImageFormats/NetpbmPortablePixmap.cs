using System;
using System.IO;
using System.Text;
using Cairo;
using Gtk;

namespace Pinta.Core;

public sealed class NetpbmPortablePixmap : IImageExporter
{
	public void Export (ImageSurface flattenedImage, Stream outputStream)
	{
		using StreamWriter writer = new (outputStream, Encoding.ASCII) {
			NewLine = "\n", // Always use LF endings to generate the same output on every platform and simplify unit tests
		};
		Size imageSize = flattenedImage.GetSize ();
		ReadOnlySpan<ColorBgra> pixelData = flattenedImage.GetReadOnlyPixelData ();
		writer.WriteLine ("P3"); // Magic number for text-based portable pixmap format
		writer.WriteLine ($"{imageSize.Width} {imageSize.Height}");
		writer.WriteLine (byte.MaxValue);
		for (int row = 0; row < imageSize.Height; row++) {
			int rowStart = row * imageSize.Width;
			int rowEnd = rowStart + imageSize.Width;
			for (int index = rowStart; index < rowEnd; index++) {
				ColorBgra color = pixelData[index];
				string r = color.R.ToString ().PadLeft (3, ' ');
				string g = color.G.ToString ().PadLeft (3, ' ');
				string b = color.B.ToString ().PadLeft (3, ' ');
				writer.Write ($"{r} {g} {b}");
				if (index != rowEnd - 1)
					writer.Write ("   ");
			}
			writer.WriteLine ();
		}
		writer.Close ();
	}

	public void Export (
		Document document,
		Gio.File file,
		Window parent)
	{
		using ImageSurface flattenedImage = document.GetFlattenedImage ();
		using GioStream outputStream = new (file.Replace ());
		Export (flattenedImage, outputStream);
	}
}
