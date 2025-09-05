/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class BrightnessContrastEffect : BaseEffect
{
	private sealed class PreRender
	{
		public int Multiply { get; }
		public int Divide { get; }
		public ReadOnlyCollection<byte> RGBTable { get; }
		public PreRender (int brightness, int contrast)
		{
			(int multiply, int divide) = contrast switch {
				< 0 => (contrast + 100, 100),
				> 0 => (100, 100 - contrast),
				_ => (1, 1),
			};

			(Multiply, Divide) = (multiply, divide);
			RGBTable = Array.AsReadOnly (CalculateTable (brightness, multiply, divide));
		}

		private static byte[] CalculateTable (int brightness, int multiply, int divide)
		{
			byte[] result = new byte[65536];

			if (divide == 0) {
				for (int intensity = 0; intensity < 256; intensity++) {
					if (intensity + brightness < 128)
						result[intensity] = 0;
					else
						result[intensity] = 255;
				}
			} else if (divide == 100) {
				for (int intensity = 0; intensity < 256; intensity++) {
					int shift = (intensity - 127) * multiply / divide + 127 - intensity + brightness;

					for (int col = 0; col < 256; ++col) {
						int index = (intensity * 256) + col;
						result[index] = Utility.ClampToByte (col + shift);
					}
				}
			} else {
				for (int intensity = 0; intensity < 256; ++intensity) {
					int shift = (intensity - 127 + brightness) * multiply / divide + 127 - intensity;

					for (int col = 0; col < 256; ++col) {
						int index = (intensity * 256) + col;
						result[index] = Utility.ClampToByte (col + shift);
					}
				}
			}

			return result;
		}
	}

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

	private readonly record struct BrightnessContrastSettings (
		PreRender PreRender,
		Size CanvasSize,
		bool DivideIsZero);
	private static BrightnessContrastSettings CreateSettings (ImageSurface destination, PreRender preRender)
	{
		return new (
			PreRender: preRender,
			CanvasSize: destination.GetSize (),
			DivideIsZero: preRender.Divide == 0);
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		BrightnessContrastSettings settings = CreateSettings (destination, pre_render.Value);

		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();

		foreach (var pixel in Tiling.GeneratePixelOffsets (roi, settings.CanvasSize))
			destinationData[pixel.memoryOffset] = GetPixelColor (settings, sourceData[pixel.memoryOffset]);
	}

	private static ColorBgra GetPixelColor (in BrightnessContrastSettings settings, in ColorBgra originalColor)
	{
		int intensity = originalColor.GetIntensityByte ();
		if (settings.DivideIsZero) {
			uint c = settings.PreRender.RGBTable[intensity];
			return ColorBgra.FromUInt32 ((originalColor.BGRA & 0xff000000) | c | (c << 8) | (c << 16));
		} else {
			int shiftIndex = intensity * 256;
			return ColorBgra.FromBgra (
				b: settings.PreRender.RGBTable[shiftIndex + originalColor.B],
				g: settings.PreRender.RGBTable[shiftIndex + originalColor.G],
				r: settings.PreRender.RGBTable[shiftIndex + originalColor.R],
				a: originalColor.A);
		}
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
