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
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Pinta.Docking;
using Pinta.Core;
using Pinta.Gui.Widgets;
using Pinta.MacInterop;

namespace Pinta
{
	public class MainWindow : Gtk.Application
	{
		// NRT - Created in OnActivated
		WindowShell window_shell = null!;
		Dock dock = null!;
		GLib.Menu show_pad = null!;

		CanvasPad canvas_pad = null!;

		public MainWindow () : base ("com.github.PintaProject.Pinta", GLib.ApplicationFlags.NonUnique)
		{
			Register (GLib.Cancellable.Current);
		}

		protected override void OnActivated ()
		{
			base.OnActivated ();

			// Build our window
			CreateWindow ();

			// Initialize interface things
			window_shell.AddAccelGroup (PintaCore.Actions.AccelGroup);
			new ActionHandlers ();

			PintaCore.Chrome.InitializeProgessDialog (new ProgressDialog ());
			PintaCore.Chrome.InitializeErrorDialogHandler ((parent, message, details) => {
				System.Console.Error.WriteLine ("Pinta: {0}", details);
				using var errorDialog = new ErrorDialog (parent);
				errorDialog.SetMessage (message);
				errorDialog.AddDetails (details);

				while (true) {
					var response = (ResponseType) errorDialog.Run ();
					if (response != ResponseType.Help)
						break;
				}
			}
	    );

			PintaCore.Chrome.InitializeUnsupportedFormatDialog ((parent, message, details) => {
				System.Console.Error.WriteLine ("Pinta: {0}", details);
				using var unsupportedFormDialog = new FileUnsupportedFormatDialog (parent);
				unsupportedFormDialog.SetMessage (message);
				unsupportedFormDialog.Run ();
			}
			);

			PintaCore.Initialize ();

			// Initialize extensions
			// TODO-GTK3 (addins)
#if false
			AddinManager.Initialize ();
			AddinManager.Registry.Update ();
			AddinSetupService setupService = new AddinSetupService (AddinManager.Registry);
			if (!setupService.AreRepositoriesRegistered ())
				setupService.RegisterRepositories (true);

			//Look out for any changes in extensions
			AddinManager.AddExtensionNodeHandler (typeof (IExtension), OnExtensionChanged);
#else
			var tools = new Pinta.Tools.CoreToolsExtension ();
			tools.Initialize ();
			var effects = new Pinta.Effects.CoreEffectsExtension ();
			effects.Initialize ();
#endif

			// Load the user's previous settings
			LoadUserSettings ();

			// We support drag and drop for URIs
			window_shell.AddDragDropSupport (new Gtk.TargetEntry ("text/uri-list", 0, 100));

			// Handle a few main window specific actions
			PintaCore.Actions.App.BeforeQuit += delegate { SaveUserSettings (); };

			window_shell.DeleteEvent += MainWindow_DeleteEvent;
			window_shell.DragDataReceived += MainWindow_DragDataReceived;

			window_shell.KeyPressEvent += MainWindow_KeyPressEvent;
			window_shell.KeyReleaseEvent += MainWindow_KeyReleaseEvent;

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

			if (PintaCore.Workspace.OpenDocuments.IndexOf (view.Document) > -1) {
				PintaCore.Workspace.SetActiveDocument (view.Document);
				PintaCore.Actions.File.Close.Activate ();

				// User must have canceled the close
				if (PintaCore.Workspace.OpenDocuments.IndexOf (view.Document) > -1)
					e.Cancel = true;
			}
		}

		private void DockNotebook_ActiveTabChanged (object? sender, EventArgs e)
		{
			var item = canvas_pad.Notebook.ActiveItem;

			if (item == null)
				return;

			var view = (DocumentViewContent) item;

			PintaCore.Workspace.SetActiveDocument (view.Document);

			((CanvasWindow) view.Widget).Canvas.Window.Cursor = PintaCore.Tools.CurrentTool?.CurrentCursor;
		}

