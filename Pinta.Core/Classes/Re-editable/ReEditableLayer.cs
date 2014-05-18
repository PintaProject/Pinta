// 
// ReEditableLayer.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2013 Andrew Davis, GSoC 2013
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;

namespace Pinta.Core
{
	public class ReEditableLayer
	{
		Layer actualLayer;

		//Whether or not the actualLayer has already been setup.
		private bool isLayerSetup = false;

		private UserLayer parent;

		public Layer Layer
		{
			get
			{
				if (!isLayerSetup)
				{
					SetupLayer();
				}

				return actualLayer;
			}

			set
			{
				actualLayer = value;
			}
		}

		public bool IsLayerSetup
		{
			get
			{
				return isLayerSetup;
			}
		}

		public ReEditableLayer(UserLayer passedParent)
		{
			parent = passedParent;
		}

		/// <summary>
		/// Setup the Layer based on the parent UserLayer's Surface.
		/// </summary>
		private void SetupLayer()
		{
			actualLayer = new Layer(new Cairo.ImageSurface(parent.Surface.Format, parent.Surface.Width, parent.Surface.Height));

			parent.ReEditableLayers.Add(this);

			isLayerSetup = true;
		}
	}
}
