/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class PixelateEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsDistortPixelate;

	public sealed override bool IsTileable => true;

	public override string Name => Translations.GetString ("Pixelate");

	public override bool IsConfigurable => true;

	public PixelateData Data => (PixelateData) EffectData!;

	public override string EffectMenuCategory => Translations.GetString ("Distort");

	private readonly IChromeService chrome;
	private readonly IWorkspaceService workspace;
	public PixelateEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new PixelateData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	#region Algorithm Code Ported From PDN
	private static ColorBgra ComputeCellColor (
		int x,
		int y,
		ReadOnlySpan<ColorBgra> src_data,
		int cellSize,
		RectangleI srcBounds)
	{
		RectangleI cell = GetCellBox (x, y, cellSize).Intersect (srcBounds);

		int left = cell.Left;
		int right = cell.Right;
		int bottom = cell.Bottom;
		int top = cell.Top;

		ColorBgra colorTopLeft = src_data[top * srcBounds.Width + left].ToStraightAlpha ();
		ColorBgra colorTopRight = src_data[top * srcBounds.Width + right].ToStraightAlpha ();
		ColorBgra colorBottomLeft = src_data[bottom * srcBounds.Width + left].ToStraightAlpha ();
		ColorBgra colorBottomRight = src_data[bottom * srcBounds.Width + right].ToStraightAlpha ();

		ColorBgra c = ColorBgra.BlendColors4W16IP (
			colorTopLeft, 16384,
			colorTopRight, 16384,
			colorBottomLeft, 16384,
			colorBottomRight, 16384);

		return c.ToPremultipliedAlpha ();
	}

	private static RectangleI GetCellBox (int x, int y, int cellSize)
	{
		int widthBoxNum = x % cellSize;
		int heightBoxNum = y % cellSize;
		PointI leftUpper = new (x - widthBoxNum, y - heightBoxNum);
		return new (leftUpper, new Size (cellSize, cellSize));
	}


	public override void Render (
		ImageSurface src,
		ImageSurface dest,
		ReadOnlySpan<RectangleI> rois)
	{
		var cellSize = Data.CellSize;

		RectangleI src_bounds = src.GetBounds ();
		RectangleI dest_bounds = dest.GetBounds ();

		var src_data = src.GetReadOnlyPixelData ();
		var dst_data = dest.GetPixelData ();

		foreach (var rect in rois) {
			for (int y = rect.Top; y <= rect.Bottom; ++y) {
				int yEnd = y + 1;

				for (int x = rect.Left; x <= rect.Right; ++x) {
					RectangleI cellRect = GetCellBox (x, y, cellSize).Intersect (dest_bounds);
					ColorBgra color = ComputeCellColor (x, y, src_data, cellSize, src_bounds);

					int xEnd = Math.Min (rect.Right, cellRect.Right);
					yEnd = Math.Min (rect.Bottom, cellRect.Bottom);

					for (int y2 = y; y2 <= yEnd; ++y2) {
						var dst_row = dst_data.Slice (y2 * dest_bounds.Width, dest_bounds.Width);

						for (int x2 = x; x2 <= xEnd; ++x2)
							dst_row[x2] = color;
					}

					x = xEnd;
				}

				y = yEnd;
			}
		}
	}
	#endregion
}


public sealed class PixelateData : EffectData
{
	[Caption ("Cell Size"), MinimumValue (1), MaximumValue (100)]
	public int CellSize { get; set; } = 2;
}
