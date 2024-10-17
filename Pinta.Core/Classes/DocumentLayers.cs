using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Cairo;

namespace Pinta.Core;

public sealed class DocumentLayers
{
	private readonly ToolManager tools;
	private readonly Document document;
	private readonly List<UserLayer> user_layers = new ();

	private int layer_name_int = 2;

	// The layer for tools to use until their output is committed
	private Layer? tool_layer;

	// The layer used for selections
	private Layer? selection_layer;

	public DocumentLayers (
		ToolManager tools,
		Document document)
	{
		this.tools = tools;
		this.document = document;
	}

	public event EventHandler<IndexEventArgs>? LayerAdded;
	public event EventHandler<IndexEventArgs>? LayerRemoved;
	public event EventHandler? SelectedLayerChanged;
	public event PropertyChangedEventHandler? LayerPropertyChanged;

	/// <summary>
	/// Gets the currently selected user created layer.
	/// </summary>
	public UserLayer CurrentUserLayer => user_layers[CurrentUserLayerIndex];

	/// <summary>
	/// Gets the index of the currently selected user created layer.
	/// </summary>
	public int CurrentUserLayerIndex { get; private set; } = -1;

	/// <summary>
	/// Gets the layer used for drawing and managing selections.
	/// </summary>
	public Layer SelectionLayer {
		get {
			if (selection_layer is null)
				CreateSelectionLayer ();

			return selection_layer;
		}
	}

	/// <summary>
	/// Gets or sets whether the Selection layer should be shown.
	/// </summary>
	public bool ShowSelectionLayer { get; set; }

	/// <summary>
	/// Gets a scratch layer for tools to temporarily use until their content
	/// is committed to the actual layer.
	/// </summary>
	public Layer ToolLayer {
		get {
			if (tool_layer is null || tool_layer.Surface.Width != document.ImageSize.Width || tool_layer.Surface.Height != document.ImageSize.Height) {
				tool_layer = CreateLayer ("Tool Layer");
				tool_layer.Hidden = true;
			}

			return tool_layer;
		}
	}

	/// <summary>
	/// Collection of user layers.
	/// </summary>
	public IReadOnlyList<UserLayer> UserLayers => user_layers;

	/// <summary>
	/// Creates a new layer and adds it to the Layer collection after the
	/// currently selected layer.
	/// </summary>
	public UserLayer AddNewLayer (string name)
	{
		UserLayer layer =
			string.IsNullOrEmpty (name)
			? CreateLayer ()
			: CreateLayer (name);

		user_layers.Insert (CurrentUserLayerIndex + 1, layer);

		if (user_layers.Count == 1)
			CurrentUserLayerIndex = 0;

		layer.PropertyChanged += RaiseLayerPropertyChangedEvent;

		LayerAdded?.Invoke (this, new IndexEventArgs (user_layers.Count - 1));

		return layer;
	}


	/// <summary>
	/// Disposes all user created and internal layers.
	/// </summary>
	internal void Close ()
	{
		user_layers.Clear ();
		CurrentUserLayerIndex = -1;

		tool_layer = null;
		selection_layer = null;
	}

	/// <summary>
	/// Returns the number of user layers.
	/// </summary>
	public int Count () => user_layers.Count;

	/// <summary>
	/// Creates a new layer, but does not add it to the layer collection.
	/// </summary>
	public UserLayer CreateLayer (
		string? name = null,
		int? width = null,
		int? height = null)
	{
		// Translators: {0} is a unique id for new layers, e.g. "Layer 2".
		name ??= Translations.GetString ("Layer {0}", layer_name_int++);
		width ??= document.ImageSize.Width;
		height ??= document.ImageSize.Height;

		ImageSurface surface = CairoExtensions.CreateImageSurface (Format.Argb32, width.Value, height.Value);
		UserLayer layer = new (surface) { Name = name };

		return layer;
	}

	/// <summary>
	/// Creates a new SelectionLayer.
	/// </summary>
	[MemberNotNull (nameof (selection_layer))]
	public void CreateSelectionLayer ()
	{
		selection_layer = CreateLayer ();
	}

