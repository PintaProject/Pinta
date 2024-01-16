using System;
using System.Collections.ObjectModel;
using Gio;
using Pinta.Core;

namespace Pinta.Effects;

internal sealed class MockWorkspaceService : IWorkspaceService
{
	public Document ActiveDocument => throw new NotImplementedException ();

	public DocumentWorkspace ActiveWorkspace => throw new NotImplementedException ();

	public bool HasOpenDocuments => throw new NotImplementedException ();

	public SelectionModeHandler SelectionHandler => throw new NotImplementedException ();

	public Document? ActiveDocumentOrDefault => throw new NotImplementedException ();

	public ReadOnlyCollection<Document> OpenDocuments => throw new NotImplementedException ();

	public Size ImageSize { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }
	public Size CanvasSize { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }
	public double Scale { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }

	public PointD Offset => throw new NotImplementedException ();

	public event EventHandler? ActiveDocumentChanged;
	public event EventHandler? SelectionChanged;

	public PointD CanvasPointToView (PointD canvas_pos)
	{
		throw new NotImplementedException ();
	}

	public RectangleI ClampToImageSize (RectangleI r)
	{
		throw new NotImplementedException ();
	}

	public Document CreateAndActivateDocument (File? file, string? file_type, Size size)
	{
		throw new NotImplementedException ();
	}

	public void Invalidate ()
	{
		throw new NotImplementedException ();
	}

	public void Invalidate (RectangleI rect)
	{
		throw new NotImplementedException ();
	}

	public void InvalidateWindowRect (RectangleI windowRect)
	{
		throw new NotImplementedException ();
	}

	public void ResetTitle ()
	{
		throw new NotImplementedException ();
	}

	public void SetActiveDocument (Document document)
	{
		throw new NotImplementedException ();
	}

	public void SetActiveDocumentInternal (Document document)
	{
		throw new NotImplementedException ();
	}

	public PointD ViewPointToCanvas (PointD view_pos)
	{
		throw new NotImplementedException ();
	}
}
