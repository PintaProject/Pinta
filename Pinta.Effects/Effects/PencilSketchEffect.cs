/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
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

	public PencilSketchEffect ()
	{
		EffectData = new PencilSketchData ();

		blur_effect = new GaussianBlurEffect ();
		desaturate_op = new UnaryPixelOps.Desaturate ();
		invert_effect = new InvertColorsEffect ();
		bac_adjustment = new BrightnessContrastEffect ();
		color_dodge_op = new UserBlendOps.ColorDodgeBlendOp ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

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
		int dst_width = dest.Width;
		var src_data = src.GetReadOnlyPixelData ();
		int src_width = src.Width;

		foreach (Core.RectangleI roi in rois) {
			for (int y = roi.Top; y <= roi.Bottom; ++y) {
				var src_row = src_data.Slice (y * src_width, src_width);
				var dst_row = dst_data.Slice (y * dst_width, dst_width);

				for (int x = roi.Left; x <= roi.Right; ++x) {
					ColorBgra srcGrey = desaturate_op.Apply (src_row[x]);
					dst_row[x] = color_dodge_op.Apply (srcGrey, dst_row[x]);
				}
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
