/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public class InkSketchEffect : BaseEffect
	{
		private static readonly int[][] conv;
		private const int size = 5;
		private const int radius = (size - 1) / 2;

		private GlowEffect glowEffect;
		private UnaryPixelOps.Desaturate desaturateOp;
		private UserBlendOps.DarkenBlendOp darkenOp;

		public override string Icon {
			get { return "Menu.Effects.Artistic.InkSketch.png"; }
		}

		public override string Name {
			get { return Translations.GetString ("Ink Sketch"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Translations.GetString ("Artistic"); }
		}

		public InkSketchData Data { get { return (InkSketchData) EffectData!; } } // NRT - Set in constructor

		public InkSketchEffect ()
		{
			EffectData = new InkSketchData ();

			glowEffect = new GlowEffect ();
			desaturateOp = new UnaryPixelOps.Desaturate ();
			darkenOp = new UserBlendOps.DarkenBlendOp ();
		}

		static InkSketchEffect ()
		{
			conv = new int[5][];

			for (int i = 0; i < conv.Length; ++i)
				conv[i] = new int[5];

			conv[0] = new int[] { -1, -1, -1, -1, -1 };
			conv[1] = new int[] { -1, -1, -1, -1, -1 };
			conv[2] = new int[] { -1, -1, 30, -1, -1 };
			conv[3] = new int[] { -1, -1, -1, -1, -1 };
			conv[4] = new int[] { -1, -1, -5, -1, -1 };
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public override void Render (ImageSurface src, ImageSurface dest, Core.RectangleI[] rois)
		{
			// Glow backgound 
			glowEffect.Data.Radius = 6;
			glowEffect.Data.Brightness = -(Data.Coloring - 50) * 2;
			glowEffect.Data.Contrast = -(Data.Coloring - 50) * 2;

			this.glowEffect.Render (src, dest, rois);

			var src_data = src.GetReadOnlyData ();
			int width = src.Width;
			var dst_data = dest.GetData ();

			// Create black outlines by finding the edges of objects 
			foreach (Core.RectangleI roi in rois) {
				for (int y = roi.Top; y <= roi.Bottom; ++y) {
					int top = y - radius;
					int bottom = y + radius + 1;

					if (top < 0) {
						top = 0;
					}

					if (bottom > dest.Height) {
						bottom = dest.Height;
					}

					var dst_row = dst_data.Slice (y * width, width);

					for (int x = roi.Left; x <= roi.Right; ++x) {
						int left = x - radius;
						int right = x + radius + 1;

						if (left < 0) {
							left = 0;
						}

						if (right > dest.Width) {
							right = dest.Width;
						}

						int r = 0;
						int g = 0;
						int b = 0;

						for (int v = top; v < bottom; v++) {
							var src_row = src_data.Slice (v * width, width);
							int j = v - y + radius;

							for (int u = left; u < right; u++) {
								int i1 = u - x + radius;
								int w = conv[j][i1];

								ref readonly ColorBgra src_pixel = ref src_row[u];

								r += src_pixel.R * w;
								g += src_pixel.G * w;
								b += src_pixel.B * w;
							}
						}

						ColorBgra topLayer = ColorBgra.FromBgr (
						    Utility.ClampToByte (b),
						    Utility.ClampToByte (g),
						    Utility.ClampToByte (r));

						// Desaturate 
						topLayer = this.desaturateOp.Apply (topLayer);

						// Adjust Brightness and Contrast 
						if (topLayer.R > (Data.InkOutline * 255 / 100)) {
							topLayer = ColorBgra.FromBgra (255, 255, 255, topLayer.A);
						} else {
							topLayer = ColorBgra.FromBgra (0, 0, 0, topLayer.A);
						}

						// Change Blend Mode to Darken
						ref ColorBgra dst_pixel = ref dst_row[x];
						dst_pixel = this.darkenOp.Apply (topLayer, dst_pixel);
					}
				}
			}
		}
		#endregion

		public class InkSketchData : EffectData
		{
			[Caption ("Ink Outline"), MinimumValue (0), MaximumValue (99)]
			public int InkOutline = 50;

			[Caption ("Coloring"), MinimumValue (0), MaximumValue (100)]
			public int Coloring = 50;
		}
	}
}
