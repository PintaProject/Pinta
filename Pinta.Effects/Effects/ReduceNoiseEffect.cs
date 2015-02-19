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
	public class ReduceNoiseEffect : LocalHistogramEffect
	{
		private int radius;
		private double strength;

		public override string Icon {
			get { return "Menu.Effects.Noise.ReduceNoise.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Reduce Noise"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Noise"); }
		}

		public ReduceNoiseData Data { get { return EffectData as ReduceNoiseData; } }

		public ReduceNoiseEffect ()
		{
			EffectData = new ReduceNoiseData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public override unsafe ColorBgra Apply (ColorBgra color, int area, int* hb, int* hg, int* hr, int* ha)
		{
			ColorBgra normalized = GetPercentileOfColor (color, area, hb, hg, hr, ha);
			double lerp = strength * (1 - 0.75 * color.GetIntensity ());

			return ColorBgra.Lerp (color, normalized, lerp);
		}

		private static unsafe ColorBgra GetPercentileOfColor (ColorBgra color, int area, int* hb, int* hg, int* hr, int* ha)
		{
			int rc = 0;
			int gc = 0;
			int bc = 0;

			for (int i = 0; i < color.R; ++i)
				rc += hr[i];

			for (int i = 0; i < color.G; ++i)
				gc += hg[i];

			for (int i = 0; i < color.B; ++i)
				bc += hb[i];

			rc = (rc * 255) / area;
			gc = (gc * 255) / area;
			bc = (bc * 255) / area;

			return ColorBgra.FromBgr ((byte)bc, (byte)gc, (byte)rc);
		}

		public unsafe override void Render (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			this.radius = Data.Radius;
			this.strength = -0.2 * Data.Strength;

			foreach (Gdk.Rectangle rect in rois)
				RenderRect (this.radius, src, dest, rect);
		}
		#endregion

		public class ReduceNoiseData : EffectData
		{
			[Caption ("Radius"), MinimumValue (1), MaximumValue (200)]
			public int Radius = 6;

			[Caption ("Strength"), MinimumValue (0), IncrementValue (0.01), DigitsValue (2), MaximumValue (1)]
			public double Strength = 0.4;
		}
	}
}
