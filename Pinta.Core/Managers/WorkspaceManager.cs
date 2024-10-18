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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;

namespace Pinta.Core;

public interface IWorkspaceService
{
	Document ActiveDocument { get; }
	DocumentWorkspace ActiveWorkspace { get; }
	bool HasOpenDocuments { get; }

	SelectionModeHandler SelectionHandler { get; }

	event EventHandler? SelectionChanged;
	event EventHandler? ActiveDocumentChanged;

	public event EventHandler? LayerAdded;
	public event EventHandler? LayerRemoved;
	public event EventHandler? SelectedLayerChanged;
	public event PropertyChangedEventHandler? LayerPropertyChanged;
}

public static class WorkspaceServiceExtensions
{
	public static void Invalidate (this IWorkspaceService workspace)
	{
		if (workspace.HasOpenDocuments)
			workspace.ActiveWorkspace.Invalidate ();
	}

	public static void Invalidate (
		this IWorkspaceService workspace,
		RectangleI rect)
	{
		workspace.ActiveWorkspace.Invalidate (rect);
	}

	public static void InvalidateWindowRect (
		this IWorkspaceService workspace,
		RectangleI windowRect)
	{
		workspace.ActiveWorkspace.InvalidateWindowRect (windowRect);
	}

	/// <summary>
	/// Converts a point from the active document's canvas coordinates to view coordinates.
	/// </summary>
	/// <param name='canvas_pos'>
	/// The position of the canvas point
	/// </param>
	public static PointD CanvasPointToView (
		this IWorkspaceService workspace,
		PointD canvas_pos)
	{
		return workspace.ActiveWorkspace.CanvasPointToView (canvas_pos);
	}

	/// <summary>
	/// Converts a point from the active document's view coordinates to canvas coordinates.
	/// </summary>
	/// <param name='canvas_pos'>
	/// The position of the view point
	/// </param>
	public static PointD ViewPointToCanvas (
		this IWorkspaceService workspace,
		PointD view_pos)
	{
		return workspace.ActiveWorkspace.ViewPointToCanvas (view_pos);
	}

	public static void ResizeImage (
		this IWorkspaceService workspace,
		Size newSize,
		ResamplingMode resamplingMode)
	{
		workspace.ActiveDocument.ResizeImage (newSize, resamplingMode);
	}

	public static void ResizeCanvas (
		this IWorkspaceService workspace,
		Size newSize,
		Anchor anchor,
		CompoundHistoryItem? compoundAction)
	{
		workspace.ActiveDocument.ResizeCanvas (newSize, anchor, compoundAction);
	}

	public static void CloseActiveDocument (
		this WorkspaceManager workspace,
		ActionManager actions)
	{
		workspace.CloseDocument (actions, workspace.ActiveDocument);
	}

	public static RectangleI ClampToImageSize (
		this IWorkspaceService workspace,
		RectangleI r)
	{
		return workspace.ActiveDocument.ClampToImageSize (r);
	}

	public static Document NewDocument (
		this WorkspaceManager workspace,
		ActionManager actions,
		Size imageSize,
		Color backgroundColor)
	{
		Document doc = new (imageSize);
		doc.Workspace.ViewSize = imageSize;
		workspace.AttachDocument (doc, actions);

		// Start with an empty white layer
		Layer background = doc.Layers.AddNewLayer (Translations.GetString ("Background"));

		if (backgroundColor.A != 0) {
			Context g = new (background.Surface);
			g.SetSourceColor (backgroundColor);
			g.Paint ();
		}

		doc.Workspace.History.PushNewItem (new BaseHistoryItem (Resources.StandardIcons.DocumentNew, Translations.GetString ("New Image")));
		doc.Workspace.History.SetClean ();

		return doc;
	}
}

public sealed class WorkspaceManager : IWorkspaceService
{
	private int active_document_index = -1;
	private int new_file_name = 1;

	private readonly ChromeManager chrome_manager;
	private readonly ImageConverterManager image_formats;

	public WorkspaceManager (
		SystemManager systemManager,
		ChromeManager chromeManager,
		ImageConverterManager imageFormats)
	{
		open_documents = new List<Document> ();
		OpenDocuments = new ReadOnlyCollection<Document> (open_documents);
		SelectionHandler = new SelectionModeHandler (systemManager);

		chrome_manager = chromeManager;
		image_formats = imageFormats;
	}

