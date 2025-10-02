using BenchmarkDotNet.Attributes;
using Cairo;
using Pinta.Core;

namespace PintaBenchmarks;

[MemoryDiagnoser]
[Config (typeof (MillisecondConfig))]
public class SurfaceDifferenceBenchmarks
{
	private readonly ImageSurface empty;
	private readonly ImageSurface photo;
	private readonly ImageSurface shape;

	public SurfaceDifferenceBenchmarks ()
	{
		ImageSurface shapeSurface = new (Format.Argb32, 2000, 2000);
		using Context context = new (shapeSurface);
		context.SetSourceColor (Color.Blue);
		context.Rectangle (750, 750, 500, 500);
		context.Fill ();

		empty = new (Format.Argb32, 2000, 2000);
		photo = TestData.Get2000PixelImage ();
		shape = shapeSurface;
	}

	[Benchmark]
	public void Create_Fully_Different ()
	{
		SurfaceDiff.Create (empty, photo);
	}

	[Benchmark]
	public void Create_Partially_Different ()
	{
		SurfaceDiff.Create (empty, shape);
	}

	[Benchmark]
	public void Create_Same ()
	{
		SurfaceDiff.Create (photo, photo);
	}
}
