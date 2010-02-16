// 
// PosterizeDialog.cs
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

namespace Pinta
{
	public partial class PosterizeDialog : Gtk.Dialog
	{
		public int Red {
			get { return hscalespinRed.Value; }
		}
		
		public int Green { 
			get { return hscalespinGreen.Value; }
		}
		
		public int Blue {
			get { return hscalespinBlue.Value; }
		}

		public PosterizeDialog ()
		{
			this.Build ();
			
			this.hscalespinRed.ValueChanged += HandleHscalespinRedValueChanged;
			this.hscalespinGreen.ValueChanged += HandleHscalespinGreenValueChanged;
			this.hscalespinBlue.ValueChanged += HandleHscalespinBlueValueChanged;
		}

		private void HandleHscalespinRedValueChanged (object sender, EventArgs e)
		{
			if (checkLinked.Active) 
				hscalespinGreen.Value = hscalespinBlue.Value = hscalespinRed.Value;
		}
		
		private void HandleHscalespinGreenValueChanged (object sender, EventArgs e)
		{
			if (checkLinked.Active)
				hscalespinBlue.Value = hscalespinRed.Value = hscalespinGreen.Value;
		}
		
		private void HandleHscalespinBlueValueChanged (object sender, EventArgs e)
		{
			if (checkLinked.Active)
				hscalespinRed.Value = hscalespinGreen.Value = hscalespinBlue.Value;
		}
	}
}
