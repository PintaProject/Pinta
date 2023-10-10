/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class HueSaturationEffect : BaseEffect
{
	public sealed override bool IsTileable => true;

	public override string Icon => Pinta.Resources.Icons.AdjustmentsHueSaturation;

	public override string Name => Translations.GetString ("Hue / Saturation");

	public override bool IsConfigurable => true;

	public override string AdjustmentMenuKey => "U";

	public HueSaturationEffect ()
	{
		EffectData = new HueSaturationData ();
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	private UnaryPixelOp CreateOptimalOp ()
	{
		if (Data.IsDefault) {
			return new UnaryPixelOps.Identity ();
		} else {
			return new UnaryPixelOps.HueSaturationLightness (
				hueDelta: Data.Hue,
				satDelta: Data.Saturation,
				lightness: Data.Lightness
			);
		}
	}

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		UnaryPixelOp op = CreateOptimalOp ();
		op.Apply (dest, src, rois);
	}

	public HueSaturationData Data => (HueSaturationData) EffectData!;  // NRT - Set in constructor

	public sealed class HueSaturationData : EffectData
	{
		[Caption ("Hue"), MinimumValue (-180), MaximumValue (180)]
		public int Hue { get; set; } = 0;

		[Caption ("Saturation"), MinimumValue (0), MaximumValue (200)]
		public int Saturation { get; set; } = 100;

		[Caption ("Lightness")]
		public int Lightness { get; set; } = 0;

		[Skip]
		public override bool IsDefault => Hue == 0 && Saturation == 100 && Lightness == 0;
	}
}
