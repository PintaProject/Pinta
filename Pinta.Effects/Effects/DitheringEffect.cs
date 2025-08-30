using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class DitheringEffect : BaseEffect
{
	public override string Name
		=> Translations.GetString ("Dithering");

	public override bool IsConfigurable
		=> true;

	public override string Icon
		=> Resources.Icons.EffectsColorDithering;

	public override string EffectMenuCategory
		=> Translations.GetString ("Color");

	public DitheringData Data
		=> (DitheringData) EffectData!; // NRT - Set in constructor

	public override bool IsTileable
		=> false;

	private readonly IChromeService chrome;
	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;
	public DitheringEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();

		EffectData = new DitheringData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private sealed record DitheringSettings (
		int ChangedPixelCount,
		ImmutableArray<PixelOffset> PixelOffsets,
		ImmutableArray<ColorBgra> ChangedColors);

	private DitheringSettings CreateSettings (ImageSurface source, RectangleI roi)
	{
		DitheringData data = Data;

		Size canvasSize = source.GetSize ();

		if (roi.Width == 0 || roi.Height == 0)
			return new (
			  ChangedPixelCount: 0,
			  PixelOffsets: [],
			  ChangedColors: []);

		ImmutableArray<ColorBgra> chosenPalette = data.PaletteSource switch {
			PaletteSource.PresetPalettes => PaletteHelper.GetPredefined (data.PaletteChoice),
			PaletteSource.CurrentPalette => [.. palette.CurrentPalette.Colors.Select (CairoExtensions.ToColorBgra)],
			PaletteSource.RecentlyUsedColors => [.. palette.RecentlyUsedColors.Select (CairoExtensions.ToColorBgra)],
			_ => throw new UnreachableException (),
		};

		ErrorDiffusionMatrix diffusionMatrix = ErrorDiffusionMatrix.GetPredefined (data.ErrorDiffusionMethod);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();

		ColorBgra[] colorBuffer = new ColorBgra[roi.Width * roi.Height];

		int canvasWidth = canvasSize.Width;
		for (int y = 0; y < roi.Height; y++) {
			int sourceIndexStart = ((roi.Top + y) * canvasWidth) + roi.Left;
			int bufferIndexStart = y * roi.Width;
			sourceData.Slice (sourceIndexStart, roi.Width).CopyTo (colorBuffer.AsSpan (bufferIndexStart, roi.Width));
		}

		ImmutableArray<PixelOffset> pixelOffsets = [.. Tiling.GeneratePixelOffsets (roi, canvasSize)];

		foreach (var pixel in pixelOffsets) {

			PointI roiRelative = new (
				X: pixel.coordinates.X - roi.Left,
				Y: pixel.coordinates.Y - roi.Top);

			int bufferIndex = (roiRelative.Y * roi.Width) + roiRelative.X;

			ColorBgra originalPixel = colorBuffer[bufferIndex];
			ColorBgra closestColor = FindClosestPaletteColor (chosenPalette, originalPixel);

			colorBuffer[bufferIndex] = closestColor;

			int errorRed = originalPixel.R - closestColor.R;
			int errorGreen = originalPixel.G - closestColor.G;
			int errorBlue = originalPixel.B - closestColor.B;

			for (int r = 0; r < diffusionMatrix.Rows; r++) {

				for (int c = 0; c < diffusionMatrix.Columns; c++) {

					int weight = diffusionMatrix[r, c];

					if (weight <= 0)
						continue;

					PointI neighbor = new (
						X: roiRelative.X + c - diffusionMatrix.ColumnsToLeft,
						Y: roiRelative.Y + r);

					if (neighbor.X < 0 || neighbor.X >= roi.Width)
						continue;

					if (neighbor.Y >= roi.Height)
						continue;

					int neighborIndex = (neighbor.Y * roi.Width) + neighbor.X;

					double factor = weight * diffusionMatrix.WeightReductionFactor;

					colorBuffer[neighborIndex] = AddError (colorBuffer[neighborIndex], factor, errorRed, errorGreen, errorBlue);
				}
			}
		}

		ColorBgra[] changedColors = new ColorBgra[pixelOffsets.Length];
		for (int i = 0; i < pixelOffsets.Length; i++) {
			PointI coords = pixelOffsets[i].coordinates;
			int bufferIndex = (coords.Y - roi.Top) * roi.Width + (coords.X - roi.Left);
			changedColors[i] = colorBuffer[bufferIndex];
		}

		return new (
			ChangedPixelCount: pixelOffsets.Length,
			PixelOffsets: pixelOffsets,
			ChangedColors: [.. changedColors]);
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		DitheringSettings settings = CreateSettings (source, roi);
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		for (int i = 0; i < settings.ChangedPixelCount; i++)
			destinationData[settings.PixelOffsets[i].memoryOffset] = settings.ChangedColors[i];
	}

	private static ColorBgra AddError (ColorBgra color, double factor, int errorRed, int errorGreen, int errorBlue)
		=> ColorBgra.FromBgra (
			b: Utility.ClampToByte (color.B + (int) (factor * errorBlue)),
			g: Utility.ClampToByte (color.G + (int) (factor * errorGreen)),
			r: Utility.ClampToByte (color.R + (int) (factor * errorRed)),
			a: 255
		);

	private static ColorBgra FindClosestPaletteColor (ImmutableArray<ColorBgra> palette, ColorBgra original)
	{
		if (palette.IsDefault) throw new ArgumentException ("Palette not initialized", nameof (palette));
		if (palette.Length == 0) throw new ArgumentException ("Palette cannot be empty", nameof (palette));
		if (palette.Length == 1) return palette[0];
		double minDistance = double.MaxValue;
		ColorBgra closestColor = ColorBgra.FromBgra (0, 0, 0, 1);
		foreach (var paletteColor in palette) {
			double distance = CalculateSquaredDistance (original, paletteColor);
			if (distance >= minDistance) continue;
			minDistance = distance;
			closestColor = paletteColor;
		}
		return closestColor;
	}

	private static double CalculateSquaredDistance (ColorBgra color1, ColorBgra color2)
	{
		double deltaR = color1.R - color2.R;
		double deltaG = color1.G - color2.G;
		double deltaB = color1.B - color2.B;
		return deltaR * deltaR + deltaG * deltaG + deltaB * deltaB;
	}

	public enum PaletteSource
	{
		[Caption ("Preset Palettes")]
		PresetPalettes,

		[Caption ("Current Palette")]
		CurrentPalette,

		[Caption ("Recently Used Colors")]
		RecentlyUsedColors,
	}

	public sealed class DitheringData : EffectData
	{
		[Caption ("Error Diffusion Method")]
		public PredefinedDiffusionMatrices ErrorDiffusionMethod { get; set; } = PredefinedDiffusionMatrices.FloydSteinberg;

		[Caption ("Palette Source")]
		public PaletteSource PaletteSource { get; set; } = PaletteSource.PresetPalettes;

		[Caption ("Palette")]
		public PredefinedPalettes PaletteChoice { get; set; } = PredefinedPalettes.OldWindows16;
	}
}
