using System.Collections.Generic;
using Cairo;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class FileFormatTests
{
	[TestCase ("sixcolorsinput.png", "sixcolorsoutput_lf.ppm")]
	[TestCase ("sixcolorsinput.png", "sixcolorsoutput_crlf.ppm")]
	[TestCase ("sixcolorsoutput_lf.ppm", "sixcolorsoutput_crlf.ppm")]
	public void Files_NotEqual (string file1, string file2)
	{
		var path1 = Utilities.GetAssetPath (file1);
		var path2 = Utilities.GetAssetPath (file2);
		Assert.IsFalse (Utilities.AreFilesEqual (path1, path2));
	}

	[TestCaseSource (nameof (netpbm_pixmap_text_cases))]
	public void Export_NetpbmPixmap_TextBased (string inputFile, IEnumerable<string> acceptableOutputs)
	{
		var inputFilePath = Utilities.GetAssetPath (inputFile);
		ImageSurface loaded = Utilities.LoadImage (inputFilePath);
		NetpbmPortablePixmap exporter = new ();
		Gio.MemoryOutputStream memoryOutput = Gio.MemoryOutputStream.NewResizable ();
		using GioStream outputStream = new (memoryOutput);
		exporter.Export (loaded, outputStream);
		outputStream.Close ();
		memoryOutput.Close (null);
		var exportedBytes = memoryOutput.StealAsBytes ();
		bool matched = false;
		foreach (string fileName in acceptableOutputs) {
			var bytesStream = Gio.MemoryInputStream.NewFromBytes (exportedBytes);
			var bytesReader = Gio.DataInputStream.New (bytesStream);
			var filePath = Utilities.GetAssetPath (fileName);
			using var context = Utilities.OpenFile (filePath);
			if (Utilities.AreFilesEqual (bytesReader, context.DataStream)) {
				matched = true;
				break;
			}
		}
		Assert.IsTrue (matched);
	}

	static readonly IReadOnlyList<TestCaseData> netpbm_pixmap_text_cases = new TestCaseData[] {
		new (
			"sixcolorsinput.png",
			new [] { "sixcolorsoutput_lf.ppm", "sixcolorsoutput_crlf.ppm" }
		),
	};
}
