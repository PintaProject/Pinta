using System;
using System.Collections.Generic;
using System.IO;
using Cairo;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class FileFormatTests
{
	[TestCase ("sixcolorsinput.gif", "sixcolors_standard_lf.ppm")]
	[TestCase ("sixcolorsinput.gif", "sixcolors_chaotic.ppm")]
	public void Files_NotEqual (string file1, string file2)
	{
		string path1 = Utilities.GetAssetPath (file1);
		string path2 = Utilities.GetAssetPath (file2);
		Assert.That (Utilities.AreFilesEqual (path1, path2), Is.False);
	}

	[TestCaseSource (nameof (netpbm_pixmap_text_cases))]
	public void Export_NetpbmPixmap_TextBased (string inputFile, IEnumerable<string> acceptableOutputs)
	{
		string inputFilePath = Utilities.GetAssetPath (inputFile);
		using ImageSurface loaded = Utilities.LoadImage (inputFilePath);
		NetpbmPortablePixmap exporter = new ();
		using Gio.MemoryOutputStream memoryOutput = Gio.MemoryOutputStream.NewResizable ();
		using GioStream outputStream = new (memoryOutput);
		exporter.Export (loaded, outputStream);
		outputStream.Close ();
		memoryOutput.Close (null);
		var exportedBytes = memoryOutput.StealAsBytes ();
		bool matched = false;
		foreach (string fileName in acceptableOutputs) {
			var bytesStream = Gio.MemoryInputStream.NewFromBytes (exportedBytes);
			var bytesReader = Gio.DataInputStream.New (bytesStream);
			string filePath = Utilities.GetAssetPath (fileName);
			using var context = Utilities.OpenFile (filePath);
			if (!Utilities.AreFilesEqual (bytesReader, context.DataStream)) continue;
			matched = true;
			break;
		}
		Assert.That (matched, Is.True);
	}

	[TestCase ("sixcolorsinput.gif", "sixcolors_standard_lf.ppm")]
	[TestCase ("sixcolorsinput.gif", "sixcolors_chaotic.ppm")]
	public void ImportNetpbmPixmap_TextBased (string referenceImageName, string ppmFileName)
	{
		string ppmFilePath = Utilities.GetAssetPath (ppmFileName);
		string referenceImagePath = Utilities.GetAssetPath (referenceImageName);
		using ImageSurface loaded = Utilities.LoadImage (referenceImagePath);
		using FileStream stream = File.OpenRead (ppmFilePath);
		NetpbmPortablePixmap importer = new ();
		using ImageSurface imported = NetpbmPortablePixmap.Import (stream);
		Utilities.CompareImages (imported, loaded);
	}

	[Test]
	public void Export_Tga_ConvertsSemiTransparentPixelsToStraightAlpha ()
	{
		ReadOnlySpan<ColorBgra> pixels = [ColorBgra.FromBgra (b: 25, g: 50, r: 100, a: 100)];

		using MemoryStream stream = new ();
		TgaExporter.Export (pixels, new Size (1, 1), stream);
		byte[] bytes = stream.ToArray ();

		// Last 4 bytes in 1x1 32-bit TGA are B, G, R, A in straight alpha
		Assert.That (bytes[^4], Is.EqualTo (63));
		Assert.That (bytes[^3], Is.EqualTo (127));
		Assert.That (bytes[^2], Is.EqualTo (255));
		Assert.That (bytes[^1], Is.EqualTo (100));
	}

	static readonly IReadOnlyList<TestCaseData> netpbm_pixmap_text_cases = [
		new (
			"sixcolorsinput.gif",
			new[] { "sixcolors_standard_lf.ppm" }
		),
	];
}
