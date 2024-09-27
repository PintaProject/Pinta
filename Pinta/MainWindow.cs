//
// MainWindow.cs
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
using System.Linq;
using Mono.Addins;
using Pinta.Core;
using Pinta.Docking;
using Pinta.Gui.Widgets;

namespace Pinta;

public sealed class MainWindow
{
	readonly Adw.Application app;
	// NRT - Created in OnActivated
	WindowShell window_shell = null!;
	Dock dock = null!;
	Gio.Menu show_pad = null!;

	CanvasPad canvas_pad = null!;

	private Gtk.DropTarget drop_target = null!;

	public MainWindow (Adw.Application app)
	{
		this.app = app;

		// This needs to match the name of the .desktop file in order to
		// show the correct application icon under some environments (e.g.
		// KDE Wayland). See bug 1967687.
		GLib.Functions.SetPrgname ("pinta");
		// Set the human-readable application name, used by e.g. gtk_recent_manager_add_item().
		GLib.Functions.SetApplicationName (Translations.GetString ("Pinta"));
	}

	public void Activate ()
	{
		// Build our window
		CreateWindow ();

		// Initialize interface things
		_ = new ActionHandlers ();

		PintaCore.Chrome.InitializeProgessDialog (new ProgressDialog (PintaCore.Chrome));
		PintaCore.Chrome.InitializeErrorDialogHandler (ErrorDialog.ShowError);
		PintaCore.Chrome.InitializeMessageDialog (ErrorDialog.ShowMessage);
		PintaCore.Chrome.InitializeSimpleEffectDialog (SimpleEffectDialog.Launch);

		PintaCore.Initialize ();

		// Initialize extensions
		AddinManager.Initialize ();
		AddinManager.Registry.Update ();
		var setupService = new AddinSetupService (AddinManager.Registry);
		if (!setupService.AreRepositoriesRegistered ())
			setupService.RegisterRepositories (true);

		// Look out for any changes in extensions
		AddinManager.AddExtensionNodeHandler (typeof (IExtension), OnExtensionChanged);

		// Load the user's previous settings
		LoadUserSettings ();
		PintaCore.Actions.App.BeforeQuit += delegate { SaveUserSettings (); };

		// We support drag and drop for URIs, which are converted into a Gdk.FileList.
		drop_target = Gtk.DropTarget.New (Gdk.FileList.GetGType (), Gdk.DragAction.Copy);
		drop_target.OnDrop += HandleDrop;
		window_shell.Window.AddController (drop_target);

		// Handle a few main window specific actions
		window_shell.Window.OnCloseRequest += HandleCloseRequest;

		// Add custom key handling during the capture phase. For example, this ensures that the "-" key
		// can be typed into the dash pattern box instead of being handled as a shortcut
		var key_controller = Gtk.EventControllerKey.New ();
		key_controller.SetPropagationPhase (Gtk.PropagationPhase.Capture);
		key_controller.OnKeyPressed += HandleGlobalKeyPress;
		key_controller.OnKeyReleased += HandleGlobalKeyRelease;
		window_shell.Window.AddController (key_controller);

		// TODO: These need to be [re]moved when we redo zoom support
		PintaCore.Actions.View.ZoomToWindow.Activated += ZoomToWindow_Activated;
		PintaCore.Actions.View.ZoomToSelection.Activated += ZoomToSelection_Activated;

		PintaCore.Workspace.ActiveDocumentChanged += ActiveDocumentChanged;

		PintaCore.Workspace.DocumentCreated += Workspace_DocumentCreated;
		PintaCore.Workspace.DocumentClosed += Workspace_DocumentClosed;

		var notebook = canvas_pad.Notebook;
		notebook.TabClosed += DockNotebook_TabClosed;
		notebook.ActiveTabChanged += DockNotebook_ActiveTabChanged;
	}

	private void Workspace_DocumentClosed (object? sender, DocumentEventArgs e)
	{
		var tab = FindTabWithCanvas ((PintaCanvas) e.Document.Workspace.Canvas);

		if (tab != null)
			canvas_pad.Notebook.RemoveTab (tab);
	}

