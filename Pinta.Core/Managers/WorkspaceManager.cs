//
// WorkspaceManager.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cairo;
using Gtk;

namespace Pinta.Core;

public interface IWorkspaceService
{
	Document ActiveDocument { get; }
	DocumentWorkspace ActiveWorkspace { get; }
	event EventHandler? ActiveDocumentChanged;
	RectangleI ClampToImageSize (RectangleI r);
	bool HasOpenDocuments { get; }
	event EventHandler? SelectionChanged;
	SelectionModeHandler SelectionHandler { get; }
}

public static class WorkspaceServiceExtensions
{
	public static void Invalidate (this IWorkspaceService workspace)
	{
		if (workspace.HasOpenDocuments)
			workspace.ActiveWorkspace.Invalidate ();
	}

	public static void Invalidate (this IWorkspaceService workspace, RectangleI rect)
	{
		workspace.ActiveWorkspace.Invalidate (rect);
	}

	public static void InvalidateWindowRect (this IWorkspaceService workspace, RectangleI windowRect)
	{
		workspace.ActiveWorkspace.InvalidateWindowRect (windowRect);
	}

	/// <summary>
	/// Converts a point from the active document's canvas coordinates to view coordinates.
	/// </summary>
	/// <param name='canvas_pos'>
	/// The position of the canvas point
	/// </param>
	public static PointD CanvasPointToView (this IWorkspaceService workspace, PointD canvas_pos)
	{
		return workspace.ActiveWorkspace.CanvasPointToView (canvas_pos);
	}

	/// <summary>
	/// Converts a point from the active document's view coordinates to canvas coordinates.
	/// </summary>
	/// <param name='canvas_pos'>
	/// The position of the view point
	/// </param>
	public static PointD ViewPointToCanvas (this IWorkspaceService workspace, PointD view_pos)
	{
		return workspace.ActiveWorkspace.ViewPointToCanvas (view_pos);
	}

	public static void ResizeImage (this IWorkspaceService workspace, Size newSize, ResamplingMode resamplingMode)
	{
		workspace.ActiveDocument.ResizeImage (newSize, resamplingMode);
	}

	public static void ResizeCanvas (this IWorkspaceService workspace, Size newSize, Anchor anchor, CompoundHistoryItem? compoundAction)
	{
		workspace.ActiveDocument.ResizeCanvas (newSize, anchor, compoundAction);
	}
}

public sealed class WorkspaceManager : IWorkspaceService
{
	private int active_document_index = -1;
	private int new_file_name = 1;

	public WorkspaceManager ()
	{
		open_documents = new List<Document> ();
		OpenDocuments = new ReadOnlyCollection<Document> (open_documents);
		SelectionHandler = new SelectionModeHandler ();
	}

	public int ActiveDocumentIndex
		=> active_document_index;

	public Document ActiveDocument {
		get {
			if (HasOpenDocuments)
				return open_documents[active_document_index];

			throw new InvalidOperationException ("Tried to get WorkspaceManager.ActiveDocument when there are no open Documents.  Check HasOpenDocuments first.");
		}
	}

	public Document? ActiveDocumentOrDefault
		=> HasOpenDocuments ? open_documents[active_document_index] : null;

	public SelectionModeHandler SelectionHandler { get; }

	public DocumentWorkspace ActiveWorkspace {
		get {
			if (HasOpenDocuments)
				return open_documents[active_document_index].Workspace;

			throw new InvalidOperationException ("Tried to get WorkspaceManager.ActiveWorkspace when there are no open Documents.  Check HasOpenDocuments first.");
		}
	}

	public Size ImageSize {
		get => ActiveDocument.ImageSize;
		set => ActiveDocument.ImageSize = value;
	}

	public Size CanvasSize {
		get => ActiveWorkspace.ViewSize;
		set => ActiveWorkspace.ViewSize = value;
	}

	public PointD Offset
		=> ActiveWorkspace.Offset;

	public double Scale {
		get => ActiveWorkspace.Scale;
		set => ActiveWorkspace.Scale = value;
	}

	private readonly List<Document> open_documents;
	public ReadOnlyCollection<Document> OpenDocuments { get; }
	public bool HasOpenDocuments => open_documents.Count > 0;