	public int ActiveDocumentIndex
		=> active_document_index;

	public Document ActiveDocument =>
		HasOpenDocuments
		? open_documents[active_document_index]
		: throw new InvalidOperationException ($"Tried to get {nameof (WorkspaceManager)}.{nameof (ActiveDocument)} when there are no open Documents.  Check HasOpenDocuments first.");

	public Document? ActiveDocumentOrDefault =>
		HasOpenDocuments
		? open_documents[active_document_index]
		: null;

	public SelectionModeHandler SelectionHandler { get; }

	public DocumentWorkspace ActiveWorkspace =>
		HasOpenDocuments
		? open_documents[active_document_index].Workspace
		: throw new InvalidOperationException ("Tried to get WorkspaceManager.ActiveWorkspace when there are no open Documents.  Check HasOpenDocuments first.");

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

	public void AttachDocument (
		Document document,
		ActionManager actions)
	{
		document.Layers.LayerAdded += Document_LayerAdded;
		document.Layers.LayerRemoved += Document_LayerRemoved;
		document.Layers.SelectedLayerChanged += Document_SelectedLayerChanged;
		document.Layers.LayerPropertyChanged += Document_LayerPropertyChanged;
		document.MarkAttached ();

		open_documents.Add (document);

		OnDocumentAttached (new DocumentEventArgs (document));

		actions.Window.SetActiveDocument (document);
	}

	private void Document_LayerPropertyChanged (object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		LayerPropertyChanged?.Invoke (sender, e);
		this.Invalidate ();
	}

	private void Document_SelectedLayerChanged (object? sender, EventArgs e)
	{
		SelectedLayerChanged?.Invoke (sender, e);
	}

	private void Document_LayerRemoved (object? sender, IndexEventArgs e)
	{
		LayerRemoved?.Invoke (sender, e);
	}

	private void Document_LayerAdded (object? sender, IndexEventArgs e)
	{
		LayerAdded?.Invoke (sender, e);
	}

	public void CloseDocument (
		ActionManager actions,
		Document document)
	{
		int index = open_documents.IndexOf (document);

		if (index == -1) return; // TODO: Maybe throw an exception?

		open_documents.Remove (document);

		if (index == active_document_index) {
			// If there's other documents open, switch to one of them
			if (HasOpenDocuments) {
				if (index > 0)
					SetActiveDocument (actions, index - 1);
				else
					SetActiveDocument (actions, index);
			} else {
				active_document_index = -1;
				OnActiveDocumentChanged (EventArgs.Empty);
			}
		}

		document.Layers.LayerAdded -= Document_LayerAdded;
		document.Layers.LayerRemoved -= Document_LayerRemoved;
		document.Layers.SelectedLayerChanged -= Document_SelectedLayerChanged;
		document.Layers.LayerPropertyChanged -= Document_LayerPropertyChanged;
		document.Close ();

		document.MarkDetached ();

		OnDocumentClosed (new DocumentEventArgs (document));
	}

	/// <summary>
	/// Creates a new Document with a specified image as content.
	/// Primarily used for Paste Into New Image.
	/// </summary>
	public Document NewDocumentFromImage (ActionManager actions, Cairo.ImageSurface image)
	{
		Document doc = this.NewDocument (
			actions,
			new Size (image.Width, image.Height),
			new Color (0, 0, 0, 0));

		Context g = new (doc.Layers[0].Surface);
		g.SetSourceSurface (image, 0, 0);
		g.Paint ();

		// A normal document considers the "New Image" history to not be dirty, as it's just a
		// blank background. We put an image there, so we should try to save if the user closes it.
		doc.Workspace.History.SetDirty ();

		return doc;
	}

