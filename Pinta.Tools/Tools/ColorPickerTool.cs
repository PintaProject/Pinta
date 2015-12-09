// 
// ColorPickerTool.cs
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
using System.Collections.Generic;
using Cairo;
using Mono.Unix;
using Pinta.Core;

namespace Pinta.Tools
{
	public class ColorPickerTool : BaseTool
	{
		private int button_down = 0;

		private ToolBarDropDownButton tool_select;
		private ToolBarLabel tool_select_label;
		private ToolBarLabel sampling_label;
		private ToolBarDropDownButton sample_size;
		private ToolBarDropDownButton sample_type;
		private Gtk.ToolItem sample_sep;

		public ColorPickerTool ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Toolbar.Sampling.1x1.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.Sampling.1x1.png")));
			fact.Add ("Toolbar.Sampling.3x3.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.Sampling.3x3.png")));
			fact.Add ("Toolbar.Sampling.5x5.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.Sampling.5x5.png")));
			fact.Add ("Toolbar.Sampling.7x7.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.Sampling.7x7.png")));
			fact.Add ("Toolbar.Sampling.9x9.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Toolbar.Sampling.9x9.png")));
			fact.Add ("ResizeCanvas.Image.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("ResizeCanvas.Image.png")));
			fact.Add ("Tools.ColorPicker.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Tools.ColorPicker.png")));
			fact.Add ("Tools.ColorPicker.PreviousTool.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Tools.ColorPicker.PreviousTool.png")));
			fact.Add ("Tools.Pencil.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Tools.Pencil.png")));
			fact.AddDefault ();
		}

		#region Properties
		public override string Name {
			get { return Catalog.GetString ("Color Picker"); }
		}
		public override string Icon {
			get { return "Tools.ColorPicker.png"; }
		}
		public override string StatusBarText {
			get { return Catalog.GetString ("Left click to set primary color. Right click to set secondary color."); }
		}

		public override Gdk.Cursor DefaultCursor {
			get {
				int iconOffsetX, iconOffsetY;
				var icon = CreateIconWithShape ("Cursor.ColorPicker.png",
				                                CursorShape.Rectangle, SampleSize, 7, 27,
				                                out iconOffsetX, out iconOffsetY);
                return new Gdk.Cursor (Gdk.Display.Default, icon, iconOffsetX, iconOffsetY);
			}
		}
		public override bool CursorChangesOnZoom { get { return true; } }

		public override Gdk.Key ShortcutKey {
			get { return Gdk.Key.K; }
		}
		public override int Priority {
			get { return 31; }
		}
		private int SampleSize {
			get
			{
				if (sample_size != null)
				{
					return (int)sample_size.SelectedItem.Tag;
				}
				else
				{
					return 1;
				}
			}
		}
		private bool SampleLayerOnly {
			get { return (bool)sample_type.SelectedItem.Tag; }
		}
		#endregion

		#region ToolBar
		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			if (sampling_label == null)
				sampling_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("Sampling")));

			tb.AppendItem (sampling_label);

			if (sample_size == null) {
				sample_size = new ToolBarDropDownButton (true);

				// Change the cursor when the SampleSize is changed.
				sample_size.SelectedItemChanged += (sender, e) => SetCursor (DefaultCursor);

				sample_size.AddItem (Catalog.GetString ("Single Pixel"), "Toolbar.Sampling.1x1.png", 1);
				sample_size.AddItem (Catalog.GetString ("3 x 3 Region"), "Toolbar.Sampling.3x3.png", 3);
				sample_size.AddItem (Catalog.GetString ("5 x 5 Region"), "Toolbar.Sampling.5x5.png", 5);
				sample_size.AddItem (Catalog.GetString ("7 x 7 Region"), "Toolbar.Sampling.7x7.png", 7);
				sample_size.AddItem (Catalog.GetString ("9 x 9 Region"), "Toolbar.Sampling.9x9.png", 9);
			}

			tb.AppendItem (sample_size);

			if (sample_type == null) {
				sample_type = new ToolBarDropDownButton (true);

				sample_type.AddItem (Catalog.GetString ("Layer"), "Menu.Layers.MergeLayerDown.png", true);
				sample_type.AddItem (Catalog.GetString ("Image"), "ResizeCanvas.Image.png", false);
			}

			tb.AppendItem (sample_type);

			if (sample_sep == null)
				sample_sep = new Gtk.SeparatorToolItem ();

			tb.AppendItem (sample_sep);

			if (tool_select_label == null)
				tool_select_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("After select")));

			tb.AppendItem (tool_select_label);

			if (tool_select == null) {
				tool_select = new ToolBarDropDownButton (true);

				tool_select.AddItem (Catalog.GetString ("Do not switch tool"), "Tools.ColorPicker.png", 0);
				tool_select.AddItem (Catalog.GetString ("Switch to previous tool"), "Tools.ColorPicker.PreviousTool.png", 1);
				tool_select.AddItem (Catalog.GetString ("Switch to Pencil tool"), "Tools.Pencil.png", 2);
			}

			tb.AppendItem (tool_select);
		}
		#endregion
		
		#region Mouse Handlers
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			if (args.Event.Button == 1)
				button_down = 1;
			else if (args.Event.Button == 3)
				button_down = 3;

			if (!doc.Workspace.PointInCanvas (point))
				return;

			var color = GetColorFromPoint (point);

			if (button_down == 1)
				PintaCore.Palette.PrimaryColor = color;
			else if (button_down == 3)
				PintaCore.Palette.SecondaryColor = color;
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			if (button_down == 0)
				return;

			if (!doc.Workspace.PointInCanvas (point))
				return;

			var color = GetColorFromPoint (point);

			if (button_down == 1)
				PintaCore.Palette.PrimaryColor = color;
			else if (button_down == 3)
				PintaCore.Palette.SecondaryColor = color;
		}
		
		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, PointD point)
		{
			button_down = 0;
			
			if ((int)tool_select.SelectedItem.Tag == 1)
				PintaCore.Tools.SetCurrentTool(PintaCore.Tools.PreviousTool);
			else if ((int)tool_select.SelectedItem.Tag == 2)
				PintaCore.Tools.SetCurrentTool(Catalog.GetString("Pencil"));
		}
		#endregion

		#region Private Methods
		private unsafe Color GetColorFromPoint (PointD point)
		{
			var pixels = GetPixelsFromPoint (point);

		    fixed (ColorBgra* ptr = pixels)
		    {
		        var color = ColorBgra.BlendPremultiplied (ptr, pixels.Length);
		        return color.ToStraightAlpha ().ToCairoColor ();
		    }
		}

		private ColorBgra[] GetPixelsFromPoint (PointD point)
		{
			var doc = PintaCore.Workspace.ActiveDocument;
			var x = (int)point.X;
			var y = (int)point.Y;
			var size = SampleSize;
			var half = size / 2;

			// Short circuit for single pixel
			if (size == 1)
				return new ColorBgra[] { GetPixel (x, y) };

			// Find the pixels we need (clamp to the size of the image)
			var rect = new Gdk.Rectangle (x - half, y - half, size, size);
			rect.Intersect (new Gdk.Rectangle (Gdk.Point.Zero, doc.ImageSize));

			var pixels = new List<ColorBgra> ();

			for (int i = rect.Left; i <= rect.GetRight (); i++)
				for (int j = rect.Top; j <= rect.GetBottom (); j++)
					pixels.Add (GetPixel (i, j));

			return pixels.ToArray ();
		}

		private ColorBgra GetPixel (int x, int y)
		{
			if (SampleLayerOnly)
				return PintaCore.Workspace.ActiveDocument.CurrentUserLayer.Surface.GetColorBgraUnchecked (x, y);
			else
				return PintaCore.Workspace.ActiveDocument.GetComputedPixel (x, y);
		}
		#endregion
	}
}