	private void DockNotebook_TabClosed (object? sender, TabClosedEventArgs e)
	{
		var view = (DocumentViewContent) e.Item;

		if (PintaCore.Workspace.OpenDocuments.IndexOf (view.Document) < 0)
			return;

		PintaCore.Actions.Window.SetActiveDocument (view.Document);
		PintaCore.Actions.File.Close.Activate ();

		if (PintaCore.Workspace.OpenDocuments.IndexOf (view.Document) < 0)
			return;

		// User must have canceled the close
		e.Cancel = true;
	}

	private void DockNotebook_ActiveTabChanged (object? sender, EventArgs e)
	{
		var item = canvas_pad.Notebook.ActiveItem;

		if (item == null)
			return;

		var view = (DocumentViewContent) item;

		PintaCore.Actions.Window.SetActiveDocument (view.Document);
		((CanvasWindow) view.Widget).Canvas.Cursor = PintaCore.Tools.CurrentTool?.CurrentCursor;
	}

	private void Workspace_DocumentCreated (object? sender, DocumentEventArgs e)
	{
		var doc = e.Document;

		var notebook = canvas_pad.Notebook;
		int selected_index = notebook.ActiveItemIndex;

		CanvasWindow canvas = new (PintaCore.Workspace, doc) {
			RulersVisible = PintaCore.Actions.View.Rulers.Value,
			RulerMetric = GetCurrentRulerMetric ()
		};
		doc.Workspace.Canvas = canvas.Canvas;

		DocumentViewContent my_content = new (doc, canvas);

		// Insert our tab to the right of the currently selected tab
		notebook.InsertTab (my_content, selected_index + 1);

		// Zoom to window only on first show (if we do it always, it will be called on every resize)
		// Note: this does seem to allow a small flicker where large images are shown at 100% zoom before
		// zooming out (Bug 1959673)

		bool canvasHasBeenShown = false;

		canvas.Canvas.OnResize += (o, e2) => {

			if (canvasHasBeenShown)
				return;

			GLib.Functions.TimeoutAdd (
				0,
				0,
				() => {
					ZoomToWindow_Activated (o, e);
					PintaCore.Workspace.Invalidate ();
					return false;
				}
			);

			canvasHasBeenShown = true;
		};

		PintaCore.Actions.View.Rulers.Toggled += (active) => { canvas.RulersVisible = active; };
		PintaCore.Actions.View.RulerMetric.OnActivate += (o, args) => {
			PintaCore.Actions.View.RulerMetric.ChangeState (args.Parameter!);
			canvas.RulerMetric = GetCurrentRulerMetric ();
		};
	}

	private static MetricType GetCurrentRulerMetric ()
	{
		GLib.Variant state = PintaCore.Actions.View.RulerMetric.GetState () ??
			throw new InvalidOperationException ("action should not be stateless");

		return (MetricType) state.GetInt32 ();
	}

	private bool HandleGlobalKeyPress (Gtk.EventControllerKey controller, Gtk.EventControllerKey.KeyPressedSignalArgs args)
	{
		// Give the widget that has focus a first shot at handling the event.
		// Otherwise, key presses may be intercepted by shortcuts for menu items.
		if (SendToFocusWidget (controller))
			return true;

		// Give the Canvas (and by extension the tools)
		// first shot at handling the event if
		// the mouse pointer is on the canvas
		if (PintaCore.Workspace.HasOpenDocuments) {
			var canvas_window = ((PintaCanvas) PintaCore.Workspace.ActiveWorkspace.Canvas).CanvasWindow;

			if ((canvas_window.Canvas.HasFocus || canvas_window.IsMouseOnCanvas) &&
				 canvas_window.Canvas.DoKeyPressEvent (controller, args)) {
				return true;
			}
		}

		// If the canvas/tool didn't consume it, see if its a toolbox shortcut
		if (!args.State.HasModifierKey () && PintaCore.Tools.SetCurrentTool (args.GetKey ()))
			return true;

		// Finally, see if the palette widget wants it.
		return PintaCore.Palette.DoKeyPress (args);
	}

