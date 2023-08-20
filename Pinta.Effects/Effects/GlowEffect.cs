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

public sealed class GlowEffect : BaseEffect
{
	private readonly GaussianBlurEffect blur_effect;
	private readonly BrightnessContrastEffect contrast_effect;
	private readonly UserBlendOps.ScreenBlendOp screen_blend_op;

	public override string Icon => Pinta.Resources.Icons.EffectsPhotoGlow;

	public override string Name => Translations.GetString ("Glow");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Photo");

	public GlowData Data => (GlowData) EffectData!;  // NRT - Set in constructor

	public GlowEffect ()
	{
		EffectData = new GlowData ();

		blur_effect = new GaussianBlurEffect ();
		contrast_effect = new BrightnessContrastEffect ();
		screen_blend_op = new UserBlendOps.ScreenBlendOp ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		blur_effect.Data.Radius = Data.Radius;
		blur_effect.Render (src, dest, rois);

		contrast_effect.Data.Brightness = Data.Brightness;
		contrast_effect.Data.Contrast = Data.Contrast;
		contrast_effect.Render (dest, dest, rois);

		var dst_data = dest.GetPixelData ();
		var src_data = src.GetReadOnlyPixelData ();
		int src_width = src.Width;
		int dst_width = dest.Width;

		foreach (Core.RectangleI roi in rois) {
			for (int y = roi.Top; y <= roi.Bottom; ++y) {
				var dst_row = dst_data.Slice (y * dst_width + roi.Left, roi.Width);
				var src_row = dst_data.Slice (y * src_width + roi.Left, roi.Width);
				screen_blend_op.Apply (dst_row, src_row, dst_row);
			}
		}
	}
	#endregion

	public sealed class GlowData : EffectData
	{
		[Caption ("Radius"), MinimumValue (1), MaximumValue (20)]
		public int Radius = 6;
		[Caption ("Brightness"), MinimumValue (-100), MaximumValue (100)]
		public int Brightness = 10;
		[Caption ("Contrast"), MinimumValue (-100), MaximumValue (100)]
		public int Contrast = 10;
	}
}