	public Document CreateAndActivateDocument (Gio.File? file, string? file_type, Size size)
	{
		Document doc = new Document (size);

		if (file is not null) {

			if (string.IsNullOrEmpty (file_type))
				throw new ArgumentNullException ($"nameof{file_type} must contain value.");

			doc.File = file;
			doc.FileType = file_type;
		} else
			doc.DisplayName = Translations.GetString ("Unsaved Image {0}", new_file_name++);

		open_documents.Add (doc);

		OnDocumentCreated (new DocumentEventArgs (doc));

		SetActiveDocument (doc);

		return doc;
	}

	public void CloseActiveDocument ()
	{
		CloseDocument (ActiveDocument);
	}

	public void CloseDocument (Document document)
	{
		int index = open_documents.IndexOf (document);
		open_documents.Remove (document);

		if (index == active_document_index) {
			// If there's other documents open, switch to one of them
			if (HasOpenDocuments) {
				if (index > 0)
					SetActiveDocument (index - 1);
				else
					SetActiveDocument (index);
			} else {
				active_document_index = -1;
				OnActiveDocumentChanged (EventArgs.Empty);
			}
		}

		document.Close ();

		OnDocumentClosed (new DocumentEventArgs (document));
	}

	public Document NewDocument (Size imageSize, Color backgroundColor)
	{
		Document doc = CreateAndActivateDocument (null, null, imageSize);
		doc.Workspace.ViewSize = imageSize;

		// Start with an empty white layer
		Layer background = doc.Layers.AddNewLayer (Translations.GetString ("Background"));

		if (backgroundColor.A != 0) {
			var g = new Cairo.Context (background.Surface);
			g.SetSourceColor (backgroundColor);
			g.Paint ();
		}

		doc.Workspace.History.PushNewItem (new BaseHistoryItem (Resources.StandardIcons.DocumentNew, Translations.GetString ("New Image")));
		doc.Workspace.History.SetClean ();

		return doc;
	}

	/// <summary>
	/// Creates a new Document with a specified image as content.
	/// Primarily used for Paste Into New Image.
	/// </summary>
	public Document NewDocumentFromImage (Cairo.ImageSurface image)
	{
		Document doc = NewDocument (new Size (image.Width, image.Height), new Color (0, 0, 0, 0));

		Context g = new (doc.Layers[0].Surface);
		g.SetSourceSurface (image, 0, 0);
		g.Paint ();

		// A normal document considers the "New Image" history to not be dirty, as it's just a
		// blank background. We put an image there, so we should try to save if the user closes it.
		doc.Workspace.History.SetDirty ();

		return doc;
	}

	// TODO: Standardize add to recent files
	public bool OpenFile (Gio.File file, Window? parent = null)
	{
		bool fileOpened = false;

		parent ??= PintaCore.Chrome.MainWindow;

		try {
			// Open the image and add it to the layers
			IImageImporter? importer = PintaCore.ImageFormats.GetImporterByFile (file.GetDisplayName ());
			if (importer is not null) {
				importer.Import (file, parent);
			} else {
				// Unknown extension, so try every loader.
				var errors = new StringBuilder ();
				bool loaded = false;
				foreach (var format in PintaCore.ImageFormats.Formats.Where (f => !f.IsWriteOnly ())) {
					try {
						format.Importer!.Import (file, parent);
						loaded = true;
						break;
					} catch (UnauthorizedAccessException) {
						// If the file can't be accessed, don't retry for every format.
						ShowFilePermissionErrorDialog (parent, file.GetParseName ());
						return false;
					} catch (Exception e) {
						// Record errors in case none of the formats work.
						errors.AppendLine ($"Failed to load image as {format.Filter.Name}:");
						errors.Append (e.ToString ());
						errors.AppendLine ();
					}
				}

				if (!loaded) {
					ShowUnsupportedFormatDialog (parent, file.GetParseName (),
						Translations.GetString ("Unsupported file format"), errors.ToString ());
					return false;
				}
			}

			PintaCore.Workspace.ActiveWorkspace.History.PushNewItem (new BaseHistoryItem (Resources.StandardIcons.DocumentOpen, Translations.GetString ("Open Image")));
			PintaCore.Workspace.ActiveDocument.History.SetClean ();

			fileOpened = true;
		} catch (UnauthorizedAccessException) {
			ShowFilePermissionErrorDialog (parent, file.GetParseName ());
		} catch (Exception e) {
			ShowOpenFileErrorDialog (parent, file.GetParseName (), e.Message, e.ToString ());
		}

		return fileOpened;
	}

