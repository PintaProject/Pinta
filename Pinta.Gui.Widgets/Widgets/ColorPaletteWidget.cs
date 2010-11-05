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
using Cairo;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem (true)]
	public class ColorPaletteWidget : Gtk.DrawingArea
	{
		private Rectangle primary_rect = new Rectangle (7, 7, 30, 30);
		private Rectangle secondary_rect = new Rectangle (22, 22, 30, 30);
		private Rectangle swap_rect = new Rectangle (37, 6, 15, 15);
		
		private Gdk.Pixbuf swap_icon;
		private Palette palette;
		
		public ColorPaletteWidget ()
		{
			// Insert initialization code here.
			this.AddEvents ((int)Gdk.EventMask.ButtonPressMask);
			
			swap_icon = PintaCore.Resources.GetIcon ("ColorPalette.SwapIcon.png");
			palette = PintaCore.Palette.CurrentPalette;
		}

		public void Initialize ()
		{
			PintaCore.Palette.PrimaryColorChanged += new EventHandler (Palette_ColorChanged);
			PintaCore.Palette.SecondaryColorChanged += new EventHandler (Palette_ColorChanged);
			PintaCore.Palette.CurrentPalette.PaletteChanged += new EventHandler (Palette_ColorChanged);
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
				Gtk.ColorSelectionDialog csd = new Gtk.ColorSelectionDialog (Catalog.GetString ("Choose Primary Color"));
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
				Gtk.ColorSelectionDialog csd = new Gtk.ColorSelectionDialog (Catalog.GetString ("Choose Secondary Color"));
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
				else if (ev.Button == 1)
					PintaCore.Palette.PrimaryColor = palette[pal];
				else {
					Gtk.ColorSelectionDialog csd = new Gtk.ColorSelectionDialog (Catalog.GetString ("Choose Palette Color"));
					csd.ColorSelection.PreviousColor = palette[pal].ToGdkColor ();
					csd.ColorSelection.CurrentColor = palette[pal].ToGdkColor ();
					csd.ColorSelection.CurrentAlpha = palette[pal].GdkColorAlpha ();
					csd.ColorSelection.HasOpacityControl = true;

					int response = csd.Run ();

					if (response == (int)Gtk.ResponseType.Ok) {
						palette[pal] = csd.ColorSelection.GetCairoColor ();
					}
					
					csd.Destroy ();
				}
				
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
				int roundedCount = (palette.Count % 3 == 0) ?
					palette.Count : palette.Count + 3 - (palette.Count % 3);
				
				for (int i = 0; i < palette.Count; i++) {
					int x = 7 + 15 * (i / (roundedCount / 3));
					int y = 60 +15 * (i % (roundedCount / 3));
					
					g.FillRectangle (new Rectangle (x, y, 15, 15), palette[i]);
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
			int roundedCount = (palette.Count % 3 == 0) ?
					palette.Count : palette.Count + 3 - (palette.Count % 3);
			
			if (x >= 7 && x < 22)
				col = 0;
			else if (x >= 22 && x < 38)
				col = roundedCount / 3;
			else if (x >= 38 && x < 54)
				col = (roundedCount / 3) * 2;
			else
				return -1;
			
			if (y < 60 || y > 60 + ((roundedCount / 3) * 15))
				return -1;
			
			row = (y - 60) / 15;
			
			return (col + row >= palette.Count) ? -1 : col + row;
		}
	}
}
