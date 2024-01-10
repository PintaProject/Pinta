using System;
using System.Collections.Immutable;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class DitheringEffect : BaseEffect
{
	public override string Name => Translations.GetString ("Dithering");
	public override bool IsConfigurable => true;
	// TODO: Icon
	public override string EffectMenuCategory => Translations.GetString ("Color");
	public DitheringData Data => (DitheringData) EffectData!; // NRT - Set in constructor

	public override bool IsTileable => false;

	public DitheringEffect ()
	{
		EffectData = new DitheringData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	private sealed record DitheringSettings (
		ErrorDiffusionMatrix diffusionMatrix,
		ImmutableArray<ColorBgra> palette,
		int sourceWidth,
		int sourceHeight);

	private DitheringSettings CreateSettings (ImageSurface src)
		=> new (
			diffusionMatrix: ErrorDiffusionMatrix.GetPredefined (Data.DiffusionMatrix),
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

		for (int y = roi.Top; y <= roi.Bottom; y++) {

			for (int x = roi.Left; x <= roi.Right; x++) {

				int currentIndex = y * settings.sourceWidth + x;
				ColorBgra originalPixel = dst_data[currentIndex];
				ColorBgra closestColor = FindClosestPaletteColor (settings.palette, originalPixel);

				dst_data[currentIndex] = closestColor;

				int errorRed = originalPixel.R - closestColor.R;
				int errorGreen = originalPixel.G - closestColor.G;
				int errorBlue = originalPixel.B - closestColor.B;

				for (int r = 0; r < settings.diffusionMatrix.Rows; r++) {

					for (int c = 0; c < settings.diffusionMatrix.Columns; c++) {

						var weight = settings.diffusionMatrix[r, c];

						if (weight <= 0)
							continue;

						PointI thisItem = new (
							X: x + c - settings.diffusionMatrix.ColumnsToLeft,
							Y: y + r
						);

						if (thisItem.X < 0 || thisItem.X >= settings.sourceWidth)
							continue;

						if (thisItem.Y < 0 || thisItem.Y >= settings.sourceHeight)
							continue;

						int idx = (thisItem.Y * settings.sourceWidth) + thisItem.X;

						double factor = ((double) weight) / settings.diffusionMatrix.TotalWeight;

						dst_data[idx] = AddError (dst_data[idx], factor, errorRed, errorGreen, errorBlue);
					}
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
		[Caption ("Diffusion Matrix")]
		public PredefinedDiffusionMatrices DiffusionMatrix { get; set; } = PredefinedDiffusionMatrices.FloydSteinberg;

		[Caption ("Palette")]
		public PredefinedPalettes PaletteChoice { get; set; } = PredefinedPalettes.OldWindows16;
	}
}
