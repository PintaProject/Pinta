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

public sealed class GlowEffect : BaseEffect<DBNull>
{
	private readonly UserBlendOps.ScreenBlendOp screen_blend_op;
	private readonly IServiceProvider services;

	public override string Icon
		=> Pinta.Resources.Icons.EffectsPhotoGlow;

	public sealed override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Glow");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Photo");

	public GlowData Data
		=> (GlowData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public GlowEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new GlowData ();
		screen_blend_op = new UserBlendOps.ScreenBlendOp ();
		this.services = services;
	}

	public override void LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	public override DBNull GetPreRender (ImageSurface src, ImageSurface dst)
		=> DBNull.Value;

	#region Algorithm Code Ported From PDN
	public override void Render (
		DBNull preRender,
		ImageSurface src,
		ImageSurface dest,
		ReadOnlySpan<RectangleI> rois)
	{
		GaussianBlurEffect blurEffect = new (services);
		blurEffect.Data.Radius = Data.Radius;
		var blurPreRender = blurEffect.GetPreRender (src, dest);
		blurEffect.Render (blurPreRender, src, dest, rois);

		BrightnessContrastEffect contrastEffect = new (services);
		contrastEffect.Data.Brightness = Data.Brightness;
		contrastEffect.Data.Contrast = Data.Contrast;
		var contrastPreRender = contrastEffect.GetPreRender (dest, dest);
		contrastEffect.Render (contrastPreRender, dest, dest, rois);

		var dst_data = dest.GetPixelData ();
		var src_data = src.GetReadOnlyPixelData ();
		int src_width = src.Width;
		int dst_width = dest.Width;

		foreach (RectangleI roi in rois) {
			for (int y = roi.Top; y <= roi.Bottom; ++y) {
				var dst_row = dst_data.Slice (y * dst_width + roi.Left, roi.Width);
				var src_row = src_data.Slice (y * src_width + roi.Left, roi.Width);
				screen_blend_op.Apply (dst_row, src_row, dst_row);
			}
		}
	}

	#endregion

	public sealed class GlowData : EffectData
	{
		[Caption ("Radius"), MinimumValue (1), MaximumValue (20)]
		public int Radius { get; set; } = 6;

		[Caption ("Brightness"), MinimumValue (-100), MaximumValue (100)]
		public int Brightness { get; set; } = 10;

		[Caption ("Contrast"), MinimumValue (-100), MaximumValue (100)]
		public int Contrast { get; set; } = 10;
	}
}
