using System;
using Pinta.Core;

namespace Pinta.Effects;

internal sealed class MockWorkspaceService : IWorkspaceService
{
	public Document ActiveDocument => throw new NotImplementedException ();

	public DocumentWorkspace ActiveWorkspace => throw new NotImplementedException ();

	public bool HasOpenDocuments => throw new NotImplementedException ();

	public SelectionModeHandler SelectionHandler => throw new NotImplementedException ();

#pragma warning disable CS0067
	// CS0067 is the compiler warning that tells us these events are never used
	public event EventHandler? ActiveDocumentChanged;
	public event EventHandler? SelectionChanged;
#pragma warning restore CS0067
}