	/// <summary>
	/// Creates a new SelectionLayer with the specified dimensions.
	/// </summary>
	[MemberNotNull (nameof (selection_layer))]
	public void CreateSelectionLayer (int width, int height)
	{
		selection_layer = CreateLayer (null, width, height);
	}

	/// <summary>
	/// Deletes the current layer and removes it from the layer collection.
	/// </summary>
	public void DeleteCurrentLayer () => DeleteLayer (CurrentUserLayerIndex);

	/// <summary>
	/// Deletes the user layer at the specified index and removes it from the
	/// layer collection.
	/// </summary>
	/// <param name="index"></param>
	public void DeleteLayer (int index)
	{
		var layer = user_layers[index];

		user_layers.RemoveAt (index);

		// Only change this if this wasn't already the bottom layer
		if (CurrentUserLayerIndex > 0)
			CurrentUserLayerIndex--;

		layer.PropertyChanged -= RaiseLayerPropertyChangedEvent;

		LayerRemoved?.Invoke (this, new IndexEventArgs (index));

		document.Workspace.Invalidate ();
	}

	/// <summary>
	/// Hide and reset the SelectionLayer.
	/// </summary>
	public void DestroySelectionLayer ()
	{
		ShowSelectionLayer = false;
		SelectionLayer.Clear ();
		SelectionLayer.Transform.InitIdentity ();
	}

	/// <summary>
	/// Duplicate the currently selected user layer, adding the new
	/// layer to the layer collection after the current layer.
	/// </summary>
	public UserLayer DuplicateCurrentLayer ()
	{
		UserLayer source = CurrentUserLayer;
		// Translators: this is the auto-generated name for a duplicated layer.
		// {0} is the name of the source layer. Example: "Layer 3 copy".
		UserLayer layer = CreateLayer (Translations.GetString ("{0} copy", source.Name));

		Context g = new (layer.Surface);
		g.SetSourceSurface (source.Surface, 0, 0);
		g.Paint ();

		layer.Hidden = source.Hidden;
		layer.Opacity = source.Opacity;

		user_layers.Insert (++CurrentUserLayerIndex, layer);

		layer.PropertyChanged += RaiseLayerPropertyChangedEvent;

		LayerAdded?.Invoke (this, new IndexEventArgs (CurrentUserLayerIndex));

		return layer;
	}

	/// <summary>
	/// Flatten all user layers to a single layer.
	/// </summary>
	public void FlattenLayers ()
	{
		if (user_layers.Count < 2)
			throw new InvalidOperationException ("Cannot flatten image because there is only one layer.");

		// Find the "bottom" layer
		UserLayer bottom_layer = user_layers[0];

		// Replace the bottom surface with the flattened image,
		// and dispose the old surface
		bottom_layer.Surface = GetFlattenedImage ();

		// Reset our layer pointer to the only remaining layer
		CurrentUserLayerIndex = 0;

		// Delete all other layers
		while (user_layers.Count > 1)
			DeleteLayer (user_layers.Count - 1);

		document.Workspace.Invalidate ();
	}

	/// <summary>
	/// Gets a copy of the specified layer, clipped to the current selection.
	/// </summary>
	public ImageSurface GetClippedLayer (int index)
	{
		ImageSurface surf = CairoExtensions.CreateImageSurface (Format.Argb32, document.ImageSize.Width, document.ImageSize.Height);

		Context g = new (surf);
		document.Selection.Clip (g);

		g.SetSourceSurface (user_layers[index].Surface, 0, 0);
		g.Paint ();

		return surf;
	}

	/// <summary>
	/// Returns all layers flattened to a new surface, optionally clipped by the selection.
	/// </summary>
	internal ImageSurface GetFlattenedImage (bool clip_to_selection = false)
	{
		// Create a new image surface
		ImageSurface surf = CairoExtensions.CreateImageSurface (Format.Argb32, document.ImageSize.Width, document.ImageSize.Height);

		Context g = new (surf);

		if (clip_to_selection)
			document.Selection.Clip (g);

		// Blend each visible layer onto our surface
		foreach (var layer in GetLayersToPaint (includeToolLayer: false))
			layer.Draw (g);

		surf.MarkDirty ();
		return surf;
	}

