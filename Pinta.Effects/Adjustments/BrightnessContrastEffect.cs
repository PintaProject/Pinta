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

public sealed class BrightnessContrastEffect : BaseEffect
{
	private int multiply;
	private int divide;
	private byte[]? rgb_table;
	private bool table_calculated;

	public sealed override bool IsTileable => true;

	public override string Icon => Pinta.Resources.Icons.AdjustmentsBrightnessContrast;

	public override string Name => Translations.GetString ("Brightness / Contrast");

	public override bool IsConfigurable => true;

	public override string AdjustmentMenuKey => "B";

	public BrightnessContrastData Data => (BrightnessContrastData) EffectData!;  // NRT - Set in constructor

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
		table_calculated = false;
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		if (!table_calculated)
			Calculate ();

		var src_data = src.GetReadOnlyPixelData ();
		var dst_data = dest.GetPixelData ();
		int width = src.Width;

		foreach (RectangleI rect in rois) {
			for (int y = rect.Top; y <= rect.Bottom; y++) {
				var src_row = src_data.Slice (y * width + rect.Left, rect.Width);
				var dst_row = dst_data.Slice (y * width + rect.Left, rect.Width);

				if (divide == 0) {
					for (int i = 0; i < src_row.Length; ++i) {
						ColorBgra col = src_row[i];
						uint c = rgb_table![col.GetIntensityByte ()]; // NRT - Set in Calculate
						dst_row[i].Bgra = (col.Bgra & 0xff000000) | c | (c << 8) | (c << 16);
					}
				} else {
					for (int i = 0; i < src_row.Length; ++i) {
						ColorBgra col = src_row[i];
						int intensity = col.GetIntensityByte ();
						int shiftIndex = intensity * 256;

						col.R = rgb_table![shiftIndex + col.R];
						col.G = rgb_table[shiftIndex + col.G];
						col.B = rgb_table[shiftIndex + col.B];

						dst_row[i] = col;
					}
				}
			}
		}
	}

	private void Calculate ()
	{
		if (Data.Contrast < 0) {
			multiply = Data.Contrast + 100;
			divide = 100;
		} else if (Data.Contrast > 0) {
			multiply = 100;
			divide = 100 - Data.Contrast;
		} else {
			multiply = 1;
			divide = 1;
		}

		rgb_table ??= new byte[65536];

		if (divide == 0) {
			for (int intensity = 0; intensity < 256; intensity++) {
				if (intensity + Data.Brightness < 128)
					rgb_table[intensity] = 0;
				else
					rgb_table[intensity] = 255;
			}
		} else if (divide == 100) {
			for (int intensity = 0; intensity < 256; intensity++) {
				int shift = (intensity - 127) * multiply / divide + 127 - intensity + Data.Brightness;

				for (int col = 0; col < 256; ++col) {
					int index = (intensity * 256) + col;
					rgb_table[index] = Utility.ClampToByte (col + shift);
				}
			}
		} else {
			for (int intensity = 0; intensity < 256; ++intensity) {
				int shift = (intensity - 127 + Data.Brightness) * multiply / divide + 127 - intensity;

				for (int col = 0; col < 256; ++col) {
					int index = (intensity * 256) + col;
					rgb_table[index] = Utility.ClampToByte (col + shift);
				}
			}
		}

		table_calculated = true;
	}

	public sealed class BrightnessContrastData : EffectData
	{
		private int brightness = 0;
		private int contrast = 0;

		[Caption ("Brightness")]
		public int Brightness {
			get => brightness;
			set {
				if (value != brightness) {
					brightness = value;
					FirePropertyChanged (nameof (Brightness));
				}
			}
		}

		[Caption ("Contrast")]
		public int Contrast {
			get => contrast;
			set {
				if (value != contrast) {
					contrast = value;
					FirePropertyChanged (nameof (Contrast));
				}
			}
		}

		[Skip]
		public override bool IsDefault => Brightness == 0 && Contrast == 0;
	}
}