		private void Workspace_DocumentCreated (object? sender, DocumentEventArgs e)
		{
			var doc = e.Document;

			var notebook = canvas_pad.Notebook;
			var selected_index = notebook.CurrentPage;


			var canvas = new CanvasWindow (doc) {
				RulersVisible = PintaCore.Actions.View.Rulers.Value,
				RulerMetric = GetCurrentRulerMetric ()
			};

			var my_content = new DocumentViewContent (doc, canvas);

			// Insert our tab to the right of the currently selected tab
			notebook.InsertTab (my_content, selected_index + 1);

			doc.Workspace.Canvas = canvas.Canvas;

			// Zoom to window only on first show (if we do it always, it will be called on every resize)
			canvas.SizeAllocated += (o, e2) => {
				if (!canvas.HasBeenShown)
					ZoomToWindow_Activated (o, e);

				canvas.HasBeenShown = true;
			};

			PintaCore.Actions.View.Rulers.Toggled += (active) => { canvas.RulersVisible = active; };
			PintaCore.Actions.View.RulerMetric.Activated += (o, args) => {
				PintaCore.Actions.View.RulerMetric.ChangeState (args.P0);
				canvas.RulerMetric = GetCurrentRulerMetric ();
			};
		}

		private MetricType GetCurrentRulerMetric ()
		{
			return (MetricType) (int) PintaCore.Actions.View.RulerMetric.State;
		}

		[GLib.ConnectBefore]
		private void MainWindow_KeyPressEvent (object o, KeyPressEventArgs e)
		{
			// Give the widget that has focus a first shot at handling the event.
			// Otherwise, key presses may be intercepted by shortcuts for menu items.
			if (SendToFocusWidget (e, e.Event))
				return;

			// Give the Canvas (and by extension the tools)
			// first shot at handling the event if
			// the mouse pointer is on the canvas
			if (PintaCore.Workspace.HasOpenDocuments) {
				var canvas_window = ((PintaCanvas) PintaCore.Workspace.ActiveWorkspace.Canvas).CanvasWindow;

				if (canvas_window.Canvas.HasFocus || canvas_window.IsMouseOnCanvas)
					canvas_window.Canvas.DoKeyPressEvent (o, e);
			}

			// If the canvas/tool didn't consume it, see if its a toolbox shortcut
			if (e.RetVal is not true) {
				if (e.Event.State.FilterModifierKeys () == Gdk.ModifierType.None)
					PintaCore.Tools.SetCurrentTool (e.Event.Key);
			}

			// Finally, see if the palette widget wants it.
			if (e.RetVal is not true) {
				PintaCore.Palette.DoKeyPress (o, e);
			}
		}

		[GLib.ConnectBefore]
		private void MainWindow_KeyReleaseEvent (object o, KeyReleaseEventArgs e)
		{
			if (SendToFocusWidget (e, e.Event) || !PintaCore.Workspace.HasOpenDocuments)
				return;

			// Give the Canvas (and by extension the tools)
			// first shot at handling the event if
			// the mouse pointer is on the canvas
			var canvas_window = ((PintaCanvas) PintaCore.Workspace.ActiveWorkspace.Canvas).CanvasWindow;

			if (canvas_window.Canvas.HasFocus || canvas_window.IsMouseOnCanvas)
				canvas_window.Canvas.DoKeyReleaseEvent (o, e);
		}

		private bool SendToFocusWidget (GLib.SignalArgs args, Gdk.EventKey e)
		{
			var widget = window_shell.Focus;
			if (widget != null && widget.ProcessEvent (e)) {
				args.RetVal = true;
				return true;
			}

			return false;
		}

		// Called when an extension node is added or removed
		// TODO-GTK3 (addins)
#if false
		private void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			IExtension extension = (IExtension) args.ExtensionObject;
			if (args.Change == ExtensionChange.Add)
				extension.Initialize ();
			else
				extension.Uninitialize ();
		}
#endif

		#region GUI Construction
		private void CreateWindow ()
		{
			// Check for stored window settings
			var width = PintaCore.Settings.GetSetting<int> ("window-size-width", 1100);
			var height = PintaCore.Settings.GetSetting<int> ("window-size-height", 750);
			var maximize = PintaCore.Settings.GetSetting<bool> ("window-maximized", false);

			window_shell = new WindowShell (this, "Pinta.GenericWindow", "Pinta", width, height, maximize);

			CreateMainMenu (window_shell);
			CreateMainToolBar (window_shell);
			CreateToolToolBar (window_shell);

			CreatePanels (window_shell);
			CreateStatusBar (window_shell);

			AddWindow (window_shell);
			window_shell.ShowAll ();

			PintaCore.Chrome.InitializeApplication (this);
			PintaCore.Chrome.InitializeWindowShell (window_shell);
		}

