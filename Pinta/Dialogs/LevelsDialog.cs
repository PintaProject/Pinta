// 
// LevelsDialog.cs
//  
// Author:
//      Krzysztof Marecki <marecki.krzysztof@gmail.com>
// 
// Copyright (c) 2010 Krzysztof Marecki
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
using Gtk;
using Cairo;

using Pinta.Core;

namespace Pinta
{
	public partial class LevelsDialog : Gtk.Dialog
	{	
		private bool[] mask;
		
		public UnaryPixelOps.Level Levels { get; private set; }
		
		public LevelsDialog ()
		{
			this.Build ();
			this.Levels = new UnaryPixelOps.Level ();
			mask = new bool[] {true, true, true};
		
			this.HasSeparator = false;
			//hack allowing adding hbox with rgb checkboxes into dialog action area
			VBox.Remove(hboxBottom);
			AddActionWidget(hboxBottom,ResponseType.None);
			
			checkRed.Toggled += HandleCheckRedToggled;
			checkGreen.Toggled += HandleCheckGreenToggled;
			checkBlue.Toggled += HandleCheckBlueToggled;
			buttonCancel.Clicked += HandleButtonCancelClicked;
			buttonOk.Clicked += HandleButtonOkClicked;
			colorgradientInput.ValueChanged +=	HandleColorgradientInputValueChanged;
			colorgradientOutput.ValueChanged += HandleColorgradientOutputValueChanged;
			
			MotionNotifyEvent += HandleMotionNotifyEvent;
		}

		private void HandleColorgradientInputValueChanged (object sender, IndexEventArgs e)
		{
			int val = colorgradientInput.GetValue (e.Index);
			
			if (e.Index == 0)
				spinbuttonInLow.Value = val;
			else
				spinInHigh.Value = val;
		}
		
		private void HandleColorgradientOutputValueChanged (object sender, IndexEventArgs e)
		{
			
		}
		
		private void HandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			colorgradientInput.MotionNotify ();
			colorgradientOutput.MotionNotify ();
		}

		private void MaskChanged ()
		{
			ColorBgra max = ColorBgra.Black;

            max.Bgra |= mask[0] ? (uint)0xFF0000 : 0;
            max.Bgra |= mask[1] ? (uint)0xFF00 : 0;
            max.Bgra |= mask[2] ? (uint)0xFF : 0;
			
			Color maxcolor = max.ToCairoColor();
			colorgradientInput.MaxColor = maxcolor;
			colorgradientOutput.MaxColor = maxcolor;
			
			GdkWindow.Invalidate ();
		}
		
		private void HandleCheckRedToggled (object sender, EventArgs e)
		{
			mask [0] = checkRed.Active;
			MaskChanged();
		}
		
		private void HandleCheckGreenToggled (object sender, EventArgs e)
		{
			mask [1] = checkGreen.Active;
			MaskChanged ();
		}

		private void HandleCheckBlueToggled (object sender, EventArgs e)
		{
			mask [2] = checkBlue.Active;
			MaskChanged ();
		}
		
		private void HandleButtonOkClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Ok);
		}

		private void HandleButtonCancelClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Cancel);
		}
	}
}
