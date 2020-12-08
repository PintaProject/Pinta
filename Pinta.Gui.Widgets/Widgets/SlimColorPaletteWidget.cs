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

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem (true)]
	public class SlimColorPaletteWidget : Gtk.DrawingArea
	{
		private Rectangle primary_rect = new Rectangle (3, 3, 24, 24);
		private Rectangle secondary_rect = new Rectangle (16, 16, 24, 24);
		private Rectangle swap_rect = new Rectangle (26, 2, 15, 15);
		private Rectangle reset_rect = new Rectangle (1, 27, 15, 15);

        const int primarySecondaryAreaSize = 35;
        const int swatchSize = 19;
        const int swatchAreaMargin = 2;
        const int swatchLeftMargin = 12;

        OrientationEnum orientation;

        const int defaultNumOfJRows = 2;
        int jRows;
        int iRows;

		private Gdk.Pixbuf swap_icon;
		private Gdk.Pixbuf reset_icon;
		private Palette palette;

        enum OrientationEnum
        {
            Horizontal,
            Vertical
        }

		public SlimColorPaletteWidget ()
		{
			// Insert initialization code here.
			this.AddEvents ((int)Gdk.EventMask.ButtonPressMask);
			
			swap_icon = PintaCore.Resources.GetIcon ("ColorPalette.SwapIcon.png");
			reset_icon = PintaCore.Resources.GetIcon ("ColorPalette.ResetIcon.png");
			palette = PintaCore.Palette.CurrentPalette;

			HasTooltip = true;
			QueryTooltip += HandleQueryTooltip;
		}

		public void Initialize ()
		{
			PintaCore.Palette.PrimaryColorChanged += new EventHandler (Palette_ColorChanged);
			PintaCore.Palette.SecondaryColorChanged += new EventHandler (Palette_ColorChanged);
			PintaCore.Palette.CurrentPalette.PaletteChanged += new EventHandler (Palette_ColorChanged);
		}
		
		private void Palette_ColorChanged (object sender, EventArgs e)
		{
			// Color change events may be received while the widget is minimized,
			// so we only call Invalidate() if the widget is shown.
			if (IsRealized)
			{
				Window.Invalidate ();
			}
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			if (swap_rect.ContainsPoint (ev.X, ev.Y)) {
				Color temp = PintaCore.Palette.PrimaryColor;
				PintaCore.Palette.PrimaryColor = PintaCore.Palette.SecondaryColor;
				PintaCore.Palette.SecondaryColor = temp;
				Window.Invalidate ();
			} else if (reset_rect.ContainsPoint (ev.X, ev.Y)) {
				PintaCore.Palette.PrimaryColor = new Color (0, 0, 0);
				PintaCore.Palette.SecondaryColor = new Color (1, 1, 1);
				Window.Invalidate ();
			}

			if (primary_rect.ContainsPoint (ev.X, ev.Y)) {
                using (var ccd = new Gtk.ColorChooserDialog(Translations.GetString("Choose Primary Color"), PintaCore.Chrome.MainWindow))
                {
					ccd.UseAlpha = true;
					ccd.Rgba = PintaCore.Palette.PrimaryColor.ToGdkRGBA();

					var response = (Gtk.ResponseType)ccd.Run();
					if (response == Gtk.ResponseType.Ok)
						PintaCore.Palette.PrimaryColor = ccd.Rgba.ToCairoColor();
				}
			} else if (secondary_rect.ContainsPoint (ev.X, ev.Y)) {
				using (var ccd = new Gtk.ColorChooserDialog(Translations.GetString("Choose Secondary Color"), PintaCore.Chrome.MainWindow))
				{
					ccd.UseAlpha = true;
					ccd.Rgba = PintaCore.Palette.SecondaryColor.ToGdkRGBA();

					var response = (Gtk.ResponseType)ccd.Run();
					if (response == Gtk.ResponseType.Ok)
						PintaCore.Palette.SecondaryColor = ccd.Rgba.ToCairoColor();
				}
			}
			
			int pal = PointToPalette ((int)ev.X, (int)ev.Y);
			
			if (pal >= 0) {
				if (ev.Button == 3)
					PintaCore.Palette.SecondaryColor = palette[pal];
				else if (ev.Button == 1)
					PintaCore.Palette.PrimaryColor = palette[pal];
				else {
					using (var ccd = new Gtk.ColorChooserDialog(Translations.GetString("Choose Palette Color"), PintaCore.Chrome.MainWindow))
					{
						ccd.UseAlpha = true;
						ccd.Rgba = palette[pal].ToGdkRGBA();

						var response = (Gtk.ResponseType)ccd.Run();
						if (response == Gtk.ResponseType.Ok)
							palette[pal] = ccd.Rgba.ToCairoColor();
					}
				}
				
				Window.Invalidate ();	
			}
				
			// Insert button press handling code here.
			return base.OnButtonPressEvent (ev);
		}

        protected override bool OnDrawn(Context g)
        {
			base.OnDrawn (g);

            // Draw Primary / Secondary Area

            g.FillRectangle (secondary_rect, PintaCore.Palette.SecondaryColor);
        
            g.DrawRectangle (new Rectangle (secondary_rect.X + 1, secondary_rect.Y + 1, secondary_rect.Width - 2, secondary_rect.Height - 2), new Color (1, 1, 1), 1);
            g.DrawRectangle (secondary_rect, new Color (0, 0, 0), 1);

            g.FillRectangle (primary_rect, PintaCore.Palette.PrimaryColor);
            g.DrawRectangle (new Rectangle (primary_rect.X + 1, primary_rect.Y + 1, primary_rect.Width - 2, primary_rect.Height - 2), new Color (1, 1, 1), 1);
            g.DrawRectangle (primary_rect, new Color (0, 0, 0), 1);

            g.DrawPixbuf (swap_icon, swap_rect.Location ());
            g.DrawPixbuf (reset_icon, reset_rect.Location ());

            // Draw color swatches

            int startI = primarySecondaryAreaSize;
            int startJ = swatchAreaMargin;

            int paletteIndex = 0;
            for (int jRow = 0; jRow < jRows; jRow++)
            {
                for (int iRow = 0; iRow < iRows; iRow++)
                {
                    if (paletteIndex >= palette.Count)
                        break;

                    int x = (orientation == OrientationEnum.Horizontal) ? startI + iRow * swatchSize : startJ + jRow * swatchSize;
                    int y = (orientation == OrientationEnum.Horizontal) ? startJ + jRow * swatchSize : startI + iRow * swatchSize;

                    g.FillRectangle(new Rectangle(x + swatchLeftMargin, y, swatchSize, swatchSize), palette[paletteIndex]);

                    paletteIndex++;
                }
            }
			
			return true;
		}

   //     protected override void OnGetPreferredWidth(out int minimum_width, out int natural_width)
   //     {
			//minimum_width = natural_width = primarySecondaryAreaSize;
   //     }

        protected override void OnGetPreferredHeight(out int minimum_height, out int natural_height)
        {
			minimum_height = natural_height = primarySecondaryAreaSize;
        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);

            int iSpaceAvailable, jSpaceAvailable;

            // The orientation is horizontal when the widget is wider than it is tall
            // The direction of 'i' is the horizontal (left-to-right) when
            // widget orientation is horizontal,
            // and vertical (top-to-bottom) when widget orientation is vertical.
            // 'j' is in the other direction.
            if (Allocation.Width > Allocation.Height)
            {
                orientation = OrientationEnum.Horizontal;
                iSpaceAvailable = Allocation.Width;
                jSpaceAvailable = Allocation.Height;
            }
            else
            {
                orientation = OrientationEnum.Vertical;
                iSpaceAvailable = Allocation.Height;
                jSpaceAvailable = Allocation.Width;
            }

            iSpaceAvailable -= primarySecondaryAreaSize + swatchAreaMargin;
            jSpaceAvailable -= 2 * swatchAreaMargin;

            // Determine max number of rows that can be displayed in available area
            int maxPossibleIRows = Math.Max(1, iSpaceAvailable / swatchSize);
            int maxPossibleJRows = Math.Max(1, jSpaceAvailable / swatchSize);

            int iRowsWithDefaultNumOfJRows = (int)Math.Ceiling((double)palette.Count / defaultNumOfJRows);

            // Display palette in default configuration if space is available
            // (it looks better)
            if ((maxPossibleJRows >= defaultNumOfJRows) && (maxPossibleIRows >= iRowsWithDefaultNumOfJRows))
            {
                iRows = iRowsWithDefaultNumOfJRows;
                jRows = defaultNumOfJRows;
            }
            else // Compress palette to fit within available space
            {
                iRows = (int)Math.Ceiling((double)palette.Count / maxPossibleJRows);
                jRows = (int)Math.Ceiling((double)palette.Count / iRows);
            }

            //iRows = 15;
        }
		
		private int PointToPalette (int x, int y)
		{
            int i, j;

            if (orientation == OrientationEnum.Horizontal)
            {
                i = x;
                j = y;
            }
            else
            {
                i = y;
                j = x;
            }

            i -= primarySecondaryAreaSize;
            j -= swatchAreaMargin;

            i -= swatchLeftMargin;

            // Determine the swatch position under the mouse pointer.
            int iRow = i / swatchSize;
            int jRow = j / swatchSize;

            if ((i < 0) || (iRow >= iRows) || (j < 0) || (jRow >= jRows))
            { // Mouse pointer is outside of swatch area
                return -1;
            }
            else if ((jRow * iRows + iRow) >= palette.Count)
            { // Mouse pointer is within swatch area, but not on valid color
                return -1;
            }
            else
            { // Return the palette color number under the mouse pointer
                return jRow * iRows + iRow;
            }
		}

		/// <summary>
		/// Provide a custom tooltip based on the cursor location.
		/// </summary>
		private void HandleQueryTooltip (object o, Gtk.QueryTooltipArgs args)
		{
			int x = args.X;
			int y = args.Y;
			string text = null;

			if (swap_rect.ContainsPoint (x, y)) {
				text = string.Format (
					"{0} {1}: {2}", 
					Translations.GetString ("Click to switch between primary and secondary color."), 
					Translations.GetString ("Shortcut key"), 
					"X"
				);
			} else if (reset_rect.ContainsPoint (x, y)) {
				text = Translations.GetString ("Click to reset primary and secondary color.");
			} else if (primary_rect.ContainsPoint (x, y)) {
				text = Translations.GetString ("Click to select primary color.");
			} else if (secondary_rect.ContainsPoint (x, y)) {
				text = Translations.GetString ("Click to select secondary color.");
			} else if (PointToPalette (x, y) >= 0) {
				text = Translations.GetString ("Left click to set primary color. Right click to set secondary color. Middle click to choose palette color.");
			}

			args.Tooltip.Text = text;
			args.RetVal = (text != null);
		}
	}
}
