// 
// NewImageDialog.cs
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
using Pinta.Core;
using Gtk;

namespace Pinta
{
	public partial class NewImageDialog : Gtk.Dialog
	{
		/// <summary>
		/// Configures and builds a NewImageDialog object.
		/// </summary>
		/// <param name="imgWidth">Initial value of the width spin control.</param>
		/// <param name="imgHeight">nitial value of the height spin control.</param>
		public NewImageDialog (int imgWidth, int imgHeight) : base (string.Empty, PintaCore.Chrome.MainWindow, DialogFlags.Modal)
		{
			this.Build ();

			this.Icon = PintaCore.Resources.GetIcon (Stock.New, 16);
			DefaultResponse = ResponseType.Ok;
			AlternativeButtonOrder = new int[] { (int) ResponseType.Ok, (int) ResponseType.Cancel };

			widthSpinner.ActivatesDefault = true;
			heightSpinner.ActivatesDefault = true;

			// Initialize the spin control values
			widthSpinner.Value = imgWidth;
			heightSpinner.Value =imgHeight;

			// Set focus to the width spin control and select it's text
			widthSpinner.GrabFocus();
		}

		public int NewImageWidth { get { return widthSpinner.ValueAsInt; } }
		public int NewImageHeight { get { return heightSpinner.ValueAsInt; } }
	}
}

