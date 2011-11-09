﻿// 
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
using Cairo;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Tools
{
	public class ColorPickerTool : BaseTool
	{
		private int button_down = 0;

		private ToolBarComboBox tool_select;
		private ToolBarLabel tool_select_label;

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
			get { return new Gdk.Cursor (PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon ("Cursor.ColorPicker.png"), 1, 16); }
		}
		public override Gdk.Key ShortcutKey {
			get { return Gdk.Key.K; }
		}
		public override int Priority {
			get { return 31; }
		}
		#endregion

		#region ToolBar
		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			if (tool_select_label == null)
				tool_select_label = new ToolBarLabel (string.Format (" {0}: ", Catalog.GetString ("After select")));

			tb.AppendItem (tool_select_label);

			// TODO: Enable when we have the Pencil tool
			if (tool_select == null)
				tool_select = new ToolBarComboBox (170, 0, false, Catalog.GetString ("Do not switch tool"), Catalog.GetString ("Switch to previous tool"), Catalog.GetString ("Switch to Pencil tool"));

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

			Color color = doc.CurrentLayer.Surface.GetPixel ((int)point.X, (int)point.Y);

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

			Color color = doc.CurrentLayer.Surface.GetPixel ((int)point.X, (int)point.Y);

			if (button_down == 1)
				PintaCore.Palette.PrimaryColor = color;
			else if (button_down == 3)
				PintaCore.Palette.SecondaryColor = color;
		}
		
		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, PointD point)
		{
			button_down = 0;
			
			if (tool_select.ComboBox.Active == 1)
				PintaCore.Tools.SetCurrentTool (PintaCore.Tools.PreviousTool);
			else if (tool_select.ComboBox.Active == 2)
				PintaCore.Tools.SetCurrentTool (Catalog.GetString ("Pencil"));
		}
		#endregion
	}
}
