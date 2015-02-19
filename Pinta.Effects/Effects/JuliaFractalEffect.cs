/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Pinta.Gui.Widgets;
using Cairo;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class JuliaFractalEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Render.JuliaFractal.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Julia Fractal"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Render"); }
		}

		public JuliaFractalData Data { get { return EffectData as JuliaFractalData; } }

		public JuliaFractalEffect ()
		{
			EffectData = new JuliaFractalData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		private static readonly double log2_10000 = Math.Log (10000);

		private static double Julia (double x, double y, double r, double i)
		{
			double c = 0;
			
			while (c < 256 && x * x + y * y < 10000) {
				double t = x;
				x = x * x - y * y + r;
				y = 2 * t * y + i;
				++c;
			}
			
			c -= 2 - 2 * log2_10000 / Math.Log (x * x + y * y);
			
			return c;
		}

		unsafe public override void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			const double jr = 0.3125;
			const double ji = 0.03;
			
			int w = dst.Width;
			int h = dst.Height;
			double invH = 1.0 / h;
			double invZoom = 1.0 / Data.Zoom;
			double invQuality = 1.0 / Data.Quality;
			double aspect = (double)h / (double)w;
			int count = Data.Quality * Data.Quality + 1;
			double invCount = 1.0 / (double)count;
			double angleTheta = (Data.Angle * Math.PI * 2) / 360.0;
			
			ColorBgra* dst_dataptr = (ColorBgra*)dst.DataPtr;
			int dst_width = dst.Width;
			
			foreach (Gdk.Rectangle rect in rois) {
				for (int y = rect.Top; y <= rect.GetBottom (); y++) {
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (dst_dataptr, dst_width, rect.Left, y);
					
					for (int x = rect.Left; x <= rect.GetRight (); x++) {
						int r = 0;
						int g = 0;
						int b = 0;
						int a = 0;
						
						for (double i = 0; i < count; i++) {
							double u = (2.0 * x - w + (i * invCount)) * invH;
							double v = (2.0 * y - h + ((i * invQuality) % 1)) * invH;
							
							double radius = Math.Sqrt ((u * u) + (v * v));
							double radiusP = radius;
							double theta = Math.Atan2 (v, u);
							double thetaP = theta + angleTheta;
							
							double uP = radiusP * Math.Cos (thetaP);
							double vP = radiusP * Math.Sin (thetaP);
							
							double jX = (uP - vP * aspect) * invZoom;
							double jY = (vP + uP * aspect) * invZoom;
							
							double j = Julia (jX, jY, jr, ji);
							
							double c = Data.Factor * j;
							
							b += Utility.ClampToByte (c - 768);
							g += Utility.ClampToByte (c - 512);
							r += Utility.ClampToByte (c - 256);
							a += Utility.ClampToByte (c - 0);
						}
						
						*dstPtr = ColorBgra.FromBgra (Utility.ClampToByte (b / count), Utility.ClampToByte (g / count), Utility.ClampToByte (r / count), Utility.ClampToByte (a / count));
						
						++dstPtr;
					}
				}
			}
		}
		#endregion

		public class JuliaFractalData : EffectData
		{
			[Caption ("Factor"), MinimumValue(1), MaximumValue(10)]
			public int Factor = 4;

			[Caption ("Quality"), MinimumValue(1), MaximumValue(5)]
			public int Quality = 2;

			[Caption ("Zoom"), MinimumValue(0), MaximumValue(50)]
			public int Zoom = 1;

			[Caption ("Angle")]
			public double Angle = 0;
		}
	}
}
