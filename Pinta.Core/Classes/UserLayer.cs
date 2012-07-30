// 
// UserLayer.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2012 Andrew Davis, GSoC 2012
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
using System.ComponentModel;
using System.Collections.Specialized;
using Cairo;
using Gdk;

namespace Pinta.Core
{
	/// <summary>
	/// A UserLayer is a Layer that the user interacts with directly. Each UserLayer contains a TextLayer
	/// and some other text related variables that allow for the re-editability of text.
	/// </summary>
	public class UserLayer : Layer
	{
		//Call the base class constructor, and then setup the TextLayer.
		public UserLayer (ImageSurface surface) : base (surface)
		{
			SetupTextLayer();
		}

		//Call the base class constructor, and then setup the TextLayer.
		public UserLayer(ImageSurface surface, bool hidden, double opacity, string name) : base(surface, hidden, opacity, name)
		{
			SetupTextLayer();
		}

		/// <summary>
		/// Setup the TextLayer based on this UserLayer's Surface.
		/// </summary>
		private void SetupTextLayer()
		{
			TextLayer = new Layer(new Cairo.ImageSurface(Surface.Format, Surface.Width, Surface.Height));
			tEngine = new TextEngine(new Cairo.ImageSurface(Surface.Format, Surface.Width, Surface.Height));
		}

		//The Layer for Text to be drawn on while it is still editable.
		public Layer TextLayer;

		//The TextEngine that stores most of the editable text's data, including the text itself.
		public TextEngine tEngine;

		//The rectangular boundary surrounding the editable text.
		public Gdk.Rectangle textBounds = Gdk.Rectangle.Zero;
	}
}
