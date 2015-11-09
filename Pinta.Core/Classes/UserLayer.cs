// 
// UserLayer.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2013 Andrew Davis, GSoC 2012 and GSoC 2013
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
using System.Collections.Generic;

namespace Pinta.Core
{
	/// <summary>
	/// A UserLayer is a Layer that the user interacts with directly. Each UserLayer contains special layers
	/// and some other special variables that allow for re-editability of various things.
	/// </summary>
	public class UserLayer : Layer
	{
		//Special layers to be drawn on to keep things editable by drawing them separately from the UserLayers.
		public List<ReEditableLayer> ReEditableLayers = new List<ReEditableLayer>();
		public ReEditableLayer TextLayer;

		//Call the base class constructor and setup the engines.
		public UserLayer(ImageSurface surface) : base(surface)
		{
			setupUserLayer();
		}

		//Call the base class constructor and setup the engines.
		public UserLayer(ImageSurface surface, bool hidden, double opacity, string name) : base(surface, hidden, opacity, name)
		{
			setupUserLayer();
		}

		private void setupUserLayer()
		{
			tEngine = new TextEngine();

			TextLayer = new ReEditableLayer(this);
		}

		//Stores most of the editable text's data, including the text itself.
		public TextEngine tEngine;

		//Rectangular boundary surrounding the editable text.
		public Gdk.Rectangle textBounds = Gdk.Rectangle.Zero;
		public Gdk.Rectangle previousTextBounds = Gdk.Rectangle.Zero;

        public override void ApplyTransform (Matrix xform, Size new_size)
		{
			base.ApplyTransform (xform, new_size);

			foreach (ReEditableLayer rel in ReEditableLayers)
			{
				if (rel.IsLayerSetup)
                    rel.Layer.ApplyTransform (xform, new_size);
			}
		}

        public void Rotate (double angle, Size new_size)
        {
			double radians = (angle / 180d) * Math.PI;
		    var old_size = PintaCore.Workspace.ImageSize;

            var xform = new Matrix ();
            xform.Translate (new_size.Width / 2.0, new_size.Height / 2.0);
            xform.Rotate (radians);
            xform.Translate (-old_size.Width / 2.0, -old_size.Height / 2.0);

            ApplyTransform (xform, new_size);
        }

        public override void Crop (Gdk.Rectangle rect, Path selection)
		{
			base.Crop (rect, selection);

			foreach (ReEditableLayer rel in ReEditableLayers)
			{
				if (rel.IsLayerSetup)
                    rel.Layer.Crop (rect, selection);
			}
		}

		public override void ResizeCanvas(int width, int height, Anchor anchor)
		{
			base.ResizeCanvas (width, height, anchor);

			foreach (ReEditableLayer rel in ReEditableLayers)
			{
				if (rel.IsLayerSetup)
				{
					rel.Layer.ResizeCanvas(width, height, anchor);
				}
			}
		}

		public override void Resize(int width, int height)
		{
			base.Resize (width, height);

			foreach (ReEditableLayer rel in ReEditableLayers)
			{
				if (rel.IsLayerSetup)
				{
					rel.Layer.Resize(width, height);
				}
			}
		}
	}
}
