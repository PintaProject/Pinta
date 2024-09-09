using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class OutlineObjectEffect : BaseEffect
{

	public override string Icon => Pinta.Resources.Icons.EffectsStylizeOutline;

	// Takes two passes, so must be multithreaded internally
	public sealed override bool IsTileable => false;

	public override string Name => Translations.GetString ("Outline Object");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Object");

	public OutlineObjectData Data => (OutlineObjectData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly ISystemService system;
	private readonly IPaletteService palette;

	public OutlineObjectEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		system = services.GetService<ISystemService> ();
		palette = services.GetService<IPaletteService> ();
		EffectData = new OutlineObjectData ();
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	protected override void Render (ImageSurface src, ImageSurface dest, RectangleI roi)
	{
		int top = roi.Top;
		int bottom = roi.Bottom;
		int left = roi.Left;
		int right = roi.Right;
		int srcHeight = src.Height;
		int srcWidth = src.Width;
		int radius = Data.Radius;
		int threads = system.RenderThreads;

		ColorBgra primaryColor = palette.PrimaryColor.ToColorBgra ();
		ColorBgra secondaryColor = palette.SecondaryColor.ToColorBgra ();
		ConcurrentBag<PointI> borderPixels = new ConcurrentBag<PointI> ();

		// First pass
		// Clean up dest, then collect all border pixels
		Parallel.For (top, bottom + 1, new ParallelOptions { MaxDegreeOfParallelism = threads }, y => {
			var srcData = src.GetReadOnlyPixelData ();

			// reset dest to src
			// Removing this causes preview to not update to lower radius levels
			var srcRow = srcData.Slice (y * srcWidth, srcWidth);
			var dstRow = dest.GetPixelData ().Slice (y * srcWidth, srcWidth);
			for (int x = left; x <= right; x++)
				dstRow[x].Bgra = srcRow[x].Bgra;

			// Produces different behaviour at radius == 0 and radius == 1
			// When radius == 0, only consider direct border pixels
			// When radius == 1, consider border pixels on diagonal
			Span<PointI> pixels = stackalloc PointI[8];
			// Collect a list of pixels that surround the object (border pixels)
			for (int x = left; x <= right; x++) {
				PointI potentialBorderPixel = new (x, y);
				if (Data.OutlineBorder && (x == 0 || x == srcWidth - 1 || y == 0 || y == srcHeight - 1)) {
					borderPixels.Add (potentialBorderPixel);
				} else if (src.GetColorBgra (srcData, srcWidth, potentialBorderPixel).A <= Data.Tolerance) {
					// Test pixel above, below, left, & right
					pixels[0] = new (x - 1, y);
					pixels[1] = new (x + 1, y);
					pixels[2] = new (x, y - 1);
					pixels[3] = new (x, y + 1);
					int pixelCount = 4;
					if (radius == 1) {
						// if radius == 1, also test pixels on diagonals
						pixels[4] = new (x - 1, y - 1);
						pixels[5] = new (x - 1, y + 1);
						pixels[6] = new (x + 1, y - 1);
						pixels[7] = new (x + 1, y + 1);
						pixelCount = 8;
					}

					for (int i = 0; i < pixelCount; i++) {
						var px = pixels[i].X;
						var py = pixels[i].Y;
						if (px < 0 || px >= srcWidth || py < 0 || py >= srcHeight)
							continue;
						if (src.GetColorBgra (srcData, srcWidth, new PointI (px, py)).A > Data.Tolerance) {
							borderPixels.Add (potentialBorderPixel);
							// Remove comments below to draw border pixels
							// You will also have to comment out the 2nd pass because it will overwrite this
							//int pos = srcWidth * y + x;
							//borderData[pos].Bgra = 0;
							//borderData[pos].A = 255;

							break;
						}
					}


				}
			}
		});


		// Second pass
		// Generate outline and blend to dest
		Parallel.For (top, bottom + 1, new ParallelOptions { MaxDegreeOfParallelism = threads }, y => {
			// otherwise produces nothing at radius == 0
			if (radius == 0)
				radius = 1;
			var relevantBorderPixels = borderPixels.Where (borderPixel => borderPixel.Y > y - radius && borderPixel.Y < y + radius).ToArray ();
			var destRow = dest.GetPixelData ().Slice (y * srcWidth, srcWidth);

			for (int x = left; x <= right; x++) {
				byte highestAlpha = 0;

				// optimization: no change if destination has max alpha already
				if (destRow[x].A == 255)
					continue;

				if (Data.FillObjectBackground && destRow[x].A >= Data.Tolerance)
					highestAlpha = 255;

				// Grab nearest border pixel, and calculate outline alpha based off it
				foreach (var borderPixel in relevantBorderPixels) {
					if (borderPixel.X == x && borderPixel.Y == y)
						highestAlpha = 255;

					if (highestAlpha == 255)
						break;

					if (borderPixel.X > x - radius && borderPixel.X < x + radius) {
						var dx = borderPixel.X - x;
						var dy = borderPixel.Y - y;
						float distance = MathF.Sqrt (dx * dx + dy * dy);
						if (distance <= radius) {
							float mult = 1 - distance / radius;
							if (mult <= 0)
								continue;
							byte alpha = (byte) (255 * mult);
							if (alpha > highestAlpha)
								highestAlpha = alpha;
						}
					}
				}

				// Handle color gradient / no alpha gradient option
				var color = primaryColor;
				if (Data.ColorGradient)
					color = ColorBgra.Blend (secondaryColor, primaryColor, highestAlpha);
				if (!Data.AlphaGradient && highestAlpha != 0)
					highestAlpha = 255;

				var outlineColor = color.NewAlpha (highestAlpha).ToPremultipliedAlpha ();
				destRow[x] = ColorBgra.AlphaBlend (destRow[x], outlineColor);
			}
		});
	}

	public sealed class OutlineObjectData : EffectData
	{
		[Caption ("Radius"), MinimumValue (0), MaximumValue (100)]
		public int Radius { get; set; } = 6;

		[Caption ("Tolerance"), MinimumValue (0), MaximumValue (255)]
		public int Tolerance { get; set; } = 20;

		[Caption ("Alpha Gradient")]
		public bool AlphaGradient { get; set; } = true;

		[Caption ("Color Gradient")]
		public bool ColorGradient { get; set; } = true;

		[Caption ("Outline Border")]
		public bool OutlineBorder { get; set; } = false;

		[Caption ("Fill Object Background")]
		public bool FillObjectBackground { get; set; } = true;
	}
}
