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
using Pinta.Gui.Widgets;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class BulgeEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Distort.Bulge.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Bulge"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Distort"); }
		}

		public BulgeData Data {
			get { return EffectData as BulgeData; }
		}

		public BulgeEffect ()
		{
			EffectData = new BulgeData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		unsafe public override void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			float bulge = (float)Data.Amount;
			
			float hw = dst.Width / 2f;
			float hh = dst.Height / 2f;
			float maxrad = Math.Min (hw, hh);
			float amt = bulge / 100f;
			
			hh = hh + (float)Data.Offset.Y * hh;
			hw = hw + (float)Data.Offset.X * hw;

			int src_width = src.Width;
			int src_height = src.Height;
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;
			
			foreach (Gdk.Rectangle rect in rois) {

				for (int y = rect.Top; y <= rect.GetBottom (); y++) {
					
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);
					ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_data_ptr, src_width, rect.Left, y);
					float v = y - hh;
					
					for (int x = rect.Left; x <= rect.GetRight (); x++) {
						float u = x - hw;
						float r = (float)Math.Sqrt (u * u + v * v);
						float rscale1 = (1f - (r / maxrad));
						
						if (rscale1 > 0) {
							float rscale2 = 1 - amt * rscale1 * rscale1;
							
							float xp = u * rscale2;
							float yp = v * rscale2;
							
							*dstPtr = src.GetBilinearSampleClamped (src_data_ptr, src_width, src_height, xp + hw, yp + hh);
						} else {
							*dstPtr = *srcPtr;
						}
						
						++dstPtr;
						++srcPtr;
					}
				}
			}
		}
		#endregion

		public class BulgeData : EffectData
		{
			[Caption ("Amount"), MinimumValue(-200), MaximumValue(100)]
			public int Amount = 45;

			[Caption ("Offset")]
			public Cairo.PointD Offset = new Cairo.PointD (0.0, 0.0);

			[Skip]
			public override bool IsDefault {
				get { return Amount == 0; }
			}
		}
	}
}