	private void HandleGlobalKeyRelease (Gtk.EventControllerKey controller, Gtk.EventControllerKey.KeyReleasedSignalArgs args)
	{
		if (SendToFocusWidget (controller) || !PintaCore.Workspace.HasOpenDocuments)
			return;

		// Give the Canvas (and by extension the tools)
		// first shot at handling the event if
		// the mouse pointer is on the canvas
		var canvas_window = ((PintaCanvas) PintaCore.Workspace.ActiveWorkspace.Canvas).CanvasWindow;

		if (canvas_window.Canvas.HasFocus || canvas_window.IsMouseOnCanvas)
			canvas_window.Canvas.DoKeyReleaseEvent (controller, args);
	}

	private bool SendToFocusWidget (Gtk.EventControllerKey key_controller)
	{
		var widget = window_shell.Window.FocusWidget;
		if (widget != null && key_controller.Forward (widget)) return true;
		return false;
	}

	// Called when an extension node is added or removed
	private void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
	{
		IExtension extension = (IExtension) args.ExtensionObject;
		if (args.Change == ExtensionChange.Add) {
			try {
				extension.Initialize ();
			} catch (Exception e) {
				// Translators: {0} is the name of an add-in.
				var body = Translations.GetString ("The '{0}' add-in may not be compatible with this version of Pinta", args.ExtensionNode.Addin.Id);
				PintaCore.Chrome.ShowErrorDialog (PintaCore.Chrome.MainWindow,
						Translations.GetString ("Failed to initialize add-in"),
						body, e.ToString ());
			}
		} else
			extension.Uninitialize ();
	}

	#region GUI Construction
	private void CreateWindow ()
	{
		// Check for stored window settings
		int width = PintaCore.Settings.GetSetting ("window-size-width", 1100);
		int height = PintaCore.Settings.GetSetting ("window-size-height", 750);
		bool maximize = PintaCore.Settings.GetSetting ("window-maximized", false);

		window_shell = new WindowShell (
			app,
			"Pinta.GenericWindow",
			"Pinta",
			width,
			height,
			useHeaderBar: PintaCore.System.OperatingSystem != OS.Mac, // On macOS the global menubar is used, but otherwise use a header bar. We also use a regular Gtk window on macOS to have a traditional titlebar with the standard close / minimize buttons.
			maximize);

		CreateMainMenu ();
		CreateMainToolBar ();
		CreateToolToolBar ();

		CreatePanels ();
		CreateStatusBar ();

		app.AddWindow (window_shell.Window);

		PintaCore.Chrome.InitializeApplication (app);
		PintaCore.Chrome.InitializeWindowShell (window_shell.Window);
	}
	#endregion

