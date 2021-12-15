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

namespace Pinta.Tools
{
	public class RecolorTool : BaseBrushTool
	{
		private readonly IWorkspaceService workspace;

		private Point last_point = point_empty;
		private BitMask? stencil;

		private const string TOLERANCE_SETTING = "recolor-tolerance";

		public RecolorTool (IServiceManager services) : base (services)
		{
			workspace = services.GetService<IWorkspaceService> ();
		}

		public override string Name => Translations.GetString ("Recolor");
		public override string Icon => Pinta.Resources.Icons.ToolRecolor;
		public override string StatusBarText => Translations.GetString ("Left click to replace the secondary color with the primary color. " +
							  "Right click to reverse.");
		public override Gdk.Cursor DefaultCursor => new Gdk.Cursor (Gdk.Display.Default, Resources.GetIcon ("Cursor.Recolor.png"), 9, 18);
		public override Gdk.Key ShortcutKey => Gdk.Key.R;
		protected float Tolerance => (float) (ToleranceSlider.Slider.Value / 100);
		public override int Priority => 49;

		protected override void OnBuildToolBar (Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			tb.AppendItem (Separator);

			tb.AppendItem (ToleranceLabel);
			tb.AppendItem (ToleranceSlider);
		}

		protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
		{
			document.Layers.ToolLayer.Clear ();
			stencil = new BitMask (document.ImageSize.Width, document.ImageSize.Height);

			base.OnMouseDown (document, e);
		}

		protected unsafe override void OnMouseMove (Document document, ToolMouseEventArgs e)
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
				last_point = point_empty;
				return;
			}

			var x = e.Point.X;
			var y = e.Point.Y;

			if (last_point.Equals (point_empty))
				last_point = new Point (x, y);

			if (document.Workspace.PointInCanvas (e.PointDouble))
				surface_modified = true;

			var surf = document.Layers.CurrentUserLayer.Surface;
			var tmp_layer = document.Layers.ToolLayer.Surface;

			var roi = CairoExtensions.GetRectangleFromPoints (last_point, new Point (x, y), BrushWidth + 2);

			roi = workspace.ClampToImageSize (roi);
			var myTolerance = (int) (Tolerance * 256);

			tmp_layer.Flush ();

			var tmp_data_ptr = (ColorBgra*) tmp_layer.DataPtr;
			var tmp_width = tmp_layer.Width;
			var surf_data_ptr = (ColorBgra*) surf.DataPtr;
			var surf_width = surf.Width;

			// The stencil lets us know if we've already checked this
			// pixel, providing a nice perf boost
			// Maybe this should be changed to a BitVector2DSurfaceAdapter?
			for (var i = roi.X; i <= roi.GetRight (); i++)
				for (var j = roi.Y; j <= roi.GetBottom (); j++) {
					if (stencil[i, j])
						continue;

					if (ColorBgra.ColorsWithinTolerance (new_color, surf.GetColorBgraUnchecked (surf_data_ptr, surf_width, i, j), myTolerance))
						*tmp_layer.GetPointAddressUnchecked (tmp_data_ptr, tmp_width, i, j) = AdjustColorDifference (new_color, old_color, surf.GetColorBgraUnchecked (surf_data_ptr, surf_width, i, j));

					stencil[i, j] = true;
				}

			tmp_layer.MarkDirty ();

			using (var g = document.CreateClippedContext ()) {
				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				g.MoveTo (last_point.X, last_point.Y);
				g.LineTo (x, y);

				g.LineWidth = BrushWidth;
				g.LineJoin = LineJoin.Round;
				g.LineCap = LineCap.Round;

				g.SetSource (tmp_layer);

				g.Stroke ();
			}

			document.Workspace.Invalidate (roi);

			last_point = new Point (x, y);
		}

		protected override void OnSaveSettings (ISettingsService settings)
		{
			base.OnSaveSettings (settings);

			if (tolerance_slider is not null)
				settings.PutSetting (TOLERANCE_SETTING, (int) tolerance_slider.Slider.Value);
		}

		#region Private PDN Methods
		private static ColorBgra AdjustColorDifference (ColorBgra oldColor, ColorBgra newColor, ColorBgra basisColor)
		{
			ColorBgra returnColor;

			// eliminate testing for the "equal to" case
			returnColor = basisColor;

			returnColor.B = AdjustColorByte (oldColor.B, newColor.B, basisColor.B);
			returnColor.G = AdjustColorByte (oldColor.G, newColor.G, basisColor.G);
			returnColor.R = AdjustColorByte (oldColor.R, newColor.R, basisColor.R);

			return returnColor;
		}

		private static byte AdjustColorByte (byte oldByte, byte newByte, byte basisByte)
		{
			if (oldByte > newByte)
				return Utility.ClampToByte (basisByte - (oldByte - newByte));
			else
				return Utility.ClampToByte (basisByte + (newByte - oldByte));
		}
		#endregion

		private ToolBarLabel? tolerance_label;
		private ToolBarSlider? tolerance_slider;
		private SeparatorToolItem? separator;

		private ToolBarLabel ToleranceLabel => tolerance_label ??= new ToolBarLabel (string.Format ("  {0}: ", Translations.GetString ("Tolerance")));
		private ToolBarSlider ToleranceSlider => tolerance_slider ??= new ToolBarSlider (0, 100, 1, Settings.GetSetting (TOLERANCE_SETTING, 50));
		private SeparatorToolItem Separator => separator ??= new SeparatorToolItem ();
	}
}
