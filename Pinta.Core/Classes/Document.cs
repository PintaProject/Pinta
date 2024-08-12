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
using Cairo;

namespace Pinta.Core;

// The differentiation between Document and DocumentWorkspace is
// somewhat arbitrary.  In general:
// Document - Data about the image itself
// Workspace - Data about Pinta's state for the image
public sealed class Document
{
	private string display_name = string.Empty;
	private Gio.File? file = null;

	private bool is_dirty;

	private DocumentSelection selection = null!; // NRT - Set by constructor via Selection property
	public DocumentSelection Selection {
		get => selection;
		set {
			selection = value;

			// Listen for any changes to this selection.
			selection.SelectionModified += (sender, args) => {
				OnSelectionChanged ();
			};

			// Notify listeners that our selection has been modified.
			OnSelectionChanged ();
		}
	}

	public DocumentSelection PreviousSelection { get; set; } = new ();

	public Document (Size size)
	{
		Selection = new DocumentSelection ();

		Layers = new DocumentLayers (this);
		Workspace = new DocumentWorkspace (this);
		IsDirty = false;
		HasBeenSavedInSession = false;
		ImageSize = size;

		ResetSelectionPaths ();
	}

	#region Public Properties

	/// <summary>
	/// Just the file name, like "dog.jpg".
	/// If HasFile is false, this may be something like "Unsaved image 1".
	/// </summary>
	public string DisplayName {
		get => display_name;
		set {
			display_name = value;
			OnRenamed ();
		}
	}

	/// <summary>
	/// File type of the image, if File is not null. This is an extensions, e.g. "png" or "ora".
	/// This cannot necessarily be derived from the file name's extension (e.g. when opening
	/// a file with no extension or an incorrect extension) so it's easiest to record
	/// the file type used when opening the image.
	/// </summary>
	public string? FileType { get; set; }

	/// <summary>
	/// Identifier for the file location on disk.
	/// </summary>
	public Gio.File? File {
		get => file;
		set {
			file = value;
			DisplayName = file?.GetDisplayName () ?? string.Empty;
		}
	}

	/// <summary>
	/// Whether the document is associated with a file on disk.
	/// </summary>
	public bool HasFile => file is not null;

	/// Whether or not the document has been saved to the file that it is currently associated with in the
	/// current session. This should be false if the Document has not yet been saved, if it was just loaded into
	/// Pinta from a file, or if the user just clicked Save As.
	public bool HasBeenSavedInSession { get; set; }

	public DocumentHistory History => Workspace.History;

	public Core.Size ImageSize { get; set; }

	public bool IsDirty {
		get => is_dirty;
		set {
			if (is_dirty != value) {
				is_dirty = value;
				OnIsDirtyChanged ();
			}
		}
	}

	public DocumentLayers Layers { get; }

	public DocumentWorkspace Workspace { get; }

	public delegate void LayerCloneEvent ();
	#endregion

	#region Public Methods
	public RectangleI ClampToImageSize (RectangleI r)
	{
		int x = Math.Clamp (r.X, 0, ImageSize.Width);
		int y = Math.Clamp (r.Y, 0, ImageSize.Height);
		int width = Math.Min (r.Width, ImageSize.Width - x);
		int height = Math.Min (r.Height, ImageSize.Height - y);
		return new (x, y, width, height);
	}

	public PointD ClampToImageSize (PointD p)
		=> new (
			Math.Clamp (p.X, 0, ImageSize.Width),
			Math.Clamp (p.Y, 0, ImageSize.Height)
		);

	/// <summary>
	/// Sets File to null while keeping the DisplayName the same.
	/// This will force the user to choose a new filename when saving (i.e. "Save As").
	/// </summary>
	public void ClearFileReference ()
	{
		var name = DisplayName;
		File = null;
		FileType = null;
		DisplayName = name;
	}

	// Clean up any native resources we had
	public void Close ()
	{
		Layers.Close ();
		Workspace.History.Clear ();
	}

	public Context CreateClippedContext ()
	{
		Context g = new (Layers.CurrentUserLayer.Surface);
		Selection.Clip (g);
		return g;
	}

	public Context CreateClippedContextFromSurface(ImageSurface surf)
	{
		Context g = new (surf);
		Selection.Clip (g);
		return g;
	}

	public Context CreateClippedContext (bool antialias)
	{
		Context g = new (Layers.CurrentUserLayer.Surface);
		Selection.Clip (g);
		g.Antialias = antialias ? Antialias.Subpixel : Antialias.None;
		return g;
	}

	public Context CreateClippedToolContext ()
	{
		Context g = new (Layers.ToolLayer.Surface);
		Selection.Clip (g);
		return g;
	}

	public Context CreateClippedToolContext (bool antialias)
	{
		Context g = new (Layers.ToolLayer.Surface);
		Selection.Clip (g);
		g.Antialias = antialias ? Antialias.Subpixel : Antialias.None;
		return g;
	}