	private void CreateMainMenu ()
	{
		bool using_header_bar = window_shell.HeaderBar is not null;
		var menu_bar = Gio.Menu.New ();

		if (!using_header_bar) {
			app.Menubar = menu_bar;

			if (PintaCore.System.OperatingSystem == OS.Mac) {
				// Only use the application menu on macOS. On other platforms, these
				// menu items appear under File, Help, etc.
				// The first menu seems to be treated as the application menu.
				var app_menu = Gio.Menu.New ();
				PintaCore.Actions.App.RegisterActions (app, app_menu);
				menu_bar.AppendSubmenu ("_Application", app_menu);
			}
		}

		var file_menu = Gio.Menu.New ();
		PintaCore.Actions.File.RegisterActions (app, file_menu);
		menu_bar.AppendSubmenu (Translations.GetString ("_File"), file_menu);

		var edit_menu = Gio.Menu.New ();
		PintaCore.Actions.Edit.RegisterActions (app, edit_menu);
		menu_bar.AppendSubmenu (Translations.GetString ("_Edit"), edit_menu);

		var view_menu = Gio.Menu.New ();
		PintaCore.Actions.View.RegisterActions (app, view_menu);
		menu_bar.AppendSubmenu (Translations.GetString ("_View"), view_menu);

		var image_menu = Gio.Menu.New ();
		PintaCore.Actions.Image.RegisterActions (app, image_menu);
		if (!using_header_bar)
			menu_bar.AppendSubmenu (Translations.GetString ("_Image"), image_menu);

		var layers_menu = Gio.Menu.New ();
		PintaCore.Actions.Layers.RegisterActions (app, layers_menu);
		menu_bar.AppendSubmenu (Translations.GetString ("_Layers"), layers_menu);

		var adj_menu = Gio.Menu.New ();
		if (!using_header_bar)
			menu_bar.AppendSubmenu (Translations.GetString ("_Adjustments"), adj_menu);

		var effects_menu = Gio.Menu.New ();
		if (!using_header_bar)
			menu_bar.AppendSubmenu (Translations.GetString ("Effe_cts"), effects_menu);

		var addins_menu = Gio.Menu.New ();
		PintaCore.Actions.Addins.RegisterActions (app, addins_menu);
		menu_bar.AppendSubmenu (Translations.GetString ("A_dd-ins"), addins_menu);

		var window_menu = Gio.Menu.New ();
		PintaCore.Actions.Window.RegisterActions (app, window_menu);
		menu_bar.AppendSubmenu (Translations.GetString ("_Window"), window_menu);

		var help_menu = Gio.Menu.New ();
		PintaCore.Actions.Help.RegisterActions (app, help_menu);
		menu_bar.AppendSubmenu (Translations.GetString ("_Help"), help_menu);

		var pad_section = Gio.Menu.New ();
		view_menu.AppendSection (null, pad_section);

		show_pad = Gio.Menu.New ();
		pad_section.AppendSubmenu (Translations.GetString ("Tool Windows"), show_pad);

		if (window_shell.HeaderBar is not null) {
			var header_bar = window_shell.HeaderBar;
			header_bar.PackEnd (new Gtk.MenuButton () {
				MenuModel = menu_bar,
				IconName = Resources.StandardIcons.OpenMenu,
				TooltipText = Translations.GetString ("Main Menu"),
			});

			header_bar.PackEnd (new Gtk.MenuButton () {
				MenuModel = effects_menu,
				IconName = Resources.Icons.EffectsDefault,
				TooltipText = Translations.GetString ("Effects"),
			});

			header_bar.PackEnd (new Gtk.MenuButton () {
				MenuModel = adj_menu,
				IconName = Resources.Icons.AdjustmentsBrightnessContrast,
				TooltipText = Translations.GetString ("Adjustments"),
			});

			header_bar.PackEnd (new Gtk.MenuButton () {
				MenuModel = image_menu,
				IconName = Resources.StandardIcons.ImageGeneric,
				TooltipText = Translations.GetString ("Image"),
			});
		}

		PintaCore.Chrome.InitializeMainMenu (adj_menu, effects_menu);
	}

	private void CreateMainToolBar ()
	{
		if (window_shell.HeaderBar is not null)
			PintaCore.Actions.CreateHeaderToolBar (window_shell.HeaderBar!);
		else {
			var main_toolbar = window_shell.CreateToolBar ("main_toolbar");
			PintaCore.Actions.CreateToolBar (main_toolbar);
		}
	}

	private void CreateToolToolBar ()
	{
		var tool_toolbar = window_shell.CreateToolBar ("tool_toolbar");
		tool_toolbar.HeightRequest = 48;

		PintaCore.Chrome.InitializeToolToolBar (tool_toolbar);
	}

	private void CreateStatusBar ()
	{
		var statusbar = window_shell.CreateStatusBar ("statusbar");

		statusbar.Append (new StatusBarColorPaletteWidget () {
			Hexpand = true,
			Halign = Gtk.Align.Fill,
		});

		PintaCore.Actions.CreateStatusBar (statusbar, PintaCore.Workspace);

		PintaCore.Chrome.InitializeStatusBar (statusbar);
	}

	private void CreatePanels ()
	{
		Gtk.Box panel_container = window_shell.CreateWorkspace ();
		CreateDockAndPads (panel_container);
	}

