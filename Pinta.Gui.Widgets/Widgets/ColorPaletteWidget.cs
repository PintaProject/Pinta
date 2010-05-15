// 
// ColorPaletteWidget.cs
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
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem (true)]
	public class ColorPaletteWidget : Gtk.DrawingArea
	{
		private Rectangle primary_rect = new Rectangle (7, 7, 30, 30);
		private Rectangle secondary_rect = new Rectangle (22, 22, 30, 30);
		private Rectangle swap_rect = new Rectangle (37, 6, 15, 15);
		
		private Gdk.Pixbuf swap_icon;
		
		private List<Color> palette;
		
		public ColorPaletteWidget ()
		{
			// Insert initialization code here.
			this.AddEvents ((int)Gdk.EventMask.ButtonPressMask);
			
			swap_icon = PintaCore.Resources.GetIcon ("ColorPalette.SwapIcon.png");
			palette = new List<Color> ();

			palette.Add (new Color (255 / 255f, 255 / 255f, 255 / 255f));
			palette.Add (new Color (128 / 255f, 128 / 255f, 128 / 255f));			
			palette.Add (new Color (127 / 255f, 0 / 255f, 0 / 255f));			
			palette.Add (new Color (127 / 255f, 51 / 255f, 0 / 255f));			
			palette.Add (new Color (127 / 255f, 106 / 255f, 0 / 255f));			
			palette.Add (new Color (91 / 255f, 127 / 255f, 0 / 255f));			
			palette.Add (new Color (38 / 255f, 127 / 255f, 0 / 255f));			
			palette.Add (new Color (0 / 255f, 127 / 255f, 14 / 255f));			
			palette.Add (new Color (0 / 255f, 127 / 255f, 70 / 255f));			
			palette.Add (new Color (0 / 255f, 127 / 255f, 127 / 255f));			
			palette.Add (new Color (0 / 255f, 74 / 255f, 127 / 255f));			
			palette.Add (new Color (0 / 255f, 19 / 255f, 127 / 255f));			
			palette.Add (new Color (33 / 255f, 0 / 255f, 127 / 255f));			
			palette.Add (new Color (87 / 255f, 0 / 255f, 127 / 255f));			
			palette.Add (new Color (127 / 255f, 0 / 255f, 110 / 255f));			
			palette.Add (new Color (127 / 255f, 0 / 255f, 55 / 255f));	
			
			palette.Add (new Color (0 / 255f, 0 / 255f, 0 / 255f));
			palette.Add (new Color (64 / 255f, 64 / 255f, 64 / 255f));			
			palette.Add (new Color (255 / 255f, 0 / 255f, 0 / 255f));			
			palette.Add (new Color (255 / 255f, 106 / 255f, 0 / 255f));			
			palette.Add (new Color (255 / 255f, 216 / 255f, 0 / 255f));			
			palette.Add (new Color (182 / 255f, 255 / 255f, 0 / 255f));			
			palette.Add (new Color (76 / 255f, 255 / 255f, 0 / 255f));			
			palette.Add (new Color (0 / 255f, 255 / 255f, 33 / 255f));			
			palette.Add (new Color (0 / 255f, 255 / 255f, 144 / 255f));			
			palette.Add (new Color (0 / 255f, 255 / 255f, 255 / 255f));			
			palette.Add (new Color (0 / 255f, 148 / 255f, 255 / 255f));			
			palette.Add (new Color (0 / 255f, 38 / 255f, 255 / 255f));			
			palette.Add (new Color (72 / 255f, 0 / 255f, 255 / 255f));			
			palette.Add (new Color (178 / 255f, 0 / 255f, 255 / 255f));			
			palette.Add (new Color (255 / 255f, 0 / 255f, 220 / 255f));			
			palette.Add (new Color (255 / 255f, 0 / 255f, 110 / 255f));			

			palette.Add (new Color (160 / 255f, 160 / 255f, 160 / 255f));
			palette.Add (new Color (48 / 255f, 48 / 255f, 48 / 255f));			
			palette.Add (new Color (255 / 255f, 127 / 255f, 127 / 255f));			
			palette.Add (new Color (255 / 255f, 178 / 255f, 127 / 255f));			
			palette.Add (new Color (255 / 255f, 233 / 255f, 127 / 255f));			
			palette.Add (new Color (218 / 255f, 255 / 255f, 127 / 255f));			
			palette.Add (new Color (165 / 255f, 255 / 255f, 127 / 255f));			
			palette.Add (new Color (127 / 255f, 255 / 255f, 142 / 255f));			
			palette.Add (new Color (127 / 255f, 255 / 255f, 197 / 255f));			
			palette.Add (new Color (127 / 255f, 255 / 255f, 255 / 255f));			
			palette.Add (new Color (127 / 255f, 201 / 255f, 255 / 255f));			
			palette.Add (new Color (127 / 255f, 146 / 255f, 255 / 255f));			
			palette.Add (new Color (161 / 255f, 127 / 255f, 255 / 255f));			
			palette.Add (new Color (214 / 255f, 127 / 255f, 255 / 255f));			
			palette.Add (new Color (255 / 255f, 127 / 255f, 237 / 255f));			
			palette.Add (new Color (255 / 255f, 127 / 255f, 182 / 255f));
		}

		public void Initialize ()
		{
			PintaCore.Palette.PrimaryColorChanged += new EventHandler (Palette_ColorChanged);
			PintaCore.Palette.SecondaryColorChanged += new EventHandler (Palette_ColorChanged);
		}
		
		private void Palette_ColorChanged (object sender, EventArgs e)
		{
			GdkWindow.Invalidate ();
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			if (swap_rect.ContainsPoint (ev.X, ev.Y)) {
				Color temp = PintaCore.Palette.PrimaryColor;
				PintaCore.Palette.PrimaryColor = PintaCore.Palette.SecondaryColor;
				PintaCore.Palette.SecondaryColor = temp;
				GdkWindow.Invalidate ();
			}

			if (primary_rect.ContainsPoint (ev.X, ev.Y)) {
				Gtk.ColorSelectionDialog csd = new Gtk.ColorSelectionDialog ("Choose Primary Color");
				csd.ColorSelection.PreviousColor = PintaCore.Palette.PrimaryColor.ToGdkColor ();
				csd.ColorSelection.CurrentColor = PintaCore.Palette.PrimaryColor.ToGdkColor ();
				csd.ColorSelection.CurrentAlpha = PintaCore.Palette.PrimaryColor.GdkColorAlpha ();
				csd.ColorSelection.HasOpacityControl = true;

				int response = csd.Run ();

				if (response == (int)Gtk.ResponseType.Ok) {
					PintaCore.Palette.PrimaryColor = csd.ColorSelection.GetCairoColor ();
				}

				csd.Destroy ();
			} else if (secondary_rect.ContainsPoint (ev.X, ev.Y)) {
				Gtk.ColorSelectionDialog csd = new Gtk.ColorSelectionDialog ("Choose Secondary Color");
				csd.ColorSelection.PreviousColor = PintaCore.Palette.SecondaryColor.ToGdkColor ();
				csd.ColorSelection.CurrentColor = PintaCore.Palette.SecondaryColor.ToGdkColor ();
				csd.ColorSelection.CurrentAlpha = PintaCore.Palette.SecondaryColor.GdkColorAlpha ();
				csd.ColorSelection.HasOpacityControl = true;

				int response = csd.Run ();

				if (response == (int)Gtk.ResponseType.Ok) {
					PintaCore.Palette.SecondaryColor = csd.ColorSelection.GetCairoColor ();
				}

				csd.Destroy ();
			}
			
			int pal = PointToPalette ((int)ev.X, (int)ev.Y);
			
			if (pal >= 0) {
				if (ev.Button == 3)
					PintaCore.Palette.SecondaryColor = palette[pal];
				else
					PintaCore.Palette.PrimaryColor = palette[pal];
				
				GdkWindow.Invalidate ();	
			}
				
			// Insert button press handling code here.
			return base.OnButtonPressEvent (ev);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);
			
			using (Context g = Gdk.CairoHelper.Create (GdkWindow)) {
				
				g.FillRectangle (secondary_rect, PintaCore.Palette.SecondaryColor);
			
				g.DrawRectangle (new Rectangle (secondary_rect.X + 1, secondary_rect.Y + 1, secondary_rect.Width - 2, secondary_rect.Height - 2), new Color (1, 1, 1), 1);
				g.DrawRectangle (secondary_rect, new Color (0, 0, 0), 1);
	
				g.FillRectangle (primary_rect, PintaCore.Palette.PrimaryColor);
				g.DrawRectangle (new Rectangle (primary_rect.X + 1, primary_rect.Y + 1, primary_rect.Width - 2, primary_rect.Height - 2), new Color (1, 1, 1), 1);
				g.DrawRectangle (primary_rect, new Color (0, 0, 0), 1);
	
				g.DrawPixbuf (swap_icon, swap_rect.Location ());
				
				// Draw swatches
				int x = 7;
				
				for (int i = 0; i < 48; i++) {
					if (i == 16 || i == 32)
						x += 15;
					
					g.FillRectangle (new Rectangle (x, 60 + ((i % 16) * 15), 15, 15), palette[i]);
				}
			}
			
			return true;
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			// Calculate desired size here.
			requisition.Height = 305;
			requisition.Width = 60;
		}
		
		private int PointToPalette (int x, int y)
		{
			int col = -1;
			int row = 0;
			
			if (x >= 7 && x < 22)
				col = 0;
			else if (x >= 22 && x < 38)
				col = 16;
			else if (x >= 38 && x < 54)
				col = 32;
			else
				return -1;
			
			if (y < 60 || y > 60 + (16 * 15))
				return -1;
			
			row = (y - 60) / 15;
			
			return col + row;			
		}
	}
}
