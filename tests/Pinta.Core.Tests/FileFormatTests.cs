using System.Collections.Generic;
using Cairo;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class FileFormatTests
{
	[TestCase ("sixcolorsinput.gif", "sixcolorsoutput_lf.ppm")]
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
			string filePath = Utilities.GetAssetPath (fileName);
			using var context = Utilities.OpenFile (filePath);
			if (Utilities.AreFilesEqual (bytesReader, context.DataStream)) {
				matched = true;
				break;
			}
		}
		Assert.That (matched, Is.True);
	}

	static readonly IReadOnlyList<TestCaseData> netpbm_pixmap_text_cases = [
		new (
			"sixcolorsinput.gif",
			new [] { "sixcolorsoutput_lf.ppm" }
		),
	];
}
