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
using Gtk;
using System.Collections.Generic;
using Cairo;
using System.ComponentModel;
using Pinta;

namespace Pinta.Core
{
	// The differentiation between Document and DocumentWorkspace is
	// somewhat arbitrary.  In general:
	// Document - Data about the image itself
	// Workspace - Data about Pinta's state for the image
	public class Document
	{
		private string filename;
		private bool is_dirty;
		private int layer_name_int = 2;
		private int current_layer = -1;

		// The layer for tools to use until their output is committed
		private Layer tool_layer;

		// The layer used for selections
		private Layer selection_layer;

		private DocumentSelection selection;
		public DocumentSelection Selection
		{
			get { return selection; }
			set
			{
				selection = value;

				// Listen for any changes to this selection.
				selection.SelectionModified += (sender, args) => {
					OnSelectionChanged ();
				};

				// Notify listeners that our selection has been modified.
				OnSelectionChanged();
			}
		}

		public DocumentSelection PreviousSelection = new DocumentSelection ();

		public Document (Gdk.Size size)
		{
			Selection = new DocumentSelection ();

			Guid = Guid.NewGuid ();
			
			Workspace = new DocumentWorkspace (this);
			IsDirty = false;
			HasFile = false;
			HasBeenSavedInSession = false;
			ImageSize = size;

			UserLayers = new List<UserLayer>();

			tool_layer = CreateLayer ("Tool Layer");
			tool_layer.Hidden = true;

			selection_layer = CreateLayer ("Selection Layer");
			selection_layer.Hidden = true;

			ResetSelectionPaths ();
		}

		#region Public Properties
		public UserLayer CurrentUserLayer
		{
			get { return UserLayers[current_layer]; }
		}

		public int CurrentUserLayerIndex {
			get { return current_layer; }
		}
		
		/// <summary>
		/// Just the file name, like "dog.jpg".
		/// </summary>
		public string Filename { 
			get { return filename; }
			set {
				if (filename != value) {
					filename = value;
					OnRenamed ();
				}
			}
		}
		
		public Guid Guid { get; private set; }
		
		public bool HasFile { get; set; }

		//Determines whether or not the Document has been saved to the file that it is currently associated with in the
		//current session. This should be false if the Document has not yet been saved, if it was just loaded into
		//Pinta from a file, or if the user just clicked Save As.
		public bool HasBeenSavedInSession { get; set; }
		
		public DocumentWorkspaceHistory History { get { return Workspace.History; } }

		public Gdk.Size ImageSize { get; set; }
		
		public bool IsDirty {
			get { return is_dirty; }
			set {
				if (is_dirty != value) {
					is_dirty = value;
					OnIsDirtyChanged ();
				}
			}
		}
		
		public List<UserLayer> UserLayers { get; private set; }

		/// <summary>
		/// Just the directory name, like "C:\MyPictures".
		/// </summary>
		public string Pathname { get; set; }

		/// <summary>
		/// Directory and file name, like "C:\MyPictures\dog.jpg".
		/// </summary>
		public string PathAndFileName {
			get { return System.IO.Path.Combine (Pathname, Filename); }
			set {
				if (string.IsNullOrEmpty (value)) {
					Pathname = string.Empty;
					Filename = string.Empty;
				} else {
					Pathname = System.IO.Path.GetDirectoryName (value);
					Filename = System.IO.Path.GetFileName (value);
				}
			}
		}

