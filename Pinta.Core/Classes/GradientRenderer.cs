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
using Rectangle = Gdk.Rectangle;

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
				
				this.lerpAlphas = new byte[256];
				this.lerpColors = new ColorBgra[256];
				
				for (int i = 0; i < 256; ++i) {
					byte a = (byte)i;
					this.lerpColors[a] = ColorBgra.Blend (this.startColor, this.endColor, a);
					this.lerpAlphas[a] = (byte)(startAlpha + ((endAlpha - startAlpha) * a) / 255);
				}
				
				this.lerpCacheIsValid = true;
			}
		}

		public abstract byte ComputeByteLerp(int x, int y);

		public virtual void AfterRender ()
		{
		}

		private static void ComputeAlphaOnlyValuesFromColors (ColorBgra startColor, ColorBgra endColor, out byte startAlpha, out byte endAlpha)
		{
			startAlpha = startColor.A;
			endAlpha = (byte)(255 - endColor.A);
		}

		unsafe public void Render (ImageSurface surface, Gdk.Rectangle[] rois)
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
			
			ColorBgra* src_data_ptr = (ColorBgra*)surface.DataPtr;
			int src_width = surface.Width;
			
			for (int ri = 0; ri < rois.Length; ++ri) {
				Gdk.Rectangle rect = rois[ri];
				
				if (this.startPoint.X == this.endPoint.X && this.startPoint.Y == this.endPoint.Y) {
					// Start and End point are the same ... fill with solid color.
					for (int y = rect.Top; y <= rect.GetBottom (); ++y) {
						ColorBgra* pixelPtr = surface.GetPointAddress(rect.Left, y);
						
						for (int x = rect.Left; x <= rect.GetRight (); ++x) {
							ColorBgra result;
							
							if (this.alphaOnly && this.alphaBlending) {
								byte resultAlpha = (byte)Utility.FastDivideShortByByte ((ushort)(pixelPtr->A * endAlpha), 255);
								result = *pixelPtr;
								result.A = resultAlpha;
							} else if (this.alphaOnly && !this.alphaBlending) {
								result = *pixelPtr;
								result.A = endAlpha;
							} else if (!this.alphaOnly && this.alphaBlending) {
								result = this.normalBlendOp.Apply (*pixelPtr, this.endColor);
							//if (!this.alphaOnly && !this.alphaBlending)
							} else {
								result = this.endColor;
							}
							
							*pixelPtr = result;
							++pixelPtr;
						}
					}
				} else {
					var mainrect = rect;
					Parallel.ForEach(Enumerable.Range (rect.Top, rect.Height),
						(y) => ProcessGradientLine(startAlpha, endAlpha, y, mainrect, surface, src_data_ptr, src_width));
				}
			}
			
			surface.MarkDirty ();
			AfterRender ();
		}

		private unsafe bool ProcessGradientLine (byte startAlpha, byte endAlpha, int y, Rectangle rect, ImageSurface surface, ColorBgra* src_data_ptr, int src_width)
		{
			var pixelPtr = surface.GetPointAddressUnchecked(src_data_ptr, src_width, rect.Left, y);
			var right = rect.GetRight ();
			if (alphaOnly && alphaBlending)
			{
				for (var x = rect.Left; x <= right; ++x)
				{
					var lerpByte = ComputeByteLerp(x, y);
					var lerpAlpha = lerpAlphas[lerpByte];
					var resultAlpha = Utility.FastScaleByteByByte(pixelPtr->A, lerpAlpha);
					pixelPtr->A = resultAlpha;
					++pixelPtr;
				}
			}
			else if (alphaOnly && !alphaBlending)
			{
				for (var x = rect.Left; x <= right; ++x)
				{
					var lerpByte = ComputeByteLerp(x, y);
					var lerpAlpha = lerpAlphas[lerpByte];
					pixelPtr->A = lerpAlpha;
					++pixelPtr;
				}
			}
			else if (!alphaOnly && (alphaBlending && (startAlpha != 255 || endAlpha != 255)))
			{
				// If we're doing all color channels, and we're doing alpha blending, and if alpha blending is necessary
				for (var x = rect.Left; x <= right; ++x)
				{
					var lerpByte = ComputeByteLerp(x, y);
					var lerpColor = lerpColors[lerpByte];
					var result = normalBlendOp.Apply(*pixelPtr, lerpColor);
					*pixelPtr = result;
					++pixelPtr;
				}
				//if (!this.alphaOnly && !this.alphaBlending) // or sC.A == 255 && eC.A == 255
			}
			else
			{
				for (var x = rect.Left; x <= right; ++x)
				{
					var lerpByte = ComputeByteLerp(x, y);
					var lerpColor = lerpColors[lerpByte];
					*pixelPtr = lerpColor;
					++pixelPtr;
				}
			}
			return true;
		}

		protected internal GradientRenderer (bool alphaOnly, BinaryPixelOp normalBlendOp)
		{
			this.normalBlendOp = normalBlendOp;
			this.alphaOnly = alphaOnly;
		}
	}
}
