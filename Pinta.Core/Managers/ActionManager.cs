//
// ActionManager.cs
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

namespace Pinta.Core;

public sealed class ActionManager
{
	public AppActions App { get; }
	public FileActions File { get; }
	public EditActions Edit { get; }
	public ViewActions View { get; }
	public ImageActions Image { get; }
	public LayerActions Layers { get; }
	public AdjustmentsActions Adjustments { get; }
	public EffectsActions Effects { get; }
	public WindowActions Window { get; }
	public HelpActions Help { get; }
	public AddinActions Addins { get; }

	private readonly SystemManager system;
	private readonly ChromeManager chrome;
	public ActionManager (
		ChromeManager chrome,
		ImageConverterManager imageFormats,
		PaletteFormatManager paletteFormats,
		PaletteManager palette,
		RecentFileManager recentFiles,
		SystemManager system,
		ToolManager tools,
		WorkspaceManager workspace)
	{
		// --- Action handlers that don't depend on other handlers

		AddinActions addins = new ();
		AdjustmentsActions adjustments = new ();
		AppActions app = new ();
		EditActions edit = new (chrome, paletteFormats, palette, tools, workspace);
		EffectsActions effects = new (chrome);
		ViewActions view = new (chrome, workspace);
		WindowActions window = new (workspace);

		// --- Action handlers that depend on other handlers

		FileActions file = new (system, app);
		HelpActions help = new (system, app);
		ImageActions image = new (tools, workspace, view);
		LayerActions layers = new (chrome, imageFormats, recentFiles, tools, workspace, image);

		// --- References to keep

		App = app;
		File = file;
		Edit = edit;
		View = view;
		Image = image;
		Layers = layers;
		Adjustments = adjustments;
		Effects = effects;
		Window = window;
		Help = help;
		Addins = addins;

		this.system = system;
		this.chrome = chrome;
	}

	public void CreateToolBar (Gtk.Box toolbar)
	{
		toolbar.Append (File.New.CreateToolBarItem ());
		toolbar.Append (File.Open.CreateToolBarItem ());
		toolbar.Append (File.Save.CreateToolBarItem ());
		// Printing is disabled for now until it is fully functional.
#if false
		toolbar.AppendItem (File.Print.CreateToolBarItem ());
#endif
		toolbar.Append (GtkExtensions.CreateToolBarSeparator ());

		// Cut/Copy/Paste comes before Undo/Redo on Windows
		if (system.OperatingSystem == OS.Windows) {
			toolbar.Append (Edit.Cut.CreateToolBarItem ());
			toolbar.Append (Edit.Copy.CreateToolBarItem ());
			toolbar.Append (Edit.Paste.CreateToolBarItem ());
			toolbar.Append (GtkExtensions.CreateToolBarSeparator ());
			toolbar.Append (Edit.Undo.CreateToolBarItem ());
			toolbar.Append (Edit.Redo.CreateToolBarItem ());
		} else {
			toolbar.Append (Edit.Undo.CreateToolBarItem ());
			toolbar.Append (Edit.Redo.CreateToolBarItem ());
			toolbar.Append (GtkExtensions.CreateToolBarSeparator ());
			toolbar.Append (Edit.Cut.CreateToolBarItem ());
			toolbar.Append (Edit.Copy.CreateToolBarItem ());
			toolbar.Append (Edit.Paste.CreateToolBarItem ());
		}

		toolbar.Append (GtkExtensions.CreateToolBarSeparator ());
		toolbar.Append (Image.CropToSelection.CreateToolBarItem ());
		toolbar.Append (Edit.Deselect.CreateToolBarItem ());
	}