		private void CreateMainMenu (WindowShell shell)
		{
			if (PintaCore.System.OperatingSystem == OS.Mac) {
				// Only use the application on macOS. On other platforms, these
				// menu items appear under File, Help, etc.
				var app_menu = new GLib.Menu ();
				PintaCore.Actions.App.RegisterActions (this, app_menu);
				AppMenu = app_menu;
			}

			var menu_bar = new GLib.Menu ();
			Menubar = menu_bar;

			var file_menu = new GLib.Menu ();
			PintaCore.Actions.File.RegisterActions (this, file_menu);
			menu_bar.AppendSubmenu (Translations.GetString ("_File"), file_menu);

			var edit_menu = new GLib.Menu ();
			PintaCore.Actions.Edit.RegisterActions (this, edit_menu);
			menu_bar.AppendSubmenu (Translations.GetString ("_Edit"), edit_menu);

			var view_menu = new GLib.Menu ();
			PintaCore.Actions.View.RegisterActions (this, view_menu);
			menu_bar.AppendSubmenu (Translations.GetString ("_View"), view_menu);

			var image_menu = new GLib.Menu ();
			PintaCore.Actions.Image.RegisterActions (this, image_menu);
			menu_bar.AppendSubmenu (Translations.GetString ("_Image"), image_menu);

			var layers_menu = new GLib.Menu ();
			PintaCore.Actions.Layers.RegisterActions (this, layers_menu);
			menu_bar.AppendSubmenu (Translations.GetString ("_Layers"), layers_menu);

			var adj_menu = new GLib.Menu ();
			menu_bar.AppendSubmenu (Translations.GetString ("_Adjustments"), adj_menu);

			var effects_menu = new GLib.Menu ();
			menu_bar.AppendSubmenu (Translations.GetString ("Effe_cts"), effects_menu);

			var addins_menu = new GLib.Menu ();
			PintaCore.Actions.Addins.RegisterActions (this, addins_menu);
			menu_bar.AppendSubmenu (Translations.GetString ("A_dd-ins"), addins_menu);

			var window_menu = new GLib.Menu ();
			PintaCore.Actions.Window.RegisterActions (this, window_menu);
			menu_bar.AppendSubmenu (Translations.GetString ("_Window"), window_menu);

			var help_menu = new GLib.Menu ();
			PintaCore.Actions.Help.RegisterActions (this, help_menu);
			menu_bar.AppendSubmenu (Translations.GetString ("_Help"), help_menu);

			var pad_section = new GLib.Menu ();
			view_menu.AppendSection (null, pad_section);

			show_pad = new GLib.Menu ();
			pad_section.AppendSubmenu (Translations.GetString ("Tool Windows"), show_pad);

			PintaCore.Chrome.InitializeMainMenu (adj_menu, effects_menu);
		}

		private void CreateMainToolBar (WindowShell shell)
		{
			var main_toolbar = window_shell.CreateToolBar ("main_toolbar");

			if (PintaCore.System.OperatingSystem == OS.Windows) {
				main_toolbar.ToolbarStyle = ToolbarStyle.Icons;
				main_toolbar.IconSize = IconSize.SmallToolbar;
			}

			PintaCore.Actions.CreateToolBar (main_toolbar);

			PintaCore.Chrome.InitializeMainToolBar (main_toolbar);
		}

		private void CreateToolToolBar (WindowShell shell)
		{
			var tool_toolbar = window_shell.CreateToolBar ("tool_toolbar");

			tool_toolbar.ToolbarStyle = ToolbarStyle.Icons;
			tool_toolbar.IconSize = IconSize.SmallToolbar;

			if (PintaCore.System.OperatingSystem == OS.Windows)
				tool_toolbar.HeightRequest = 28;
			else
				tool_toolbar.HeightRequest = 32;

			PintaCore.Chrome.InitializeToolToolBar (tool_toolbar);
		}

		private void CreateStatusBar (WindowShell shell)
		{
			var statusbar = shell.CreateStatusBar ("statusbar");

			statusbar.PackStart (new StatusBarColorPaletteWidget (), true, true, 0);

			PintaCore.Actions.CreateStatusBar (statusbar);

			PintaCore.Chrome.InitializeStatusBar (statusbar);
		}

		private void CreatePanels (WindowShell shell)
		{
			HBox panel_container = shell.CreateWorkspace ();

			CreateDockAndPads (panel_container);
			panel_container.ShowAll ();
		}

