/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cairo;

namespace Pinta.Core
{
	public abstract class GradientRenderer
	{
		private BinaryPixelOp normalBlendOp;
		private ColorBgra startColor;
		private ColorBgra endColor;
		private PointD startPoint;
		private PointD endPoint;
		private bool alphaBlending;
		private bool alphaOnly;

		private bool lerpCacheIsValid = false;
		private byte[] lerpAlphas;
		private ColorBgra[] lerpColors;

		public ColorBgra StartColor {
			get { return this.startColor; }

			set {
				if (this.startColor != value) {
					this.startColor = value;
					this.lerpCacheIsValid = false;
				}
			}
		}

		public ColorBgra EndColor {
			get { return this.endColor; }

			set {
				if (this.endColor != value) {
					this.endColor = value;
					this.lerpCacheIsValid = false;
				}
			}
		}

		public PointD StartPoint {
			get { return this.startPoint; }

			set { this.startPoint = value; }
		}

		public PointD EndPoint {
			get { return this.endPoint; }

			set { this.endPoint = value; }
		}

		public bool AlphaBlending {
			get { return this.alphaBlending; }

			set { this.alphaBlending = value; }
		}

		public bool AlphaOnly {
			get { return this.alphaOnly; }

			set { this.alphaOnly = value; }
		}

		public virtual void BeforeRender ()
		{
			if (!this.lerpCacheIsValid) {
				byte startAlpha;
				byte endAlpha;

				if (this.alphaOnly) {
					ComputeAlphaOnlyValuesFromColors (this.startColor, this.endColor, out startAlpha, out endAlpha);
				} else {
					startAlpha = this.startColor.A;
					endAlpha = this.endColor.A;
				}

				for (int i = 0; i < 256; ++i) {
					byte a = (byte) i;
					this.lerpColors[a] = ColorBgra.Blend (this.startColor, this.endColor, a);
					this.lerpAlphas[a] = (byte) (startAlpha + ((endAlpha - startAlpha) * a) / 255);
				}

				this.lerpCacheIsValid = true;
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

			if (this.alphaOnly) {
				ComputeAlphaOnlyValuesFromColors (this.startColor, this.endColor, out startAlpha, out endAlpha);
			} else {
				startAlpha = this.startColor.A;
				endAlpha = this.endColor.A;
			}

			surface.Flush ();

			Span<ColorBgra> src_data = surface.GetPixelData ();
			int src_width = surface.Width;

			for (int ri = 0; ri < rois.Length; ++ri) {
				RectangleI rect = rois[ri];

				if (this.startPoint.X == this.endPoint.X && this.startPoint.Y == this.endPoint.Y) {
					// Start and End point are the same ... fill with solid color.
					for (int y = rect.Top; y <= rect.Bottom; ++y) {
						var row = src_data.Slice (y * src_width, src_width);

						for (int x = rect.Left; x <= rect.Right; ++x) {
							ref ColorBgra pixel = ref row[x];
							ColorBgra result;

							if (this.alphaOnly && this.alphaBlending) {
								byte resultAlpha = (byte) Utility.FastDivideShortByByte ((ushort) (pixel.A * endAlpha), 255);
								result = pixel;
								result.A = resultAlpha;
							} else if (this.alphaOnly && !this.alphaBlending) {
								result = pixel;
								result.A = endAlpha;
							} else if (!this.alphaOnly && this.alphaBlending) {
								result = this.normalBlendOp.Apply (pixel, this.endColor);
								//if (!this.alphaOnly && !this.alphaBlending)
							} else {
								result = this.endColor;
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
			if (alphaOnly && alphaBlending) {
				for (var x = rect.Left; x <= right; ++x) {
					var lerpByte = ComputeByteLerp (x, y);
					var lerpAlpha = lerpAlphas[lerpByte];
					ref ColorBgra pixel = ref row[x];

					pixel.B = Utility.FastScaleByteByByte (pixel.B, lerpAlpha);
					pixel.G = Utility.FastScaleByteByByte (pixel.G, lerpAlpha);
					pixel.R = Utility.FastScaleByteByByte (pixel.R, lerpAlpha);
					pixel.A = Utility.FastScaleByteByByte (pixel.A, lerpAlpha);
				}
			} else if (alphaOnly && !alphaBlending) {
				for (var x = rect.Left; x <= right; ++x) {
					var lerpByte = ComputeByteLerp (x, y);
					var lerpAlpha = lerpAlphas[lerpByte];
					ref ColorBgra pixel = ref row[x];

					var color = pixel.ToStraightAlpha ();
					color.A = lerpAlpha;
					pixel = color.ToPremultipliedAlpha ();
				}
			} else if (!alphaOnly && (alphaBlending && (startAlpha != 255 || endAlpha != 255))) {
				// If we're doing all color channels, and we're doing alpha blending, and if alpha blending is necessary
				for (var x = rect.Left; x <= right; ++x) {
					var lerpByte = ComputeByteLerp (x, y);
					var lerpColor = lerpColors[lerpByte];
					ref ColorBgra pixel = ref row[x];
					pixel = normalBlendOp.Apply (pixel, lerpColor);
				}
				//if (!this.alphaOnly && !this.alphaBlending) // or sC.A == 255 && eC.A == 255
			} else {
				for (var x = rect.Left; x <= right; ++x) {
					var lerpByte = ComputeByteLerp (x, y);
					var lerpColor = lerpColors[lerpByte];
					row[x] = lerpColor;
				}
			}
			return true;
		}

		protected internal GradientRenderer (bool alphaOnly, BinaryPixelOp normalBlendOp)
		{
			this.normalBlendOp = normalBlendOp;
			this.alphaOnly = alphaOnly;
			this.lerpAlphas = new byte[256];
			this.lerpColors = new ColorBgra[256];
		}
	}
}
