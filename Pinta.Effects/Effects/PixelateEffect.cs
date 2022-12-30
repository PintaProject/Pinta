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
	public class PixelateEffect : BaseEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Distort.Pixelate.png"; }
		}

		public override string Name {
			get { return Translations.GetString ("Pixelate"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public PixelateData Data {
			get { return (PixelateData) EffectData!; } // NRT - Set in constructor
		}

		public override string EffectMenuCategory {
			get { return Translations.GetString ("Distort"); }
		}

		public PixelateEffect ()
		{
			EffectData = new PixelateData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN
		private ColorBgra ComputeCellColor (int x, int y, ReadOnlySpan<ColorBgra> src_data, int cellSize, Core.RectangleI srcBounds)
		{
			Core.RectangleI cell = GetCellBox (x, y, cellSize);
			cell.Intersect (srcBounds);

			int left = cell.Left;
			int right = cell.Right;
			int bottom = cell.Bottom;
			int top = cell.Top;

			ColorBgra colorTopLeft = src_data[top * srcBounds.Width + left].ToStraightAlpha ();
			ColorBgra colorTopRight = src_data[top * srcBounds.Width + right].ToStraightAlpha ();
			ColorBgra colorBottomLeft = src_data[bottom * srcBounds.Width + left].ToStraightAlpha ();
			ColorBgra colorBottomRight = src_data[bottom * srcBounds.Width + right].ToStraightAlpha ();

			ColorBgra c = ColorBgra.BlendColors4W16IP (colorTopLeft, 16384, colorTopRight, 16384, colorBottomLeft, 16384, colorBottomRight, 16384);

			return c.ToPremultipliedAlpha ();
		}

		private Core.RectangleI GetCellBox (int x, int y, int cellSize)
		{
			int widthBoxNum = x % cellSize;
			int heightBoxNum = y % cellSize;
			var leftUpper = new Core.PointI (x - widthBoxNum, y - heightBoxNum);

			return new Core.RectangleI (leftUpper, new Core.Size (cellSize, cellSize));
		}


		public override void Render (ImageSurface src, ImageSurface dest, Core.RectangleI[] rois)
		{
			var cellSize = Data.CellSize;

			Core.RectangleI src_bounds = src.GetBounds ();
			Core.RectangleI dest_bounds = dest.GetBounds ();

			var src_data = src.GetReadOnlyData ();
			var dst_data = dest.GetData ();

			foreach (var rect in rois) {
				for (int y = rect.Top; y <= rect.Bottom; ++y) {
					int yEnd = y + 1;

					for (int x = rect.Left; x <= rect.Right; ++x) {
						var cellRect = GetCellBox (x, y, cellSize);
						cellRect.Intersect (dest_bounds);
						var color = ComputeCellColor (x, y, src_data, cellSize, src_bounds);

						int xEnd = Math.Min (rect.Right, cellRect.Right);
						yEnd = Math.Min (rect.Bottom, cellRect.Bottom);

						for (int y2 = y; y2 <= yEnd; ++y2) {
							var dst_row = dst_data.Slice (y2 * dest_bounds.Width, dest_bounds.Width);

							for (int x2 = x; x2 <= xEnd; ++x2) {
								dst_row[x2].Bgra = color.Bgra;
							}
						}

						x = xEnd;
					}

					y = yEnd;
				}
			}
		}
		#endregion
	}


	public class PixelateData : EffectData
	{
		[Caption ("Cell Size"), MinimumValue (1), MaximumValue (100)]
		public int CellSize = 2;
	}
}
