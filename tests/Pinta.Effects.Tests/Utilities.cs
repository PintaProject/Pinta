using System;
using Cairo;
using NUnit.Framework;
using Pinta.Core;

namespace Pinta.Effects.Tests;

internal static class Utilities
{
	static Utilities ()
	{
		Gio.Module.Initialize ();
		GdkPixbuf.Module.Initialize ();
		Cairo.Module.Initialize ();
		Gdk.Module.Initialize ();
	}

	public static IServiceProvider CreateMockServices ()
	{
		Size imageSize = new (250, 250);

		ServiceManager manager = new ();
		manager.AddService<IPaletteService> (new MockPalette ());
		manager.AddService<IChromeService> (new MockChromeManager ());
		manager.AddService<IWorkspaceService> (new MockWorkspaceService (imageSize));
		manager.AddService<ISystemService> (new MockSystemService ());
		manager.AddService<ILivePreview> (new MockLivePreview (new RectangleI (0, 0, imageSize.Width, imageSize.Height)));
		return manager;
	}

	public static ImageSurface LoadImage (string image_name)
	{
		string assembly_path = System.IO.Path.GetDirectoryName (typeof (Utilities).Assembly.Location)!;
		Gio.File file = Gio.FileHelper.NewForPath (System.IO.Path.Combine (assembly_path, "Assets", image_name));

		using Gio.FileInputStream fs = file.Read (null);
		try {
			using GdkPixbuf.Pixbuf bg = GdkPixbuf.Pixbuf.NewFromStream (fs, cancellable: null)!; // NRT: only nullable when error is thrown.
			ImageSurface surf = CairoExtensions.CreateImageSurface (Format.Argb32, bg.Width, bg.Height); // Not disposing because it will be returned
			using Context context = new (surf);
			context.DrawPixbuf (bg, 0, 0);
			return surf;
		} finally {
			fs.Close (null);
		}
	}

	public static void CompareImages (
		ImageSurface result,
		ImageSurface expected,
		int tolerance = 1)
	{
		Assert.That (expected.GetSize (), Is.EqualTo (result.GetSize ()));

		var result_pixels = result.GetReadOnlyPixelData ();
		var expected_pixels = expected.GetReadOnlyPixelData ();

		int diffs = 0;
		for (int i = 0; i < result_pixels.Length; ++i) {

			if (ColorBgra.ColorsWithinTolerance (result_pixels[i], expected_pixels[i], tolerance))
				continue;

			++diffs;

			// Display info about the first few failures.
			if (diffs <= 10)
				Assert.Warn ($"Difference at pixel {i}, got {result_pixels[i]} vs {expected_pixels[i]}, diff. of {ColorBgra.ColorDifference (result_pixels[i], expected_pixels[i])}");
		}

		Assert.That (diffs, Is.EqualTo (0));
	}

	public static void TestEffect (
		BaseEffect effect,
		string result_image_name,
		string? save_image_name = null,
		string source_image_name = "input.png")
	{
		using ImageSurface source = Utilities.LoadImage (source_image_name);
		using ImageSurface result = CairoExtensions.CreateImageSurface (Format.Argb32, source.Width, source.Height);
		using ImageSurface expected = LoadImage (result_image_name);

		effect.Render (source, result, [source.GetBounds ()]);

		// For debugging, optionally save out the result to a file.
		if (save_image_name != null)
			result.ToPixbuf ().Savev (
				save_image_name,
				"png",
				[],
				[]);

		CompareImages (result, expected);
	}
}
