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
	public class ZoomBlurEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.ZoomBlur.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Zoom Blur"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Blurs"); }
		}

		public ZoomBlurData Data { get { return EffectData as ZoomBlurData; } }

		public ZoomBlurEffect ()
		{
			EffectData = new ZoomBlurData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			if (Data.Amount == 0) {
				// Copy src to dest
				return;
			}

			int src_width = src.Width;
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;
			int dst_width = dst.Width;
			ColorBgra* dst_data_ptr = (ColorBgra*)dst.DataPtr;
			Gdk.Rectangle src_bounds = src.GetBounds ();
			
			long w = dst.Width;
			long h = dst.Height;
			long fox = (long)(dst.Width * Data.Offset.X * 32768.0);
			long foy = (long)(dst.Height * Data.Offset.Y * 32768.0);
			long fcx = fox + (w << 15);
			long fcy = foy + (h << 15);
			long fz = Data.Amount;

			const int n = 64;
			
			foreach (Gdk.Rectangle rect in rois) {
				for (int y = rect.Top; y <= rect.GetBottom (); ++y) {
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (dst_data_ptr, dst_width, rect.Left, y);
					ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_data_ptr, src_width, rect.Left, y);

					for (int x = rect.Left; x <= rect.GetRight (); ++x) {
						long fx = (x << 16) - fcx;
						long fy = (y << 16) - fcy;

						int sr = 0;
						int sg = 0;
						int sb = 0;
						int sa = 0;
						int sc = 0;

						sr += srcPtr->R * srcPtr->A;
						sg += srcPtr->G * srcPtr->A;
						sb += srcPtr->B * srcPtr->A;
						sa += srcPtr->A;
						++sc;

						for (int i = 0; i < n; ++i) {
							fx -= ((fx >> 4) * fz) >> 10;
							fy -= ((fy >> 4) * fz) >> 10;

							int u = (int)(fx + fcx + 32768 >> 16);
							int v = (int)(fy + fcy + 32768 >> 16);

							if (src_bounds.Contains (u, v)) {
								ColorBgra* srcPtr2 = src.GetPointAddressUnchecked (src_data_ptr, src_width, u, v);

								sr += srcPtr2->R * srcPtr2->A;
								sg += srcPtr2->G * srcPtr2->A;
								sb += srcPtr2->B * srcPtr2->A;
								sa += srcPtr2->A;
								++sc;
							}
						}

						if (sa != 0) {
							*dstPtr = ColorBgra.FromBgra (
							    Utility.ClampToByte (sb / sa),
							    Utility.ClampToByte (sg / sa),
							    Utility.ClampToByte (sr / sa),
							    Utility.ClampToByte (sa / sc));
						} else {
							dstPtr->Bgra = 0;
						}

						++srcPtr;
						++dstPtr;
					}
				}
			}
		}
		#endregion

		public class ZoomBlurData : EffectData
		{
			[Caption ("Amount"), MinimumValue (0), MaximumValue (100)]
			public int Amount = 10;

			[Caption ("Offset")]
			public Gdk.Point Offset = new Gdk.Point (0, 0);

			[Skip]
			public override bool IsDefault { get { return Amount == 0; } }
		}
	}
}
