// 
// ColorPanelWidget.cs
//  
// Author:
//      Krzysztof Marecki <marecki.krzysztof@gmail.com>
// 
// Copyright (c) 2010 Krzysztof Marecki
//
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

namespace Pinta
{


	[System.ComponentModel.ToolboxItem(true)]
	public partial class ColorPanelWidget : Gtk.Bin
	{
		public Color CairoColor { get; private set; }

		public ColorPanelWidget ()
		{
			this.Build ();
			
			ExposeEvent += HandleExposeEvent;
		}
		
		public void SetCairoColor (Color color)
		{
			CairoColor = color;
		}

		private void HandleExposeEvent (object o, Gtk.ExposeEventArgs args)
		{
			using (Context context = Gdk.CairoHelper.Create (this.GdkWindow)) {
				
				int rad = 4;
				Rectangle rect = Allocation.ToCairoRectangle ();
				
				//clipping rounded rectangle
				context.MoveTo(rect.X, rect.Y + rad);
				context.Arc (rect.X + rad, rect.Y + rad, rad, 180, 225);
				//due rounding error, arc ends on rect.Y + rad + 1
				context.LineTo (rect.X + rad, rect.Y);
				context.LineTo (rect.X + rect.Width - rad, rect.Y);
				context.Arc (rect.X + rect.Width - rad, rect.Y + rad, rad, 225, 270);
				context.LineTo (rect.X + rect.Width, rect.Y + rad);
				context.LineTo (rect.X + rect.Width, rect.Y + rect.Height - rad);
				context.Arc (rect.X + rect.Width - rad, rect.Y + rect.Height - rad, rad, 270, 315);
				//due rounding error, arc ends on rect.Y + rect.Height - 1
				context.LineTo (rect.X + rect.Width - rad, rect.Y + rect.Height);
				context.LineTo (rect.X + rad, rect.Y + rect.Height);
				context.Arc (rect.X + rad, rect.Y + rect.Height - rad, rad, 315, 360);
				context.LineTo (rect.X, rect.Y + rect.Height - rad);
				context.LineTo (rect.X, rect.Y + rad);
				context.Clip();
				
				context.Color = CairoColor;
				context.Rectangle (Allocation.ToCairoRectangle ());
				context.Fill();
			}
		}
	}
}
