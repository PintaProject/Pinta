using System;
using System.ComponentModel;
using Pinta.Core;

namespace Pinta.Effects;

internal sealed class MockWorkspaceService (Size imageSize) : IWorkspaceService
{
	public Document ActiveDocument => throw new NotImplementedException ();

	public DocumentWorkspace ActiveWorkspace => throw new NotImplementedException ();

	public bool HasOpenDocuments => throw new NotImplementedException ();

	public SelectionModeHandler SelectionHandler => throw new NotImplementedException ();

	public Size ImageSize => imageSize;

#pragma warning disable CS0067
	// CS0067 is the compiler warning that tells us these events are never used
	public event EventHandler? ActiveDocumentChanged;
	public event EventHandler? SelectionChanged;

	public event EventHandler? LayerAdded;
	public event EventHandler? LayerRemoved;
	public event EventHandler? SelectedLayerChanged;
	public event PropertyChangedEventHandler? LayerPropertyChanged;
#pragma warning restore CS0067
}
