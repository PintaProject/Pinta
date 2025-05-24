using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class DitheringEffect : BaseEffect
{
	public override string Name => Translations.GetString ("Dithering");
	public override bool IsConfigurable => true;
	public override string Icon => Resources.Icons.EffectsColorDithering;
	public override string EffectMenuCategory => Translations.GetString ("Color");
	public DitheringData Data => (DitheringData) EffectData!; // NRT - Set in constructor

	public override bool IsTileable => false;

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public DitheringEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new DitheringData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private sealed record DitheringSettings (
		ErrorDiffusionMatrix diffusionMatrix,
		ImmutableArray<ColorBgra> palette,
		int sourceWidth,
		int sourceHeight);

	private DitheringSettings CreateSettings (ImageSurface src)
		=> new (
			diffusionMatrix: ErrorDiffusionMatrix.GetPredefined (Data.ErrorDiffusionMethod),
			palette: PaletteHelper.GetPredefined (Data.PaletteChoice),
			sourceWidth: src.Width,
			sourceHeight: src.Height
		);

	protected override void Render (ImageSurface src, ImageSurface dest, RectangleI roi)
	{
		DitheringSettings settings = CreateSettings (src);

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dest.GetPixelData ();

		for (int y = roi.Top; y <= roi.Bottom; y++) {
			for (int x = roi.Left; x <= roi.Right; x++) {
				int currentIndex = y * settings.sourceWidth + x;
				dst_data[currentIndex] = src_data[currentIndex];
			}
		}

		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, dest.GetSize ())) {

			ColorBgra originalPixel = dst_data[pixel.memoryOffset];
			ColorBgra closestColor = FindClosestPaletteColor (settings.palette, originalPixel);

			dst_data[pixel.memoryOffset] = closestColor;

			int errorRed = originalPixel.R - closestColor.R;
			int errorGreen = originalPixel.G - closestColor.G;
			int errorBlue = originalPixel.B - closestColor.B;

			for (int r = 0; r < settings.diffusionMatrix.Rows; r++) {

				for (int c = 0; c < settings.diffusionMatrix.Columns; c++) {

					var weight = settings.diffusionMatrix[r, c];

					if (weight <= 0)
						continue;

					PointI thisItem = new (
						X: pixel.coordinates.X + c - settings.diffusionMatrix.ColumnsToLeft,
						Y: pixel.coordinates.Y + r
					);

					if (thisItem.X < roi.Left || thisItem.X >= roi.Right)
						continue;

					if (thisItem.Y < roi.Top || thisItem.Y >= roi.Bottom)
						continue;

					int idx = (thisItem.Y * settings.sourceWidth) + thisItem.X;

					double factor = ((double) weight) / settings.diffusionMatrix.TotalWeight;

					dst_data[idx] = AddError (dst_data[idx], factor, errorRed, errorGreen, errorBlue);
				}
			}
		}
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

	public sealed class DitheringData : EffectData
	{
		[Caption ("Error Diffusion Method")]
		public PredefinedDiffusionMatrices ErrorDiffusionMethod { get; set; } = PredefinedDiffusionMatrices.FloydSteinberg;

		[Caption ("Palette")]
		public PredefinedPalettes PaletteChoice { get; set; } = PredefinedPalettes.OldWindows16;
	}
}
