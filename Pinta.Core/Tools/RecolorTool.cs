// 
// RecolorTool.cs
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
using Gtk;

namespace Pinta.Core
{
	public class RecolorTool : BaseBrushTool
	{
		protected ToolBarLabel tolerance_label;
		protected ToolBarSlider tolerance_slider;
		
		private Point last_point = point_empty;
		private bool[,] stencil;
		private int myTolerance;

		public RecolorTool ()
		{
		}

		#region Properties
		public override string Name { get { return "Recolor"; } }
		public override string Icon { get { return "Tools.Recolor.png"; } }
		public override string StatusBarText { get { return "Left click to replace the secondary color with the primary color."; } }
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.R; } }
		protected float Tolerance { get { return (float)(tolerance_slider.Slider.Value / 100); } }
		#endregion

		#region ToolBar
		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			if (tolerance_label == null)
				tolerance_label = new ToolBarLabel ("    Tolerance: ");

			tb.AppendItem (tolerance_label);

			if (tolerance_slider == null)
				tolerance_slider = new ToolBarSlider (0, 100, 1, 50);

			tb.AppendItem (tolerance_slider);
		}
		#endregion

		#region Mouse Handlers
		protected override void OnMouseDown (DrawingArea canvas, ButtonPressEventArgs args, PointD point)
		{
			PintaCore.Layers.ToolLayer.Clear ();
			stencil = new bool[PintaCore.Workspace.ImageSize.X, PintaCore.Workspace.ImageSize.Y];

			base.OnMouseDown (canvas, args, point);
		}
		
		protected unsafe override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			ColorBgra old_color;
			ColorBgra new_color;
			
			if (mouse_button == 1) {
				old_color = PintaCore.Palette.PrimaryColor.ToColorBgra ();
				new_color = PintaCore.Palette.SecondaryColor.ToColorBgra ();
			} else if (mouse_button == 3) {
				old_color = PintaCore.Palette.SecondaryColor.ToColorBgra ();
				new_color = PintaCore.Palette.PrimaryColor.ToColorBgra ();
			} else {
				last_point = point_empty;
				return;
			}
				
			int x = (int)point.X;
			int y = (int)point.Y;
			
			if (last_point.Equals (point_empty))
				last_point = new Point (x, y);
			
			if (PintaCore.Workspace.PointInCanvas (point))
				surface_modified = true;

			ImageSurface surf = PintaCore.Layers.CurrentLayer.Surface;
			ImageSurface tmp_layer = PintaCore.Layers.ToolLayer.Surface;

			Gdk.Rectangle roi = GetRectangleFromPoints (last_point, new Point (x, y));
			
			roi = PintaCore.Workspace.ClampToImageSize (roi);
			myTolerance = (int)(Tolerance * 256);

			ColorBgra* tmp_data_ptr = (ColorBgra*)tmp_layer.DataPtr;
			int tmp_width = tmp_layer.Width;
			ColorBgra* surf_data_ptr = (ColorBgra*)surf.DataPtr;
			int surf_width = surf.Width;
			
			// The stencil lets us know if we've already checked this
			// pixel, providing a nice perf boost
			// Maybe this should be changed to a BitVector2DSurfaceAdapter?
			for (int i = roi.X; i < roi.Right; i++)
				for (int j = roi.Y; j < roi.Bottom; j++) {
					if (stencil[i, j])
						continue;
						
					if (IsColorInTolerance (new_color, surf.GetColorBgra (surf_data_ptr, surf_width, i, j)))
						*tmp_layer.GetPointAddressUnchecked (tmp_data_ptr, tmp_width, i, j) = AdjustColorDifference (new_color, old_color, surf.GetColorBgra (surf_data_ptr, surf_width, i, j));

					stencil[i, j] = true;
				}

			
			using (Context g = new Context (surf)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.Antialias = Antialias.Subpixel;
				
				g.MoveTo (last_point.X, last_point.Y);
				g.LineTo (x, y);

				g.LineWidth = BrushWidth;
				g.LineJoin = LineJoin.Round;
				g.LineCap = LineCap.Round;
				
				g.SetSource (tmp_layer);
				
				g.Stroke ();
			}
			
			PintaCore.Workspace.Invalidate (roi);
			
			last_point = new Point (x, y);
		}
		#endregion

		#region Private PDN Methods
		private bool IsColorInTolerance (ColorBgra colorA, ColorBgra colorB)
		{
			return Utility.ColorDifference (colorA, colorB) <= myTolerance;
		}

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

		private ColorBgra AdjustColorDifference (ColorBgra oldColor, ColorBgra newColor, ColorBgra basisColor)
		{
			ColorBgra returnColor;

			// eliminate testing for the "equal to" case
			returnColor = basisColor;

			returnColor.B = AdjustColorByte (oldColor.B, newColor.B, basisColor.B);
			returnColor.G = AdjustColorByte (oldColor.G, newColor.G, basisColor.G);
			returnColor.R = AdjustColorByte (oldColor.R, newColor.R, basisColor.R);

			return returnColor;
		}
		private byte AdjustColorByte (byte oldByte, byte newByte, byte basisByte)
		{
			if (oldByte > newByte)
				return Utility.ClampToByte (basisByte - (oldByte - newByte));
			else
				return Utility.ClampToByte (basisByte + (newByte - oldByte));
		}
		#endregion
	}
}
