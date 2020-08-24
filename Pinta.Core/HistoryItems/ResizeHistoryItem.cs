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

		public DocumentSelection RestoreSelection;
		
		public override void Undo ()
		{
			// maintain the current scaling setting after the operation
			double scale = PintaCore.Workspace.Scale;

			Size swap = PintaCore.Workspace.ImageSize;

			var window = PintaCore.Workspace.ActiveWorkspace.Canvas.GdkWindow;
			window.FreezeUpdates ();

			PintaCore.Workspace.ImageSize = old_size;
			PintaCore.Workspace.CanvasSize = old_size;
			
			old_size = swap;
			
			base.Undo ();
			
			if (RestoreSelection != null) {
				DocumentSelection old = PintaCore.Workspace.ActiveDocument.Selection;
				PintaCore.Workspace.ActiveDocument.Selection = RestoreSelection.Clone();

				if (old != null) {
					old.Dispose ();
				}
			} else {
				PintaCore.Layers.ResetSelectionPath ();
			}
			
			PintaCore.Workspace.Invalidate ();

			PintaCore.Workspace.Scale = scale;
			PintaCore.Actions.View.UpdateCanvasScale ();

			window.ThawUpdates ();
		}

		public override void Redo ()
		{
			// maintain the current scaling setting after the operation
			double scale = PintaCore.Workspace.Scale;

			Size swap = PintaCore.Workspace.ImageSize;

			var window = PintaCore.Workspace.ActiveWorkspace.Canvas.GdkWindow;
			window.FreezeUpdates ();

			PintaCore.Workspace.ImageSize = old_size;
			PintaCore.Workspace.CanvasSize = old_size;

			old_size = swap;

			base.Redo ();

			PintaCore.Layers.ResetSelectionPath ();
			PintaCore.Workspace.Invalidate ();

			PintaCore.Workspace.Scale = scale;
			PintaCore.Actions.View.UpdateCanvasScale ();

			window.ThawUpdates ();
		}

		public override void Dispose ()
		{
			base.Dispose ();

			if (RestoreSelection != null) {
				RestoreSelection.Dispose ();
			}
		}
	}
}
