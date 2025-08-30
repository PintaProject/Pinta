using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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

		ImmutableArray<ColorBgra> chosenPalette = data.PaletteSource switch {
			PaletteSource.PresetPalettes => PaletteHelper.GetPredefined (data.PaletteChoice),
			PaletteSource.CurrentPalette => [.. palette.CurrentPalette.Colors.Select (CairoExtensions.ToColorBgra)],
			PaletteSource.RecentlyUsedColors => [.. palette.RecentlyUsedColors.Select (CairoExtensions.ToColorBgra)],
			_ => throw new UnreachableException (),
		};

		ErrorDiffusionMatrix diffusionMatrix = ErrorDiffusionMatrix.GetPredefined (data.ErrorDiffusionMethod);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();

		ColorBgra[] colorBuffer = sourceData.ToArray ();

		ImmutableArray<PixelOffset> pixelOffsets = [.. Tiling.GeneratePixelOffsets (roi, canvasSize)];

		foreach (var pixel in pixelOffsets) {

			ColorBgra originalPixel = colorBuffer[pixel.memoryOffset];
			ColorBgra closestColor = FindClosestPaletteColor (chosenPalette, originalPixel);

			colorBuffer[pixel.memoryOffset] = closestColor;

			int errorRed = originalPixel.R - closestColor.R;
			int errorGreen = originalPixel.G - closestColor.G;
			int errorBlue = originalPixel.B - closestColor.B;

			for (int r = 0; r < diffusionMatrix.Rows; r++) {

				for (int c = 0; c < diffusionMatrix.Columns; c++) {

					int weight = diffusionMatrix[r, c];

					if (weight <= 0)
						continue;

					PointI thisItem = new (
						X: pixel.coordinates.X + c - diffusionMatrix.ColumnsToLeft,
						Y: pixel.coordinates.Y + r);

					if (thisItem.X < roi.Left || thisItem.X >= roi.Right)
						continue;

					if (thisItem.Y < roi.Top || thisItem.Y >= roi.Bottom)
						continue;

					int neighborIndex = (thisItem.Y * canvasSize.Width) + thisItem.X;

					double factor = weight * diffusionMatrix.WeightReductionFactor;

					colorBuffer[neighborIndex] = AddError (colorBuffer[neighborIndex], factor, errorRed, errorGreen, errorBlue);
				}
			}
		}

		return new (
			ChangedPixelCount: roi.Width * roi.Height,
			PixelOffsets: pixelOffsets,
			ChangedColors: [.. pixelOffsets.Select (p => colorBuffer[p.memoryOffset])]);
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