	private void CreateDockAndPads (Gtk.Box container)
	{
		ToolBoxWidget toolbox = new ();
		Gtk.ScrolledWindow toolbox_scroll = new () {
			Child = toolbox,
			HscrollbarPolicy = Gtk.PolicyType.Never,
			VscrollbarPolicy = Gtk.PolicyType.Never,
			HasFrame = false,
			OverlayScrolling = true,
			WindowPlacement = Gtk.CornerType.BottomRight,
		};
		container.Append (toolbox_scroll);
		PintaCore.Chrome.InitializeToolBox (toolbox);

		// Dock widget
		dock = new Dock {
			Hexpand = true,
			Halign = Gtk.Align.Fill,
		};

		// Canvas pad
		canvas_pad = new CanvasPad ();
		canvas_pad.Initialize (dock);
		PintaCore.Chrome.InitializeImageTabsNotebook (canvas_pad.Notebook);

		// Layer pad
		LayersPad layers_pad = new ();
		layers_pad.Initialize (dock, app, show_pad);

		// History pad
		HistoryPad history_pad = new ();
		history_pad.Initialize (dock, app, show_pad);

		container.Append (dock);
	}

	#region User Settings
	private const string LastDialogDirSettingKey = "last-dialog-directory";
	private const string LastSelectedToolSettingKey = "last-selected-tool";

	private void LoadUserSettings ()
	{
		dock.LoadSettings (PintaCore.Settings);

		// Set selected tool to last selected or default to the PaintBrush
		PintaCore.Tools.SetCurrentTool (PintaCore.Settings.GetSetting (LastSelectedToolSettingKey, "PaintBrushTool"));

		PintaCore.Actions.View.Rulers.Value = PintaCore.Settings.GetSetting ("ruler-shown", false);
		PintaCore.Actions.View.ToolBar.Value = PintaCore.Settings.GetSetting ("toolbar-shown", true);
		PintaCore.Actions.View.StatusBar.Value = PintaCore.Settings.GetSetting ("statusbar-shown", true);
		PintaCore.Actions.View.ToolBox.Value = PintaCore.Settings.GetSetting ("toolbox-shown", true);
		PintaCore.Actions.View.ImageTabs.Value = PintaCore.Settings.GetSetting ("image-tabs-shown", true);
		PintaCore.Actions.View.PixelGrid.Value = PintaCore.Settings.GetSetting ("pixel-grid-shown", false);

		string dialog_uri = PintaCore.Settings.GetSetting (LastDialogDirSettingKey, PintaCore.RecentFiles.DefaultDialogDirectory?.GetUri () ?? "");
		PintaCore.RecentFiles.LastDialogDirectory = Gio.FileHelper.NewForUri (dialog_uri);

		MetricType ruler_metric = (MetricType) PintaCore.Settings.GetSetting ("ruler-metric", (int) MetricType.Pixels);
		PintaCore.Actions.View.RulerMetric.Activate (GLib.Variant.NewInt32 ((int) ruler_metric));

		int color_scheme = PintaCore.Settings.GetSetting ("color-scheme", 0);
		PintaCore.Actions.View.ColorScheme.Activate (GLib.Variant.NewInt32 (color_scheme));
	}

	private void SaveUserSettings ()
	{
		dock.SaveSettings (PintaCore.Settings);

		// Don't store the maximized height if the window is maximized
		if (!window_shell.Window.IsMaximized ()) {
			PintaCore.Settings.PutSetting ("window-size-width", window_shell.Window.GetWidth ());
			PintaCore.Settings.PutSetting ("window-size-height", window_shell.Window.GetHeight ());
		}

		PintaCore.Settings.PutSetting ("ruler-metric", (int) GetCurrentRulerMetric ());
		PintaCore.Settings.PutSetting ("color-scheme", PintaCore.Actions.View.ColorScheme.GetState ()!.GetInt32 ());
		PintaCore.Settings.PutSetting ("window-maximized", window_shell.Window.IsMaximized ());
		PintaCore.Settings.PutSetting ("ruler-shown", PintaCore.Actions.View.Rulers.Value);
		PintaCore.Settings.PutSetting ("image-tabs-shown", PintaCore.Actions.View.ImageTabs.Value);
		PintaCore.Settings.PutSetting ("toolbar-shown", PintaCore.Actions.View.ToolBar.Value);
		PintaCore.Settings.PutSetting ("statusbar-shown", PintaCore.Actions.View.StatusBar.Value);
		PintaCore.Settings.PutSetting ("toolbox-shown", PintaCore.Actions.View.ToolBox.Value);
		PintaCore.Settings.PutSetting ("pixel-grid-shown", PintaCore.Actions.View.PixelGrid.Value);
		PintaCore.Settings.PutSetting (LastDialogDirSettingKey, PintaCore.RecentFiles.LastDialogDirectory?.GetUri () ?? "");

		if (PintaCore.Tools.CurrentTool is BaseTool tool)
			PintaCore.Settings.PutSetting (LastSelectedToolSettingKey, tool.GetType ().Name);

		PintaCore.Settings.DoSaveSettingsBeforeQuit ();
	}
	#endregion

