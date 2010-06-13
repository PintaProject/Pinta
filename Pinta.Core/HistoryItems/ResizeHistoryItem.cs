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

namespace Pinta.Core
{
	public class ResizeHistoryItem : CompoundHistoryItem
	{
		private int old_width;
		private int old_height;
		
		public ResizeHistoryItem (int oldWidth, int oldHeight) : base ()
		{
			old_width = oldWidth;
			old_height = oldHeight;

			Icon = "Menu.Image.Resize.png";
			Text = Catalog.GetString ("Resize Image");
		}
		
		public Cairo.Path RestorePath { get; set; }
		
		public override void Undo ()
		{
			int swap_width = PintaCore.Workspace.ImageSize.Width;
			int swap_height = PintaCore.Workspace.ImageSize.Height;

			PintaCore.Workspace.ImageSize = new Gdk.Size (old_width, old_height);
			PintaCore.Workspace.CanvasSize = new Gdk.Size (old_width, old_height);
			
			old_width = swap_width;
			old_height = swap_height;
			
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
			int swap_width = PintaCore.Workspace.ImageSize.Width;
			int swap_height = PintaCore.Workspace.ImageSize.Height;

			PintaCore.Workspace.ImageSize = new Gdk.Size (old_width, old_height);
			PintaCore.Workspace.CanvasSize = new Gdk.Size (old_width, old_height);

			old_width = swap_width;
			old_height = swap_height;

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
