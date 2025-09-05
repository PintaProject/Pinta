/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Data;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class GlowEffect : BaseEffect
{
	private readonly UserBlendOps.ScreenBlendOp screen_blend_op;
	private readonly IServiceProvider services;

	public override string Icon => Resources.Icons.EffectsPhotoGlow;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Glow");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Photo");

	public GlowData Data => (GlowData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public GlowEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new GlowData ();
		screen_blend_op = new UserBlendOps.ScreenBlendOp ();
		this.services = services;
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	#region Algorithm Code Ported From PDN
	public override void Render (ImageSurface source, ImageSurface destination, ReadOnlySpan<RectangleI> rois)
	{
		GlowData data = Data;
		int brightness = data.Brightness;
		int contrast = data.Contrast;

		GaussianBlurEffect blurEffect = new (services);
		blurEffect.Data.Radius = data.Radius;
		blurEffect.Render (source, destination, rois);

		Lazy<BrightnessContrast.PreRender> brightnessContrast = new (() => new (brightness, contrast));

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		Size canvasSize = destination.GetSize ();

		foreach (RectangleI roi in rois) {
			foreach (var pixel in Tiling.GeneratePixelOffsets (roi, canvasSize)) {
				ColorBgra original = sourceData[pixel.memoryOffset];
				ColorBgra blurred = destinationData[pixel.memoryOffset];
				ColorBgra blurredAdjusted = brightnessContrast.Value.Apply (blurred);
				destinationData[pixel.memoryOffset] = screen_blend_op.Apply (blurredAdjusted, original);
			}
		}
	}
	#endregion

	public sealed class GlowData : EffectData
	{
		[Caption ("Radius")]
		[MinimumValue (1), MaximumValue (20)]
		public int Radius { get; set; } = 6;

		[Caption ("Brightness")]
		[MinimumValue (-100), MaximumValue (100)]
		public int Brightness { get; set; } = 10;

		[Caption ("Contrast")]
		[MinimumValue (-100), MaximumValue (100)]
		public int Contrast { get; set; } = 10;
	}
}
