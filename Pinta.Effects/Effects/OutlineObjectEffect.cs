using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
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
	private readonly IPaletteService palette;
	private readonly ISystemService system;
	private readonly IWorkspaceService workspace;
	public OutlineObjectEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		palette = services.GetService<IPaletteService> ();
		system = services.GetService<ISystemService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new OutlineObjectData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	protected override void Render (ImageSurface src, ImageSurface dest, RectangleI roi)
	{
		Size srcSize = src.GetSize ();
		int radius = Data.Radius;
		int threads = system.RenderThreads;
		int tolerance = Data.Tolerance;

		ColorBgra primaryColor = palette.PrimaryColor.ToColorBgra ();
		ColorBgra secondaryColor = palette.SecondaryColor.ToColorBgra ();

		ConcurrentBag<PointI> borderPixels = [];

		// First pass
		// Clean up dest, then collect all border pixels
		Parallel.For (
			roi.Top,
			roi.Bottom + 1,
			new ParallelOptions { MaxDegreeOfParallelism = threads },
			y => {
				var srcData = src.GetReadOnlyPixelData ();

				// reset dest to src
				// Removing this causes preview to not update to lower radius levels
				var srcRow = srcData.Slice (y * srcSize.Width, srcSize.Width);
				var dstRow = dest.GetPixelData ().Slice (y * srcSize.Width, srcSize.Width);
				srcRow.CopyTo (dstRow);

				// Produces different behaviour at radius == 0 and radius == 1
				// When radius == 0, only consider direct border pixels
				// When radius == 1, consider border pixels on diagonal
				Span<PointI> pixels = stackalloc PointI[8];
				// Collect a list of pixels that surround the object (border pixels)
				for (int x = roi.Left; x <= roi.Right; x++) {

					PointI potentialBorderPixel = new (x, y);

					if (Data.OutlineBorder && (x == 0 || x == srcSize.Width - 1 || y == 0 || y == srcSize.Height - 1)) {
						borderPixels.Add (potentialBorderPixel);
						continue;
					}

					if (src.GetColorBgra (srcData, srcSize.Width, potentialBorderPixel).A > tolerance)
						continue;

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

						PointI p = pixels[i];

						if (p.X < 0 || p.X >= srcSize.Width || p.Y < 0 || p.Y >= srcSize.Height)
							continue;

						if (src.GetColorBgra (srcData, srcSize.Width, p).A <= tolerance)
							continue;

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
		);

		// Second pass
		// Generate outline and blend to dest
		Parallel.For (
			roi.Top,
			roi.Bottom + 1,
			new ParallelOptions { MaxDegreeOfParallelism = threads },
			y => {
				// otherwise produces nothing at radius == 0
				if (radius == 0)
					radius = 1;

				var relevantBorderPixels =
					borderPixels
					.Where (borderPixel => borderPixel.Y > y - radius && borderPixel.Y < y + radius)
					.ToImmutableArray ();

				var destRow = dest.GetPixelData ().Slice (y * srcSize.Width, srcSize.Width);
				Span<ColorBgra> outlineRow = stackalloc ColorBgra[destRow.Length];

				for (int x = roi.Left; x <= roi.Right; x++) {

					byte highestAlpha = 0;

					// optimization: no change if destination has max alpha already
					if (destRow[x].A == 255)
						continue;

					if (Data.FillObjectBackground && destRow[x].A >= tolerance)
						highestAlpha = 255;

					// Grab nearest border pixel, and calculate outline alpha based off it
					foreach (var borderPixel in relevantBorderPixels) {

						if (borderPixel.X == x && borderPixel.Y == y)
							highestAlpha = 255;

						if (highestAlpha == 255)
							break;

						if (borderPixel.X <= x - radius || borderPixel.X >= x + radius)
							continue;

						int dx = borderPixel.X - x;
						int dy = borderPixel.Y - y;
						float distance = MathF.Sqrt (dx * dx + dy * dy);

						if (distance > radius)
							continue;

						float mult = 1 - distance / radius;

						if (mult <= 0)
							continue;

						byte alpha = (byte) (255 * mult);

						if (alpha > highestAlpha)
							highestAlpha = alpha;
					}

					// Handle color gradient / no alpha gradient option
					ColorBgra color = primaryColor;

					if (Data.ColorGradient)
						color = ColorBgra.Lerp (secondaryColor, primaryColor, highestAlpha);

					if (!Data.AlphaGradient && highestAlpha != 0)
						highestAlpha = 255;

					outlineRow[x] = color.NewAlpha (highestAlpha);
				}
				// Performs alpha blending
				new UserBlendOps.NormalBlendOp ().Apply (outlineRow, destRow);
				outlineRow.CopyTo (destRow);
			}
		);
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
