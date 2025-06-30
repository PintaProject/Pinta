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
	Size ImageSize { get; }

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

	public static void CloseActiveDocument (this WorkspaceManager workspace)
	{
		workspace.CloseDocument (workspace.ActiveDocument);
	}

	public static RectangleI ClampToImageSize (
		this IWorkspaceService workspace,
		RectangleI r)
	{
		return workspace.ActiveDocument.ClampToImageSize (r);
	}

	public static Document NewDocument (
		this WorkspaceManager workspace,
		Size imageSize,
		Color backgroundColor)
	{
		Document doc = new (PintaCore.Actions, PintaCore.Tools, PintaCore.Workspace, imageSize);
		doc.Workspace.ViewSize = imageSize;
		workspace.ActivateDocument (doc);

		// Start with an empty white layer
		Layer background = doc.Layers.AddNewLayer (Translations.GetString ("Background"));

		if (backgroundColor.A != 0) {
			using Context g = new (background.Surface);
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

	private readonly ChromeManager chrome_manager;
	private readonly ImageConverterManager image_formats;

	public WorkspaceManager (
		SystemManager systemManager,
		ChromeManager chromeManager,
		ImageConverterManager imageFormats)
	{
		open_documents = [];
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

	public double Scale {
		get => ActiveWorkspace.Scale;
		set => ActiveWorkspace.Scale = value;
	}

	private readonly List<Document> open_documents;
	public ReadOnlyCollection<Document> OpenDocuments { get; }
	public bool HasOpenDocuments => active_document_index >= 0;

	public void ActivateDocument (Document document)
	{
		document.Layers.LayerAdded += Document_LayerAdded;
		document.Layers.LayerRemoved += Document_LayerRemoved;
		document.Layers.SelectedLayerChanged += Document_SelectedLayerChanged;
		document.Layers.LayerPropertyChanged += Document_LayerPropertyChanged;

		open_documents.Add (document);

		OnDocumentActivated (new DocumentEventArgs (document));

		SetActiveDocument (open_documents.Count - 1);
	}

	private void Document_LayerPropertyChanged (object? sender, PropertyChangedEventArgs e)
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

	public void CloseDocument (Document document)
	{
		int index = open_documents.IndexOf (document);

		if (index == -1)
			throw new ArgumentException ("Document was not found in workspace. Did you forget to activate it?", nameof (document));

		if (index == active_document_index) {
			PreActiveDocumentChanged?.Invoke (this, EventArgs.Empty);
			open_documents.Remove (document);

			// If there's other documents open, switch to one of them
			if (open_documents.Count > 0) {
				active_document_index = Math.Max (0, index - 1);
			} else {
				active_document_index = -1;
			}

			OnActiveDocumentChanged (EventArgs.Empty);
		} else {
			open_documents.Remove (document);
		}

		document.Layers.LayerAdded -= Document_LayerAdded;
		document.Layers.LayerRemoved -= Document_LayerRemoved;
		document.Layers.SelectedLayerChanged -= Document_SelectedLayerChanged;
		document.Layers.LayerPropertyChanged -= Document_LayerPropertyChanged;
		document.Close ();

		OnDocumentClosed (new DocumentEventArgs (document));
	}

	/// <summary>
	/// Creates a new Document with a specified image as content.
	/// Primarily used for Paste Into New Image.
	/// </summary>
	public Document NewDocumentFromImage (ImageSurface image)
	{
		Document doc = this.NewDocument (
			new Size (image.Width, image.Height),
			new Color (0, 0, 0, 0));

		using Context g = new (doc.Layers[0].Surface);
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
				Document imported = importer.Import (file);
				ActivateDocument (imported);
			} else {
				// Unknown extension, so try every loader.
				StringBuilder errors = new ();
				bool loaded = false;
				foreach (var format in image_formats.Formats.Where (f => f.IsImportAvailable ())) {
					try {
						Document imported = format.Importer!.Import (file);
						ActivateDocument (imported);
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

		if (index == active_document_index)
			return;

		PreActiveDocumentChanged?.Invoke (this, EventArgs.Empty);

		active_document_index = index;

		OnActiveDocumentChanged (EventArgs.Empty);
	}

	private void OnActiveDocumentChanged (EventArgs _)
	{
		ActiveDocumentChanged?.Invoke (this, EventArgs.Empty);
		OnSelectionChanged ();
		ResetTitle ();
	}

	private void OnDocumentActivated (DocumentEventArgs e)
	{
		e.Document.SelectionChanged += (_, _) => OnSelectionChanged ();
		DocumentActivated?.Invoke (this, e);
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

	public event EventHandler<DocumentEventArgs>? DocumentActivated;
	public event EventHandler<DocumentEventArgs>? DocumentClosed;

	/// <summary>
	/// Emitted before the active document has changed.
	/// This can be used to e.g. have tools commit actions before switching documents.
	/// </summary>
	public event EventHandler? PreActiveDocumentChanged;
	/// <summary>
	/// Emitted after the active document has changed.
	/// </summary>
	public event EventHandler? ActiveDocumentChanged;
	public event EventHandler? SelectionChanged;
}
