// 
// MotionBlurEffect.cs
//  
// Author:
//       dufoli <${AuthorEmail}>
// 
// Copyright (c) 2010 dufoli
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Pinta.Gui.Widgets;
using Cairo;

namespace Pinta.Core
{
	public class MotionBlurEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Blurs.MotionBlur.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Motion Blur"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public MotionBlurData Data { get; private set; }

		public MotionBlurEffect ()
		{
			Data = new MotionBlurData ();
		}

		public override bool LaunchConfiguration ()
		{
			SimpleEffectDialog dialog = new SimpleEffectDialog (Text, PintaCore.Resources.GetIcon (Icon), Data);

			int response = dialog.Run ();

			if (response == (int)Gtk.ResponseType.Ok) {
				dialog.Destroy ();
				return !Data.IsEmpty;
			}

			dialog.Destroy ();

			return false;
		}

		#region Algorithm Code Ported From PDN
		private unsafe ColorBgra DoLineAverage (Gdk.Point[] points, int x, int y, ImageSurface dst, ImageSurface src)
		{
			long bSum = 0;
			long gSum = 0;
			long rSum = 0;
			long aSum = 0;
			int cDiv = 0;
			int aDiv = 0;

			foreach (Gdk.Point p in points) {
				Gdk.Point srcPoint = new Gdk.Point (x + p.X, y + p.Y);

				if (src.GetBounds ().Contains (srcPoint)) {
					ColorBgra c = src.GetPointUnchecked (srcPoint.X, srcPoint.Y);

					bSum += c.B * c.A;
					gSum += c.G * c.A;
					rSum += c.R * c.A;
					aSum += c.A;

					aDiv++;
					cDiv += c.A;
				}
			}

			int b;
			int g;
			int r;
			int a;

			if (cDiv == 0) {
				b = 0;
				g = 0;
				r = 0;
				a = 0;
			} else {
				b = (int)(bSum /= cDiv);
				g = (int)(gSum /= cDiv);
				r = (int)(rSum /= cDiv);
				a = (int)(aSum /= aDiv);
			}

			return ColorBgra.FromBgra ((byte)b, (byte)g, (byte)r, (byte)a);
		}

		private unsafe ColorBgra DoLineAverageUnclipped (Gdk.Point[] points, int x, int y, ImageSurface dst, ImageSurface src)
		{
			long bSum = 0;
			long gSum = 0;
			long rSum = 0;
			long aSum = 0;
			int cDiv = 0;
			int aDiv = 0;

			foreach (Gdk.Point p in points) {
				Point srcPoint = new Point (x + p.X, y + p.Y);
				ColorBgra c = src.GetPointUnchecked (srcPoint.X, srcPoint.Y);

				bSum += c.B * c.A;
				gSum += c.G * c.A;
				rSum += c.R * c.A;
				aSum += c.A;

				aDiv++;
				cDiv += c.A;
			}

			int b;
			int g;
			int r;
			int a;

			if (cDiv == 0) {
				b = 0;
				g = 0;
				r = 0;
				a = 0;
			} else {
				b = (int)(bSum /= cDiv);
				g = (int)(gSum /= cDiv);
				r = (int)(rSum /= cDiv);
				a = (int)(aSum /= aDiv);
			}

			return ColorBgra.FromBgra ((byte)b, (byte)g, (byte)r, (byte)a);
		}

		public unsafe override void RenderEffect (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			if (Data.IsEmpty) {
				// Copy src to dest
				return;
			}
			Gdk.Point[] points = Data.LinePoints;

			foreach (Gdk.Rectangle rect in rois) {
				for (int y = rect.Top; y < rect.Bottom; ++y) {
					ColorBgra* dstPtr = dst.GetPointAddress (rect.Left, y);

					for (int x = rect.Left; x < rect.Right; ++x) {
						Gdk.Point a = new Gdk.Point (x + points[0].X, y + points[0].Y);
						Gdk.Point b = new Gdk.Point (x + points[points.Length - 1].X, y + points[points.Length - 1].Y);

						// If both ends of this line are in bounds, we don't need to do silly clipping
						if (src.GetBounds ().Contains (a) && src.GetBounds ().Contains (b))
							*dstPtr = DoLineAverageUnclipped (points, x, y, dst, src);
						else
							*dstPtr = DoLineAverage (points, x, y, dst, src);

						++dstPtr;
					}
				}
			}
		}
		#endregion

		public class MotionBlurData
		{
			[Skip]
			public bool IsEmpty { get { return (angle == 0) && (distance == 0); } }

			//TODO move angle to double and made a slider for double
			[Skip]
			private int angle = 25;

			[MinimumValue (-180), MaximumValue (180)]
			public int Angle {
				get { return angle; }
				set {
					this.angle = value;
					linePoints = null;
				}
			}

			[Skip]
			private int distance = 10;

			[MinimumValue (1), MaximumValue (200)]
			public int Distance
			{
				get { return distance; }
				set {
					this.distance = value;
					linePoints = null;
				}
			}

			[Skip]
			private bool centered = true;

			public bool Centered
			{
				get { return centered; }
				set {
					centered = value;
					linePoints = null;
				}
			}

			[Skip]
			private Gdk.Point[] linePoints = null;
			
			[Skip]
			public Gdk.Point[] LinePoints {
				get {
					Gdk.Point[] returnPoints = linePoints;

					if (linePoints == null) {
						Gdk.Point start = new Gdk.Point (0, 0);
						double theta = ((double)(angle + 180) * 2 * Math.PI) / 360.0;
						double alpha = (double)distance;
						double x = alpha * Math.Cos (theta);
						double y = alpha * Math.Sin (theta);
						Gdk.Point end = new Gdk.Point ((int)x, -(int)y);

						if (centered) {
							start.X = -end.X / 2;
							start.Y = -end.Y / 2;
							end.X /= 2;
							end.Y /= 2;
						}

						returnPoints = Utility.GetLinePoints (start, end);
						linePoints = returnPoints;
					}

					return returnPoints;
				}
			}
		}
	}
}
