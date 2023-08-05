/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;
using System.Threading.Tasks;
using Cairo;

namespace Pinta.Core
{
	public abstract class GradientRenderer
	{
		private readonly BinaryPixelOp normal_blend_op;
		private ColorBgra start_color;
		private ColorBgra end_color;
		private PointD start_point;
		private PointD end_point;
		private bool alpha_blending;
		private bool alpha_only;

		private bool lerp_cache_is_valid = false;
		private readonly byte[] lerp_alphas;
		private readonly ColorBgra[] lerp_colors;

		public ColorBgra StartColor {
			get { return this.start_color; }

			set {
				if (this.start_color != value) {
					this.start_color = value;
					this.lerp_cache_is_valid = false;
				}
			}
		}

		public ColorBgra EndColor {
			get { return this.end_color; }

			set {
				if (this.end_color != value) {
					this.end_color = value;
					this.lerp_cache_is_valid = false;
				}
			}
		}

		public PointD StartPoint {
			get { return this.start_point; }

			set { this.start_point = value; }
		}

		public PointD EndPoint {
			get { return this.end_point; }

			set { this.end_point = value; }
		}

		public bool AlphaBlending {
			get { return this.alpha_blending; }

			set { this.alpha_blending = value; }
		}

		public bool AlphaOnly {
			get { return this.alpha_only; }

			set { this.alpha_only = value; }
		}

		public virtual void BeforeRender ()
		{
			if (!this.lerp_cache_is_valid) {
				byte startAlpha;
				byte endAlpha;

				if (this.alpha_only) {
					ComputeAlphaOnlyValuesFromColors (this.start_color, this.end_color, out startAlpha, out endAlpha);
				} else {
					startAlpha = this.start_color.A;
					endAlpha = this.end_color.A;
				}

				for (int i = 0; i < 256; ++i) {
					byte a = (byte) i;
					this.lerp_colors[a] = ColorBgra.Blend (this.start_color, this.end_color, a);
					this.lerp_alphas[a] = (byte) (startAlpha + ((endAlpha - startAlpha) * a) / 255);
				}

				this.lerp_cache_is_valid = true;
			}
		}

		public abstract byte ComputeByteLerp (int x, int y);

		public virtual void AfterRender ()
		{
		}

		private static void ComputeAlphaOnlyValuesFromColors (ColorBgra startColor, ColorBgra endColor, out byte startAlpha, out byte endAlpha)
		{
			startAlpha = startColor.A;
			endAlpha = (byte) (255 - endColor.A);
		}

		public void Render (ImageSurface surface, RectangleI[] rois)
		{
			byte startAlpha;
			byte endAlpha;

			if (this.alpha_only) {
				ComputeAlphaOnlyValuesFromColors (this.start_color, this.end_color, out startAlpha, out endAlpha);
			} else {
				startAlpha = this.start_color.A;
				endAlpha = this.end_color.A;
			}

			surface.Flush ();

			Span<ColorBgra> src_data = surface.GetPixelData ();
			int src_width = surface.Width;

			for (int ri = 0; ri < rois.Length; ++ri) {
				RectangleI rect = rois[ri];

				if (this.start_point.X == this.end_point.X && this.start_point.Y == this.end_point.Y) {
					// Start and End point are the same ... fill with solid color.
					for (int y = rect.Top; y <= rect.Bottom; ++y) {
						var row = src_data.Slice (y * src_width, src_width);

						for (int x = rect.Left; x <= rect.Right; ++x) {
							ref ColorBgra pixel = ref row[x];
							ColorBgra result;

							if (this.alpha_only && this.alpha_blending) {
								byte resultAlpha = (byte) Utility.FastDivideShortByByte ((ushort) (pixel.A * endAlpha), 255);
								result = pixel;
								result.A = resultAlpha;
							} else if (this.alpha_only && !this.alpha_blending) {
								result = pixel;
								result.A = endAlpha;
							} else if (!this.alpha_only && this.alpha_blending) {
								result = this.normal_blend_op.Apply (pixel, this.end_color);
								//if (!this.alphaOnly && !this.alphaBlending)
							} else {
								result = this.end_color;
							}

							pixel = result;
						}
					}
				} else {
					var mainrect = rect;
					Parallel.ForEach (Enumerable.Range (rect.Top, rect.Height),
						(y) => ProcessGradientLine (startAlpha, endAlpha, y, mainrect, surface.GetPixelData (), src_width));
				}
			}

			surface.MarkDirty ();
			AfterRender ();
		}

		private bool ProcessGradientLine (byte startAlpha, byte endAlpha, int y, RectangleI rect, Span<ColorBgra> surface_data, int src_width)
		{
			var row = surface_data.Slice (y * src_width, src_width);
			var right = rect.Right;

			// Note that Cairo uses premultiplied alpha.
			if (alpha_only && alpha_blending) {
				for (var x = rect.Left; x <= right; ++x) {
					var lerpByte = ComputeByteLerp (x, y);
					var lerpAlpha = lerp_alphas[lerpByte];
					ref ColorBgra pixel = ref row[x];

					pixel.B = Utility.FastScaleByteByByte (pixel.B, lerpAlpha);
					pixel.G = Utility.FastScaleByteByByte (pixel.G, lerpAlpha);
					pixel.R = Utility.FastScaleByteByByte (pixel.R, lerpAlpha);
					pixel.A = Utility.FastScaleByteByByte (pixel.A, lerpAlpha);
				}
			} else if (alpha_only && !alpha_blending) {
				for (var x = rect.Left; x <= right; ++x) {
					var lerpByte = ComputeByteLerp (x, y);
					var lerpAlpha = lerp_alphas[lerpByte];
					ref ColorBgra pixel = ref row[x];

					var color = pixel.ToStraightAlpha ();
					color.A = lerpAlpha;
					pixel = color.ToPremultipliedAlpha ();
				}
			} else if (!alpha_only && (alpha_blending && (startAlpha != 255 || endAlpha != 255))) {
				// If we're doing all color channels, and we're doing alpha blending, and if alpha blending is necessary
				for (var x = rect.Left; x <= right; ++x) {
					var lerpByte = ComputeByteLerp (x, y);
					var lerpColor = lerp_colors[lerpByte];
					ref ColorBgra pixel = ref row[x];
					pixel = normal_blend_op.Apply (pixel, lerpColor);
				}
				//if (!this.alphaOnly && !this.alphaBlending) // or sC.A == 255 && eC.A == 255
			} else {
				for (var x = rect.Left; x <= right; ++x) {
					var lerpByte = ComputeByteLerp (x, y);
					var lerpColor = lerp_colors[lerpByte];
					row[x] = lerpColor;
				}
			}
			return true;
		}

		protected internal GradientRenderer (bool alphaOnly, BinaryPixelOp normalBlendOp)
		{
			this.normal_blend_op = normalBlendOp;
			this.alpha_only = alphaOnly;
			this.lerp_alphas = new byte[256];
			this.lerp_colors = new ColorBgra[256];
		}
	}
}