	// TODO: Standardize add to recent files
	/// <returns>Flag that indicates if file was opened successfully</returns>
	public bool OpenFile (
		Gio.File file,
		Gtk.Window? parent = null)
	{
		parent ??= chrome_manager.MainWindow;

		try {
			// Open the image and add it to the layers
			IImageImporter? importer = image_formats.GetImporterByFile (file.GetDisplayName ());
			if (importer is not null) {
				importer.Import (file, parent);
			} else {
				// Unknown extension, so try every loader.
				StringBuilder errors = new ();
				bool loaded = false;
				foreach (var format in image_formats.Formats.Where (f => !f.IsWriteOnly ())) {
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

			ActiveWorkspace.History.PushNewItem (new BaseHistoryItem (Resources.StandardIcons.DocumentOpen, Translations.GetString ("Open Image")));
			ActiveDocument.History.SetClean ();

			return true;

		} catch (UnauthorizedAccessException) {
			ShowFilePermissionErrorDialog (parent, file.GetParseName ());
		} catch (Exception e) {
			ShowOpenFileErrorDialog (parent, file.GetParseName (), e.Message, e.ToString ());
		}

		return false;
	}

	public bool ImageFitsInWindow
		=> ActiveWorkspace.ImageFitsInWindow;

	internal void ResetTitle ()
	{
		if (HasOpenDocuments)
			chrome_manager.MainWindow.Title = $"{ActiveDocument.DisplayName}{(ActiveDocument.IsDirty ? "*" : "")} - Pinta";
		else
			chrome_manager.MainWindow.Title = "Pinta";
	}

	public void SetActiveDocument (
		ActionManager actions,
		int index)
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

		actions.Window.SetActiveDocument (open_documents[index]);
	}

	internal void SetActiveDocumentInternal (
		ToolManager tools,
		Document document)
	{
		// Work around a case where we closed a document but haven't updated
		// the active_document_index yet and it points to the closed document
		if (HasOpenDocuments && active_document_index != -1 && open_documents.Count > active_document_index)
			tools.Commit ();

		int index = open_documents.IndexOf (document);
		active_document_index = index;

		OnActiveDocumentChanged (EventArgs.Empty);
	}

	private void OnActiveDocumentChanged (EventArgs _)
	{
		ActiveDocumentChanged?.Invoke (this, EventArgs.Empty);
		OnSelectionChanged ();
		ResetTitle ();
	}

	private void OnDocumentAttached (DocumentEventArgs e)
	{
		e.Document.SelectionChanged += (_, _) => OnSelectionChanged ();
		DocumentAttached?.Invoke (this, e);
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

	private Task ShowOpenFileErrorDialog (
		Gtk.Window parent,
		string filename,
		string primary_text,
		string details)
	{
		string secondary_text = Translations.GetString ("Could not open file: {0}", filename);
		return chrome_manager.ShowErrorDialog (parent, primary_text, secondary_text, details);
	}

	private Task ShowUnsupportedFormatDialog (
		Gtk.Window parent,
		string filename,
		string message,
		string errors)
	{
		StringBuilder body = new ();

		body.AppendLine (Translations.GetString ("Could not open file: {0}", filename));
		body.AppendLine (Translations.GetString ("Pinta supports the following file formats:"));

		var extensions =
			from format in image_formats.Formats
			where format.Importer != null
			from extension in format.Extensions
			where char.IsLower (extension.FirstOrDefault ())
			orderby extension
			select extension;

		body.AppendJoin (", ", extensions);

		return chrome_manager.ShowErrorDialog (parent, message, body.ToString (), errors);
	}

	private Task ShowFilePermissionErrorDialog (
		Gtk.Window parent,
		string filename)
	{
		string message = Translations.GetString ("Failed to open image");

		// Translators: {0} is the name of a file that the user does not have permission to open.
		string details = Translations.GetString ("You do not have access to '{0}'.", filename);

		return chrome_manager.ShowMessageDialog (parent, message, details);
	}

	public event EventHandler? LayerAdded;
	public event EventHandler? LayerRemoved;
	public event EventHandler? SelectedLayerChanged;
	public event PropertyChangedEventHandler? LayerPropertyChanged;

	public event EventHandler<DocumentEventArgs>? DocumentAttached;
	public event EventHandler<DocumentEventArgs>? DocumentOpened;
	public event EventHandler<DocumentEventArgs>? DocumentClosed;

	public event EventHandler? ActiveDocumentChanged;
	public event EventHandler? SelectionChanged;
}