		private void CreateDockAndPads (HBox container)
		{
			var toolbox = new ToolBoxWidget ();
			container.PackStart (toolbox, false, false, 0);
			PintaCore.Chrome.InitializeToolBox (toolbox);

			// Dock widget
			dock = new Dock ();

			// Canvas pad
			canvas_pad = new CanvasPad ();
			canvas_pad.Initialize (dock);

			// Layer pad
			var layers_pad = new LayersPad ();
			layers_pad.Initialize (dock, this, show_pad);

			// History pad
			var history_pad = new HistoryPad ();
			history_pad.Initialize (dock, this, show_pad);

			container.PackStart (dock, true, true, 0);

			// TODO-GTK3 (docking)
#if false
			string layout_file = Path.Combine (PintaCore.Settings.GetUserSettingsDirectory (), "layouts.xml");

            if (System.IO.File.Exists(layout_file))
            {
                try
                {
                    dock.LoadLayouts(layout_file);
                }
                // If parsing layouts.xml fails for some reason, proceed to create the default layout.
                catch (Exception e)
                {
                    System.Console.Error.WriteLine ("Error reading " + layout_file + ": " + e.ToString());
                }
            }
			
			if (!dock.HasLayout ("Default"))
				dock.CreateLayout ("Default", false);
				
			dock.CurrentLayout = "Default";
#endif
		}
		#endregion

		#region User Settings
		private const string LastDialogDirSettingKey = "last-dialog-directory";
		private const string LastSelectedToolSettingKey = "last-selected-tool";

		private void LoadUserSettings ()
		{
			// Set selected tool to last selected or default to the PaintBrush
			PintaCore.Tools.SetCurrentTool (PintaCore.Settings.GetSetting (LastSelectedToolSettingKey, "PaintBrushTool"));

			PintaCore.Actions.View.Rulers.Value = PintaCore.Settings.GetSetting ("ruler-shown", false);
			PintaCore.Actions.View.ToolBar.Value = PintaCore.Settings.GetSetting ("toolbar-shown", true);
			PintaCore.Actions.View.ImageTabs.Value = PintaCore.Settings.GetSetting ("image-tabs-shown", true);
			PintaCore.Actions.View.PixelGrid.Value = PintaCore.Settings.GetSetting ("pixel-grid-shown", false);
			PintaCore.System.LastDialogDirectory = PintaCore.Settings.GetSetting (LastDialogDirSettingKey,
											      PintaCore.System.DefaultDialogDirectory);

			var ruler_metric = (MetricType) PintaCore.Settings.GetSetting ("ruler-metric", (int) MetricType.Pixels);
			PintaCore.Actions.View.RulerMetric.Activate (new GLib.Variant ((int) ruler_metric));
		}

		private void SaveUserSettings ()
		{
			// TODO-GTK3 (docking)
#if false
			dock.SaveLayouts (Path.Combine (PintaCore.Settings.GetUserSettingsDirectory (), "layouts.xml"));
#endif

			// Don't store the maximized height if the window is maximized
			if ((window_shell.Window.State & Gdk.WindowState.Maximized) == 0) {
				PintaCore.Settings.PutSetting ("window-size-width", window_shell.Window.GetSize ().Width);
				PintaCore.Settings.PutSetting ("window-size-height", window_shell.Window.GetSize ().Height);
			}

			PintaCore.Settings.PutSetting ("ruler-metric", (int) GetCurrentRulerMetric ());
			PintaCore.Settings.PutSetting ("window-maximized", (window_shell.Window.State & Gdk.WindowState.Maximized) != 0);
			PintaCore.Settings.PutSetting ("ruler-shown", PintaCore.Actions.View.Rulers.Value);
			PintaCore.Settings.PutSetting ("image-tabs-shown", PintaCore.Actions.View.ImageTabs.Value);
			PintaCore.Settings.PutSetting ("toolbar-shown", PintaCore.Actions.View.ToolBar.Value);
			PintaCore.Settings.PutSetting ("pixel-grid-shown", PintaCore.Actions.View.PixelGrid.Value);
			PintaCore.Settings.PutSetting (LastDialogDirSettingKey, PintaCore.System.LastDialogDirectory);

			if (PintaCore.Tools.CurrentTool is BaseTool tool)
				PintaCore.Settings.PutSetting (LastSelectedToolSettingKey, tool.GetType ().Name);

			PintaCore.Settings.DoSaveSettingsBeforeQuit ();
		}
		#endregion

		#region Action Handlers
		private void MainWindow_DeleteEvent (object o, DeleteEventArgs args)
		{
			// leave window open so user can cancel quitting
			args.RetVal = true;

			PintaCore.Actions.App.Exit.Activate ();
		}

