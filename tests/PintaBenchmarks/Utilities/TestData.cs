using Cairo;

namespace PintaBenchmarks;

internal class TestData
{
	public static ImageSurface Get2000PixelImage ()
	{
		var assembly_path = System.IO.Path.GetDirectoryName (typeof (TestData).Assembly.Location);
		var file = System.IO.Path.Combine (assembly_path!, "Assets", "2000px-test.png");
		var surf = new ImageSurface (file);

		return surf;
	}
}