	public RectangleI ClampToImageSize (RectangleI r)
	{
		return ActiveDocument.ClampToImageSize (r);
	}

	public bool ImageFitsInWindow
		=> ActiveWorkspace.ImageFitsInWindow;

	internal void ResetTitle ()
	{
		if (HasOpenDocuments)
			PintaCore.Chrome.MainWindow.Title = $"{ActiveDocument.DisplayName}{(ActiveDocument.IsDirty ? "*" : "")} - Pinta";
		else
			PintaCore.Chrome.MainWindow.Title = "Pinta";
	}

	public void SetActiveDocument (int index)
	{
		if (index >= open_documents.Count)
			throw new ArgumentOutOfRangeException (
				nameof (index),
				$"Tried to {nameof (WorkspaceManager)}.{nameof (SetActiveDocument)} greater than {nameof (OpenDocuments)}."
			);
		if (index < 0)
			throw new ArgumentOutOfRangeException (
				nameof (index),
				$"Tried to {nameof (WorkspaceManager)}.{nameof (SetActiveDocument)} less that zero."
			);

		SetActiveDocument (open_documents[index]);
	}

	public void SetActiveDocument (Document document)
	{
		PintaCore.Actions.Window.SetActiveDocument (document);
	}

	internal void SetActiveDocumentInternal (Document document)
	{
		// Work around a case where we closed a document but haven't updated
		// the active_document_index yet and it points to the closed document
		if (HasOpenDocuments && active_document_index != -1 && open_documents.Count > active_document_index)
			PintaCore.Tools.Commit ();

		int index = open_documents.IndexOf (document);
		active_document_index = index;

		OnActiveDocumentChanged (EventArgs.Empty);
	}

	private void OnActiveDocumentChanged (EventArgs e)
	{
		ActiveDocumentChanged?.Invoke (this, EventArgs.Empty);

		OnSelectionChanged ();

		ResetTitle ();
	}

	private void OnDocumentCreated (DocumentEventArgs e)
	{
		e.Document.SelectionChanged += (sender, args) => {
			OnSelectionChanged ();
		};

		DocumentCreated?.Invoke (this, e);
	}

	private void OnDocumentOpened (DocumentEventArgs e)
	{
		DocumentOpened?.Invoke (this, e);
	}

	private void OnDocumentClosed (DocumentEventArgs e)
	{
		DocumentClosed?.Invoke (this, e);
	}

	private void OnSelectionChanged ()
	{
		SelectionChanged?.Invoke (this, EventArgs.Empty);
	}

	private static void ShowOpenFileErrorDialog (Window parent, string filename, string primary_text, string details)
	{
		string secondary_text = Translations.GetString ("Could not open file: {0}", filename);
		PintaCore.Chrome.ShowErrorDialog (parent, primary_text, secondary_text, details);
	}

	private static void ShowUnsupportedFormatDialog (Window parent, string filename, string message, string errors)
	{
		StringBuilder body = new ();

		body.AppendLine (Translations.GetString ("Could not open file: {0}", filename));
		body.AppendLine (Translations.GetString ("Pinta supports the following file formats:"));

		var extensions =
			from format in PintaCore.ImageFormats.Formats
			where format.Importer != null
			from extension in format.Extensions
			where char.IsLower (extension.FirstOrDefault ())
			orderby extension
			select extension;

		body.AppendJoin (", ", extensions);

		PintaCore.Chrome.ShowErrorDialog (parent, message, body.ToString (), errors);
	}

	private static void ShowFilePermissionErrorDialog (Window parent, string filename)
	{
		string message = Translations.GetString ("Failed to open image");
		// Translators: {0} is the name of a file that the user does not have permission to open.
		string details = Translations.GetString ("You do not have access to '{0}'.", filename);

		PintaCore.Chrome.ShowMessageDialog (parent, message, details);
	}

	#region Public Events

	public event EventHandler<DocumentEventArgs>? DocumentCreated;
	public event EventHandler<DocumentEventArgs>? DocumentOpened;
	public event EventHandler<DocumentEventArgs>? DocumentClosed;

	public event EventHandler? ActiveDocumentChanged;
	public event EventHandler? SelectionChanged;

	#endregion

}
