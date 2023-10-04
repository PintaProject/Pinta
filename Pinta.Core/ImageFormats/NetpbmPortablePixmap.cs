using System;
using System.IO;
using System.Text;
using Cairo;
using Gtk;

namespace Pinta.Core;

internal sealed class NetpbmPortablePixmap : IImageExporter
{
	public void Export (Document document, Gio.File file, Window parent)
	{
		using GioStream fileStream = new (file.Replace ());
		using StreamWriter writer = new (fileStream, Encoding.ASCII);
		ImageSurface flattenedImage = document.GetFlattenedImage ();
		Size imageSize = flattenedImage.GetSize ();
		ReadOnlySpan<ColorBgra> pixelData = flattenedImage.GetReadOnlyPixelData ();
		writer.WriteLine ("P3"); // Magic number for text-based portable pixmap format
		writer.WriteLine ($"{imageSize.Width} {imageSize.Height}");
		writer.WriteLine ("255");
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
}
