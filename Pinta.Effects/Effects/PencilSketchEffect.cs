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
	private readonly GaussianBlurEffect blur_effect;
	private readonly UnaryPixelOps.Desaturate desaturate_op;
	private readonly InvertColorsEffect invert_effect;
	private readonly UserBlendOps.ColorDodgeBlendOp color_dodge_op;

	public override string Icon => Resources.Icons.EffectsArtisticPencilSketch;

	public sealed override bool IsTileable => true;

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

		blur_effect = new GaussianBlurEffect (services);
		desaturate_op = new UnaryPixelOps.Desaturate ();
		invert_effect = new InvertColorsEffect (services);
		color_dodge_op = new UserBlendOps.ColorDodgeBlendOp ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface source, ImageSurface destination, ReadOnlySpan<RectangleI> rois)
	{
		PencilSketchData data = Data;
		int colorRange = data.ColorRange;

		Size canvasSize = source.GetSize ();

		Lazy<BrightnessContrast.PreRender> brightnessContrast = new (() => new (-colorRange, -colorRange));

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		foreach (RectangleI roi in rois)
			foreach (var pixel in Tiling.GeneratePixelOffsets (roi, canvasSize))
				destinationData[pixel.memoryOffset] = brightnessContrast.Value.GetPixelColor (sourceData[pixel.memoryOffset]);

		blur_effect.Data.Radius = Data.PencilTipSize;
		blur_effect.Render (source, destination, rois);

		invert_effect.Render (destination, destination, rois);
		desaturate_op.Apply (destination, destination, rois);

		foreach (RectangleI roi in rois) {
			foreach (var pixel in Tiling.GeneratePixelOffsets (roi, canvasSize)) {
				ColorBgra srcGrey = desaturate_op.Apply (sourceData[pixel.memoryOffset]);
				destinationData[pixel.memoryOffset] = color_dodge_op.Apply (srcGrey, destinationData[pixel.memoryOffset]);
			}
		}
	}
	#endregion

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
