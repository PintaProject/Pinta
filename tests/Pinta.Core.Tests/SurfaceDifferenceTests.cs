using System;
using Cairo;
using NGettext.Loaders;
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

		// Cloning "B". Can't use the "Clone" extension method because it depends on PintaCore
		using ImageSurface modifiable = new ImageSurface (Format.Argb32, originalA.Width, originalA.Height);
		ReadOnlySpan<ColorBgra> originalBData = originalB.GetReadOnlyPixelData ();
		Span<ColorBgra> modifiableData = modifiable.GetPixelData ();
		originalBData.CopyTo (modifiableData);

		SurfaceDiff difference = SurfaceDiff.Create (originalA, originalB, force: true)!;

		difference.ApplyAndSwap (modifiable);

		Utilities.CompareImages (modifiable, originalA);

		difference.ApplyAndSwap (modifiable);

		Utilities.CompareImages (modifiable, originalB);
	}

	[TestCase ("visual_minimal.png")]
	public void Returning_Null_If_Same_Surfaces (string fileName)
	{
		string path = Utilities.GetAssetPath (fileName);
		using ImageSurface image = Utilities.LoadImage (path);
		SurfaceDiff? difference = SurfaceDiff.Create (image, image);
		Assert.That (difference, Is.Null);
	}
}
