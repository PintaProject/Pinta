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

namespace Pinta.Gui.Widgets
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
			using (Context g = Gdk.CairoHelper.Create (this.GdkWindow)) {
				
				int rad = 4;
				Rectangle rect = Allocation.ToCairoRectangle ();
				
				g.FillRoundedRectangle (rect, rad, CairoColor);
			}
		}
	}
}
