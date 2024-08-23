using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class FeatherEffect : BaseEffect
{

	public override string Icon => Pinta.Resources.Icons.EffectsDefault;

	public sealed override bool IsTileable => true;

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

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		int radius = Data.Radius;
		var src_data = src.GetReadOnlyPixelData ();
		int src_width = src.Width;
		int src_height = src.Height;
		var dst_data = dest.GetPixelData ();
		int dst_width = dest.Width;

		// reset dest to src
		// Removing this causes preview to not update to lower radius levels
		foreach (RectangleI roi in rois) {
			for (int y = roi.Top; y <= roi.Bottom; ++y) {
				var src_row = src_data.Slice (y * src_width + roi.Left, roi.Width);
				var dst_row = dst_data.Slice (y * dst_width + roi.Left, roi.Width);
				for (int x = roi.Left; x <= roi.Right; x++) {
					if (x < dst_row.Length)
						dst_row[x].Bgra = src_row[x].Bgra;
				}
			}
		}


		// Collect a list of pixels that surround the object
		List<PointI> borderPixels = new List<PointI> ();
		foreach (RectangleI roi in rois) {
			for (int y = roi.Top; y <= roi.Bottom; ++y) {
				for (int x = roi.Left; x <= roi.Right; x++) {
					PointI potentialBorderPixel = new (x, y);
					if (src.GetColorBgra (src_data, src_width, potentialBorderPixel).A <= Data.TransparencyThreshold) {
						for (int sx = x - 1; sx <= x + 1; sx++) {
							for (int sy = y - 1; sy <= y + 1; sy++) {
								PointI pixel = new (sx, sy);

								if (sx < 0 || sx >= src_width || sy < 0 || sy >= src_height)
									continue;

								if (src.GetColorBgra (src_data, src_width, pixel).A != 0)
									borderPixels.Add (potentialBorderPixel);
							}
						}
					}
				}
			}
		}

		// For each pixel, lower alpha based off distance to border pixel
		foreach (var borderPixel in borderPixels) {
			for (int y = borderPixel.Y - radius; y <= borderPixel.Y + radius; ++y) {
				for (int x = borderPixel.X - radius; x <= borderPixel.X + radius; x++) {
					// Within manhattan distance to narrow points down
					var dx = borderPixel.X - x;
					var dy = borderPixel.Y - y;
					float distance = MathF.Sqrt (dx * dx + dy * dy);
					// If within actual distance
					if (distance <= radius) {
						int pixel_index = y * src_width + x;
						float mult = distance / radius;
						byte alpha = (byte) (src_data[pixel_index].A * mult);
						if (alpha < dst_data[pixel_index].A)
							dst_data[pixel_index].Bgra = src_data[pixel_index].NewAlpha (alpha).ToPremultipliedAlpha ().Bgra;
					}
				}
			}
		}
	}

	public sealed class FeatherData : EffectData
	{
		[Caption ("Radius"), MinimumValue (1), MaximumValue (100)]
		public int Radius { get; set; } = 6;

		[Caption ("Transparency Threshold"), MinimumValue (0), MaximumValue (255)]
		public int TransparencyThreshold { get; set; } = 20;
	}
}
