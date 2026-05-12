using System.Linq;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class PaletteHelperTests
{
	[Test]
	public void DefaultPaletteHasExpectedColorCount ()
	{
		Assert.That (PaletteHelper.EnumerateDefaultColors ().Count (), Is.EqualTo (48));
	}
}