		public Layer SelectionLayer {
			get { return selection_layer; }
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

		public delegate void LayerCloneEvent();
		#endregion

		#region Public Methods
		// Adds a new layer above the current one
		public UserLayer AddNewLayer(string name)
		{
			UserLayer layer;

			if (string.IsNullOrEmpty (name))
				layer = CreateLayer ();
			else
				layer = CreateLayer (name);

			UserLayers.Insert (current_layer + 1, layer);

			if (UserLayers.Count == 1)
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
			while (UserLayers.Count > 0) {
				Layer l = UserLayers[UserLayers.Count - 1];
				UserLayers.RemoveAt (UserLayers.Count - 1);
				(l.Surface as IDisposable).Dispose ();
			}

			current_layer = -1;
			PintaCore.Layers.OnLayerRemoved ();
		}
		
		// Clean up any native resources we had
		public void Close ()
		{
			// Dispose all of our layers
			while (UserLayers.Count > 0) {
				Layer l = UserLayers[UserLayers.Count - 1];
				UserLayers.RemoveAt (UserLayers.Count - 1);
				(l.Surface as IDisposable).Dispose ();
			}

			current_layer = -1;

			if (tool_layer != null)
				(tool_layer.Surface as IDisposable).Dispose ();

			if (selection_layer != null)
				(selection_layer.Surface as IDisposable).Dispose ();

			Selection.Dispose ();
			PreviousSelection.Dispose ();

			Workspace.History.Clear ();
		}

		public Context CreateClippedContext ()
		{
			Context g = new Context (CurrentUserLayer.Surface);
			Selection.Clip (g);
			return g;
		}

		public Context CreateClippedContext (bool antialias)
		{
			Context g = new Context (CurrentUserLayer.Surface);
			Selection.Clip (g);
			g.Antialias = antialias ? Antialias.Subpixel : Antialias.None;
			return g;
		}

		public Context CreateClippedToolContext ()
		{
			Context g = new Context (ToolLayer.Surface);
			Selection.Clip (g);
			return g;
		}

		public Context CreateClippedToolContext (bool antialias)
		{
			Context g = new Context (ToolLayer.Surface);
			Selection.Clip (g);
			g.Antialias = antialias ? Antialias.Subpixel : Antialias.None;
			return g;
		}

		public UserLayer CreateLayer ()
		{
			return CreateLayer (string.Format ("{0} {1}", Catalog.GetString ("Layer"), layer_name_int++));
		}

		public UserLayer CreateLayer (int width, int height)
		{
			return CreateLayer (string.Format ("{0} {1}", Catalog.GetString ("Layer"), layer_name_int++), width, height);
		}

		public UserLayer CreateLayer (string name)
		{
			return CreateLayer (name, ImageSize.Width, ImageSize.Height);
		}

		public UserLayer CreateLayer(string name, int width, int height)
		{
			Cairo.ImageSurface surface = new Cairo.ImageSurface (Cairo.Format.ARGB32, width, height);
			UserLayer layer = new UserLayer(surface) { Name = name };

			return layer;
		}

		public void CreateSelectionLayer ()
		{
			Layer old = selection_layer;

			selection_layer = CreateLayer ();

			if (old != null)
				(old.Surface as IDisposable).Dispose ();
		}

		public void CreateSelectionLayer (int width, int height)
		{
			Layer old = selection_layer;

			selection_layer = CreateLayer (width, height);

			if (old != null)
				(old.Surface as IDisposable).Dispose ();
		}

		// Delete the current layer
		public void DeleteCurrentLayer ()
		{
			Layer layer = CurrentUserLayer;

			UserLayers.RemoveAt (current_layer);

			// Only change this if this wasn't already the bottom layer
			if (current_layer > 0)
				current_layer--;

			layer.PropertyChanged -= RaiseLayerPropertyChangedEvent;

			PintaCore.Layers.OnLayerRemoved ();
		}

		// Delete the layer
		public void DeleteLayer (int index, bool dispose)
		{
			Layer layer = UserLayers[index];

			UserLayers.RemoveAt (index);

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
			SelectionLayer.Transform.InitIdentity();
		}

		// Duplicate current layer
		public UserLayer DuplicateCurrentLayer()
		{
			UserLayer source = CurrentUserLayer;
			UserLayer layer = CreateLayer(string.Format("{0} {1}", source.Name, Catalog.GetString("copy")));

			using (Cairo.Context g = new Cairo.Context (layer.Surface)) {
				g.SetSource (source.Surface);
				g.Paint ();
			}

			layer.Hidden = source.Hidden;
			layer.Opacity = source.Opacity;
			layer.Tiled = source.Tiled;

			UserLayers.Insert (++current_layer, layer);

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

			using (Cairo.Context g = new Cairo.Context (CurrentUserLayer.Surface)) {
				selection.Clip (g);
				layer.DrawWithOperator(g, layer.Surface, Operator.Source, 1.0f, true);
			}

			DestroySelectionLayer ();
			Workspace.Invalidate ();

			Workspace.History.PushNewItem (hist);
		}
		
		// Flatten image
		public void FlattenImage ()
		{
			if (UserLayers.Count < 2)
				throw new InvalidOperationException ("Cannot flatten image because there is only one layer.");

			// Find the "bottom" layer
			var bottom_layer = UserLayers[0];
			var old_surf = bottom_layer.Surface;

			// Replace the bottom surface with the flattened image,
			// and dispose the old surface
			bottom_layer.Surface = GetFlattenedImage ();
			(old_surf as IDisposable).Dispose ();

			// Reset our layer pointer to the only remaining layer
			current_layer = 0;

			// Delete all other layers
			while (UserLayers.Count > 1)
				UserLayers.RemoveAt (1);

			PintaCore.Layers.OnLayerRemoved ();
			Workspace.Invalidate ();
		}

		// Flip image horizontally
		public void FlipImageHorizontal ()
		{
			foreach (var layer in UserLayers)
				layer.FlipHorizontal ();

			Workspace.Invalidate ();
		}

		// Flip image vertically
		public void FlipImageVertical ()
		{
			foreach (var layer in UserLayers)
				layer.FlipVertical ();

			Workspace.Invalidate ();
		}
		
		public ImageSurface GetClippedLayer (int index)
		{
			Cairo.ImageSurface surf = new Cairo.ImageSurface (Cairo.Format.Argb32, ImageSize.Width, ImageSize.Height);

			using (Cairo.Context g = new Cairo.Context (surf)) {
				g.AppendPath(Selection.SelectionPath);
				g.Clip ();

				g.SetSource (UserLayers[index].Surface);
				g.Paint ();
			}

			return surf;
		}

		/// <summary>
		/// Gets the final pixel color for the given point, taking layers, opacity, and blend modes into account.
		/// </summary>
		public ColorBgra GetComputedPixel (int x, int y)
		{
			using (var dst = new ImageSurface (Format.Argb32, 1, 1)) {
				using (var g = new Context (dst)) {
					foreach (var layer in GetLayersToPaint ()) {
						var color = layer.Surface.GetColorBgraUnchecked (x, y).ToStraightAlpha ().ToCairoColor ();

						g.SetBlendMode (layer.BlendMode);
						g.SetSourceColor (color);

						g.Rectangle (dst.GetBounds ().ToCairoRectangle ());
						g.PaintWithAlpha (layer.Opacity);
					}
				}

				return dst.GetColorBgraUnchecked (0, 0);
			}
		}

		public ImageSurface GetFlattenedImage ()
		{
			// Create a new image surface
			var surf = new Cairo.ImageSurface (Cairo.Format.Argb32, ImageSize.Width, ImageSize.Height);

			// Blend each visible layer onto our surface
			foreach (var layer in GetLayersToPaint (include_tool_layer: false)) {
				using (var g = new Context (surf))
					layer.Draw (g);
			}

			surf.MarkDirty ();
			return surf;
		}

		public List<Layer> GetLayersToPaint (bool include_tool_layer = true)
		{
			List<Layer> paint = new List<Layer> ();

			foreach (var layer in UserLayers) {
				if (!layer.Hidden)
					paint.Add (layer);

				if (layer == CurrentUserLayer) {
					if (!ToolLayer.Hidden && include_tool_layer)
						paint.Add (ToolLayer);

					if (ShowSelectionLayer && (!SelectionLayer.Hidden))
						paint.Add (SelectionLayer);
				}

				if (!layer.Hidden)
				{
					foreach (ReEditableLayer rel in layer.ReEditableLayers)
					{
						//Make sure that each UserLayer's ReEditableLayer is in use before adding it to the List of Layers to Paint.
						if (rel.IsLayerSetup)
						{
							paint.Add(rel.Layer);
						}
					}
				}
			}

			return paint;
		}

		/// <param name="canvasOnly">false for the whole selection, true for the part only on our canvas</param>
		public Gdk.Rectangle GetSelectedBounds (bool canvasOnly)
		{
			var bounds = Selection.SelectionPath.GetBounds();

			if (canvasOnly)
				bounds = ClampToImageSize (bounds);

			return bounds;
		}

		public int IndexOf(UserLayer layer)
		{
			return UserLayers.IndexOf (layer);
		}

		// Adds a new layer above the current one
		public void Insert(UserLayer layer, int index)
		{
			UserLayers.Insert (index, layer);

			if (UserLayers.Count == 1)
				current_layer = 0;

			layer.PropertyChanged += RaiseLayerPropertyChangedEvent;

			PintaCore.Layers.OnLayerAdded ();
		}
		
		// Flatten current layer
		public void MergeCurrentLayerDown ()
		{
			if (current_layer == 0)
				throw new InvalidOperationException ("Cannot flatten layer because current layer is the bottom layer.");

			// Get our source and destination layers
			var source = CurrentUserLayer;
			var dest = UserLayers[current_layer - 1];

			// Blend the layers
			using (var g = new Context (dest.Surface))
				source.Draw (g);

			DeleteCurrentLayer ();
		}
		
		// Move current layer down
		public void MoveCurrentLayerDown ()
		{
			if (current_layer == 0)
				throw new InvalidOperationException ("Cannot move layer down because current layer is the bottom layer.");

			UserLayer layer = CurrentUserLayer;
			UserLayers.RemoveAt (current_layer);
			UserLayers.Insert (--current_layer, layer);

			PintaCore.Layers.OnSelectedLayerChanged ();

			Workspace.Invalidate ();
		}

		// Move current layer up
		public void MoveCurrentLayerUp ()
		{
			if (current_layer == UserLayers.Count)
				throw new InvalidOperationException ("Cannot move layer up because current layer is the top layer.");

			UserLayer layer = CurrentUserLayer;
			UserLayers.RemoveAt (current_layer);
			UserLayers.Insert (++current_layer, layer);

			PintaCore.Layers.OnSelectedLayerChanged ();

			Workspace.Invalidate ();
		}
		
		public void ResetSelectionPaths()
		{
			var rect = new Cairo.Rectangle (0, 0, ImageSize.Width, ImageSize.Height);
			Selection.CreateRectangleSelection (rect);
			PreviousSelection.CreateRectangleSelection (rect);
			Selection.Visible = false;
			PreviousSelection.Visible = false;
		}

		/// <summary>
		/// Resizes the canvas.
		/// </summary>
		/// <param name="width">The new width of the canvas.</param>
		/// <param name="height">The new height of the canvas.</param>
		/// <param name="anchor">Direction in which to adjust the canvas</param>
		/// <param name='compoundAction'>
		/// Optionally, the history item for resizing the canvas can be added to
		/// a CompoundHistoryItem if it is part of a larger action (e.g. pasting an image).
		/// </param>
		public void ResizeCanvas (int width, int height, Anchor anchor, CompoundHistoryItem compoundAction)
		{
			double scale;

			if (ImageSize.Width == width && ImageSize.Height == height)
				return;

			PintaCore.Tools.Commit ();

			ResizeHistoryItem hist = new ResizeHistoryItem (ImageSize);
			hist.Icon = "Menu.Image.CanvasSize.png";
			hist.Text = Catalog.GetString ("Resize Canvas");
			hist.StartSnapshotOfImage ();

			scale = Workspace.Scale;

			ImageSize = new Gdk.Size (width, height);

			foreach (var layer in UserLayers)
				layer.ResizeCanvas (width, height, anchor);

			hist.FinishSnapshotOfImage ();

			if (compoundAction != null) {
				compoundAction.Push (hist);
			} else {
				Workspace.History.PushNewItem (hist);
			}

			ResetSelectionPaths ();

			Workspace.Scale = scale;
		}
		
		public void ResizeImage (int width, int height)
		{
			double scale;

			if (ImageSize.Width == width && ImageSize.Height == height)
				return;

			PintaCore.Tools.Commit ();

			ResizeHistoryItem hist = new ResizeHistoryItem (ImageSize);
			hist.StartSnapshotOfImage ();

			scale = Workspace.Scale;

			ImageSize = new Gdk.Size (width, height);

			foreach (var layer in UserLayers)
				layer.Resize (width, height);

			hist.FinishSnapshotOfImage ();

			Workspace.History.PushNewItem (hist);

			ResetSelectionPaths ();

			Workspace.Scale = scale;
		}
		
		// Rotate image 180 degrees (flip H+V)
		public void RotateImage180 ()
		{
			RotateImage (180);
		}

		public void RotateImageCW ()
		{
			RotateImage (90);
		}

		public void RotateImageCCW ()
		{
			RotateImage (-90);
		}

		/// <summary>
		/// Rotates the image by the specified angle (in degrees)
		/// </summary>
		private void RotateImage (double angle)
		{
			var new_size = Layer.RotateDimensions (ImageSize, angle);
			foreach (var layer in UserLayers)
				layer.Rotate (angle, new_size);

			ImageSize = new_size;
			Workspace.CanvasSize = new_size;

			PintaCore.Actions.View.UpdateCanvasScale ();
			Workspace.Invalidate ();
		}

		// Returns true if successful, false if canceled
		public bool Save (bool saveAs)
		{
			return PintaCore.Actions.File.RaiseSaveDocument (this, saveAs);
		}

		public void SetCurrentUserLayer (int i)
		{
			// Ensure that the current tool's modifications are finalized before
			// switching layers.
			PintaCore.Tools.CurrentTool.DoCommit ();

			current_layer = i;
			PintaCore.Layers.OnSelectedLayerChanged ();
		}

		public void SetCurrentUserLayer(UserLayer layer)
		{
			SetCurrentUserLayer (UserLayers.IndexOf (layer));
		}

		/// <summary>
		/// Pastes an image from the clipboard.
		/// </summary>
		/// <param name="toNewLayer">Set to TRUE to paste into a
		/// new layer.  Otherwise, will paste to the current layer.</param>
		/// <param name="x">Optional. Location within image to paste to.
		/// Position will be adjusted if pasted image would hang
		/// over right or bottom edges of canvas.</param>
		/// <param name="y">Optional. Location within image to paste to.
		/// Position will be adjusted if pasted image would hang
		/// over right or bottom edges of canvas.</param>
		public void Paste (bool toNewLayer, int x = 0, int y = 0)
		{
			// Create a compound history item for recording several
			// operations so that they can all be undone/redone together.
			CompoundHistoryItem paste_action;
			if (toNewLayer)
			{
				paste_action = new CompoundHistoryItem (Stock.Paste, Catalog.GetString ("Paste Into New Layer"));
			}
			else
			{
				paste_action = new CompoundHistoryItem (Stock.Paste, Catalog.GetString ("Paste"));
			}

			Gtk.Clipboard cb = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

			// See if the current tool wants to handle the paste
			// operation (e.g., the text tool could paste text)
			if (!toNewLayer)
			{
				if (PintaCore.Tools.CurrentTool.TryHandlePaste (cb))
					return;
			}

			PintaCore.Tools.Commit ();

			// Don't dispose this, as we're going to give it to the history
			Gdk.Pixbuf cbImage = null;

			if (cb.WaitIsImageAvailable ()) {
				cbImage = cb.WaitForImage ();
			}

			if (cbImage == null)
			{
				ShowClipboardEmptyDialog();
				return;
			}

			Gdk.Size canvas_size = PintaCore.Workspace.ImageSize;

			// If the image being pasted is larger than the canvas size, allow the user to optionally resize the canvas
			if (cbImage.Width > canvas_size.Width || cbImage.Height > canvas_size.Height)
			{
				ResponseType response = ShowExpandCanvasDialog ();
			
				if (response == ResponseType.Accept)
				{
					var new_width = Math.Max(canvas_size.Width, cbImage.Width);
					var new_height = Math.Max(canvas_size.Height, cbImage.Height);
                    PintaCore.Workspace.ResizeCanvas(new_width, new_height, Pinta.Core.Anchor.Center, paste_action);
                    PintaCore.Actions.View.UpdateCanvasScale ();
				}
				else if (response == ResponseType.Cancel || response == ResponseType.DeleteEvent)
				{
					return;
				}
			}

			// If the pasted image would fall off bottom- or right-
			// side of image, adjust paste position
			x = Math.Max (0, Math.Min (x, canvas_size.Width - cbImage.Width));
			y = Math.Max (0, Math.Min (y, canvas_size.Height - cbImage.Height));

			// If requested, create a new layer, make it the current
			// layer and record it's creation in the history
			if (toNewLayer)
			{
				UserLayer l = AddNewLayer (string.Empty);
				SetCurrentUserLayer (l);
				paste_action.Push (new AddLayerHistoryItem ("Menu.Layers.AddNewLayer.png", Catalog.GetString ("Add New Layer"), UserLayers.IndexOf (l)));
			}

			// Copy the paste to the temp layer, which should be at least the size of this document.
			CreateSelectionLayer (Math.Max(ImageSize.Width, cbImage.Width),
								  Math.Max(ImageSize.Height, cbImage.Height));
			ShowSelectionLayer = true;
			
			using (Cairo.Context g = new Cairo.Context (SelectionLayer.Surface))
			{
				g.DrawPixbuf (cbImage, new Cairo.Point (0, 0));
			}

			SelectionLayer.Transform.InitIdentity();
			SelectionLayer.Transform.Translate (x, y);

			PintaCore.Tools.SetCurrentTool (Catalog.GetString ("Move Selected Pixels"));
			
			DocumentSelection old_selection = Selection.Clone();

			Selection.CreateRectangleSelection (new Cairo.Rectangle (x, y, cbImage.Width, cbImage.Height));
			selection.Visible = true;

			Workspace.Invalidate ();

			paste_action.Push (new PasteHistoryItem (cbImage, old_selection));
			History.PushNewItem (paste_action);
		}

		private ResponseType ShowExpandCanvasDialog ()
		{
			const string markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}";
			string primary = Catalog.GetString ("Image larger than canvas");
			string secondary = Catalog.GetString ("The image being pasted is larger than the canvas size. What would you like to do?");
			string message = string.Format (markup, primary, secondary);

			var enlarge_dialog = new MessageDialog (PintaCore.Chrome.MainWindow, DialogFlags.Modal, MessageType.Question, ButtonsType.None, message);
			enlarge_dialog.AddButton (Catalog.GetString ("Expand canvas"), ResponseType.Accept);
			enlarge_dialog.AddButton (Catalog.GetString ("Don't change canvas size"), ResponseType.Reject);
			enlarge_dialog.AddButton (Stock.Cancel, ResponseType.Cancel);
			enlarge_dialog.DefaultResponse = ResponseType.Accept;

			ResponseType response = (ResponseType)enlarge_dialog.Run ();

			enlarge_dialog.Destroy ();

			return response;
		}

