using BenchmarkDotNet.Attributes;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace PintaBenchmarks;

[MemoryDiagnoser]
[Config (typeof (MillisecondConfig))]
public class CanvasRendererBenchmarks
{
	private readonly ImageSurface surface;

	private readonly ImageSurface dest_surface;
	private readonly ImageSurface dest_surface_zoom_in;
	private readonly ImageSurface dest_surface_zoom_out;

	private readonly Gdk.Size src_size;
	private readonly Gdk.Size dest_size_zoom_in;
	private readonly Gdk.Size dest_size_zoom_out;

	private readonly List<Layer> layers = new List<Layer> ();
	private readonly List<Layer> ten_laters = new List<Layer> ();

	public CanvasRendererBenchmarks ()
	{
		surface = TestData.Get2000PixelImage ();

		layers.Add (new Layer (surface));

		for (var i = 0; i < 10; i++)
			ten_laters.Add (new Layer (surface));

		dest_surface = new ImageSurface (Format.Argb32, 2000, 2000);
		dest_surface_zoom_in = new ImageSurface (Format.Argb32, 3500, 3500);
		dest_surface_zoom_out = new ImageSurface (Format.Argb32, 1350, 1350);

		src_size = dest_surface.GetSize ();
		dest_size_zoom_in = dest_surface_zoom_in.GetSize ();
		dest_size_zoom_out = dest_surface_zoom_out.GetSize ();
	}

	[Benchmark]
	public void RenderOneToOne ()
	{
		var renderer = new CanvasRenderer (false, false);

		renderer.Initialize (src_size, src_size);
		renderer.Render (layers, dest_surface, Gdk.Point.Zero);
	}

	[Benchmark]
	public void RenderManyOneToOne ()
	{
		var renderer = new CanvasRenderer (false, false);

		renderer.Initialize (src_size, src_size);
		renderer.Render (ten_laters, dest_surface, Gdk.Point.Zero);
	}

	[Benchmark]
	public void RenderZoomIn ()
	{
		var renderer = new CanvasRenderer (false, false);

		renderer.Initialize (src_size, dest_size_zoom_in);
		renderer.Render (layers, dest_surface_zoom_in, Gdk.Point.Zero);
	}

	[Benchmark]
	public void RenderManyZoomIn ()
	{
		var renderer = new CanvasRenderer (false, false);

		renderer.Initialize (src_size, dest_size_zoom_in);
		renderer.Render (ten_laters, dest_surface_zoom_in, Gdk.Point.Zero);
	}

	[Benchmark]
	public void RenderZoomOut ()
	{
		var renderer = new CanvasRenderer (false, false);

		renderer.Initialize (src_size, dest_size_zoom_out);
		renderer.Render (layers, dest_surface_zoom_out, Gdk.Point.Zero);
	}

	[Benchmark]
	public void RenderManyZoomOut ()
	{
		var renderer = new CanvasRenderer (false, false);

		renderer.Initialize (src_size, dest_size_zoom_out);
		renderer.Render (ten_laters, dest_surface_zoom_out, Gdk.Point.Zero);
	}
}
