// 
// DocumentSelection.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2010 Andrew Davis
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
using System.Linq;
using System.Collections.Generic;
using Gdk;
using Cairo;
using ClipperLibrary;

namespace Pinta.Core
{
	class DocumentSelection
	{
		private Path selection_path;

		public Clipper SelectionClipper = new Clipper();

		public Path SelectionPath
		{
			get { return selection_path; }
			set
			{
				if (selection_path == value)
					return;

				selection_path = value;
			}
		}

		public void ResetSelection(Surface selectionSurface, Size imageSize)
		{
			using (Cairo.Context g = new Cairo.Context(selectionSurface))
			{
				SelectionPath = g.CreateRectanglePath(new Cairo.Rectangle(0, 0, imageSize.Width, imageSize.Height));
			}

			SelectionClipper.Clear();
		}

		public DocumentSelection Clone()
		{
			return (DocumentSelection)MemberwiseClone();
		}

		public void DisposePath()
		{
			if (selection_path != null)
			{
				(selection_path as IDisposable).Dispose();
			}
		}

		public void DisposeOldPath()
		{
			Path old = SelectionPath;

			SelectionPath = null;

			if (old != null)
			{
				(old as IDisposable).Dispose();
			}
		}
	}
}
