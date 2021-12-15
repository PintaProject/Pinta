using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Cairo;

namespace Pinta.Core
{
	public class DocumentLayers
	{
		private readonly Document document;
		private readonly List<UserLayer> user_layers = new ();

		private int layer_name_int = 2;

		// The layer for tools to use until their output is committed
		private Layer? tool_layer;

		// The layer used for selections
		private Layer? selection_layer;

		public DocumentLayers (Document document)
		{
			this.document = document;
		}

		public event EventHandler? LayerAdded;
		public event EventHandler? LayerRemoved;
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
					tool_layer?.Surface.Dispose ();
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
			UserLayer layer;

			if (string.IsNullOrEmpty (name))
				layer = CreateLayer ();
			else
				layer = CreateLayer (name);

			user_layers.Insert (CurrentUserLayerIndex + 1, layer);

			if (user_layers.Count == 1)
				CurrentUserLayerIndex = 0;

			layer.PropertyChanged += RaiseLayerPropertyChangedEvent;

			LayerAdded?.Invoke (this, EventArgs.Empty);
			PintaCore.Layers.OnLayerAdded ();
			return layer;
		}


		/// <summary>
		/// Disposes all user created and internal layers.
		/// </summary>
		internal void Close ()
		{
			// Dispose all of our layers
			while (user_layers.Count > 0) {
				var l = user_layers[user_layers.Count - 1];
				user_layers.RemoveAt (user_layers.Count - 1);
				l.Surface.Dispose ();
			}

			CurrentUserLayerIndex = -1;

			tool_layer?.Surface.Dispose ();
			selection_layer?.Surface.Dispose ();
		}

		/// <summary>
		/// Returns the number of user layers.
		/// </summary>
		public int Count () => user_layers.Count;

		/// <summary>
		/// Creates a new layer, but does not add it to the layer collection.
		/// </summary>
		public UserLayer CreateLayer (string? name = null, int? width = null, int? height = null)
		{
			// Translators: {0} is a unique id for new layers, e.g. "Layer 2".
			name ??= Translations.GetString ("Layer {0}", layer_name_int++);
			width ??= document.ImageSize.Width;
			height ??= document.ImageSize.Height;

			var surface = CairoExtensions.CreateImageSurface (Format.ARGB32, width.Value, height.Value);
			var layer = new UserLayer (surface) { Name = name };

			return layer;
		}

		/// <summary>
		/// Creates a new SelectionLayer.
		/// </summary>
		[MemberNotNull (nameof (selection_layer))]
		public void CreateSelectionLayer ()
		{
			var old = selection_layer;

			selection_layer = CreateLayer ();

			old?.Surface.Dispose ();
		}

		/// <summary>
		/// Creates a new SelectionLayer with the specified dimensions.
		/// </summary>
		[MemberNotNull (nameof (selection_layer))]
		public void CreateSelectionLayer (int width, int height)
		{
			var old = selection_layer;

			selection_layer = CreateLayer (null, width, height);

			old?.Surface.Dispose ();
		}

		/// <summary>
		/// Deletes the current layer and removes it from the layer collection.
		/// </summary>
		public void DeleteCurrentLayer ()
		{
			var layer = CurrentUserLayer;

			user_layers.RemoveAt (CurrentUserLayerIndex);

			// Only change this if this wasn't already the bottom layer
			if (CurrentUserLayerIndex > 0)
				CurrentUserLayerIndex--;

			layer.PropertyChanged -= RaiseLayerPropertyChangedEvent;

			LayerRemoved?.Invoke (this, EventArgs.Empty);
			PintaCore.Layers.OnLayerRemoved ();
		}

		/// <summary>
		/// Deletes the user layer at the specified index and removes it from the
		/// layer collection, optionally Disposing the layer surface.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="dispose"></param>
		public void DeleteLayer (int index, bool dispose)
		{
			var layer = user_layers[index];

			user_layers.RemoveAt (index);

			if (dispose)
				layer.Surface.Dispose ();

			// Only change this if this wasn't already the bottom layer
			if (CurrentUserLayerIndex > 0)
				CurrentUserLayerIndex--;

			layer.PropertyChanged -= RaiseLayerPropertyChangedEvent;

			LayerRemoved?.Invoke (this, EventArgs.Empty);
			PintaCore.Layers.OnLayerRemoved ();
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
			var source = CurrentUserLayer;
			// Translators: this is the auto-generated name for a duplicated layer.
			// {0} is the name of the source layer. Example: "Layer 3 copy".
			var layer = CreateLayer (Translations.GetString ("{0} copy", source.Name));

			using (var g = new Context (layer.Surface)) {
				g.SetSource (source.Surface);
				g.Paint ();
			}

			layer.Hidden = source.Hidden;
			layer.Opacity = source.Opacity;
			layer.Tiled = source.Tiled;

			user_layers.Insert (++CurrentUserLayerIndex, layer);

			layer.PropertyChanged += RaiseLayerPropertyChangedEvent;

			LayerAdded?.Invoke (this, EventArgs.Empty);
			PintaCore.Layers.OnLayerAdded ();

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
			var bottom_layer = user_layers[0];
			var old_surf = bottom_layer.Surface;

			// Replace the bottom surface with the flattened image,
			// and dispose the old surface
			bottom_layer.Surface = GetFlattenedImage ();
			old_surf.Dispose ();

			// Reset our layer pointer to the only remaining layer
			CurrentUserLayerIndex = 0;

			// Delete all other layers
			while (user_layers.Count > 1)
				user_layers.RemoveAt (1);

			LayerRemoved?.Invoke (this, EventArgs.Empty);
			PintaCore.Layers.OnLayerRemoved ();
			document.Workspace.Invalidate ();
		}

