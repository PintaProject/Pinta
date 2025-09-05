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
using static Pinta.Effects.BrightnessContrast;

namespace Pinta.Effects;

public sealed class BrightnessContrastEffect : BaseEffect
{
	private Lazy<PreRender> pre_render = new (() => new (DEFAULT_BRIGHTNESS, DEFAULT_CONTRAST));

	public sealed override bool IsTileable
		=> true;

	public override string Icon
		=> Resources.Icons.AdjustmentsBrightnessContrast;

	public override string Name
		=> Translations.GetString ("Brightness / Contrast");

	public override bool IsConfigurable
		=> true;

	public override string AdjustmentMenuKey
		=> "B";

	public BrightnessContrastData Data
		=> (BrightnessContrastData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public BrightnessContrastEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new BrightnessContrastData ();
		EffectData.PropertyChanged += HandleEffectDataPropertyChanged;
	}

	/// <summary>
	/// If any of the effect data was changed, we need to recalculate the rgb table before rendering
	/// </summary>
	void HandleEffectDataPropertyChanged (object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		BrightnessContrastData data = Data;
		int brightness = data.Brightness;
		int contrast = data.Contrast;
		pre_render = new Lazy<PreRender> (() => new (brightness, contrast));
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private readonly record struct BrightnessContrastSettings (PreRender PreRender, Size CanvasSize);
	private static BrightnessContrastSettings CreateSettings (ImageSurface destination, PreRender preRender)
		=> new (PreRender: preRender, CanvasSize: destination.GetSize ());

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		BrightnessContrastSettings settings = CreateSettings (destination, pre_render.Value);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.CanvasSize))
			destinationData[pixel.memoryOffset] = settings.PreRender.Apply (sourceData[pixel.memoryOffset]);
	}

	const int DEFAULT_BRIGHTNESS = 0;
	const int DEFAULT_CONTRAST = 0;

	public sealed class BrightnessContrastData : EffectData
	{
		private int brightness = DEFAULT_BRIGHTNESS;
		private int contrast = DEFAULT_CONTRAST;

		[Caption ("Brightness")]
		public int Brightness {
			get => brightness;
			set {
				if (value == brightness) return;
				brightness = value;
				FirePropertyChanged (nameof (Brightness));
			}
		}

		[Caption ("Contrast")]
		public int Contrast {
			get => contrast;
			set {
				if (value == contrast) return;
				contrast = value;
				FirePropertyChanged (nameof (Contrast));
			}
		}

		[Skip]
		public override bool IsDefault => Brightness == DEFAULT_BRIGHTNESS && Contrast == DEFAULT_CONTRAST;
	}
}
