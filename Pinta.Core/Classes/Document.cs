// 
// WorkspaceManager.cs
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
using Mono.Unix;
using Gdk;

namespace Pinta.Core
{
	// The differentiation between Document and DocumentWorkspace is
	// somewhat arbitrary.  In general:
	// Document - Data about the image itself
	// Workspace - Data about Pinta's state for the image
	public class Document
	{
		private bool is_dirty;
		private string pathname;
		
		public Document ()
		{
			Workspace = new DocumentWorkspace (this);
			IsDirty = false;
			HasFile = false;
		}

		#region Public Properties
		public string Filename {
			get { return System.IO.Path.GetFileName (Pathname); }
			set { 
				if (value != null)
					Pathname = System.IO.Path.Combine (Pathname, value);
			}
		}
		
		public bool HasFile { get; set; }
		
		public Gdk.Size ImageSize { get; set; }
		
		public bool IsDirty {
			get { return is_dirty; }
			set {
				if (is_dirty != value) {
					is_dirty = value;
					PintaCore.Workspace.ResetTitle ();
				}
			}
		}
		
		public string Pathname {
			get { return (pathname != null) ? pathname : string.Empty; }
			set { pathname = value; }
		}

		public DocumentWorkspace Workspace { get; private set; }
		#endregion

		#region Public Methods
		public Rectangle ClampToImageSize (Rectangle r)
		{
			int x = Utility.Clamp (r.X, 0, ImageSize.Width);
			int y = Utility.Clamp (r.Y, 0, ImageSize.Height);
			int width = Math.Min (r.Width, ImageSize.Width - x);
			int height = Math.Min (r.Height, ImageSize.Height - y);

			return new Gdk.Rectangle (x, y, width, height);
		}

		public void ResizeCanvas (int width, int height, Anchor anchor)
		{
			double scale;

			if (ImageSize.Width == width && ImageSize.Height == height)
				return;

			PintaCore.Layers.FinishSelection ();

			ResizeHistoryItem hist = new ResizeHistoryItem (ImageSize.Width, ImageSize.Height);
			hist.Icon = "Menu.Image.CanvasSize.png";
			hist.Text = Catalog.GetString ("Resize Canvas");
			hist.TakeSnapshotOfImage ();

			ImageSize = new Gdk.Size (width, height);

			scale = Workspace.Scale;

			foreach (var layer in PintaCore.Layers)
				layer.ResizeCanvas (width, height, anchor);

			PintaCore.History.PushNewItem (hist);

			PintaCore.Layers.ResetSelectionPath ();

			Workspace.Scale = scale;
		}
		
		public void ResizeImage (int width, int height)
		{
			double scale;

			if (ImageSize.Width == width && ImageSize.Height == height)
				return;

			PintaCore.Layers.FinishSelection ();

			ResizeHistoryItem hist = new ResizeHistoryItem (ImageSize.Width, ImageSize.Height);
			hist.TakeSnapshotOfImage ();

			scale = Workspace.Scale;

			ImageSize = new Gdk.Size (width, height);

			foreach (var layer in PintaCore.Layers)
				layer.Resize (width, height);

			PintaCore.History.PushNewItem (hist);

			PintaCore.Layers.ResetSelectionPath ();

			Workspace.Scale = scale;
		}
		#endregion
	}
}
