using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class ColorQuantizationEffect : BaseEffect
{
	public override string Name
		=> Translations.GetString ("Color Quantization");

	public override string EffectMenuCategory
		=> Translations.GetString ("Color");

	public override string Icon
		=> Resources.Icons.EffectsColorQuantization;

	public override bool IsConfigurable
		=> true;
	public override bool IsTileable
		=> false;

	public QuantizationData Data
		=> (QuantizationData) EffectData!; // NRT - Set in constructor


	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;

	public ColorQuantizationEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();

		EffectData = new QuantizationData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private sealed record Settings (
		int ChangedPixelCount,
		ImmutableArray<PixelOffset> PixelOffsets,
		ImmutableArray<ColorBgra> ChangedColors
	);

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		var s = CreateSettings (source, roi);
		var dest = destination.GetPixelData ();
		for (int i = 0; i < s.ChangedPixelCount; i++)
			dest[s.PixelOffsets[i].memoryOffset] = s.ChangedColors[i];
	}

	private Settings CreateSettings (ImageSurface source, RectangleI roi)
	{
		ReadOnlySpan<ColorBgra> src = source.GetReadOnlyPixelData ();
		Size size = source.GetSize ();

		var pixelOffsets = ImmutableArray.CreateRange (Tiling.GeneratePixelOffsets (roi, size));

		// Compute the palette using the median-cut algorithm.
		const int maxSample = 20_000; // Should be large enough that the sampling is unnoticeable 
		var palette = BuildPaletteWithMedianCut (src, size, roi, Data.ColorCount, maxSample);

		// Map every pixel to the nearest palette entry.
		ColorBgra[] mapped = new ColorBgra[pixelOffsets.Length];
		for (int i = 0; i < pixelOffsets.Length; i++) {
			var px = src[pixelOffsets[i].memoryOffset];
			mapped[i] = Nearest (px, palette);
		}

		return new Settings (pixelOffsets.Length, pixelOffsets, mapped.ToImmutableArray ());
	}

	// A slice into the pixel array that contains all the samples. We use slices to avoid needing to reallocate constantly
	// It also has cached values for the min and max for each channel since we read them repeatedly
	private sealed class Box
	{
		public int Start, Length;
		public byte MinR, MaxR, MinG, MaxG, MinB, MaxB;

		public Box (int start, int length) { Start = start; Length = length; }

		public void RefreshMinMax (ColorBgra[] pts)
		{
			// figure out how wide this box is on each channel
			byte minR = 255, maxR = 0, minG = 255, maxG = 0, minB = 255, maxB = 0;
			int end = Start + Length;
			for (int i = Start; i < end; i++) {
				var p = pts[i];
				if (p.R < minR) minR = p.R; if (p.R > maxR) maxR = p.R;
				if (p.G < minG) minG = p.G; if (p.G > maxG) maxG = p.G;
				if (p.B < minB) minB = p.B; if (p.B > maxB) maxB = p.B;
			}
			MinR = minR; MaxR = maxR;
			MinG = minG; MaxG = maxG;
			MinB = minB; MaxB = maxB;
		}

		// pick the channel that has the largest range
		// 0: red, 1: green, 2: blue
		public int WidestChannel ()
		{
			int rRange = MaxR - MinR;
			int gRange = MaxG - MinG;
			int bRange = MaxB - MinB;
			if (rRange >= gRange && rRange >= bRange) return 0;
			if (gRange >= rRange && gRange >= bRange) return 1;
			return 2;
		}
	}

	private static ColorBgra[] BuildPaletteWithMedianCut (ReadOnlySpan<ColorBgra> src, Size size, RectangleI roi, int k, int maxSample)
	{
		// We use sampling so that the algorithm doesn't take too long on large images
		int total = roi.Width * roi.Height;
		int target = Math.Min (maxSample, Math.Max (1, total));
		var sample = new ColorBgra[target];

		int distBetweenSamples = Math.Max (1, (int) Math.Ceiling (Math.Sqrt (total / (double) target)));
		int count = 0;
		for (int y = roi.Top; y < roi.Bottom && count < target; y += distBetweenSamples) {
			int row = y * size.Width;
			for (int x = roi.Left; x < roi.Right && count < target; x += distBetweenSamples)
				sample[count++] = src[row + x];
		}
		// we assume later that the list has at least one element, so we need to handle the 0 case here
		if (count == 0) return new[] { ColorBgra.Black, ColorBgra.White };
		if (count < target) Array.Resize (ref sample, count);

		// Start with one big box covering every color
		var boxes = new List<Box> (k) { new Box (0, sample.Length) };
		boxes[0].RefreshMinMax (sample);

		// Then keep splitting boxes until we have enough colors
		// We pick the box with the widest color range, then split it in half along that channel
		while (boxes.Count < k) {
			int pick = PickBoxToSplit (boxes);
			var box = boxes[pick];

			// If there's nothing left to split, we can stop early
			if (box.Length <= 1 || (box.MaxR == box.MinR && box.MaxG == box.MinG && box.MaxB == box.MinB)) {
				break;
			}

			int channel = box.WidestChannel ();

			// We sort the box's slice by that channel
			// Since the box should be the only one with a view of that specific slice, we can just sort in-place
			switch (channel) {
				case 0: // red
					Array.Sort (sample, box.Start, box.Length, Comparer<ColorBgra>.Create ((a, b) => a.R.CompareTo (b.R)));
					break;
				case 1: // green
					Array.Sort (sample, box.Start, box.Length, Comparer<ColorBgra>.Create ((a, b) => a.G.CompareTo (b.G)));
					break;
				default: // blue
					Array.Sort (sample, box.Start, box.Length, Comparer<ColorBgra>.Create ((a, b) => a.B.CompareTo (b.B)));
					break;
			}

			// Split at the median
			int leftLen = box.Length / 2;
			if (leftLen == 0) break;
			var left = new Box (box.Start, leftLen);
			var right = new Box (box.Start + leftLen, box.Length - leftLen);

			left.RefreshMinMax (sample);
			right.RefreshMinMax (sample);

			// We replace the old box since it's no longer relevant
			boxes[pick] = left;
			boxes.Add (right);
		}

		// Once we have all our boxes, we just compute the average color in each final box
		var palette = new ColorBgra[boxes.Count];
		for (int i = 0; i < boxes.Count; i++) {
			var b = boxes[i];
			long r = 0, g = 0, bl = 0;
			int end = b.Start + b.Length;
			for (int j = b.Start; j < end; j++) {
				r += sample[j].R;
				g += sample[j].G;
				bl += sample[j].B;
			}
			int n = Math.Max (1, b.Length);
			palette[i] = ColorBgra.FromBgra ((byte) (bl / n), (byte) (g / n), (byte) (r / n), 255);
		}

		return palette;
	}

	private static int PickBoxToSplit (List<Box> boxes)
	{
		// We just find the box with the largest range for a single channel.
		// Maybe using euclidean distance could also be a good heuristic, but this works well 
		// and there doesn't seem to be a commonly agreed-on preferred way to do it anyway.
		int best = 0;
		int bestRange = MaxChannelRange (boxes[0]);
		for (int i = 1; i < boxes.Count; i++) {
			int r = MaxChannelRange (boxes[i]);
			if (r > bestRange) { best = i; bestRange = r; }
		}
		return best;

		static int MaxChannelRange (Box b)
			=> Math.Max (b.MaxR - b.MinR, Math.Max (b.MaxG - b.MinG, b.MaxB - b.MinB));
	}

	// Maps a pixels to the nearest palette color
	// This is just brute-force, but performance is fine enough since the max palette size is 256
	private static ColorBgra Nearest (ColorBgra p, ColorBgra[] palette)
	{
		int best = 0;
		int bd = ColorDist (p, palette[0]);
		for (int i = 1; i < palette.Length; i++) {
			int d = ColorDist (p, palette[i]);
			if (d < bd) { bd = d; best = i; }
		}
		return palette[best];
	}

	// Simple euclidean distance between 2 colors
	private static int ColorDist (ColorBgra a, ColorBgra b)
	{
		int dr = a.R - b.R;
		int dg = a.G - b.G;
		int db = a.B - b.B;
		return dr * dr + dg * dg + db * db;
	}

	public sealed class QuantizationData : EffectData
	{
		[Caption ("Colors (N)")]
		[MinimumValue (2), MaximumValue (256)]
		public int ColorCount { get; set; } = 16;
	}
}
