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
		//The Layer for Text to be drawn on while it is still editable.
		private Layer actualTextLayer;

		//Call the base class constructor and setup the TextEngine.
		public UserLayer(ImageSurface surface) : base(surface)
		{
			tEngine = new TextEngine();
		}

		//Call the base class constructor and setup the TextEngine.
		public UserLayer(ImageSurface surface, bool hidden, double opacity, string name) : base(surface, hidden, opacity, name)
		{
			tEngine = new TextEngine();
		}

		/// <summary>
		/// Setup the TextLayer based on this UserLayer's Surface.
		/// </summary>
		private void SetupTextLayer()
		{
			actualTextLayer = new Layer(new Cairo.ImageSurface(Surface.Format, Surface.Width, Surface.Height));

			IsTextLayerSetup = true;
		}

		//Whether or not the TextLayer and TextEngine have already been setup.
		public bool IsTextLayerSetup = false;

		//A public property for the actual TextLayer that creates a new one when it's first used.
		public Layer TextLayer
		{
			get
			{
				if (!IsTextLayerSetup)
				{
					SetupTextLayer();
				}

				return actualTextLayer;
			}

			set
			{
				actualTextLayer = value;
			}
		}

		//The TextEngine that stores most of the editable text's data, including the text itself.
		public TextEngine tEngine;

		//The rectangular boundary surrounding the editable text.
		public Gdk.Rectangle textBounds = Gdk.Rectangle.Zero;
		public Gdk.Rectangle previousTextBounds = Gdk.Rectangle.Zero;

		public override void Rotate(double angle)
		{
			base.Rotate (angle);
			if (IsTextLayerSetup) {
				TextLayer.Rotate (angle);
			}
		}

		public override void Crop(Gdk.Rectangle rect, Path path)
		{
			base.Crop (rect, path);
			if (IsTextLayerSetup) {
				TextLayer.Crop (rect, path);
			}
		}

		public override void ResizeCanvas(int width, int height, Anchor anchor)
		{
			base.ResizeCanvas (width, height, anchor);
			if (IsTextLayerSetup) {
				TextLayer.ResizeCanvas (width, height, anchor);
			}
		}

		public override void Resize(int width, int height)
		{
			base.Resize (width, height);
			if (IsTextLayerSetup) {
				TextLayer.Resize (width, height);
			}
		}
	}
}
