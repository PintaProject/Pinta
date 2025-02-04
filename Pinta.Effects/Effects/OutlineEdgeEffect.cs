/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Hanh Pham <hanh.pham@gmx.com>                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class OutlineEdgeEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsStylizeOutline;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Outline Edge");

	public override bool IsConfigurable => true;

	public override string EffectMenuCategory => Translations.GetString ("Stylize");

	public OutlineEdgeData Data => (OutlineEdgeData) EffectData!;  // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public OutlineEdgeEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new OutlineEdgeData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN
	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		int thickness = Data.Thickness;
		int intensity = Data.Intensity;

		foreach (var rect in rois)
			LocalHistogram.RenderRect (Apply, thickness, src, dest, rect);

		// === Methods ===

		ColorBgra Apply (ColorBgra src, int area, Span<int> hb, Span<int> hg, Span<int> hr, Span<int> ha)
		{
			int minCount1 = area * (100 - intensity) / 200;
			int minCount2 = area * (100 + intensity) / 200;

			int bCount = 0;
			int b1 = 0;

			while (b1 < 255 && hb[b1] == 0)
				++b1;

			while (b1 < 255 && bCount < minCount1) {
				bCount += hb[b1];
				++b1;
			}

			int b2 = b1;
			while (b2 < 255 && bCount < minCount2) {
				bCount += hb[b2];
				++b2;
			}

			int gCount = 0;
			int g1 = 0;
			while (g1 < 255 && hg[g1] == 0)
				++g1;

			while (g1 < 255 && gCount < minCount1) {
				gCount += hg[g1];
				++g1;
			}

			int g2 = g1;
			while (g2 < 255 && gCount < minCount2) {
				gCount += hg[g2];
				++g2;
			}

			int rCount = 0;
			int r1 = 0;
			while (r1 < 255 && hr[r1] == 0)
				++r1;

			while (r1 < 255 && rCount < minCount1) {
				rCount += hr[r1];
				++r1;
			}

			int r2 = r1;
			while (r2 < 255 && rCount < minCount2) {
				rCount += hr[r2];
				++r2;
			}

			int aCount = 0;
			int a1 = 0;
			while (a1 < 255 && ha[a1] == 0)
				++a1;

			while (a1 < 255 && aCount < minCount1) {
				aCount += ha[a1];
				++a1;
			}

			int a2 = a1;
			while (a2 < 255 && aCount < minCount2) {
				aCount += ha[a2];
				++a2;
			}

			return ColorBgra.FromBgra (
				b: (byte) (255 - (b2 - b1)),
				g: (byte) (255 - (g2 - g1)),
				r: (byte) (255 - (r2 - r1)),
				a: (byte) a2)
				.ToPremultipliedAlpha ();
		}
	}

	public sealed class OutlineEdgeData : EffectData
	{
		[Caption ("Thickness"), MinimumValue (1), MaximumValue (200)]
		public int Thickness { get; set; } = 3;

		[Caption ("Intensity"), MinimumValue (0), MaximumValue (100)]
		public int Intensity { get; set; } = 50;

		[Skip]
		public override bool IsDefault => Thickness == 0;
	}
}
