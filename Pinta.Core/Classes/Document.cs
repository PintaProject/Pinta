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
		private string filename = string.Empty;
		private bool is_dirty;

		private DocumentSelection selection = null!; // NRT - Set by constructor via Selection property
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

			Layers = new DocumentLayers (this);
			Workspace = new DocumentWorkspace (this);
			IsDirty = false;
			HasFile = false;
			HasBeenSavedInSession = false;
			ImageSize = size;

			ResetSelectionPaths ();
		}

		#region Public Properties
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
		
		public DocumentHistory History { get { return Workspace.History; } }

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

		public DocumentLayers Layers { get; }

		/// <summary>
		/// Just the directory name, like "C:\MyPictures".
		/// </summary>
		public string Pathname { get; set; } = string.Empty;

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
					Pathname = System.IO.Path.GetDirectoryName (value) ?? string.Empty;
					Filename = System.IO.Path.GetFileName (value);
				}
			}
		}

		public DocumentWorkspace Workspace { get; private set; }

		public delegate void LayerCloneEvent();
		#endregion

		#region Public Methods
		// Adds a new layer above the current one
		public Gdk.Rectangle ClampToImageSize (Gdk.Rectangle r)
		{
			int x = Utility.Clamp (r.X, 0, ImageSize.Width);
			int y = Utility.Clamp (r.Y, 0, ImageSize.Height);
			int width = Math.Min (r.Width, ImageSize.Width - x);
			int height = Math.Min (r.Height, ImageSize.Height - y);

			return new Gdk.Rectangle (x, y, width, height);
		}
		
		// Clean up any native resources we had
		public void Close ()
		{
			Layers.Close ();

			Selection.Dispose ();
			PreviousSelection.Dispose ();

			Workspace.History.Clear ();
		}

		public Context CreateClippedContext ()
		{
			Context g = new Context (Layers.CurrentUserLayer.Surface);
			Selection.Clip (g);
			return g;
		}

		public Context CreateClippedContext (bool antialias)
		{
			Context g = new Context (Layers.CurrentUserLayer.Surface);
			Selection.Clip (g);
			g.Antialias = antialias ? Antialias.Subpixel : Antialias.None;
			return g;
		}

		public Context CreateClippedToolContext ()
		{
			Context g = new Context (Layers.ToolLayer.Surface);
			Selection.Clip (g);
			return g;
		}

		public Context CreateClippedToolContext (bool antialias)
		{
			Context g = new Context (Layers.ToolLayer.Surface);
			Selection.Clip (g);
			g.Antialias = antialias ? Antialias.Subpixel : Antialias.None;
			return g;
		}

		public void FinishSelection ()
		{
			// We don't have an uncommitted layer, abort
			if (!Layers.ShowSelectionLayer)
				return;

			FinishPixelsHistoryItem hist = new FinishPixelsHistoryItem ();
			hist.TakeSnapshot ();

			Layer layer = Layers.SelectionLayer;

			using (Cairo.Context g = new Cairo.Context (Layers.CurrentUserLayer.Surface)) {
				selection.Clip (g);
				layer.Draw (g);
			}

			Layers.DestroySelectionLayer ();
			Workspace.Invalidate ();

			Workspace.History.PushNewItem (hist);
		}

		// Flip image horizontally
		public void FlipImageHorizontal ()
		{
			foreach (var layer in Layers.UserLayers)
				layer.FlipHorizontal ();

			Workspace.Invalidate ();
		}

		// Flip image vertically
		public void FlipImageVertical ()
		{
			foreach (var layer in Layers.UserLayers)
				layer.FlipVertical ();

			Workspace.Invalidate ();
		}

		/// <summary>
		/// Gets the final pixel color for the given point, taking layers, opacity, and blend modes into account.
		/// </summary>
		public ColorBgra GetComputedPixel (int x, int y)
		{
			using (var dst = new ImageSurface (Format.Argb32, 1, 1)) {
				using (var g = new Context (dst)) {
					foreach (var layer in Layers.GetLayersToPaint ()) {
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

		public ImageSurface GetFlattenedImage () => Layers.GetFlattenedImage ();

		/// <param name="canvasOnly">false for the whole selection, true for the part only on our canvas</param>
		public Gdk.Rectangle GetSelectedBounds (bool canvasOnly)
		{
			var bounds = Selection.SelectionPath.GetBounds();

			if (canvasOnly)
				bounds = ClampToImageSize (bounds);

			return bounds;
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
		public void ResizeCanvas (int width, int height, Anchor anchor, CompoundHistoryItem? compoundAction)
		{
			double scale;

			if (ImageSize.Width == width && ImageSize.Height == height)
				return;

			PintaCore.Tools.Commit ();

			ResizeHistoryItem hist = new ResizeHistoryItem (ImageSize);
			hist.Icon = Resources.Icons.ImageResizeCanvas;
			hist.Text = Translations.GetString ("Resize Canvas");
			hist.StartSnapshotOfImage ();

			scale = Workspace.Scale;

			ImageSize = new Gdk.Size (width, height);

			foreach (var layer in Layers.UserLayers)
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

			foreach (var layer in Layers.UserLayers)
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
			foreach (var layer in Layers.UserLayers)
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
				paste_action = new CompoundHistoryItem (Resources.StandardIcons.EditPaste, Translations.GetString ("Paste Into New Layer"));
			}
			else
			{
				paste_action = new CompoundHistoryItem (Resources.StandardIcons.EditPaste, Translations.GetString ("Paste"));
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
			Gdk.Pixbuf? cbImage = null;

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
				UserLayer l = Layers.AddNewLayer (string.Empty);
				Layers.SetCurrentUserLayer (l);
				paste_action.Push (new AddLayerHistoryItem (Resources.Icons.LayerNew, Translations.GetString ("Add New Layer"), Layers.IndexOf (l)));
			}

			// Copy the paste to the temp layer, which should be at least the size of this document.
			Layers.CreateSelectionLayer (Math.Max(ImageSize.Width, cbImage.Width),
								  Math.Max(ImageSize.Height, cbImage.Height));
			Layers.ShowSelectionLayer = true;
			
			using (Cairo.Context g = new Cairo.Context (Layers.SelectionLayer.Surface))
			{
				g.DrawPixbuf (cbImage, new Cairo.Point (0, 0));
			}

			Layers.SelectionLayer.Transform.InitIdentity();
			Layers.SelectionLayer.Transform.Translate (x, y);

			PintaCore.Tools.SetCurrentTool (Translations.GetString ("Move Selected Pixels"));
			
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
			string primary = Translations.GetString ("Image larger than canvas");
			string secondary = Translations.GetString ("The image being pasted is larger than the canvas size. What would you like to do?");
			string message = string.Format (markup, primary, secondary);

			using (var enlarge_dialog = new MessageDialog(PintaCore.Chrome.MainWindow, DialogFlags.Modal, MessageType.Question, ButtonsType.None, message))
			{
				enlarge_dialog.AddButton(Translations.GetString("Expand canvas"), ResponseType.Accept);
				enlarge_dialog.AddButton(Translations.GetString("Don't change canvas size"), ResponseType.Reject);
				enlarge_dialog.AddButton(Stock.Cancel, ResponseType.Cancel);
				enlarge_dialog.DefaultResponse = ResponseType.Accept;

				return (ResponseType)enlarge_dialog.Run();
			}	
		}

		public static void ShowClipboardEmptyDialog()
		{
			var primary = Translations.GetString ("Image cannot be pasted");
			var secondary = Translations.GetString ("The clipboard does not contain an image.");
			var markup = "<span weight=\"bold\" size=\"larger\">{0}</span>\n\n{1}\n";
			markup = string.Format (markup, primary, secondary);

			using (var md = new MessageDialog(Pinta.Core.PintaCore.Chrome.MainWindow, DialogFlags.Modal,
							MessageType.Error, ButtonsType.None, true,
							markup))
            {
				md.AddButton(Stock.Ok, ResponseType.Yes);
				md.Run();
			}
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
		private void OnSelectionChanged ()
		{
			if (SelectionChanged != null)
				SelectionChanged.Invoke(this, EventArgs.Empty);
		}
		#endregion

		#region Public Events
		public event EventHandler? IsDirtyChanged;
		public event EventHandler? Renamed;
		public event LayerCloneEvent? LayerCloned;
		public event EventHandler? SelectionChanged;

		#endregion
	}
}
