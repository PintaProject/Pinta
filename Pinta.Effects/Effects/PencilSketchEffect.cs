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
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class PencilSketchEffect : BaseEffect
{
	private readonly GaussianBlurEffect blur_effect;
	private readonly UnaryPixelOps.Desaturate desaturate_op;
	private readonly InvertColorsEffect invert_effect;
	private readonly BrightnessContrastEffect bac_adjustment;
	private readonly UserBlendOps.ColorDodgeBlendOp color_dodge_op;

	public override string Icon => Pinta.Resources.Icons.EffectsArtisticPencilSketch;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Pencil Sketch");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Artistic");

	public PencilSketchData Data => (PencilSketchData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public PencilSketchEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();

		EffectData = new PencilSketchData ();

		blur_effect = new GaussianBlurEffect (services);
		desaturate_op = new UnaryPixelOps.Desaturate ();
		invert_effect = new InvertColorsEffect (services);
		bac_adjustment = new BrightnessContrastEffect (services);
		color_dodge_op = new UserBlendOps.ColorDodgeBlendOp ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		bac_adjustment.Data.Brightness = -Data.ColorRange;
		bac_adjustment.Data.Contrast = -Data.ColorRange;
		bac_adjustment.Render (src, dest, rois);

		blur_effect.Data.Radius = Data.PencilTipSize;
		blur_effect.Render (src, dest, rois);

		invert_effect.Render (dest, dest, rois);
		desaturate_op.Apply (dest, dest, rois);

		var dst_data = dest.GetPixelData ();
		var src_data = src.GetReadOnlyPixelData ();

		Size canvasSize = src.GetSize ();

		foreach (RectangleI roi in rois) {
			foreach (var pixel in Tiling.GeneratePixelOffsets (roi, canvasSize)) {
				ColorBgra srcGrey = desaturate_op.Apply (src_data[pixel.memoryOffset]);
				dst_data[pixel.memoryOffset] = color_dodge_op.Apply (srcGrey, dst_data[pixel.memoryOffset]);
			}
		}
	}
	#endregion

	public sealed class PencilSketchData : EffectData
	{
		[Caption ("Pencil Tip Size"), MinimumValue (1), MaximumValue (20)]
		public int PencilTipSize { get; set; } = 2;

		[Caption ("Color Range"), MinimumValue (-20), MaximumValue (20)]
		public int ColorRange { get; set; } = 0;
	}
}
