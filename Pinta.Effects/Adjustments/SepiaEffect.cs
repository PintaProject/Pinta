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

public sealed class SepiaEffect : BaseEffect
{
	static readonly UnaryPixelOp desaturate = new UnaryPixelOps.Desaturate ();
	static readonly UnaryPixelOp level = new UnaryPixelOps.Level (
			ColorBgra.Black,
			ColorBgra.White,
			[1.2f, 1.0f, 0.8f],
			ColorBgra.Black,
			ColorBgra.White);

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public SepiaEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();

		EffectData = new SepiaData ();
	}

	public override bool IsConfigurable
		=> true;
	public SepiaData Data
		=> (SepiaData) EffectData!;

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	public sealed override bool IsTileable
		=> true;

	public override string Icon
		=> Resources.Icons.AdjustmentsSepia;

	public override string Name
		=> Translations.GetString ("Sepia");

	public override string AdjustmentMenuKey
		=> "E";

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		desaturate.Apply (destination, source, roi);
		level.Apply (destination, destination, roi);
		float strength = Data.Strength / 100f;
		if (strength == 1) return;
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, source.GetSize ()))
			destinationData[pixel.memoryOffset] = ColorBgra.Lerp (
				sourceData[pixel.memoryOffset],
				destinationData[pixel.memoryOffset],
				strength);
	}

	public sealed class SepiaData : EffectData
	{
		[MinimumValue (0), MaximumValue (100)]
		public int Strength { get; set; } = 100;
	}
}
