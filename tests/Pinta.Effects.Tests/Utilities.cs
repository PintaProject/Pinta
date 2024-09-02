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
		ServiceManager manager = new ();
		manager.AddService<IPaletteService> (new MockPalette ());
		manager.AddService<IChromeService> (new MockChromeManager ());
		manager.AddService<IWorkspaceService> (new MockWorkspaceService ());
		manager.AddService<ISystemService> (new MockSystemService ());
		return manager;
	}

	public static ImageSurface LoadImage (string image_name)
	{
		var assembly_path = System.IO.Path.GetDirectoryName (typeof (Utilities).Assembly.Location);
		var file = Gio.FileHelper.NewForPath (System.IO.Path.Combine (assembly_path!, "Assets", image_name));

		using var fs = file.Read (null);
		try {
			var bg = GdkPixbuf.Pixbuf.NewFromStream (fs, cancellable: null)!; // NRT: only nullable when error is thrown.
			var surf = CairoExtensions.CreateImageSurface (Format.Argb32, bg.Width, bg.Height);
			var context = new Cairo.Context (surf);
			context.DrawPixbuf (bg, 0, 0);
			return surf;
		} finally {
			fs.Close (null);
		}
	}

	public static void CompareImages (ImageSurface result, ImageSurface expected, int tolerance = 1)
	{
		Assert.That (expected.GetSize (), Is.EqualTo (result.GetSize ()));

		var result_pixels = result.GetReadOnlyPixelData ();
		var expected_pixels = expected.GetReadOnlyPixelData ();

		int diffs = 0;
		for (int i = 0; i < result_pixels.Length; ++i) {
			if (!ColorBgra.ColorsWithinTolerance (result_pixels[i], expected_pixels[i], tolerance)) {
				++diffs;

				// Display info about the first few failures.
				if (diffs <= 10)
					Assert.Warn ($"Difference at pixel {i}, got {result_pixels[i]} vs {expected_pixels[i]}");
			}
		}

		Assert.That (diffs, Is.EqualTo (0));
	}

	public static void TestEffect (BaseEffect effect, string result_image_name, string? save_image_name = null)
	{
		var source = Utilities.LoadImage ("input.png");
		var result = CairoExtensions.CreateImageSurface (Format.Argb32, source.Width, source.Height);
		var expected = LoadImage (result_image_name);

		effect.Render (source, result, stackalloc[] { source.GetBounds () });

		// For debugging, optionally save out the result to a file.
		if (save_image_name != null) {
			result.ToPixbuf ().Savev (save_image_name, "png",
				System.Array.Empty<string> (), System.Array.Empty<string> ());
		}

		CompareImages (result, expected);
	}
}