	public void FinishSelection ()
	{
		// We don't have an uncommitted layer, abort
		if (!Layers.ShowSelectionLayer)
			return;

		FinishPixelsHistoryItem hist = new ();

		hist.TakeSnapshot ();

		Layer layer = Layers.SelectionLayer;

		Context g = new (Layers.CurrentUserLayer.Surface);
		selection.Clip (g);
		layer.DrawWithOperator (g, Operator.Source, opacity: 1.0, transform: true);

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
	public ColorBgra GetComputedPixel (PointI position)
	{
		ImageSurface dst = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			1,
			1);

		Context g = new (dst);

		foreach (var layer in Layers.GetLayersToPaint ()) {

			Color color = layer.Surface.GetColorBgra (position).ToStraightAlpha ().ToCairoColor ();

			g.SetBlendMode (layer.BlendMode);
			g.SetSourceColor (color);

			g.Rectangle (dst.GetBounds ().ToDouble ());
			g.PaintWithAlpha (layer.Opacity);
		}

		return dst.GetColorBgra (PointI.Zero);
	}

	public ImageSurface GetFlattenedImage (bool clip_to_selection = false) => Layers.GetFlattenedImage (clip_to_selection);

	/// <param name="canvasOnly">false for the whole selection, true for the part only on our canvas</param>
	public RectangleI GetSelectedBounds (bool canvasOnly)
	{
		RectangleI bounds = Selection.SelectionPath.GetBounds ();

		if (canvasOnly)
			bounds = ClampToImageSize (bounds);

		return bounds;
	}

	public void ResetSelectionPaths ()
	{
		RectangleD rect = new (0, 0, ImageSize.Width, ImageSize.Height);
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
	public void ResizeCanvas (
		Size newSize,
		Anchor anchor,
		CompoundHistoryItem? compoundAction)
	{
		if (ImageSize == newSize)
			return;

		PintaCore.Tools.Commit ();

		ResizeHistoryItem hist = new (ImageSize) {
			Icon = Resources.Icons.ImageResizeCanvas,
			Text = Translations.GetString ("Resize Canvas")
		};

		hist.StartSnapshotOfImage ();

		double scale = Workspace.Scale;

		ImageSize = newSize;

		foreach (var layer in Layers.UserLayers)
			layer.ResizeCanvas (newSize, anchor);

		hist.FinishSnapshotOfImage ();

		if (compoundAction != null)
			compoundAction.Push (hist);
		else
			Workspace.History.PushNewItem (hist);

		ResetSelectionPaths ();

		Workspace.Scale = scale;
	}

	public void ResizeImage (
		Size newSize,
		ResamplingMode resamplingMode)
	{
		if (ImageSize == newSize)
			return;

		PintaCore.Tools.Commit ();

		ResizeHistoryItem hist = new (ImageSize);

		hist.StartSnapshotOfImage ();

		double scale = Workspace.Scale;

		ImageSize = newSize;

		foreach (var layer in Layers.UserLayers)
			layer.Resize (newSize, resamplingMode);

		hist.FinishSnapshotOfImage ();

		Workspace.History.PushNewItem (hist);

		ResetSelectionPaths ();

		Workspace.Scale = scale;
		PintaCore.Actions.View.UpdateCanvasScale ();
	}

	// Rotate image 180 degrees (flip H+V)
	public void RotateImage180 ()
	{
		RotateImage (new DegreesAngle (180));
	}

	public void RotateImageCW ()
	{
		RotateImage (new DegreesAngle (90));
	}

	public void RotateImageCCW ()
	{
		RotateImage (new DegreesAngle (-90));
	}

	/// <summary>
	/// Rotates the image by the specified angle (in degrees)
	/// </summary>
	private void RotateImage (DegreesAngle angle)
	{
		Size new_size = Layer.RotateDimensions (ImageSize, angle);

		foreach (var layer in Layers.UserLayers)
			layer.Rotate (angle, ImageSize, new_size);

		ImageSize = new_size;
		Workspace.ViewSize = new_size;

		PintaCore.Actions.View.UpdateCanvasScale ();
		ResetSelectionPaths ();
		Workspace.Invalidate ();
	}

	// Returns true if successful, false if canceled
	public bool Save (bool saveAs)
	{
		return PintaCore.Actions.File.RaiseSaveDocument (this, saveAs);
	}

	/// <summary>
	/// Signal to the TextTool that an ImageSurface was cloned.
	/// </summary>
	public void SignalSurfaceCloned ()
	{
		LayerCloned?.Invoke ();
	}
	#endregion

	private void OnIsDirtyChanged ()
	{
		IsDirtyChanged?.Invoke (this, EventArgs.Empty);
	}

	private void OnRenamed ()
	{
		Renamed?.Invoke (this, EventArgs.Empty);
	}

	#region Private Methods
	private void OnSelectionChanged ()
	{
		SelectionChanged?.Invoke (this, EventArgs.Empty);
	}
	#endregion

	#region Public Events
	public event EventHandler? IsDirtyChanged;
	public event EventHandler? Renamed;
	public event LayerCloneEvent? LayerCloned;
	public event EventHandler? SelectionChanged;

	#endregion
}
