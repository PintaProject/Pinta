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

// Some methods from Paint.Net:

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

public class RecolorTool : BaseBrushTool
{
	private readonly IWorkspaceService workspace;

	private PointI? last_point = null;
	private BitMask? stencil;

	private const string TOLERANCE_SETTING = "recolor-tolerance";

	public RecolorTool (IServiceProvider services) : base (services)
	{
		workspace = services.GetService<IWorkspaceService> ();
	}

	public override string Name => Translations.GetString ("Recolor");
	public override string Icon => Pinta.Resources.Icons.ToolRecolor;
	public override string StatusBarText => Translations.GetString (
		"Left click to replace the secondary color with the primary color." +
		"\nRight click to reverse.");
	public override Gdk.Cursor DefaultCursor => Gdk.Cursor.NewFromTexture (Resources.GetIcon ("Cursor.Recolor.png"), 9, 18, null);
	public override Gdk.Key ShortcutKey => Gdk.Key.R;
	protected float Tolerance => (float) (ToleranceSlider.GetValue () / 100);
	public override int Priority => 49;

	protected override void OnBuildToolBar (Box tb)
	{
		base.OnBuildToolBar (tb);

		tb.Append (Separator);

		tb.Append (ToleranceLabel);
		tb.Append (ToleranceSlider);
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		document.Layers.ToolLayer.Clear ();
		stencil = new BitMask (document.ImageSize.Width, document.ImageSize.Height);

		base.OnMouseDown (document, e);
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		ColorBgra old_color;
		ColorBgra new_color;

		// This should have been created in OnMouseDown
		if (stencil is null)
			return;

		if (mouse_button == MouseButton.Left) {
			old_color = Palette.PrimaryColor.ToColorBgra ();
			new_color = Palette.SecondaryColor.ToColorBgra ();
		} else if (mouse_button == MouseButton.Right) {
			old_color = Palette.SecondaryColor.ToColorBgra ();
			new_color = Palette.PrimaryColor.ToColorBgra ();
		} else {
			last_point = null;
			return;
		}

		var x = e.Point.X;
		var y = e.Point.Y;

		if (!last_point.HasValue)
			last_point = new PointI (x, y);

		if (document.Workspace.PointInCanvas (e.PointDouble))
			surface_modified = true;

		var surf = document.Layers.CurrentUserLayer.Surface;
		var tmp_layer = document.Layers.ToolLayer.Surface;

		var roi = CairoExtensions.GetRectangleFromPoints (last_point.Value, new PointI (x, y), BrushWidth + 2);

		roi = workspace.ClampToImageSize (roi);
		var myTolerance = (int) (Tolerance * 256);

		tmp_layer.Flush ();

		var tmp_data = tmp_layer.GetPixelData ();
		var tmp_width = tmp_layer.Width;
		var surf_data = surf.GetReadOnlyPixelData ();
		var surf_width = surf.Width;

		// The stencil lets us know if we've already checked this
		// pixel, providing a nice perf boost
		// Maybe this should be changed to a BitVector2DSurfaceAdapter?
		for (var i = roi.X; i <= roi.Right; i++)
			for (var j = roi.Y; j <= roi.Bottom; j++) {
				if (stencil[i, j])
					continue;

				ColorBgra surf_color = surf_data[j * surf_width + i];
				if (ColorBgra.ColorsWithinTolerance (new_color, surf_color, myTolerance))
					tmp_data[j * tmp_width + i] = AdjustColorDifference (new_color, old_color, surf_color);

				stencil[i, j] = true;
			}

		tmp_layer.MarkDirty ();

		using Context g = document.CreateClippedContext ();
		g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

		g.MoveTo (last_point.Value.X, last_point.Value.Y);
		g.LineTo (x, y);

		g.LineWidth = BrushWidth;
		g.LineJoin = LineJoin.Round;
		g.LineCap = LineCap.Round;

		g.SetSourceSurface (tmp_layer, 0, 0);

		g.Stroke ();

		document.Workspace.Invalidate (roi);

		last_point = new PointI (x, y);
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		if (tolerance_slider is not null)
			settings.PutSetting (TOLERANCE_SETTING, (int) tolerance_slider.GetValue ());
	}

	#region Private PDN Methods
	private static ColorBgra AdjustColorDifference (ColorBgra oldColor, ColorBgra newColor, ColorBgra basisColor)
	{
		return ColorBgra.FromBgra (
			b: AdjustColorByte (oldColor.B, newColor.B, basisColor.B),
			g: AdjustColorByte (oldColor.G, newColor.G, basisColor.G),
			r: AdjustColorByte (oldColor.R, newColor.R, basisColor.R),
			a: basisColor.A
		);
	}

	private static byte AdjustColorByte (byte oldByte, byte newByte, byte basisByte)
	{
		if (oldByte > newByte)
			return Utility.ClampToByte (basisByte - (oldByte - newByte));
		else
			return Utility.ClampToByte (basisByte + (newByte - oldByte));
	}
	#endregion

	private Label? tolerance_label;
	private Scale? tolerance_slider;
	private Separator? separator;

	private Label ToleranceLabel => tolerance_label ??= Label.New (string.Format ("  {0}: ", Translations.GetString ("Tolerance")));
	private Scale ToleranceSlider => tolerance_slider ??= GtkExtensions.CreateToolBarSlider (0, 100, 1, Settings.GetSetting (TOLERANCE_SETTING, 50));
	private Separator Separator => separator ??= GtkExtensions.CreateToolBarSeparator ();
}
