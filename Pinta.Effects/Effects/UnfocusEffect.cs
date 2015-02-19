/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Hanh Pham <hanh.pham@gmx.com>                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Gui.Widgets;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class UnfocusEffect : LocalHistogramEffect
	{
		private int radius;

		public override string Icon {
			get { return "Menu.Effects.Blurs.Unfocus.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Unfocus"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Blurs"); }
		}

		public UnfocusData Data { get { return EffectData as UnfocusData; } }

		public UnfocusEffect ()
		{
			EffectData = new UnfocusData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void Render (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			this.radius = Data.Radius;

			foreach (Gdk.Rectangle rect in rois)
				RenderRectWithAlpha (this.radius, src, dest, rect);
		}

		public unsafe override ColorBgra ApplyWithAlpha (ColorBgra src, int area, int sum, int* hb, int* hg, int* hr)
		{
			//each slot of the histgram can contain up to area * 255. This will overflow an int when area > 32k
			if (area < 32768) {
				int b = 0;
				int g = 0;
				int r = 0;

				for (int i = 1; i < 256; ++i) {
					b += i * hb[i];
					g += i * hg[i];
					r += i * hr[i];
				}

				int alpha = sum / area;
				int div = area * 255;

				return ColorBgra.FromBgraClamped (b / div, g / div, r / div, alpha);
			} else {	//use a long if an int will overflow.
				long b = 0;
				long g = 0;
				long r = 0;

				for (long i = 1; i < 256; ++i) {
					b += i * hb[i];
					g += i * hg[i];
					r += i * hr[i];
				}

				int alpha = sum / area;
				int div = area * 255;

				return ColorBgra.FromBgraClamped (b / div, g / div, r / div, alpha);
			}
		}
		#endregion

		public class UnfocusData : EffectData
		{
			[Caption ("Radius"), MinimumValue (1), MaximumValue (200)]
			public int Radius = 4;

			[Skip]
			public override bool IsDefault { get { return Radius == 0; } }
		}
	}
}
