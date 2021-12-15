// 
// FreeformShapeTool.cs
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
using Pinta.Core;

namespace Pinta.Tools
{
	public class FreeformShapeTool : BaseBrushTool
	{
		private Point last_point = point_empty;

		private Path? path;
		private Color fill_color;
		private Color outline_color;

		private DashPatternBox dashPBox = new DashPatternBox ();

		private string dash_pattern = "-";

		private const string FILL_TYPE_SETTING = "freeform-shape-fill-type";
		private const string DASH_PATTERN_SETTING = "freeform-shape-dash_pattern";

		public FreeformShapeTool (IServiceManager services) : base (services)
		{
		}

		public override string Name => Translations.GetString ("Freeform Shape");
		public override string Icon => Pinta.Resources.Icons.ToolFreeformShape;
		public override string StatusBarText => Translations.GetString ("Left click to draw with primary color, right click to draw with secondary color.");
		public override Gdk.Cursor DefaultCursor => new Gdk.Cursor (Gdk.Display.Default, Resources.GetIcon ("Cursor.FreeformShape.png"), 9, 18);
		public override Gdk.Key ShortcutKey => Gdk.Key.O;
		public override int Priority => 45;

		protected override void OnBuildToolBar (Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			tb.AppendItem (Separator);
			tb.AppendItem (FillLabel);
			tb.AppendItem (FillDropDown);

			// TODO: This could be cleaner.
			// This will only return an item on the first setup so we only add the handler once.
			var dash_pattern_box = dashPBox.SetupToolbar (tb);

			if (dash_pattern_box != null) {
				dash_pattern_box.Entry.Text = Settings.GetSetting (DASH_PATTERN_SETTING, "-");

				dash_pattern_box.Changed += (o, e) => {
					dash_pattern = dash_pattern_box.ActiveText;
				};
			}
		}

		protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
		{
			surface_modified = false;
			undo_surface = document.Layers.CurrentUserLayer.Surface.Clone ();
			path = null;

			document.Layers.ToolLayer.Clear ();
			document.Layers.ToolLayer.Hidden = false;
		}

		protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
		{
			if (e.IsLeftMousePressed) {
				outline_color = Palette.PrimaryColor;
				fill_color = Palette.SecondaryColor;
			} else if (e.IsRightMousePressed) {
				outline_color = Palette.SecondaryColor;
				fill_color = Palette.PrimaryColor;
			} else {
				last_point = point_empty;
				return;
			}

			var x = e.Point.X;
			var y = e.Point.Y;

			if (last_point.Equals (point_empty)) {
				last_point = e.Point;
				return;
			}

			if (document.Workspace.PointInCanvas (e.PointDouble))
				surface_modified = true;

			document.Layers.ToolLayer.Clear ();

			using (var g = document.CreateClippedToolContext ()) {
				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				g.SetDash (DashPatternBox.GenerateDashArray (dash_pattern, BrushWidth), 0.0);

				if (path != null) {
					g.AppendPath (path);
					path.Dispose ();
				} else {
					g.MoveTo (x, y);
				}

				g.LineTo (x, y);

				path = g.CopyPath ();

				g.ClosePath ();
				g.LineWidth = BrushWidth;
				g.FillRule = FillRule.EvenOdd;

				if (FillShape && StrokeShape) {
					g.SetSourceColor (fill_color);
					g.FillPreserve ();
					g.SetSourceColor (outline_color);
					g.Stroke ();
				} else if (FillShape) {
					g.SetSourceColor (outline_color);
					g.FillPreserve ();
					g.SetSourceColor (outline_color);
					g.Stroke ();
				} else {
					g.SetSourceColor (outline_color);
					g.Stroke ();
				}
			}

			document.Workspace.Invalidate ();

			last_point = new Point (x, y);
		}

		protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
		{
			document.Layers.ToolLayer.Clear ();
			document.Layers.ToolLayer.Hidden = true;

			using (Context g = document.CreateClippedContext ()) {
				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				g.SetDash (DashPatternBox.GenerateDashArray (dash_pattern, BrushWidth), 0.0);

				if (path != null) {
					g.AppendPath (path);
					path.Dispose ();
					path = null;
				}

				g.ClosePath ();
				g.LineWidth = BrushWidth;
				g.FillRule = FillRule.EvenOdd;

				if (FillShape && StrokeShape) {
					g.SetSourceColor (fill_color);
					g.FillPreserve ();
					g.SetSourceColor (outline_color);
					g.Stroke ();
				} else if (FillShape) {
					g.SetSourceColor (outline_color);
					g.FillPreserve ();
					g.SetSourceColor (outline_color);
					g.Stroke ();
				} else {
					g.SetSourceColor (outline_color);
					g.Stroke ();
				}
			}

			if (surface_modified && undo_surface != null)
				document.History.PushNewItem (new SimpleHistoryItem (Icon, Name, undo_surface, document.Layers.CurrentUserLayerIndex));
			else if (undo_surface != null)
				undo_surface.Dispose ();

			surface_modified = false;

			document.Workspace.Invalidate ();
		}

		protected override void OnSaveSettings (ISettingsService settings)
		{
			base.OnSaveSettings (settings);

			if (fill_button is not null)
				settings.PutSetting (FILL_TYPE_SETTING, fill_button.SelectedIndex);
			if (dashPBox?.comboBox is not null)
				settings.PutSetting (DASH_PATTERN_SETTING, dashPBox.comboBox.ComboBox.ActiveText);
		}

		private bool StrokeShape => FillDropDown.SelectedItem.GetTagOrDefault (0) % 2 == 0;
		private bool FillShape => FillDropDown.SelectedItem.GetTagOrDefault (0) >= 1;

		private ToolBarLabel? fill_label;
		private ToolBarDropDownButton? fill_button;
		private SeparatorToolItem? fill_sep;

		private SeparatorToolItem Separator => fill_sep ??= new SeparatorToolItem ();
		private ToolBarLabel FillLabel => fill_label ??= new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Fill Style")));
		private ToolBarDropDownButton FillDropDown {
			get {
				if (fill_button == null) {
					fill_button = new ToolBarDropDownButton ();

					fill_button.AddItem (Translations.GetString ("Outline Shape"), Pinta.Resources.Icons.FillStyleOutline, 0);
					fill_button.AddItem (Translations.GetString ("Fill Shape"), Pinta.Resources.Icons.FillStyleFill, 1);
					fill_button.AddItem (Translations.GetString ("Fill and Outline Shape"), Pinta.Resources.Icons.FillStyleOutlineFill, 2);

					fill_button.SelectedIndex = Settings.GetSetting (FILL_TYPE_SETTING, 0);
				}

				return fill_button;
			}
		}
	}
}
