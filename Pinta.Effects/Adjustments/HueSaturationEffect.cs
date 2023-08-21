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

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		int hue_delta = Data.Hue;
		int sat_delta = Data.Saturation;
		int lightness = Data.Lightness;

		UnaryPixelOp op = Data.IsDefault ? new UnaryPixelOps.Identity () : new UnaryPixelOps.HueSaturationLightness (hue_delta, sat_delta, lightness);

		op.Apply (dest, src, rois);
	}

	public HueSaturationData Data => (HueSaturationData) EffectData!;  // NRT - Set in constructor

	public sealed class HueSaturationData : EffectData
	{
		[Caption ("Hue"), MinimumValue (-180), MaximumValue (180)]
		public int Hue = 0;

		[Caption ("Saturation"), MinimumValue (0), MaximumValue (200)]
		public int Saturation = 100;

		[Caption ("Lightness")]
		public int Lightness = 0;

		[Skip]
		public override bool IsDefault => Hue == 0 && Saturation == 100 && Lightness == 0;
	}
}
