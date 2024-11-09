using Cairo;
using Pinta.Core;

namespace PintaBenchmarks;

internal class TestData
{
	public static ImageSurface Get2000PixelImage ()
	{
		Gio.Module.Initialize ();
		GdkPixbuf.Module.Initialize ();
		Cairo.Module.Initialize ();
		Gdk.Module.Initialize ();

		var assembly_path = System.IO.Path.GetDirectoryName (typeof (TestData).Assembly.Location);
		var file = Gio.FileHelper.NewForPath (System.IO.Path.Combine (assembly_path!, "Assets", "2000px-test.png"));

		using var fs = file.Read (null);
		try {
			var bg = GdkPixbuf.Pixbuf.NewFromStream (fs, cancellable: null)!; // NRT: only nullable when error is thrown.
			var surf = CairoExtensions.CreateImageSurface (Format.Argb32, bg.Width, bg.Height);
			using var context = new Cairo.Context (surf);
			context.DrawPixbuf (bg, 0, 0);
			return surf;
		} finally {
			fs.Close (null);
		}
	}
}
