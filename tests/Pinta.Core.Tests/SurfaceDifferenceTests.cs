using Cairo;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class SurfaceDifferenceTests
{
	[TestCase ("visual_minimal.png", "visual_minimal_modified.png")]
	public void Changes_Swapped_Back_And_Forth (string a, string b)
	{
		string pathA = Utilities.GetAssetPath (a);
		string pathB = Utilities.GetAssetPath (b);

		using ImageSurface originalA = Utilities.LoadImage (pathA);
		using ImageSurface originalB = Utilities.LoadImage (pathB);

		using ImageSurface modifiable = originalB.Clone ();

		SurfaceDiff difference = SurfaceDiff.Create (originalA, originalB, force: true)!;

		difference.ApplyAndSwap (modifiable);

		Utilities.CompareImages (modifiable, originalA);

		difference.ApplyAndSwap (modifiable);

		Utilities.CompareImages (modifiable, originalB);
	}
}
