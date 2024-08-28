using System;
using System.Collections.Generic;
using System.Threading;
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

	public FeatherEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new FeatherData ();
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	public void RenderAsync (ImageSurface src, ImageSurface dest, RectangleI roi, int y)
	{
		int radius = Data.Radius;
		var src_data = src.GetReadOnlyPixelData ();
		int src_width = src.Width;
		int src_height = src.Height;
		var dst_data = dest.GetPixelData ();


		// Collect a list of pixels that surround the object
		List<PointI> borderPixels = new List<PointI> ();
		for (int x = roi.Left; x <= roi.Right; x++) {
			PointI potentialBorderPixel = new (x, y);

			if (Data.FeatherCanvasEdge && (x == 0 || x == src_width - 1 || y == 0 || y == src_height - 1)) {
				borderPixels.Add (potentialBorderPixel);
			} else if (src.GetColorBgra (src_data, src_width, potentialBorderPixel).A <= Data.Tolerance) {
				// Test pixel above, below, left, & right

				foreach (var pixel in new PointI[] { new(x - 1, y), new(x + 1, y), new(x, y - 1), new(x, y + 1) }) {
					var px = pixel.X;
					var py = pixel.Y;
					if (px < 0 || px >= src_width || py < 0 || py >= src_height)
						continue;
					if (src.GetColorBgra (src_data, src_width, pixel).A > Data.Tolerance) {
						borderPixels.Add (potentialBorderPixel);
						// Remove comments below to draw border pixels
						// You will also have to comment out the border pixel pass because it will overwrite this
						//int pos = src_width * y + x;
						//dst_data[pos].Bgra = 0;
						//dst_data[pos].A = 255;

						break;
					}
				}
			}
		}


		// Pass through all border pixels and reduce the alpha of pixels around it

		foreach (var borderPixel in borderPixels) {
			var top = Math.Max (borderPixel.Y - radius, roi.Top);
			var bottom = Math.Min (borderPixel.Y + radius, roi.Bottom);
			var left = Math.Max (borderPixel.X - radius, roi.Left);
			var right = Math.Min (borderPixel.X + radius, roi.Right);

			for (int py = top; py <= bottom; py++) {
				for (int px = left; px <= right; px++) {
					var dx = borderPixel.X - px;
					var dy = borderPixel.Y - py;
					float distance = MathF.Sqrt (dx * dx + dy * dy);
					// If within actual distance
					if (distance <= radius) {
						int pixel_index = py * src_width + px;
						float mult = distance / radius;
						byte alpha = (byte) (src_data[pixel_index].A * mult);
						if (alpha < dst_data[pixel_index].A)
							dst_data[pixel_index].Bgra = src_data[pixel_index].ToStraightAlpha ().NewAlpha (alpha).ToPremultipliedAlpha ().Bgra;
					}
				}
			}
		}
	}

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		foreach (var roi in rois) {
			int top = roi.Top;
			int bottom = roi.Bottom;
			int currentRow = top;

			var src_data = src.GetReadOnlyPixelData ();
			int src_width = src.Width;
			var dst_data = dest.GetPixelData ();

			// reset dest to src
			// Removing this causes preview to not update to lower radius levels
			// this is on the main thread to prevent race conditions
			for (int y = roi.Top; y <= roi.Bottom; y++) {
				var src_row = src_data.Slice (y * src_width, src_width);
				var dst_row = dst_data.Slice (y * src_width, src_width);
				for (int x = roi.Left; x <= roi.Right; x++) {
					dst_row[x].Bgra = src_row[x].Bgra;
				}
			}

			// crashes on test if try-catch not implemented
			int threadCount = 0;
			try {
				threadCount = PintaCore.System.RenderThreads;
			} catch {
				threadCount = 1;
			}
			var slaves = new Thread[threadCount];
			for (int threadId = 0; threadId < threadCount; threadId++) {
				var slave = new Thread (() => {
					while (true) {
						int rowIndex = Interlocked.Increment (ref currentRow) - 1;
						// Not sure why but bottom is 1 less than it should be? Effect doesn't hit bottom-most pixel without + 1
						if (rowIndex >= bottom + 1)
							return;

						RenderAsync(src, dest, roi, rowIndex);
					}
				}) { Priority = ThreadPriority.BelowNormal };
				slave.Start ();

				slaves[threadId] = slave;
			}

			foreach (var slave in slaves)
				slave.Join ();
		}
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
