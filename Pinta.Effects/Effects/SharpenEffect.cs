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
using Pinta.Gui.Widgets;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class SharpenEffect : LocalHistogramEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Photo.Sharpen.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Sharpen"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Photo"); }
		}

		public SharpenData Data { get { return EffectData as SharpenData; } }
		
		public SharpenEffect ()
		{
			EffectData = new SharpenData ();
		}
		
		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}
		
		public unsafe override void Render (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			foreach (Gdk.Rectangle rect in rois)
				RenderRect (Data.Amount, src, dest, rect);
		}
		
		public unsafe override ColorBgra Apply (ColorBgra src, int area, int* hb, int* hg, int* hr, int* ha)
		{
			ColorBgra median = GetPercentile(50, area, hb, hg, hr, ha);
			return ColorBgra.Lerp(src, median, -0.5f);
		}
	}
	
	public class SharpenData : EffectData
	{
		[Caption ("Amount"), MinimumValue (1), MaximumValue (20)]
		public int Amount = 2;
	}
}

