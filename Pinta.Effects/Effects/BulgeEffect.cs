/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class BulgeEffect : BaseEffect
{
	public sealed override bool IsTileable => true;

	public override string Icon => Pinta.Resources.Icons.EffectsDistortBulge;

	public override string Name => Translations.GetString ("Bulge");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Distort");

	public BulgeData Data => (BulgeData) EffectData!;

	private readonly IChromeService chrome;

	public BulgeEffect (IServiceManager services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new BulgeData ();
	}

	public override void LaunchConfiguration ()
	{
		chrome.LaunchSimpleEffectDialog (this);
	}

	#region Algorithm Code Ported From PDN

	private sealed record BulgeSettings (
		float hw,
		float hh,
		float maxrad,
		float amt,
		int src_width,
		int src_height);
	private BulgeSettings CreateSettings (ImageSurface src, ImageSurface dst)
	{
		float bulge = Data.Amount;

		float hw = dst.Width / 2f;
		float hh = dst.Height / 2f;
		float maxrad = Math.Min (hw, hh);
		float amt = bulge / 100f;

		hh += (float) Data.Offset.Y * hh;
		hw += (float) Data.Offset.X * hw;

		int src_width = src.Width;
		int src_height = src.Height;

		return new BulgeSettings (
			hw: hw,
			hh: hh,
			maxrad: maxrad,
			amt: amt,
			src_width: src_width,
			src_height: src_height
		);
	}

	public override void Render (ImageSurface src, ImageSurface dst, ReadOnlySpan<RectangleI> rois)
	{
		BulgeSettings settings = CreateSettings (src, dst);

		ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
		Span<ColorBgra> dst_data = dst.GetPixelData ();

		foreach (RectangleI rect in rois) {

			for (int y = rect.Top; y <= rect.Bottom; y++) {
				var src_row = src_data.Slice (y * settings.src_width, settings.src_width);
				var dst_row = dst_data.Slice (y * settings.src_width, settings.src_width);


				for (int x = rect.Left; x <= rect.Right; x++) {
					float v = y - settings.hh;
					float u = x - settings.hw;
					float r = (float) Math.Sqrt (u * u + v * v);
					float rscale1 = (1f - (r / settings.maxrad));

					if (rscale1 > 0) {
						float rscale2 = 1 - settings.amt * rscale1 * rscale1;

						float xp = u * rscale2;
						float yp = v * rscale2;

						dst_row[x] = src.GetBilinearSampleClamped (src_data, settings.src_width, settings.src_height, xp + settings.hw, yp + settings.hh);
					} else {
						dst_row[x] = src_row[x];
					}
				}
			}
		}
	}
	#endregion

	public sealed class BulgeData : EffectData
	{
		[Caption ("Amount"), MinimumValue (-200), MaximumValue (100)]
		public int Amount { get; set; } = 45;

		[Caption ("Offset")]
		public PointD Offset { get; set; } = new (0.0, 0.0);

		[Skip]
		public override bool IsDefault => Amount == 0;
	}
}
