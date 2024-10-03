/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class RedEyeRemoveEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsPhotoRedEyeRemove;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Red Eye Removal");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Photo");

	public RedEyeRemoveData Data => (RedEyeRemoveData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public RedEyeRemoveEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();

		EffectData = new RedEyeRemoveData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		var op = new UnaryPixelOps.RedEyeRemove (Data.Tolerance, Data.Saturation);
		op.Apply (dest, src, rois);
	}
}

public sealed class RedEyeRemoveData : EffectData
{
	[Caption ("Tolerance"), MinimumValue (0), MaximumValue (100)]
	public int Tolerance { get; set; } = 70;

	[MinimumValue (0), MaximumValue (100)]
	[Caption ("Saturation Percentage")]
	[Hint ("Hint: For best results, first use selection tools to select each eye.")]
	public int Saturation { get; set; } = 90;
}

