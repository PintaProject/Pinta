using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class FileFormatTests
{
	[TestCase ("sixcolorsinput.png", "sixcolorsoutput.ppm")]
	public void Files_NotEqual (string file1, string file2)
	{
		Assert.IsFalse (Utilities.AreFilesEqual (file1, file2));
	}
}
