using Pinta.Core;

namespace PintaBenchmarks;

internal sealed class MockWorkspaceService : IWorkspaceService
{
	public Document ActiveDocument => throw new NotImplementedException ();

	public DocumentWorkspace ActiveWorkspace => throw new NotImplementedException ();

	public bool HasOpenDocuments => throw new NotImplementedException ();

	public SelectionModeHandler SelectionHandler => throw new NotImplementedException ();

	public event EventHandler? ActiveDocumentChanged;
	public event EventHandler? SelectionChanged;

	public RectangleI ClampToImageSize (RectangleI r)
	{
		throw new NotImplementedException ();
	}
}
