/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Gui.Widgets;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class TwistEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Distort.Twist.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Twist"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Distort"); }
		}

		public TwistData Data { get { return EffectData as TwistData; } }

		public TwistEffect ()
		{
			EffectData = new TwistData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public unsafe override void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			float twist = Data.Amount;

			float hw = dst.Width / 2.0f;
			float hh = dst.Height / 2.0f;
			float maxrad = Math.Min (hw, hh);

			twist = twist * twist * Math.Sign (twist);

			int aaLevel = Data.Antialias;
			int aaSamples = aaLevel * aaLevel + 1;
			PointD* aaPoints = stackalloc PointD[aaSamples];

			for (int i = 0; i < aaSamples; ++i) {
				PointD pt = new PointD (
				    ((i * aaLevel) / (float)aaSamples),
				    i / (float)aaSamples);

				pt.X -= (int)pt.X;
				aaPoints[i] = pt;
			}

			int src_width = src.Width;
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;

			foreach (var rect in rois) {
				for (int y = rect.Top; y <= rect.GetBottom (); y++) {
					float j = y - hh;
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);
					ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_data_ptr, src_width, rect.Left, y);

					for (int x = rect.Left; x <= rect.GetRight (); x++) {
						float i = x - hw;

						if (i * i + j * j > (maxrad + 1) * (maxrad + 1)) {
							*dstPtr = *srcPtr;
						} else {
							int b = 0;
							int g = 0;
							int r = 0;
							int a = 0;

							for (int p = 0; p < aaSamples; ++p) {
								float u = i + (float)aaPoints[p].X;
								float v = j + (float)aaPoints[p].Y;
								double rad = Math.Sqrt (u * u + v * v);
								double theta = Math.Atan2 (v, u);

								double t = 1 - rad / maxrad;

								t = (t < 0) ? 0 : (t * t * t);

								theta += (t * twist) / 100;

								ColorBgra sample = src.GetPointUnchecked (src_data_ptr, src_width, 
								    (int)(hw + (float)(rad * Math.Cos (theta))),
								    (int)(hh + (float)(rad * Math.Sin (theta))));

								b += sample.B;
								g += sample.G;
								r += sample.R;
								a += sample.A;
							}

							*dstPtr = ColorBgra.FromBgra (
							    (byte)(b / aaSamples),
							    (byte)(g / aaSamples),
							    (byte)(r / aaSamples),
							    (byte)(a / aaSamples));
						}

						++dstPtr;
						++srcPtr;
					}
				}
			}
		}
		#endregion

		public class TwistData : EffectData
		{
			[Caption ("Amount"), MinimumValue (-100), MaximumValue (100)]
			public int Amount = 45;
			[Caption ("Antialias"), MinimumValue (0), MaximumValue (5)]
			public int Antialias = 2;
		}
	}
}