		private void MainWindow_DragDataReceived (object o, DragDataReceivedArgs args)
		{
			// TODO: Generate random name for the picture being downloaded

			// Only handle URIs
			if (args.Info != 100)
				return;

			string fullData = System.Text.Encoding.UTF8.GetString (args.SelectionData.Data);

			foreach (string individualFile in fullData.Split ('\n')) {
				string file = individualFile.Trim ();

				if (file.StartsWith ("http") || file.StartsWith ("ftp")) {
					var client = new System.Net.WebClient ();
					string tempFilePath = System.IO.Path.GetTempPath () + System.IO.Path.GetFileName (file);

					var progressDialog = PintaCore.Chrome.ProgressDialog;

					try {
						PintaCore.Chrome.MainWindowBusy = true;

						progressDialog.Title = Translations.GetString ("Downloading Image");
						progressDialog.Text = "";
						progressDialog.Show ();

						client.DownloadProgressChanged += (sender, e) => {
							progressDialog.Progress = e.ProgressPercentage;
						};

						client.DownloadFile (file, tempFilePath);

						if (PintaCore.Workspace.OpenFile (tempFilePath)) {
							// Mark as not having a file, so that the user doesn't unintentionally
							// save using the temp file.
							PintaCore.Workspace.ActiveDocument.HasFile = false;
						}
					} catch (Exception e) {
						progressDialog.Hide ();
						PintaCore.Chrome.ShowErrorDialog (PintaCore.Chrome.MainWindow,
							Translations.GetString ("Download failed"),
							string.Format (Translations.GetString ("Unable to download image from {0}.\nDetails: {1}"), file, e.Message));
					} finally {
						client.Dispose ();
						progressDialog.Hide ();
						PintaCore.Chrome.MainWindowBusy = false;
					}
				} else if (file.StartsWith ("file://")) {
					PintaCore.Workspace.OpenFile (new Uri (file).LocalPath);
				}
			}
		}

		private void ZoomToSelection_Activated (object sender, EventArgs e)
		{
			PintaCore.Workspace.ActiveWorkspace.ZoomToRectangle (PintaCore.Workspace.ActiveDocument.Selection.SelectionPath.GetBounds ().ToCairoRectangle ());
		}

		private void ZoomToWindow_Activated (object sender, EventArgs e)
		{
			// The image is small enough to fit in the window
			if (PintaCore.Workspace.ImageFitsInWindow) {
				PintaCore.Actions.View.ActualSize.Activate ();
			} else {
				int image_x = PintaCore.Workspace.ImageSize.Width;
				int image_y = PintaCore.Workspace.ImageSize.Height;

				var canvas_window = PintaCore.Workspace.ActiveWorkspace.Canvas.Parent;

				var window_x = canvas_window.Allocation.Width;
				var window_y = canvas_window.Allocation.Height;

				double ratio;

				// The image is more constrained by width than height
				if ((double) image_x / (double) window_x >= (double) image_y / (double) window_y) {
					ratio = (double) (window_x - 20) / (double) image_x;
				} else {
					ratio = (double) (window_y - 20) / (double) image_y;
				}

				PintaCore.Workspace.Scale = ratio;
				PintaCore.Actions.View.SuspendZoomUpdate ();
				PintaCore.Actions.View.ZoomComboBox.ComboBox.Entry.Text = ViewActions.ToPercent (PintaCore.Workspace.Scale);
				PintaCore.Actions.View.ResumeZoomUpdate ();
			}

			PintaCore.Actions.View.ZoomToWindowActivated = true;
		}

		private void ActiveDocumentChanged (object? sender, EventArgs e)
		{
			if (PintaCore.Workspace.HasOpenDocuments) {
				PintaCore.Actions.View.SuspendZoomUpdate ();
				PintaCore.Actions.View.ZoomComboBox.ComboBox.Entry.Text = ViewActions.ToPercent (PintaCore.Workspace.Scale);
				PintaCore.Actions.View.ResumeZoomUpdate ();

				var doc = PintaCore.Workspace.ActiveDocument;
				var tab = FindTabWithCanvas ((PintaCanvas) doc.Workspace.Canvas);

				if (tab != null) {
					canvas_pad.Notebook.ActiveItem = tab;
				}

				doc.Workspace.Canvas.GrabFocus ();
			}
		}

		private IDockNotebookItem? FindTabWithCanvas (PintaCanvas canvas)
		{
			return canvas_pad.Notebook.Items
				.Where (i => ((CanvasWindow) i.Widget).Canvas == canvas)
				.FirstOrDefault ();
		}
		#endregion
	}
}
