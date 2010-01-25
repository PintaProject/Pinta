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
using Cairo;

namespace Pinta.Core
{
	public class WorkspaceManager
	{
		private string filename;
		private bool is_dirty;
		
		public PointD ImageSize { get; set; }
		public PointD CanvasSize { get; set; }
		
		public PointD Offset {
			get { return new PointD ((PintaCore.Chrome.DrawingArea.Allocation.Width - CanvasSize.X) / 2, (PintaCore.Chrome.DrawingArea.Allocation.Height - CanvasSize.Y) / 2); }
		}

		public WorkspaceManager ()
		{
			CanvasSize = new PointD (800, 600);
			ImageSize = new PointD (800, 600);
		}
		
		public double Scale {
			get { return CanvasSize.X / ImageSize.X; }
			set {
					if (Scale != value) {
						CanvasSize = new PointD (ImageSize.X * value, ImageSize.Y * value);
						Invalidate ();
				}
			}
		}
		
		public void Invalidate ()
		{
			OnCanvasInvalidated (new CanvasInvalidatedEventArgs ());
		}
			
		public void Invalidate (Gdk.Rectangle rect)
		{
			rect = new Gdk.Rectangle ((int)((rect.X) * Scale + Offset.X), (int)((rect.Y) * Scale + Offset.Y), (int)(rect.Width * Scale), (int)(rect.Height * Scale));
			OnCanvasInvalidated (new CanvasInvalidatedEventArgs (rect));
		}
		
		public void ZoomIn ()
		{
			if (PintaCore.Actions.View.ZoomComboBox.ComboBox.Active > 1)
				PintaCore.Actions.View.ZoomComboBox.ComboBox.Active--;
		}
		
		public void ZoomOut ()
		{
			if (PintaCore.Actions.View.ZoomComboBox.ComboBox.Active < 19)
				PintaCore.Actions.View.ZoomComboBox.ComboBox.Active++;
		}

		public void ResizeImage (int width, int height)
		{
			if (ImageSize.X == width && ImageSize.Y == height)
				return;
				
			PintaCore.Layers.FinishSelection ();
			
			ResizeHistoryItem hist = new ResizeHistoryItem ((int)ImageSize.X, (int)ImageSize.Y);
			hist.TakeSnapshotOfImage ();
			
			ImageSize = new PointD (width, height);
			CanvasSize = new PointD (width, height);
			
			foreach (var layer in PintaCore.Layers)
				layer.Resize (width, height);
			
			PintaCore.History.PushNewItem (hist);
			
			PintaCore.Layers.ResetSelectionPath ();
			PintaCore.Workspace.Invalidate ();
		}
		
		public Cairo.PointD WindowPointToCanvas (double x, double y)
		{
			return new Cairo.PointD ((x - Offset.X) / PintaCore.Workspace.Scale, (y - Offset.Y) / PintaCore.Workspace.Scale);
		}

		public bool PointInCanvas (Cairo.PointD point)
		{
			if (point.X < 0 || point.Y < 0)
				return false;

			if (point.X >= PintaCore.Workspace.ImageSize.X || point.Y >= PintaCore.Workspace.ImageSize.Y)
				return false;

			return true;
		}

		public string Filename {
			get { return filename; }
			set {
				if (filename != value) {
					filename = value;
					ResetTitle ();
				}
			}
		}
		
		public bool IsDirty {
			get { return is_dirty; }
			set {
				if (is_dirty != value) {
					is_dirty = value;
					ResetTitle ();
				}
			}
		}
		
		private void ResetTitle ()
		{
			PintaCore.Chrome.MainWindow.Title = string.Format ("{0}{1} - Pinta", filename, is_dirty ? "*" : "");
		}

		#region Protected Methods
		protected void OnCanvasInvalidated (CanvasInvalidatedEventArgs e)
		{
			if (CanvasInvalidated != null)
				CanvasInvalidated (this, e);
		}
		#endregion

		#region Public Events
		public event EventHandler<CanvasInvalidatedEventArgs> CanvasInvalidated;
		#endregion
	}
}
