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
using Mono.Unix;

namespace Pinta.Tools
{
	public abstract class FloodTool : BaseTool
	{
		protected ToolBarLabel mode_label;
		protected ToolBarDropDownButton mode_button;
		protected Gtk.ToolItem mode_sep;
		protected ToolBarLabel tolerance_label;
		protected ToolBarSlider tolerance_slider;
		private bool limitToSelection = true;
		
		#region Protected Properties
		protected bool IsContinguousMode { get { return (bool)mode_button.SelectedItem.Tag; } }
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
				mode_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("Flood Mode")));

			tb.AppendItem (mode_label);

			if (mode_button == null) {
				mode_button = new ToolBarDropDownButton ();

				mode_button.AddItem (Catalog.GetString ("Contiguous"), "Tools.FreeformShape.png", true);
				mode_button.AddItem (Catalog.GetString ("Global"), "Menu.Help.Website.png", false);
			}

			tb.AppendItem (mode_button);

			if (mode_sep == null)
				mode_sep = new Gtk.SeparatorToolItem ();

			tb.AppendItem (mode_sep);
					
			if (tolerance_label == null)
				tolerance_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("Tolerance")));

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

			Gdk.Region currentRegion = Gdk.Region.Rectangle (doc.GetSelectedBounds (true));

			// See if the mouse click is valid
			if (!currentRegion.PointIn (pos.X, pos.Y) && limitToSelection) {
				currentRegion.Dispose ();
				currentRegion = null;
				return;
			}

			ImageSurface surface = doc.CurrentUserLayer.Surface;
			using (var stencil_surface = new ImageSurface (Format.Argb32, (int)surface.Width, (int)surface.Height)) {
				IBitVector2D stencilBuffer = new BitVector2DSurfaceAdapter (stencil_surface);
				int tol = (int)(Tolerance * Tolerance * 256);
				Rectangle boundingBox;

				if (IsContinguousMode)
					FillStencilFromPoint (surface, stencilBuffer, pos, tol, out boundingBox, currentRegion, limitToSelection);
				else
					FillStencilByColor (surface, stencilBuffer, surface.GetColorBgraUnchecked (pos.X, pos.Y), tol, out boundingBox, currentRegion, LimitToSelection);

				OnFillRegionComputed (stencilBuffer);

				// If a derived tool is only going to use the stencil,
				// don't waste time building the polygon set
				if (CalculatePolygonSet) {
					Point[][] polygonSet = stencilBuffer.CreatePolygonSet (boundingBox, 0, 0);
					OnFillRegionComputed (polygonSet);
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

		public unsafe static void FillStencilFromPoint (ImageSurface surface, IBitVector2D stencil, Point start, int tolerance, 
		                                                out Rectangle boundingBox, Gdk.Region limitRegion, bool limitToSelection)
		{
			ColorBgra cmp = surface.GetColorBgraUnchecked (start.X, start.Y);
			int top = int.MaxValue;
			int bottom = int.MinValue;
			int left = int.MaxValue;
			int right = int.MinValue;
			Gdk.Rectangle[] scans;

			stencil.Clear (false);

			if (limitToSelection) {
				using (Gdk.Region excluded = Gdk.Region.Rectangle (new Gdk.Rectangle (0, 0, stencil.Width, stencil.Height))) {
					excluded.Xor (limitRegion);
					scans = excluded.GetRectangles ();
				}
			} else {
				scans = new Gdk.Rectangle[0];
			}

			foreach (Gdk.Rectangle rect in scans) {
				stencil.Set (rect, true);
			}

			var queue = new System.Collections.Generic.Queue<Point> (16);
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

                int surfaceWidth = surface.Width;
				while (localRight < surfaceWidth &&
				       !stencil.GetUnchecked (localRight, pt.Y) &&
				       CheckColor (cmp, rowPtr[localRight], tolerance)) {
					stencil.SetUnchecked (localRight, pt.Y, true);
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
						if (!stencil.GetUnchecked (sx, row) &&
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

			foreach (Gdk.Rectangle rect in scans)
				stencil.Set (rect, false);
			
			boundingBox = new Rectangle (left, top, right - left + 1, bottom - top + 1);
		}

		public unsafe static void FillStencilByColor (ImageSurface surface, IBitVector2D stencil, ColorBgra cmp, int tolerance, 
		                                              out Rectangle boundingBox, Gdk.Region limitRegion, bool limitToSelection)
		{
			int top = int.MaxValue;
			int bottom = int.MinValue;
			int left = int.MaxValue;
			int right = int.MinValue;
			Gdk.Rectangle[] scans;

			stencil.Clear (false);

			if (limitToSelection) {
				using (Gdk.Region excluded = Gdk.Region.Rectangle (new Gdk.Rectangle (0, 0, stencil.Width, stencil.Height))) {
					excluded.Xor (limitRegion);
					scans = excluded.GetRectangles ();
				}
			} else {
				scans = new Gdk.Rectangle[0];
			}

			foreach (Gdk.Rectangle rect in scans)
				stencil.Set (rect, true);

            Parallel.For(0, surface.Height, y =>
            {
                bool foundPixelInRow = false;
                ColorBgra* ptr = surface.GetRowAddressUnchecked(y);

                int surfaceWidth = surface.Width;
                for (int x = 0; x < surfaceWidth; ++x)
                {
                    if (CheckColor(cmp, *ptr, tolerance))
                    {
                        stencil.SetUnchecked(x, y, true);

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

			foreach (Gdk.Rectangle rect in scans)
				stencil.Set (rect, false);

			boundingBox = new Rectangle (left, top, right - left + 1, bottom - top + 1);
		}

		protected virtual void OnFillRegionComputed (Point[][] polygonSet) {}
		protected virtual void OnFillRegionComputed (IBitVector2D stencil) {}
		#endregion
	}
}
