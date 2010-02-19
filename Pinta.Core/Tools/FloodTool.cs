// 
// FloodTool.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
using Cairo;
using System.Collections.Generic;

namespace Pinta.Core
{
	public abstract class FloodTool : BaseTool
	{
		protected ToolBarLabel mode_label;
		protected ToolBarComboBox mode_combo;
		protected ToolBarLabel tolerance_label;
		protected ToolBarSlider tolerance_slider;
		
		protected IBitVector2D stencil;

		#region Protected Properties
		protected bool IsContinguousMode { get { return mode_combo.ComboBox.Active == 0; } }
		protected float Tolerance { get { return (float)(tolerance_slider.Slider.Value / 100); } }
		#endregion
		
		#region ToolBar
		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			if (mode_label == null)
				mode_label = new ToolBarLabel (" Fill mode: ");

			tb.AppendItem (mode_label);

			if (mode_combo == null)
				mode_combo = new ToolBarComboBox (100, 0, false, "Contiguous", "Global");

			tb.AppendItem (mode_combo);
					
			if (tolerance_label == null)
				tolerance_label = new ToolBarLabel ("    Tolerance: ");

			tb.AppendItem (tolerance_label);

			if (tolerance_slider == null)
				tolerance_slider = new ToolBarSlider (0, 100, 1, 50);
				
			tb.AppendItem (tolerance_slider);
		}
		#endregion

		#region Mouse Handlers
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, PointD point)
		{
			Point pos = new Point ((int)point.X, (int)point.Y);

			// Don't do anything if we're outside the canvas
			if (pos.X < 0 || pos.X >= PintaCore.Workspace.ImageSize.X)
				return;
			if (pos.Y < 0 || pos.Y >= PintaCore.Workspace.ImageSize.Y)
				return;
				
			base.OnMouseDown (canvas, args, point);

			ImageSurface surface = PintaCore.Layers.CurrentLayer.Surface;
			ImageSurface stencil_surface = new ImageSurface (Format.Argb32, (int)surface.Width, (int)surface.Height);

			IBitVector2D stencilBuffer = new BitVector2DSurfaceAdapter (stencil_surface);
			int tol = (int)(Tolerance * Tolerance * 256);
			Rectangle boundingBox;

			if (IsContinguousMode)
				FillStencilFromPoint (surface, stencilBuffer, pos, tol, out boundingBox);
			else
				FillStencilByColor (surface, stencilBuffer, surface.GetColorBgra (pos.X, pos.Y), tol, out boundingBox);
				
			stencil = stencilBuffer;
			OnFillRegionComputed (null);
		}
		#endregion

		#region Private Methods
		// These methods are ported from PDN.
		private static bool CheckColor (ColorBgra a, ColorBgra b, int tolerance)
		{
			int sum = 0;
			int diff;

			diff = a.R - b.R;
			sum += (1 + diff * diff) * a.A / 256;

			diff = a.G - b.G;
			sum += (1 + diff * diff) * a.A / 256;

			diff = a.B - b.B;
			sum += (1 + diff * diff) * a.A / 256;

			diff = a.A - b.A;
			sum += diff * diff;

			return (sum <= tolerance * tolerance * 4);
		}

		public unsafe static void FillStencilFromPoint (ImageSurface surface, IBitVector2D stencil, Point start, int tolerance, out Rectangle boundingBox)
		{
			ColorBgra cmp = surface.GetColorBgra (start.X, start.Y);
			int top = int.MaxValue;
			int bottom = int.MinValue;
			int left = int.MaxValue;
			int right = int.MinValue;

			stencil.Clear (false);

			Queue<Point> queue = new Queue<Point> (16);
			queue.Enqueue (start);

			while (queue.Count > 0) {
				Point pt = queue.Dequeue ();

				ColorBgra* rowPtr = surface.GetRowAddressUnchecked (pt.Y);
				int localLeft = pt.X - 1;
				int localRight = pt.X;

				while (localLeft >= 0 &&
				       !stencil.GetUnchecked (localLeft, pt.Y) &&
				       CheckColor (cmp, rowPtr[localLeft], tolerance)) {
					stencil.SetUnchecked (localLeft, pt.Y, true);
					--localLeft;
				}

				while (localRight < surface.Width &&
				       !stencil.GetUnchecked (localRight, pt.Y) &&
				       CheckColor (cmp, rowPtr[localRight], tolerance)) {
					stencil.SetUnchecked (localRight, pt.Y, true);
					++localRight;
				}

				++localLeft;
				--localRight;

				if (pt.Y > 0) {
					int sleft = localLeft;
					int sright = localLeft;
					ColorBgra* rowPtrUp = surface.GetRowAddressUnchecked (pt.Y - 1);

					for (int sx = localLeft; sx <= localRight; ++sx) {
						if (!stencil.GetUnchecked (sx, pt.Y - 1) &&
						    CheckColor (cmp, rowPtrUp[sx], tolerance)) {
							++sright;
						} else {
							if (sright - sleft > 0) {
								queue.Enqueue (new Point (sleft, pt.Y - 1));
							}

							++sright;
							sleft = sright;
						}
					}

					if (sright - sleft > 0) {
						queue.Enqueue (new Point (sleft, pt.Y - 1));
					}
				}

				if (pt.Y < surface.Height - 1) {
					int sleft = localLeft;
					int sright = localLeft;
					ColorBgra* rowPtrDown = surface.GetRowAddressUnchecked (pt.Y + 1);

					for (int sx = localLeft; sx <= localRight; ++sx) {
						if (!stencil.GetUnchecked (sx, pt.Y + 1) &&
						    CheckColor (cmp, rowPtrDown[sx], tolerance)) {
							++sright;
						} else {
							if (sright - sleft > 0) {
								queue.Enqueue (new Point (sleft, pt.Y + 1));
							}

							++sright;
							sleft = sright;
						}
					}

					if (sright - sleft > 0) {
						queue.Enqueue (new Point (sleft, pt.Y + 1));
					}
				}

				if (localLeft < left) {
					left = localLeft;
				}

				if (localRight > right) {
					right = localRight;
				}

				if (pt.Y < top) {
					top = pt.Y;
				}

				if (pt.Y > bottom) {
					bottom = pt.Y;
				}
			}

			boundingBox = new Rectangle (left, top, right + 1, bottom + 1);
		}

		public unsafe static void FillStencilByColor (ImageSurface surface, IBitVector2D stencil, ColorBgra cmp, int tolerance, out Rectangle boundingBox)
		{
			int top = int.MaxValue;
			int bottom = int.MinValue;
			int left = int.MaxValue;
			int right = int.MinValue;

			stencil.Clear (false);

			for (int y = 0; y < surface.Height; ++y) {
				bool foundPixelInRow = false;
				ColorBgra* ptr = surface.GetRowAddressUnchecked (y);

				for (int x = 0; x < surface.Width; ++x) {
					if (CheckColor (cmp, *ptr, tolerance)) {
						stencil.SetUnchecked (x, y, true);

						if (x < left) {
							left = x;
						}

						if (x > right) {
							right = x;
						}

						foundPixelInRow = true;
					}

					++ptr;
				}

				if (foundPixelInRow) {
					if (y < top) {
						top = y;
					}

					if (y >= bottom) {
						bottom = y;
					}
				}
			}

			boundingBox = new Rectangle (left, top, right + 1, bottom + 1);
		}

		protected abstract void OnFillRegionComputed (Point[][] polygonSet);
		#endregion
	}
}