	public void CreateHeaderToolBar (Adw.HeaderBar header)
	{
		header.PackStart (File.New.CreateToolBarItem ());
		header.PackStart (File.Open.CreateToolBarItem ());
		header.PackStart (File.Save.CreateToolBarItem ());

		header.PackStart (GtkExtensions.CreateToolBarSeparator ());
		header.PackStart (Edit.Undo.CreateToolBarItem ());
		header.PackStart (Edit.Redo.CreateToolBarItem ());

		header.PackStart (GtkExtensions.CreateToolBarSeparator ());
		header.PackStart (Edit.Cut.CreateToolBarItem ());
		header.PackStart (Edit.Copy.CreateToolBarItem ());
		header.PackStart (Edit.Paste.CreateToolBarItem ());

		header.PackStart (GtkExtensions.CreateToolBarSeparator ());
		header.PackStart (Image.CropToSelection.CreateToolBarItem ());
		header.PackStart (Edit.Deselect.CreateToolBarItem ());
	}

	public void CreateStatusBar (Gtk.Box statusbar, WorkspaceManager workspaceManager)
	{
		// Selection size widget
		var selection_icon = Gtk.Image.NewFromIconName (Resources.Icons.ToolSelectRectangle);
		statusbar.Append (selection_icon);
		var selection_size = Gtk.Label.New ("");
		selection_size.Xalign = 0.0f;
		selection_size.Halign = Gtk.Align.Start;
		selection_size.WidthChars = 11;
		statusbar.Append (selection_size);

		selection_icon.SetVisible (false);
		selection_size.SetVisible (false);

		workspaceManager.SelectionChanged += delegate {
			if (!workspaceManager.HasOpenDocuments || !workspaceManager.ActiveDocument.Selection.Visible) {
				selection_icon.SetVisible (false);
				selection_size.SetVisible (false);
				return;
			}
			var bounds = workspaceManager.ActiveDocument.Selection.GetBounds ();
			selection_size.SetText ($"{bounds.Width}, {bounds.Height}");
			selection_icon.SetVisible (true);
			selection_size.SetVisible (true);
		};

		// Cursor position widget
		statusbar.Append (Gtk.Image.NewFromIconName (Resources.Icons.CursorPosition));
		var cursor = Gtk.Label.New ("0, 0");
		cursor.Xalign = 0.0f;
		cursor.Halign = Gtk.Align.Start;
		cursor.WidthChars = 11;
		statusbar.Append (cursor);

		chrome.LastCanvasCursorPointChanged += delegate {
			var pt = chrome.LastCanvasCursorPoint;
			cursor.SetText ($"{pt.X}, {pt.Y}");
		};

		// Image dimensions widget
		statusbar.Append (Gtk.Image.NewFromIconName (Resources.Icons.ImageResize));
		var image_size = Gtk.Label.New ("");
		image_size.Xalign = 0.0f;
		image_size.Halign = Gtk.Align.Start;
		image_size.WidthChars = 24;
		statusbar.Append (image_size);

		void UpdateImageSizeLabel ()
		{
			if (!workspaceManager.HasOpenDocuments) {
				image_size.SetText ("");
				return;
			}

			var size = workspaceManager.ActiveDocument.ImageSize;
			image_size.SetText ($"{size.Width} \u00d7 {size.Height} \u00b7 {GetAspectRatio (size.Width, size.Height)}");
		}

		workspaceManager.ActiveDocumentChanged += delegate { UpdateImageSizeLabel (); };

		workspaceManager.DocumentActivated += (_, args) => {
			args.Document.ImageSizeChanged += delegate { UpdateImageSizeLabel (); };
		};

		// Document zoom widget
		View.CreateStatusBar (statusbar);
	}

	private static string GetAspectRatio (int w, int h)
	{
		int gcd = GCD (w, h);
		return $"{w / gcd}:{h / gcd}";
	}

	private static int GCD (int a, int b)
	{
		while (b != 0) {
			int temp = b;
			b = a % b;
			a = temp;
		}
		return a;
	}

	public void RegisterHandlers ()
	{
		File.RegisterHandlers ();
		Edit.RegisterHandlers ();
		Image.RegisterHandlers ();
		Layers.RegisterHandlers ();
		View.RegisterHandlers ();
		Help.RegisterHandlers ();
	}
}
