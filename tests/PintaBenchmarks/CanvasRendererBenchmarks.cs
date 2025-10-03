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

	private readonly ImageSurface destination_surface;
	private readonly ImageSurface destination_surface_zoom_in;
	private readonly ImageSurface destination_surface_zoom_out;

	private readonly Size source_size;
	private readonly Size destination_size_zoom_in;
	private readonly Size destination_size_zoom_out;

	private readonly List<Layer> layers = [];
	private readonly List<Layer> ten_layers = [];

	public CanvasRendererBenchmarks ()
	{
		surface = TestData.Get2000PixelImage ();

		layers.Add (new Layer (surface));

		for (var i = 0; i < 10; i++)
			ten_layers.Add (new Layer (surface));

		destination_surface = new ImageSurface (Format.Argb32, 2000, 2000);
		destination_surface_zoom_in = new ImageSurface (Format.Argb32, 3500, 3500);
		destination_surface_zoom_out = new ImageSurface (Format.Argb32, 1350, 1350);

		source_size = destination_surface.GetSize ();
		destination_size_zoom_in = destination_surface_zoom_in.GetSize ();
		destination_size_zoom_out = destination_surface_zoom_out.GetSize ();
	}

	[Benchmark]
	public void RenderOneToOne ()
	{
		IServiceProvider services = Utilities.CreateMockServices ();
		CanvasRenderer renderer = new (
			services.GetService<ILivePreview> (),
			services.GetService<IWorkspaceService> (),
			false);
		renderer.Initialize (source_size, source_size);
		renderer.Render (layers, destination_surface, PointI.Zero);
	}

	[Benchmark]
	public void RenderManyOneToOne ()
	{
		IServiceProvider services = Utilities.CreateMockServices ();
		CanvasRenderer renderer = new (
			services.GetService<ILivePreview> (),
			services.GetService<IWorkspaceService> (),
			false);
		renderer.Initialize (source_size, source_size);
		renderer.Render (ten_layers, destination_surface, PointI.Zero);
	}

	[Benchmark]
	public void RenderZoomIn ()
	{
		IServiceProvider services = Utilities.CreateMockServices ();
		CanvasRenderer renderer = new (
			services.GetService<ILivePreview> (),
			services.GetService<IWorkspaceService> (),
			false);
		renderer.Initialize (source_size, destination_size_zoom_in);
		renderer.Render (layers, destination_surface_zoom_in, PointI.Zero);
	}

	[Benchmark]
	public void RenderManyZoomIn ()
	{
		IServiceProvider services = Utilities.CreateMockServices ();
		CanvasRenderer renderer = new (
			services.GetService<ILivePreview> (),
			services.GetService<IWorkspaceService> (),
			false);
		renderer.Initialize (source_size, destination_size_zoom_in);
		renderer.Render (ten_layers, destination_surface_zoom_in, PointI.Zero);
	}

	[Benchmark]
	public void RenderZoomOut ()
	{
		IServiceProvider services = Utilities.CreateMockServices ();
		CanvasRenderer renderer = new (
			services.GetService<ILivePreview> (),
			services.GetService<IWorkspaceService> (),
			false);
		renderer.Initialize (source_size, destination_size_zoom_out);
		renderer.Render (layers, destination_surface_zoom_out, PointI.Zero);
	}

	[Benchmark]
	public void RenderManyZoomOut ()
	{
		IServiceProvider services = Utilities.CreateMockServices ();
		CanvasRenderer renderer = new (
			services.GetService<ILivePreview> (),
			services.GetService<IWorkspaceService> (),
			false);
		renderer.Initialize (source_size, destination_size_zoom_out);
		renderer.Render (ten_layers, destination_surface_zoom_out, PointI.Zero);
	}
}