		/// <summary>
		/// Gets a copy of the specified layer, clipped to the current selection.
		/// </summary>
		public ImageSurface GetClippedLayer (int index)
		{
			var surf = CairoExtensions.CreateImageSurface (Format.Argb32, document.ImageSize.Width, document.ImageSize.Height);

			using (var g = new Context (surf)) {
				g.AppendPath (document.Selection.SelectionPath);
				g.Clip ();

				g.SetSource (user_layers[index].Surface);
				g.Paint ();
			}

			return surf;
		}

		/// <summary>
		/// Returns all layers flattened to a new surface.
		/// </summary>
		internal ImageSurface GetFlattenedImage ()
		{
			// Create a new image surface
			var surf = CairoExtensions.CreateImageSurface (Format.Argb32, document.ImageSize.Width, document.ImageSize.Height);

			// Blend each visible layer onto our surface
			foreach (var layer in GetLayersToPaint (includeToolLayer: false)) {
				using (var g = new Context (surf))
					layer.Draw (g);
			}

			surf.MarkDirty ();
			return surf;
		}

		/// <summary>
		/// Returns all layers that are visible and need to be painted, optionally
		/// including tool and selection layers.
		/// </summary>
		public List<Layer> GetLayersToPaint (bool includeToolLayer = true)
		{
			var paint_layers = new List<Layer> ();

			foreach (var layer in user_layers) {
				if (!layer.Hidden)
					paint_layers.Add (layer);

				if (layer == CurrentUserLayer) {
					if (includeToolLayer && tool_layer is not null && !ToolLayer.Hidden)
						paint_layers.Add (ToolLayer);

					if (ShowSelectionLayer && (!SelectionLayer.Hidden))
						paint_layers.Add (SelectionLayer);
				}

				if (!layer.Hidden) {
					foreach (var rel in layer.ReEditableLayers) {
						//Make sure that each UserLayer's ReEditableLayer is in use before adding it to the List of Layers to Paint.
						if (rel.IsLayerSetup)
							paint_layers.Add (rel.Layer);
					}
				}
			}

			return paint_layers;
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

			LayerAdded?.Invoke (this, EventArgs.Empty);
			PintaCore.Layers.OnLayerAdded ();
		}

		/// <summary>
		/// Merges the current layer with the one below it.
		/// </summary>
		public void MergeCurrentLayerDown ()
		{
			if (CurrentUserLayerIndex == 0)
				throw new InvalidOperationException ("Cannot flatten layer because current layer is the bottom layer.");

			// Get our source and destination layers
			var source = CurrentUserLayer;
			var dest = user_layers[CurrentUserLayerIndex - 1];

			// Blend the layers
			using (var g = new Context (dest.Surface))
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

			var layer = CurrentUserLayer;
			user_layers.RemoveAt (CurrentUserLayerIndex);
			user_layers.Insert (--CurrentUserLayerIndex, layer);

			SelectedLayerChanged?.Invoke (this, EventArgs.Empty);
			PintaCore.Layers.OnSelectedLayerChanged ();

			document.Workspace.Invalidate ();
		}

		/// <summary>
		/// Moves the current layer up 1 position in the layer collection.
		/// </summary>
		public void MoveCurrentLayerUp ()
		{
			if (CurrentUserLayerIndex == user_layers.Count)
				throw new InvalidOperationException ("Cannot move layer up because current layer is the top layer.");

			var layer = CurrentUserLayer;
			user_layers.RemoveAt (CurrentUserLayerIndex);
			user_layers.Insert (++CurrentUserLayerIndex, layer);

			SelectedLayerChanged?.Invoke (this, EventArgs.Empty);
			PintaCore.Layers.OnSelectedLayerChanged ();

			document.Workspace.Invalidate ();
		}

		/// <summary>
		/// Set the current user layer to the index specified.
		/// </summary>
		public void SetCurrentUserLayer (int i)
		{
			// Ensure that the current tool's modifications are finalized before
			// switching layers.
			PintaCore.Tools.CurrentTool?.DoCommit (document);

			CurrentUserLayerIndex = i;
			SelectedLayerChanged?.Invoke (this, EventArgs.Empty);
			PintaCore.Layers.OnSelectedLayerChanged ();
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
			PintaCore.Layers.RaiseLayerPropertyChangedEvent (sender, e);
		}
	}
}