	/// <summary>
	/// Returns all layers that are visible and need to be painted, optionally
	/// including tool and selection layers.
	/// </summary>
	public IEnumerable<Layer> GetLayersToPaint (bool includeToolLayer = true)
	{
		foreach (var layer in user_layers) {
			if (!layer.Hidden)
				yield return layer;

			if (layer == CurrentUserLayer) {
				if (includeToolLayer && tool_layer is not null && !ToolLayer.Hidden)
					yield return ToolLayer;

				if (ShowSelectionLayer && (!SelectionLayer.Hidden))
					yield return SelectionLayer;
			}

			if (!layer.Hidden) {
				foreach (var rel in layer.ReEditableLayers) {
					//Make sure that each UserLayer's ReEditableLayer is in use before adding it to the List of Layers to Paint.
					if (rel.IsLayerSetup)
						yield return rel.Layer;
				}
			}
		}
	}

	/// <summary>
	/// Returns the index of the specified user layer.
	/// </summary>
	public int IndexOf (UserLayer layer)
	{
		return user_layers.IndexOf (layer);
	}

	/// <summary>
	/// Adds the provided layer at the requested index of the layer collection.
	/// </summary>
	public void Insert (UserLayer layer, int index)
	{
		user_layers.Insert (index, layer);

		if (user_layers.Count == 1)
			CurrentUserLayerIndex = 0;

		layer.PropertyChanged += RaiseLayerPropertyChangedEvent;

		LayerAdded?.Invoke (this, new IndexEventArgs (index));

		document.Workspace.Invalidate ();
	}

	/// <summary>
	/// Merges the current layer with the one below it.
	/// </summary>
	public void MergeCurrentLayerDown ()
	{
		if (CurrentUserLayerIndex == 0)
			throw new InvalidOperationException ("Cannot flatten layer because current layer is the bottom layer.");

		// Get our source and destination layers
		UserLayer source = CurrentUserLayer;
		UserLayer dest = user_layers[CurrentUserLayerIndex - 1];

		// Blend the layers
		Context g = new (dest.Surface);
		source.Draw (g);

		DeleteCurrentLayer ();
	}

	/// <summary>
	/// Moves the current layer down 1 position in the layer collection.
	/// </summary>
	public void MoveCurrentLayerDown ()
	{
		if (CurrentUserLayerIndex == 0)
			throw new InvalidOperationException ("Cannot move layer down because current layer is the bottom layer.");

		UserLayer layer = CurrentUserLayer;
		int index = CurrentUserLayerIndex;
		DeleteLayer (index);
		Insert (layer, index - 1);

		SelectedLayerChanged?.Invoke (this, EventArgs.Empty);

		document.Workspace.Invalidate ();
	}

	/// <summary>
	/// Moves the current layer up 1 position in the layer collection.
	/// </summary>
	public void MoveCurrentLayerUp ()
	{
		if (CurrentUserLayerIndex == user_layers.Count)
			throw new InvalidOperationException ("Cannot move layer up because current layer is the top layer.");

		UserLayer layer = CurrentUserLayer;
		int index = CurrentUserLayerIndex;

		DeleteLayer (index);
		Insert (layer, index + 1);

		CurrentUserLayerIndex = index + 1;

		SelectedLayerChanged?.Invoke (this, EventArgs.Empty);

		document.Workspace.Invalidate ();
	}

	/// <summary>
	/// Set the current user layer to the index specified.
	/// </summary>
	public void SetCurrentUserLayer (int i)
	{
		// Ensure that the current tool's modifications are finalized before
		// switching layers.
		tools.CurrentTool?.DoCommit (document);

		CurrentUserLayerIndex = i;
		SelectedLayerChanged?.Invoke (this, EventArgs.Empty);
	}

	/// <summary>
	/// Set the current user layer to the layer specified.
	/// </summary>
	public void SetCurrentUserLayer (UserLayer layer)
	{
		SetCurrentUserLayer (user_layers.IndexOf (layer));
	}

	/// <summary>
	/// Gets the user layer at the specified index.
	/// </summary>
	public UserLayer this[int index] => user_layers[index];

	private void RaiseLayerPropertyChangedEvent (object? sender, PropertyChangedEventArgs e)
	{
		LayerPropertyChanged?.Invoke (sender, e);
	}
}
