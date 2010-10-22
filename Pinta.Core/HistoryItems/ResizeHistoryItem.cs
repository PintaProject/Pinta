// 
// ResizeHistoryItem.cs
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
using System.Collections.Generic;
using Mono.Unix;
using Gdk;

namespace Pinta.Core
{
	public class ResizeHistoryItem : CompoundHistoryItem
	{
		private Size old_size;

		public ResizeHistoryItem (Size oldSize) : base ()
		{
			old_size = oldSize;

			Icon = "Menu.Image.Resize.png";
			Text = Catalog.GetString ("Resize Image");
		}
		
		public Cairo.Path RestorePath { get; set; }
		
		public override void Undo ()
		{
			Size swap = PintaCore.Workspace.ImageSize;

			PintaCore.Workspace.ImageSize = old_size;
			PintaCore.Workspace.CanvasSize = old_size;
			
			old_size = swap;
			
			base.Undo ();
			
			if (RestorePath != null) {
				Cairo.Path old = PintaCore.Layers.SelectionPath;

				PintaCore.Layers.SelectionPath = RestorePath.Clone ();
				
				if (old != null)
					(old as IDisposable).Dispose ();
					
				PintaCore.Layers.ShowSelection = true;
			} else {
				PintaCore.Layers.ResetSelectionPath ();
			}
			
			PintaCore.Workspace.Invalidate ();
		}

		public override void Redo ()
		{
			Size swap = PintaCore.Workspace.ImageSize;

			PintaCore.Workspace.ImageSize = old_size;
			PintaCore.Workspace.CanvasSize = old_size;

			old_size = swap;

			base.Redo ();

			PintaCore.Layers.ResetSelectionPath ();
			PintaCore.Workspace.Invalidate ();
		}

		public override void Dispose ()
		{
			base.Dispose ();

			if (RestorePath != null) {
				(RestorePath as IDisposable).Dispose ();
				RestorePath = null;
			}
		}
	}
}
