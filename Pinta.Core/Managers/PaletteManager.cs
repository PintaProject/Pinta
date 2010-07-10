// 
// PaletteManager.cs
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

namespace Pinta.Core
{
	public class PaletteManager
	{
		private Color primary;
		private Color secondary;
		private Palette palette;

		public Color PrimaryColor {
			get { return primary; }
			set {
				if (!primary.Equals (value)) {
					primary = value;
					OnPrimaryColorChanged ();
				}
			}
		}

		public Color SecondaryColor {
			get { return secondary; }
			set {
				if (!secondary.Equals (value)) {
					secondary = value;
					OnSecondaryColorChanged ();
				}
			}
		}
		
		public Palette CurrentPalette {
			get {
				if (palette == null) {
					palette = Palette.GetDefault ();
				}
				
				return palette;
			}
		}
		
		public PaletteManager ()
		{
			PrimaryColor = new Color (0, 0, 0);
			SecondaryColor = new Color (1, 1, 1);
		}

		#region Protected Methods
		protected void OnPrimaryColorChanged ()
		{
			if (PrimaryColorChanged != null)
				PrimaryColorChanged.Invoke (this, EventArgs.Empty);
		}

		protected void OnSecondaryColorChanged ()
		{
			if (SecondaryColorChanged != null)
				SecondaryColorChanged.Invoke (this, EventArgs.Empty);
		}
		#endregion
		
		#region Events
		public event EventHandler PrimaryColorChanged;
		public event EventHandler SecondaryColorChanged;
		#endregion
	}
}
