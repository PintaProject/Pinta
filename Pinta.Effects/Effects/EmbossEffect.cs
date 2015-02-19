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
	public class EmbossEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Stylize.Emboss.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Emboss"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Stylize"); }
		}

		public EmbossData Data {
			get { return EffectData as EmbossData; }
		}

		public EmbossEffect () {
			EffectData = new EmbossData ();
		}

		public override bool LaunchConfiguration () {
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		unsafe public override void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois) {
			double[,] weights = Weights;

			var srcWidth = src.Width;
			var srcHeight = src.Height;

			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;
			
			foreach (var rect in rois) {
				// loop through each line of target rectangle
				for (int y = rect.Top; y <= rect.GetBottom (); ++y) {
					int fyStart = 0;
					int fyEnd = 3;

					if (y == 0)
						fyStart = 1;

					if (y == srcHeight - 1)
						fyEnd = 2;
					
					// loop through each point in the line 
					ColorBgra* dstPtr = dst.GetPointAddress (rect.Left, y);

					for (int x = rect.Left; x <= rect.GetRight (); ++x) {
						int fxStart = 0;
						int fxEnd = 3;

						if (x == 0)
							fxStart = 1;

						if (x == srcWidth - 1)
							fxEnd = 2;

						// loop through each weight
						double sum = 0.0;

						for (int fy = fyStart; fy < fyEnd; ++fy) {
							for (int fx = fxStart; fx < fxEnd; ++fx) {
								double weight = weights[fy, fx];
								ColorBgra c = src.GetPointUnchecked (src_data_ptr, srcWidth, x - 1 + fx, y - 1 + fy);
								double intensity = (double)c.GetIntensityByte ();
								sum += weight * intensity;
							}
						}

						int iSum = (int)sum;
						iSum += 128;

						if (iSum > 255)
							iSum = 255;

						if (iSum < 0)
							iSum = 0;

						*dstPtr = ColorBgra.FromBgra ((byte)iSum, (byte)iSum, (byte)iSum, 255);

						++dstPtr;
					}
				}
			}
		}


		public double[,] Weights {
			get {
				// adjust and convert angle to radians
				double r = (double)Data.Angle * 2.0 * Math.PI / 360.0;

				// angle delta for each weight
				double dr = Math.PI / 4.0;

				// for r = 0 this builds an emboss filter pointing straight left
				double[,] weights = new double[3, 3];

				weights[0, 0] = Math.Cos (r + dr);
				weights[0, 1] = Math.Cos (r + 2.0 * dr);
				weights[0, 2] = Math.Cos (r + 3.0 * dr);

				weights[1, 0] = Math.Cos (r);
				weights[1, 1] = 0;
				weights[1, 2] = Math.Cos (r + 4.0 * dr);

				weights[2, 0] = Math.Cos (r - dr);
				weights[2, 1] = Math.Cos (r - 2.0 * dr);
				weights[2, 2] = Math.Cos (r - 3.0 * dr);

				return weights;
			}
		}
		#endregion


		public class EmbossData : EffectData
		{
			[Caption ("Angle")]
			public double Angle = 0;
		}
	}
}