		public static void ShowClipboardEmptyDialog()
		{
			var primary = Catalog.GetString ("Image cannot be pasted");
			var secondary = Catalog.GetString ("The clipboard does not contain an image.");
			var markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}\n";
			markup = string.Format (markup, primary, secondary);

			var md = new MessageDialog (Pinta.Core.PintaCore.Chrome.MainWindow, DialogFlags.Modal,
							MessageType.Error, ButtonsType.None, true,
							markup);

			md.AddButton (Stock.Ok, ResponseType.Yes);

			md.Run ();
			md.Destroy ();
		}

		/// <summary>
		/// Signal to the TextTool that an ImageSurface was cloned.
		/// </summary>
		public void SignalSurfaceCloned()
		{
			if (LayerCloned != null)
			{
				LayerCloned();
			}
		}
		#endregion

		#region Protected Methods
		protected void OnIsDirtyChanged ()
		{
			if (IsDirtyChanged != null)
				IsDirtyChanged (this, EventArgs.Empty);
		}

		protected void OnRenamed ()
		{
			if (Renamed != null)
				Renamed (this, EventArgs.Empty);
		}
		#endregion

		#region Private Methods
		private void RaiseLayerPropertyChangedEvent (object sender, PropertyChangedEventArgs e)
		{
			PintaCore.Layers.RaiseLayerPropertyChangedEvent (sender, e);
		}

		private void OnSelectionChanged ()
		{
			if (SelectionChanged != null)
				SelectionChanged.Invoke(this, EventArgs.Empty);
		}
		#endregion

		#region Public Events
		public event EventHandler IsDirtyChanged;
		public event EventHandler Renamed;
		public event LayerCloneEvent LayerCloned;
		public event EventHandler SelectionChanged;

		#endregion
	}
}