	#region Action Handlers
	private bool HandleCloseRequest (object o, EventArgs args)
	{
		PintaCore.Actions.App.Exit.Activate ();

		// Stop the default handler from running so the user can cancel quitting.
		return true;
	}

	private bool HandleDrop (Gtk.DropTarget sender, Gtk.DropTarget.DropSignalArgs args)
	{
		if (args.Value.GetBoxed (Gdk.FileList.GetGType ()) is not Gdk.FileList file_list)
			return false;

		foreach (Gio.File file in file_list.GetFilesHelper ()) {
			PintaCore.Workspace.OpenFile (file);

			if (file.GetUriScheme () is string scheme &&
			   (scheme.StartsWith ("http") || scheme.StartsWith ("ftp"))) {
				// If the file was likely dragged from a browser, mark as not having a file
				// so that the user must choose a new file to save to instead of hitting a permission error.
				PintaCore.Workspace.ActiveDocument.ClearFileReference ();
			}
		}

		return true;
	}

	private void ZoomToSelection_Activated (object sender, EventArgs e)
	{
		PintaCore.Workspace.ActiveWorkspace.ZoomToCanvasRectangle (PintaCore.Workspace.ActiveDocument.Selection.SelectionPath.GetBounds ().ToDouble ());
	}

	private void ZoomToWindow_Activated (object sender, EventArgs e)
	{
		// The image is small enough to fit in the window
		if (PintaCore.Workspace.ImageFitsInWindow) {
			PintaCore.Actions.View.ActualSize.Activate ();
		} else {
			int image_x = PintaCore.Workspace.ImageSize.Width;
			int image_y = PintaCore.Workspace.ImageSize.Height;

			var canvas_window = PintaCore.Workspace.ActiveWorkspace.Canvas.Parent!;

			int window_x = canvas_window.GetAllocatedWidth ();
			int window_y = canvas_window.GetAllocatedHeight ();

			double ratio =
				(image_x / (double) window_x >= image_y / (double) window_y)
				? (window_x - 20) / (double) image_x
				: (window_y - 20) / (double) image_y;

			// The image is more constrained by width than height

			PintaCore.Workspace.Scale = ratio;
			PintaCore.Actions.View.SuspendZoomUpdate ();
			PintaCore.Actions.View.ZoomComboBox.ComboBox.GetEntry ().SetText (ViewActions.ToPercent (PintaCore.Workspace.Scale));
			PintaCore.Actions.View.ResumeZoomUpdate ();
		}

		PintaCore.Actions.View.ZoomToWindowActivated = true;
	}
	#endregion


	private void ActiveDocumentChanged (object? sender, EventArgs e)
	{
		if (!PintaCore.Workspace.HasOpenDocuments)
			return;

		PintaCore.Actions.View.SuspendZoomUpdate ();
		PintaCore.Actions.View.ZoomComboBox.ComboBox.GetEntry ().SetText (ViewActions.ToPercent (PintaCore.Workspace.Scale));
		PintaCore.Actions.View.ResumeZoomUpdate ();

		var doc = PintaCore.Workspace.ActiveDocument;
		var tab = FindTabWithCanvas ((PintaCanvas) doc.Workspace.Canvas);

		if (tab != null)
			canvas_pad.Notebook.ActiveItem = tab;

		doc.Workspace.Canvas.GrabFocus ();
	}

	private IDockNotebookItem? FindTabWithCanvas (PintaCanvas canvas)
		=>
			canvas_pad.Notebook.Items
			.Where (i => ((CanvasWindow) i.Widget).Canvas == canvas)
			.FirstOrDefault ();
}
