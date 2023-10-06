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
		Assert.IsFalse (Utilities.AreFilesEqual (file1, file2));
	}
}
