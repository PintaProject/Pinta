/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class RedEyeRemoveEffect : BaseEffect
{
	private UnaryPixelOp? op = null;

	public override string Icon => Pinta.Resources.Icons.EffectsPhotoRedEyeRemove;

	public override string Name => Translations.GetString ("Red Eye Removal");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Photo");

	public RedEyeRemoveData Data => (RedEyeRemoveData) EffectData!;  // NRT - Set in constructor

	public RedEyeRemoveEffect ()
	{
		EffectData = new RedEyeRemoveData ();

		EffectData.PropertyChanged += (_, _) => {
			op = new UnaryPixelOps.RedEyeRemove (Data.Tolerance, Data.Saturation);
		};
	}

	public override void LaunchConfiguration ()
	{
		EffectHelper.LaunchSimpleEffectDialog (this);
	}

	public override void Render (ImageSurface src, ImageSurface dest, Core.RectangleI[] rois)
	{
		op?.Apply (dest, src, rois);
	}
}

public sealed class RedEyeRemoveData : EffectData
{
	[Caption ("Tolerance"), MinimumValue (0), MaximumValue (100)]
	public int Tolerance = 70;

	[MinimumValue (0), MaximumValue (100)]
	[Caption ("Saturation Percentage")]
	[Hint ("Hint: For best results, first use selection tools to select each eye.")]
	public int Saturation = 90;
}

