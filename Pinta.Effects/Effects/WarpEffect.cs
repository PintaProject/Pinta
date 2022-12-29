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
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public enum WarpEdgeBehavior
	{
		[Caption ("Clamp")]
		Clamp,
		[Caption ("Wrap")]
		Wrap,
		[Caption ("Reflect")]
		Reflect,
		[Caption ("Primary")]
		Primary,
		[Caption ("Secondary")]
		Secondary,
		[Caption ("Transparent")]
		Transparent,
		[Caption ("Original")]
		Original
	}

	public abstract class WarpEffect : BaseEffect
	{

		public WarpData Data {
			get { return (WarpData) EffectData!; } // NRT - Set in constructor
		}

		public WarpEffect ()
		{
			EffectData = new WarpData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		private double defaultRadius = 0;
		private double defaultRadius2 = 0;

		protected double DefaultRadius { get { return this.defaultRadius; } }
		protected double DefaultRadius2 { get { return this.defaultRadius2; } }

		#region Algorithm Code Ported From PDN
		public override void Render (ImageSurface src, ImageSurface dst, Core.Rectangle[] rois)
		{
#if false // TODO-GTK4
			var selection = PintaCore.LivePreview.RenderBounds;
			this.defaultRadius = Math.Min (selection.Width, selection.Height) * 0.5;
			this.defaultRadius2 = this.defaultRadius * this.defaultRadius;

			var x_center_offset = selection.Left + (selection.Width * (1.0 + Data.CenterOffset.X) * 0.5);
			var y_center_offset = selection.Top + (selection.Height * (1.0 + Data.CenterOffset.Y) * 0.5);

			ColorBgra colPrimary = PintaCore.Palette.PrimaryColor.ToColorBgra ();
			ColorBgra colSecondary = PintaCore.Palette.SecondaryColor.ToColorBgra ();
			ColorBgra colTransparent = ColorBgra.Transparent;

			int aaSampleCount = Data.Quality * Data.Quality;
			Span<PointD> aaPoints = stackalloc PointD[aaSampleCount];
			Utility.GetRgssOffsets (aaPoints, aaSampleCount, Data.Quality);
			Span<ColorBgra> samples = stackalloc ColorBgra[aaSampleCount];

			var dst_data = dst.GetData ();
			int dst_width = dst.Width;
			var src_data = src.GetReadOnlyData ();
			int src_width = src.Width;

			TransformData td;

			foreach (Core.Rectangle rect in rois) {

				for (int y = rect.Top; y <= rect.Bottom; y++) {
					var dst_row = dst_data.Slice (y * dst_width, dst_width);

					double relativeY = y - y_center_offset;

					for (int x = rect.Left; x <= rect.Right; x++) {
						double relativeX = x - x_center_offset;

						int sampleCount = 0;

						for (int p = 0; p < aaSampleCount; ++p) {
							td.X = relativeX + aaPoints[p].X;
							td.Y = relativeY - aaPoints[p].Y;

							InverseTransform (ref td);

							float sampleX = (float) (td.X + x_center_offset);
							float sampleY = (float) (td.Y + y_center_offset);

							ColorBgra sample = colPrimary;

							if (IsOnSurface (src, sampleX, sampleY)) {
								sample = src.GetBilinearSample (sampleX, sampleY);
							} else {
								switch (Data.EdgeBehavior) {
									case WarpEdgeBehavior.Clamp:
										sample = src.GetBilinearSampleClamped (sampleX, sampleY);
										break;

									case WarpEdgeBehavior.Wrap:
										sample = src.GetBilinearSampleWrapped (sampleX, sampleY);
										break;

									case WarpEdgeBehavior.Reflect:
										sample = src.GetBilinearSampleClamped (ReflectCoord (sampleX, src.Width), ReflectCoord (sampleY, src.Height));

										break;

									case WarpEdgeBehavior.Primary:
										sample = colPrimary;
										break;

									case WarpEdgeBehavior.Secondary:
										sample = colSecondary;
										break;

									case WarpEdgeBehavior.Transparent:
										sample = colTransparent;
										break;

									case WarpEdgeBehavior.Original:
										sample = src_data[y * src_width + x];
										break;
									default:

										break;
								}
							}

							samples[sampleCount] = sample;
							++sampleCount;
						}

						dst_row[x] = ColorBgra.Blend (samples.Slice (0, sampleCount));
					}
				}
			}
#endif
		}

		protected abstract void InverseTransform (ref TransformData data);

		protected struct TransformData
		{
			public double X;
			public double Y;
		}

		private static bool IsOnSurface (ImageSurface src, float u, float v)
		{
			return (u >= 0 && u <= (src.Width - 1) && v >= 0 && v <= (src.Height - 1));
		}

		private static float ReflectCoord (float value, int max)
		{
			bool reflection = false;

			while (value < 0) {
				value += max;
				reflection = !reflection;
			}

			while (value > max) {
				value -= max;
				reflection = !reflection;
			}

			if (reflection) {
				value = max - value;
			}

			return value;
		}

		#endregion
		public class WarpData : EffectData
		{
			[Caption ("Quality"), MinimumValue (1), MaximumValue (5)]
			public int Quality = 2;

			[Caption ("Center Offset")]
			public Core.PointD CenterOffset;

			public WarpEdgeBehavior EdgeBehavior = WarpEdgeBehavior.Wrap;
		}
	}
}
