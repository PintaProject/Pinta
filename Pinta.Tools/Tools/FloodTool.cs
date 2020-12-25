/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

// Additional code:
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
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Tools
{
	public abstract class FloodTool : BaseTool
	{
		// NRT - Created in OnBuildToolBar
		protected ToolBarLabel mode_label = null!;
		protected ToolBarDropDownButton mode_button = null!;
		protected Gtk.ToolItem mode_sep = null!;
		protected ToolBarLabel tolerance_label = null!;
		protected ToolBarSlider tolerance_slider = null!;
		private bool limitToSelection = true;
		
		#region Protected Properties
		protected bool IsContinguousMode { get { return mode_button.SelectedItem.GetTagOrDefault (true); } }
		protected float Tolerance { get { return (float)(tolerance_slider.Slider.Value / 100); } }
		protected virtual bool CalculatePolygonSet { get { return true; } }

		protected bool LimitToSelection {
			get { return limitToSelection; }
			set { limitToSelection = value; }
		}		
		#endregion
		
		#region ToolBar
		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			if (mode_label == null)
				mode_label = new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Flood Mode")));

			tb.AppendItem (mode_label);

			if (mode_button == null) {
				mode_button = new ToolBarDropDownButton ();

				mode_button.AddItem (Translations.GetString ("Contiguous"), Resources.Icons.ToolFreeformShape, true);
				mode_button.AddItem (Translations.GetString ("Global"), Resources.Icons.HelpWebsite, false);
			}

			tb.AppendItem (mode_button);

			if (mode_sep == null)
				mode_sep = new Gtk.SeparatorToolItem ();

			tb.AppendItem (mode_sep);
					
			if (tolerance_label == null)
				tolerance_label = new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Tolerance")));

			tb.AppendItem (tolerance_label);

			if (tolerance_slider == null)
				tolerance_slider = new ToolBarSlider (0, 100, 1, 0);
				
			tb.AppendItem (tolerance_slider);
		}
		#endregion

		#region Mouse Handlers
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			Point pos = new Point ((int)point.X, (int)point.Y);

			// Don't do anything if we're outside the canvas
			if (pos.X < 0 || pos.X >= doc.ImageSize.Width)
				return;
			if (pos.Y < 0 || pos.Y >= doc.ImageSize.Height)
				return;
				
			base.OnMouseDown (canvas, args, point);

			using (var currentRegion = new Region(doc.GetSelectedBounds(true).ToCairoRectangleInt()))
			{
				// See if the mouse click is valid
				if (!currentRegion.ContainsPoint(pos.X, pos.Y) && limitToSelection)
					return;

				ImageSurface surface = doc.Layers.CurrentUserLayer.Surface;
				var stencilBuffer = new BitMask ((int) surface.Width, (int) surface.Height);
				int tol = (int)(Tolerance * Tolerance * 256);
				Rectangle boundingBox;

				if (IsContinguousMode)
					FillStencilFromPoint(surface, stencilBuffer, pos, tol, out boundingBox, currentRegion, limitToSelection);
				else
					FillStencilByColor(surface, stencilBuffer, surface.GetColorBgraUnchecked(pos.X, pos.Y), tol, out boundingBox, currentRegion, LimitToSelection);

				OnFillRegionComputed(stencilBuffer);

				// If a derived tool is only going to use the stencil,
				// don't waste time building the polygon set
				if (CalculatePolygonSet)
				{
					Point[][] polygonSet = stencilBuffer.CreatePolygonSet(boundingBox, 0, 0);
					OnFillRegionComputed(polygonSet);
				}
			}
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

		public unsafe static void FillStencilFromPoint (ImageSurface surface, BitMask stencil, Point start, int tolerance, 
		                                                out Rectangle boundingBox, Cairo.Region limitRegion, bool limitToSelection)
		{
			ColorBgra cmp = surface.GetColorBgraUnchecked (start.X, start.Y);
			int top = int.MaxValue;
			int bottom = int.MinValue;
			int left = int.MaxValue;
			int right = int.MinValue;
			Cairo.RectangleInt[] scans;

			stencil.Clear (false);

			if (limitToSelection) {
				using (Cairo.Region excluded = new Cairo.Region (CairoExtensions.CreateRectangleInt (0, 0, stencil.Width, stencil.Height))) {
					excluded.Xor (limitRegion);
					scans = new Cairo.RectangleInt[excluded.NumRectangles];
                    for (int i = 0, n = excluded.NumRectangles; i < n; ++i)
						scans[i] = excluded.GetRectangle(i);
				}
			} else {
				scans = new Cairo.RectangleInt[0];
			}

			foreach (var rect in scans) {
				stencil.Set (rect.ToGdkRectangle(), true);
			}

			var queue = new System.Collections.Generic.Queue<Point> (16);
			queue.Enqueue (start);

			while (queue.Count > 0) {
				Point pt = queue.Dequeue ();

				ColorBgra* rowPtr = surface.GetRowAddressUnchecked (pt.Y);
				int localLeft = pt.X - 1;
				int localRight = pt.X;

				while (localLeft >= 0 &&
				       !stencil.Get (localLeft, pt.Y) &&
				       CheckColor (cmp, rowPtr[localLeft], tolerance)) {
					stencil.Set (localLeft, pt.Y, true);
					--localLeft;
				}

                int surfaceWidth = surface.Width;
				while (localRight < surfaceWidth &&
				       !stencil.Get (localRight, pt.Y) &&
				       CheckColor (cmp, rowPtr[localRight], tolerance)) {
					stencil.Set (localRight, pt.Y, true);
					++localRight;
				}

				++localLeft;
				--localRight;

                Action<int> checkRow = (row) =>
                {
					int sleft = localLeft;
					int sright = localLeft;
					ColorBgra* otherRowPtr = surface.GetRowAddressUnchecked (row);

					for (int sx = localLeft; sx <= localRight; ++sx) {
						if (!stencil.Get (sx, row) &&
						    CheckColor (cmp, otherRowPtr[sx], tolerance)) {
							++sright;
						} else {
							if (sright - sleft > 0) {
								queue.Enqueue (new Point (sleft, row));
							}

							++sright;
							sleft = sright;
						}
					}

					if (sright - sleft > 0) {
						queue.Enqueue (new Point (sleft, row));
					}
                };

				if (pt.Y > 0) {
                    checkRow (pt.Y - 1);
				}

				if (pt.Y < surface.Height - 1) {
                    checkRow (pt.Y + 1);
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

			foreach (var rect in scans)
				stencil.Set (rect.ToGdkRectangle(), false);
			
			boundingBox = new Rectangle (left, top, right - left + 1, bottom - top + 1);
		}

		public unsafe static void FillStencilByColor (ImageSurface surface, BitMask stencil, ColorBgra cmp, int tolerance, 
		                                              out Rectangle boundingBox, Cairo.Region limitRegion, bool limitToSelection)
		{
			int top = int.MaxValue;
			int bottom = int.MinValue;
			int left = int.MaxValue;
			int right = int.MinValue;
			Cairo.RectangleInt[] scans;

			stencil.Clear (false);

			if (limitToSelection) {
				using (var excluded = new Cairo.Region (CairoExtensions.CreateRectangleInt (0, 0, stencil.Width, stencil.Height))) {
					excluded.Xor (limitRegion);
					scans = new Cairo.RectangleInt[excluded.NumRectangles];
                    for (int i = 0, n = excluded.NumRectangles; i < n; ++i)
						scans[i] = excluded.GetRectangle(i);
				}
			} else {
				scans = new Cairo.RectangleInt[0];
			}

			foreach (var rect in scans)
				stencil.Set (rect.ToGdkRectangle(), true);

            Parallel.For(0, surface.Height, y =>
            {
                bool foundPixelInRow = false;
                ColorBgra* ptr = surface.GetRowAddressUnchecked(y);

                int surfaceWidth = surface.Width;
                for (int x = 0; x < surfaceWidth; ++x)
                {
                    if (CheckColor(cmp, *ptr, tolerance))
                    {
                        stencil.Set(x, y, true);

                        if (x < left)
                        {
                            left = x;
                        }

                        if (x > right)
                        {
                            right = x;
                        }

                        foundPixelInRow = true;
                    }

                    ++ptr;
                }

                if (foundPixelInRow)
                {
                    if (y < top)
                    {
                        top = y;
                    }

                    if (y >= bottom)
                    {
                        bottom = y;
                    }
                }
            });

			foreach (var rect in scans)
				stencil.Set (rect.ToGdkRectangle(), false);

			boundingBox = new Rectangle (left, top, right - left + 1, bottom - top + 1);
		}

		protected virtual void OnFillRegionComputed (Point[][] polygonSet) {}
		protected virtual void OnFillRegionComputed (BitMask stencil) {}
		#endregion
	}
}
