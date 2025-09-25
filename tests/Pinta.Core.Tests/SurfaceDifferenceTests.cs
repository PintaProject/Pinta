using System;
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

	[TestCase ("visual_minimal.png", "visual_minimal_modified.png")]
	public void Returning_Value_If_Different_Surfaces (string a, string b)
	{
		string pathA = Utilities.GetAssetPath (a);
		string pathB = Utilities.GetAssetPath (b);
		using ImageSurface imageA = Utilities.LoadImage (pathA);
		using ImageSurface imageB = Utilities.LoadImage (pathB);
		SurfaceDiff? difference = SurfaceDiff.Create (imageA, imageB);
		Assert.That (difference, Is.Not.Null);
	}

	[Test]
	public void Returning_Null_If_Savings_Too_Small ()
	{
		ImageSurface empty = new (Format.Argb32, 16, 16);
		ImageSurface withChanges = new (Format.Argb32, 16, 16);
		using Context context = new (withChanges);
		context.SetSourceColor (Color.Blue);
		context.Rectangle (0, 0, 16, 15); // Savings are 16, which is ~6.3% of 256
		context.Fill ();
		SurfaceDiff? difference = SurfaceDiff.Create (empty, withChanges, force: false);
		Assert.That (difference, Is.Null);
	}

	[Test]
	public void Returning_Non_Null_If_Savings_Big_Enough ()
	{
		ImageSurface empty = new (Format.Argb32, 16, 16);
		ImageSurface withChanges = new (Format.Argb32, 16, 16);
		using Context context = new (withChanges);
		context.SetSourceColor (Color.Blue);
		context.Rectangle (0, 0, 2, 2); // Savings are 252, which is ~98.4% of 256
		context.Fill ();
		SurfaceDiff? difference = SurfaceDiff.Create (empty, withChanges, force: false);
		Assert.That (difference, Is.Not.Null);
	}
}
