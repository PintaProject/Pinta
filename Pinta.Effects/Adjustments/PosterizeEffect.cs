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

namespace Pinta.Effects;

public sealed class PosterizeEffect : BaseEffect<DBNull>
{
	UnaryPixelOps.PosterizePixel? op = null;

	public sealed override bool IsTileable
		=> true;

	public override string Icon
		=> Pinta.Resources.Icons.AdjustmentsPosterize;

	public override string Name
		=> Translations.GetString ("Posterize");

	public override bool IsConfigurable
		=> true;

	public override string AdjustmentMenuKey
		=> "P";

	public PosterizeData Data
		=> (PosterizeData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;

	public PosterizeEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new PosterizeData ();
	}

	public override void LaunchConfiguration ()
	{
		PosterizeDialog dialog = new (chrome) {
			Title = Name,
			IconName = Icon,
			EffectData = Data
		};

		dialog.OnResponse += (_, args) => {
			OnConfigDialogResponse (args.ResponseId == (int) Gtk.ResponseType.Ok);
			dialog.Destroy ();
		};

		dialog.Present ();
	}

	public override DBNull GetPreRender (ImageSurface src, ImageSurface dst)
		=> DBNull.Value;

	public override void Render (
		DBNull preRender,
		ImageSurface src,
		ImageSurface dest,
		ReadOnlySpan<RectangleI> rois)
	{
		op ??= new UnaryPixelOps.PosterizePixel (Data.Red, Data.Green, Data.Blue);

		op.Apply (dest, src, rois);
	}
}

public sealed class PosterizeData : EffectData
{
	public int Red { get; set; } = 16;
	public int Green { get; set; } = 16;
	public int Blue { get; set; } = 16;
}
