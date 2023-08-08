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
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public class TileEffect : BaseEffect
	{
		public override string Icon => Pinta.Resources.Icons.EffectsDistortTile;

		public override string Name => Translations.GetString ("Tile Reflection");

		public override bool IsConfigurable => true;

		public override string EffectMenuCategory => Translations.GetString ("Distort");

		public TileData Data => (TileData) EffectData!;

		public TileEffect ()
		{
			EffectData = new TileData ();
		}

		public override void LaunchConfiguration ()
		{
			EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		public override void Render (ImageSurface src, ImageSurface dst, Core.RectangleI[] rois)
		{
			int width = dst.Width;
			int height = dst.Height;
			float hw = width / 2f;
			float hh = height / 2f;
			float sin = (float) Math.Sin (Data.Rotation * Math.PI / 180.0);
			float cos = (float) Math.Cos (Data.Rotation * Math.PI / 180.0);
			float scale = (float) Math.PI / Data.TileSize;
			float intensity = Data.Intensity;

			intensity = intensity * intensity / 10 * Math.Sign (intensity);

			int aaLevel = 4;
			int aaSamples = aaLevel * aaLevel + 1;
			Span<PointD> aaPoints = stackalloc PointD[aaSamples];

			for (int i = 0; i < aaSamples; ++i) {
				double x = (i * aaLevel) / (double) aaSamples;
				double y = i / (double) aaSamples;

				x -= (int) x;

				// RGSS + rotation to maximize AA quality
				aaPoints[i] = new PointD ((double) (cos * x + sin * y), (double) (cos * y - sin * x));
			}

			int src_width = src.Width;
			ReadOnlySpan<ColorBgra> src_data = src.GetReadOnlyPixelData ();
			Span<ColorBgra> dst_data = dst.GetPixelData ();

			foreach (var rect in rois) {
				for (int y = rect.Top; y <= rect.Bottom; y++) {
					float j = y - hh;
					var dst_row = dst_data.Slice (y * width, width);

					for (int x = rect.Left; x <= rect.Right; x++) {
						int b = 0;
						int g = 0;
						int r = 0;
						int a = 0;
						float i = x - hw;

						for (int p = 0; p < aaSamples; ++p) {
							PointD pt = aaPoints[p];

							float u = i + (float) pt.X;
							float v = j - (float) pt.Y;

							float s = cos * u + sin * v;
							float t = -sin * u + cos * v;

							s += intensity * (float) Math.Tan (s * scale);
							t += intensity * (float) Math.Tan (t * scale);
							u = cos * s - sin * t;
							v = sin * s + cos * t;

							int xSample = (int) (hw + u);
							int ySample = (int) (hh + v);

							xSample = (xSample + width) % width;
							// This makes it a little faster
							if (xSample < 0) {
								xSample = (xSample + width) % width;
							}

							ySample = (ySample + height) % height;
							// This makes it a little faster
							if (ySample < 0) {
								ySample = (ySample + height) % height;
							}

							ref readonly ColorBgra sample = ref src.GetColorBgra (src_data, src_width, xSample, ySample);

							b += sample.B;
							g += sample.G;
							r += sample.R;
							a += sample.A;
						}

						dst_row[x] = ColorBgra.FromBgra ((byte) (b / aaSamples), (byte) (g / aaSamples),
							(byte) (r / aaSamples), (byte) (a / aaSamples));
					}
				}
			}
		}
		#endregion


		public class TileData : EffectData
		{
			[Caption ("Rotation"), MinimumValue (-45), MaximumValue (45)]
			public double Rotation = 30;
			[Caption ("Tile Size"), MinimumValue (2), MaximumValue (200)]
			public int TileSize = 40;
			[Caption ("Intensity"), MinimumValue (-20), MaximumValue (20)]
			public int Intensity = 8;
		}
	}
}
