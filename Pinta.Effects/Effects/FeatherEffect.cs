using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class FeatherEffect : BaseEffect
{

	public override string Icon => Pinta.Resources.Icons.EffectsDefault;

	// Multithread internally within FeatherEffect to get the full-sized rois
	public sealed override bool IsTileable => false;

	public override string Name => Translations.GetString ("Feather");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Stylize");

	public FeatherData Data => (FeatherData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly ISystemService system;

	public FeatherEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		system = services.GetService<ISystemService> ();
		EffectData = new FeatherData ();
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	protected override void Render (ImageSurface src, ImageSurface dest, RectangleI roi)
	{
		int bottom = roi.Bottom;
		int src_height = src.Height;
		int radius = Data.Radius;
		int threads = system.RenderThreads;

		ConcurrentBag<PointI> borderPixels = new ConcurrentBag<PointI> ();
		// Color in any pixel that the stencil says we need to fill
		// First pass
		// Clean up dest, then collect all border pixels
		Parallel.For (0, bottom, new ParallelOptions { MaxDegreeOfParallelism = threads }, y => {
			var src_data = src.GetReadOnlyPixelData ();
			int src_width = src.Width;
			var dst_data = dest.GetPixelData ();

			// reset dest to src
			// Removing this causes preview to not update to lower radius levels
			var src_row = src_data.Slice (y * src_width, src_width);
			var dst_row = dst_data.Slice (y * src_width, src_width);
			for (int x = roi.Left; x <= roi.Right; x++) {
				dst_row[x].Bgra = src_row[x].Bgra;
			}

			Span<PointI> pixels = [new (0, 0), new (0, 0), new (0, 0), new (0, 0)];
			// Collect a list of pixels that surround the object (border pixels)
			for (int x = roi.Left; x <= roi.Right; x++) {
				PointI potentialBorderPixel = new (x, y);
				if (Data.FeatherCanvasEdge && (x == 0 || x == src_width - 1 || y == 0 || y == src_height - 1)) {
					borderPixels.Add (potentialBorderPixel);
				} else if (src.GetColorBgra (src_data, src_width, potentialBorderPixel).A <= Data.Tolerance) {
					// Test pixel above, below, left, & right
					pixels[0] = new (x - 1, y);
					pixels[1] = new (x + 1, y);
					pixels[2] = new (x, y - 1);
					pixels[3] = new (x, y + 1);

					foreach (var pixel in pixels) {
						var px = pixel.X;
						var py = pixel.Y;
						if (px < 0 || px >= src_width || py < 0 || py >= src_height)
							continue;
						if (src.GetColorBgra (src_data, src_width, new PointI (px, py)).A > Data.Tolerance) {
							borderPixels.Add (potentialBorderPixel);
							// Remove comments below to draw border pixels
							// You will also have to comment out the feather pass because it will overwrite this
							//int pos = src_width * y + x;
							//dst_data[pos].Bgra = 0;
							//dst_data[pos].A = 255;

							break;
						}
					}
				}
			}
		});

		// Second pass
		// Feather pixels according to distance to border pixels
		Parallel.For (0, bottom, new ParallelOptions { MaxDegreeOfParallelism = threads }, py => {
			var src_data = src.GetReadOnlyPixelData ();
			int src_width = src.Width;
			var dst_data = dest.GetPixelData ();

			var relevantBorderPixels = borderPixels.Where (borderPixel => borderPixel.Y > py - radius && borderPixel.Y < py + radius).ToArray ();

			for (int px = roi.Left; px <= roi.Right; px++) {
				int pixel_index = py * src_width + px;
				byte lowestAlpha = dst_data[pixel_index].A;
				// Can't feather further than alpha 0
				if (lowestAlpha == 0)
					continue;
				foreach (var borderPixel in relevantBorderPixels) {
					if (borderPixel.X > px - radius && borderPixel.X < px + radius) {
						var dx = borderPixel.X - px;
						var dy = borderPixel.Y - py;
						float distance = MathF.Sqrt (dx * dx + dy * dy);
						// If within distance to border pixel
						if (distance <= radius) {
							float mult = distance / radius;
							byte alpha = (byte) (src_data[pixel_index].A * mult);
							if (alpha < lowestAlpha)
								lowestAlpha = alpha;
						}
					}
				}
				if (lowestAlpha < dst_data[pixel_index].A)
					dst_data[pixel_index].Bgra = src_data[pixel_index].ToStraightAlpha ().NewAlpha (lowestAlpha).ToPremultipliedAlpha ().Bgra;
			}
		});
	}

	public sealed class FeatherData : EffectData
	{
		[Caption ("Radius"), MinimumValue (1), MaximumValue (100)]
		public int Radius { get; set; } = 6;

		[Caption ("Tolerance"), MinimumValue (0), MaximumValue (255)]
		public int Tolerance { get; set; } = 20;

		[Caption ("Feather Canvas Edge")]
		public bool FeatherCanvasEdge { get; set; } = false;
	}
}
