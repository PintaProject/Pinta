// 
// Document.cs
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
using System.Linq;
using Mono.Unix;
using Gdk;
using System.Collections.Generic;
using Cairo;
using System.ComponentModel;

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
		private int layer_name_int = 2;
		private int current_layer = -1;

		// The layer for tools to use until their output is committed
		private Layer tool_layer;
		// The layer used for selections
		private Layer selection_layer;

		private int selection_layer_index;
		private Path selection_path;
		private bool show_selection;

		public Document (Gdk.Size size)
		{
			Guid = Guid.NewGuid ();
			
			Workspace = new DocumentWorkspace (this);
			IsDirty = false;
			HasFile = false;
			ImageSize = size;
			
			Layers = new List<Layer> ();

			tool_layer = CreateLayer ("Tool Layer");
			tool_layer.Hidden = true;

			selection_layer = CreateLayer ("Selection Layer");
			selection_layer.Hidden = true;

			ResetSelectionPath ();
		}

		#region Public Properties
		public Layer CurrentLayer {
			get { return Layers[current_layer]; }
		}

		public int CurrentLayerIndex {
			get { return current_layer; }
		}
		
		public string Filename {
			get { return System.IO.Path.GetFileName (Pathname); }
			set { 
				if (value != null)
					Pathname = System.IO.Path.Combine (Pathname, value);
			}
		}
		
		public Guid Guid { get; private set; }
		
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
		
		public List<Layer> Layers { get; private set; }

		public string Pathname {
			get { return (pathname != null) ? pathname : string.Empty; }
			set { pathname = value; }
		}
		
		public Layer SelectionLayer {
			get { return selection_layer; }
		}
		
		public Path SelectionPath {
			get { return selection_path; }
			set {
				if (selection_path == value)
					return;

				selection_path = value;
			}
		}

		public bool ShowSelection {
			get { return show_selection; }
			set {
				show_selection = value;
				PintaCore.Actions.Edit.Deselect.Sensitive = show_selection;
				PintaCore.Actions.Edit.EraseSelection.Sensitive = show_selection;
				PintaCore.Actions.Edit.FillSelection.Sensitive = show_selection;
				PintaCore.Actions.Image.CropToSelection.Sensitive = show_selection;
			}
		}

		public bool ShowSelectionLayer { get; set; }

		public Layer ToolLayer {
			get {
				if (tool_layer.Surface.Width != ImageSize.Width || tool_layer.Surface.Height != ImageSize.Height) {
					(tool_layer.Surface as IDisposable).Dispose ();
					tool_layer = CreateLayer ("Tool Layer");
					tool_layer.Hidden = true;
				}

				return tool_layer;
			}
		}

		public DocumentWorkspace Workspace { get; private set; }
		#endregion

		#region Public Methods
		// Adds a new layer above the current one
		public Layer AddNewLayer (string name)
		{
			Layer layer;

			if (string.IsNullOrEmpty (name))
				layer = CreateLayer ();
			else
				layer = CreateLayer (name);

			Layers.Insert (current_layer + 1, layer);

			if (Layers.Count == 1)
				current_layer = 0;

			layer.PropertyChanged += RaiseLayerPropertyChangedEvent;

			PintaCore.Layers.OnLayerAdded ();
			return layer;
		}

		public Gdk.Rectangle ClampToImageSize (Gdk.Rectangle r)
		{
			int x = Utility.Clamp (r.X, 0, ImageSize.Width);
			int y = Utility.Clamp (r.Y, 0, ImageSize.Height);
			int width = Math.Min (r.Width, ImageSize.Width - x);
			int height = Math.Min (r.Height, ImageSize.Height - y);

			return new Gdk.Rectangle (x, y, width, height);
		}

		public void Clear ()
		{
			while (Layers.Count > 0) {
				Layer l = Layers[Layers.Count - 1];
				Layers.RemoveAt (Layers.Count - 1);
				(l.Surface as IDisposable).Dispose ();
			}

			current_layer = -1;
			PintaCore.Layers.OnLayerRemoved ();
		}
		
		// Clean up any native resources we had
		public void Close ()
		{
			// Dispose all of our layers
			while (Layers.Count > 0) {
				Layer l = Layers[Layers.Count - 1];
				Layers.RemoveAt (Layers.Count - 1);
				(l.Surface as IDisposable).Dispose ();
			}

			current_layer = -1;

			if (tool_layer != null)
				(tool_layer.Surface as IDisposable).Dispose ();

			if (selection_layer != null)
				(selection_layer.Surface as IDisposable).Dispose ();

			if (selection_path != null)
				(selection_path as IDisposable).Dispose ();
		}
		
		public Layer CreateLayer ()
		{
			return CreateLayer (string.Format ("{0} {1}", Catalog.GetString ("Layer"), layer_name_int++));
		}

		public Layer CreateLayer (string name)
		{
			return CreateLayer (name, ImageSize.Width, ImageSize.Height);
		}

		public Layer CreateLayer (string name, int width, int height)
		{
			Cairo.ImageSurface surface = new Cairo.ImageSurface (Cairo.Format.ARGB32, width, height);
			Layer layer = new Layer (surface) { Name = name };

			return layer;
		}

		public void CreateSelectionLayer ()
		{
			selection_layer = CreateLayer ();
			selection_layer_index = current_layer + 1;
		}

		// Delete the current layer
		public void DeleteCurrentLayer ()
		{
			Layer layer = CurrentLayer;

			Layers.RemoveAt (current_layer);

			// Only change this if this wasn't already the bottom layer
			if (current_layer > 0)
				current_layer--;

			layer.PropertyChanged -= RaiseLayerPropertyChangedEvent;

			PintaCore.Layers.OnLayerRemoved ();
		}

		// Delete the layer
		public void DeleteLayer (int index, bool dispose)
		{
			Layer layer = Layers[index];

			Layers.RemoveAt (index);

			if (dispose)
				(layer.Surface as IDisposable).Dispose ();

			// Only change this if this wasn't already the bottom layer
			if (current_layer > 0)
				current_layer--;

			layer.PropertyChanged -= RaiseLayerPropertyChangedEvent;

			PintaCore.Layers.OnLayerRemoved ();
		}
		
		public void DestroySelectionLayer ()
		{
			ShowSelectionLayer = false;
			SelectionLayer.Clear ();
			SelectionLayer.Offset = new PointD (0, 0);
		}

		// Duplicate current layer
		public Layer DuplicateCurrentLayer ()
		{
			Layer source = CurrentLayer;
			Layer layer = CreateLayer (string.Format ("{0} {1}", source.Name, Catalog.GetString ("copy")));

			using (Cairo.Context g = new Cairo.Context (layer.Surface)) {
				g.SetSource (source.Surface);
				g.Paint ();
			}

			layer.Hidden = source.Hidden;
			layer.Opacity = source.Opacity;
			layer.Tiled = source.Tiled;

			Layers.Insert (++current_layer, layer);

			layer.PropertyChanged += RaiseLayerPropertyChangedEvent;

			PintaCore.Layers.OnLayerAdded ();

			return layer;
		}

		public void FinishSelection ()
		{
			// We don't have an uncommitted layer, abort
			if (!ShowSelectionLayer)
				return;

			FinishPixelsHistoryItem hist = new FinishPixelsHistoryItem ();
			hist.TakeSnapshot ();

			Layer layer = SelectionLayer;

			using (Cairo.Context g = new Cairo.Context (CurrentLayer.Surface)) {
				g.Save ();

				g.SetSourceSurface (layer.Surface, (int)layer.Offset.X, (int)layer.Offset.Y);
				g.PaintWithAlpha (layer.Opacity);

				g.Restore ();
			}

			DestroySelectionLayer ();
			Workspace.Invalidate ();

			Workspace.History.PushNewItem (hist);
		}
		
		// Flatten image
		public void FlattenImage ()
		{
			if (Layers.Count < 2)
				throw new InvalidOperationException ("Cannot flatten image because there is only one layer.");

			Layer dest = Layers[0];

			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				for (int i = 1; i < Layers.Count; i++) {
					Layer source = Layers[i];
					g.SetSource (source.Surface);
					g.PaintWithAlpha (source.Opacity);
				}
			}

			current_layer = 0;

			while (Layers.Count > 1) {
				Layer l = Layers[1];

				Layers.RemoveAt (1);
			}

			PintaCore.Layers.OnLayerRemoved ();
			Workspace.Invalidate ();
		}

		// Flip image horizontally
		public void FlipImageHorizontal ()
		{
			foreach (var layer in Layers)
				layer.FlipHorizontal ();

			Workspace.Invalidate ();
		}

		// Flip image vertically
		public void FlipImageVertical ()
		{
			foreach (var layer in Layers)
				layer.FlipVertical ();

			Workspace.Invalidate ();
		}
		
		public ImageSurface GetClippedLayer (int index)
		{
			Cairo.ImageSurface surf = new Cairo.ImageSurface (Cairo.Format.Argb32, ImageSize.Width, ImageSize.Height);

			using (Cairo.Context g = new Cairo.Context (surf)) {
				g.AppendPath (SelectionPath);
				g.Clip ();

				g.SetSource (Layers[index].Surface);
				g.Paint ();
			}

			return surf;
		}

		public ImageSurface GetFlattenedImage ()
		{
			Cairo.ImageSurface surf = new Cairo.ImageSurface (Cairo.Format.Argb32, ImageSize.Width, ImageSize.Height);

			using (Cairo.Context g = new Cairo.Context (surf)) {
				foreach (var layer in GetLayersToPaint ()) {
					g.SetSource (layer.Surface);
					g.PaintWithAlpha (layer.Opacity);
				}
			}

			return surf;
		}

		public List<Layer> GetLayersToPaint ()
		{
			List<Layer> paint = Layers.Where (l => !l.Hidden).ToList ();

			if (!tool_layer.Hidden)
				paint.Add (tool_layer);
			if (ShowSelectionLayer)
				paint.Insert (selection_layer_index, selection_layer);

			return paint;
		}

		public int IndexOf (Layer layer)
		{
			return Layers.IndexOf (layer);
		}

		// Adds a new layer above the current one
		public void Insert (Layer layer, int index)
		{
			Layers.Insert (index, layer);

			if (Layers.Count == 1)
				current_layer = 0;

			layer.PropertyChanged += RaiseLayerPropertyChangedEvent;

			PintaCore.Layers.OnLayerAdded ();
		}
		
		// Flatten current layer
		public void MergeCurrentLayerDown ()
		{
			if (current_layer == 0)
				throw new InvalidOperationException ("Cannot flatten layer because current layer is the bottom layer.");

			Layer source = CurrentLayer;
			Layer dest = Layers[current_layer - 1];

			using (Cairo.Context g = new Cairo.Context (dest.Surface)) {
				g.SetSource (source.Surface);
				g.PaintWithAlpha (source.Opacity);
			}

			DeleteCurrentLayer ();
		}
		
		// Move current layer down
		public void MoveCurrentLayerDown ()
		{
			if (current_layer == 0)
				throw new InvalidOperationException ("Cannot move layer down because current layer is the bottom layer.");

			Layer layer = CurrentLayer;
			Layers.RemoveAt (current_layer);
			Layers.Insert (--current_layer, layer);

			PintaCore.Layers.OnSelectedLayerChanged ();

			Workspace.Invalidate ();
		}

		// Move current layer up
		public void MoveCurrentLayerUp ()
		{
			if (current_layer == Layers.Count)
				throw new InvalidOperationException ("Cannot move layer up because current layer is the top layer.");

			Layer layer = CurrentLayer;
			Layers.RemoveAt (current_layer);
			Layers.Insert (++current_layer, layer);

			PintaCore.Layers.OnSelectedLayerChanged ();

			Workspace.Invalidate ();
		}
		
		public void ResetSelectionPath ()
		{
			Path old = SelectionPath;

			using (Cairo.Context g = new Cairo.Context (selection_layer.Surface))
				SelectionPath = g.CreateRectanglePath (new Cairo.Rectangle (0, 0, ImageSize.Width, ImageSize.Height));

			if (old != null)
				(old as IDisposable).Dispose ();

			ShowSelection = false;
		}

		public void ResizeCanvas (int width, int height, Anchor anchor)
		{
			double scale;

			if (ImageSize.Width == width && ImageSize.Height == height)
				return;

			FinishSelection ();

			ResizeHistoryItem hist = new ResizeHistoryItem (ImageSize.Width, ImageSize.Height);
			hist.Icon = "Menu.Image.CanvasSize.png";
			hist.Text = Catalog.GetString ("Resize Canvas");
			hist.TakeSnapshotOfImage ();

			ImageSize = new Gdk.Size (width, height);

			scale = Workspace.Scale;

			foreach (var layer in Layers)
				layer.ResizeCanvas (width, height, anchor);

			Workspace.History.PushNewItem (hist);

			ResetSelectionPath ();

			Workspace.Scale = scale;
		}
		
		public void ResizeImage (int width, int height)
		{
			double scale;

			if (ImageSize.Width == width && ImageSize.Height == height)
				return;

			FinishSelection ();

			ResizeHistoryItem hist = new ResizeHistoryItem (ImageSize.Width, ImageSize.Height);
			hist.TakeSnapshotOfImage ();

			scale = Workspace.Scale;

			ImageSize = new Gdk.Size (width, height);

			foreach (var layer in Layers)
				layer.Resize (width, height);

			Workspace.History.PushNewItem (hist);

			ResetSelectionPath ();

			Workspace.Scale = scale;
		}
		
		// Rotate image 180 degrees (flip H+V)
		public void RotateImage180 ()
		{
			foreach (var layer in Layers)
				layer.Rotate180 ();

			Workspace.Invalidate ();
		}

		public void RotateImageCW ()
		{
			foreach (var layer in Layers)
				layer.Rotate90CW ();

			ImageSize = new Gdk.Size (ImageSize.Height, ImageSize.Width);
			Workspace.CanvasSize = new Gdk.Size (Workspace.CanvasSize.Height, Workspace.CanvasSize.Width);

			Workspace.Invalidate ();
		}

		public void RotateImageCCW ()
		{
			foreach (var layer in Layers)
				layer.Rotate90CCW ();

			ImageSize = new Gdk.Size (ImageSize.Height, ImageSize.Width);
			Workspace.CanvasSize = new Gdk.Size (Workspace.CanvasSize.Height, Workspace.CanvasSize.Width);

			Workspace.Invalidate ();

		}

		public void SetCurrentLayer (int i)
		{
			current_layer = i;

			PintaCore.Layers.OnSelectedLayerChanged ();
		}

		public void SetCurrentLayer (Layer layer)
		{
			current_layer = Layers.IndexOf (layer);

			PintaCore.Layers.OnSelectedLayerChanged ();
		}
		#endregion

		#region Private Methods
		private void RaiseLayerPropertyChangedEvent (object sender, PropertyChangedEventArgs e)
		{
			PintaCore.Layers.RaiseLayerPropertyChangedEvent (sender, e);
		}
		#endregion
	}
}
