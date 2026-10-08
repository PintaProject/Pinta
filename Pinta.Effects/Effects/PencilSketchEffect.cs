/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class PencilSketchEffect : BaseEffect
{
	private readonly GaussianBlurEffect gaussian_blur;
	private readonly UnaryPixelOps.Desaturate desaturate;
	private readonly InvertColorsEffect invert;
	private readonly BrightnessContrastEffect brightness_contrast;
	private readonly UserBlendOps.ColorDodgeBlendOp color_dodge;

	public override string Icon => Resources.Icons.EffectsArtisticPencilSketch;

	public sealed override bool IsTileable => false;

	public override string Name => Translations.GetString ("Pencil Sketch");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Artistic");

	public PencilSketchData Data => (PencilSketchData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public PencilSketchEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();

		EffectData = new PencilSketchData ();

		gaussian_blur = new GaussianBlurEffect (services);
		desaturate = new UnaryPixelOps.Desaturate ();
		invert = new InvertColorsEffect (services);
		brightness_contrast = new BrightnessContrastEffect (services);
		color_dodge = new UserBlendOps.ColorDodgeBlendOp ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	public override void Render (ImageSurface source, ImageSurface destination, ReadOnlySpan<RectangleI> rois)
	{
		PencilSketchData data = Data;

		gaussian_blur.Data.Radius = data.PencilTipSize;
		gaussian_blur.Render (source, destination, rois);

		brightness_contrast.Data.Brightness = data.ColorRange;
		brightness_contrast.Data.Contrast = -data.ColorRange;
		brightness_contrast.Render (destination, destination, rois);

		invert.Render (destination, destination, rois);

		desaturate.Apply (destination, destination, rois);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		Size canvasSize = source.GetSize ();

		foreach (RectangleI roi in rois) {
			foreach (var pixel in Tiling.GeneratePixelOffsets (roi, canvasSize)) {
				ColorBgra desaturatedSource = desaturate.Apply (sourceData[pixel.memoryOffset]);
				destinationData[pixel.memoryOffset] = color_dodge.Apply (desaturatedSource, destinationData[pixel.memoryOffset]);
			}
		}
	}

	public sealed class PencilSketchData : EffectData
	{
		[Caption ("Pencil Tip Size")]
		[MinimumValue (1), MaximumValue (20)]
		public int PencilTipSize { get; set; } = 2;

		[Caption ("Color Range")]
		[MinimumValue (-20), MaximumValue (20)]
		public int ColorRange { get; set; } = 0;
	}
}
