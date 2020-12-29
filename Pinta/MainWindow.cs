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
using Mono.Addins;
using Mono.Unix;
using Pinta.Docking;
using Pinta.Docking.DockNotebook;
using Pinta.Docking.Gui;
using Pinta.Core;
using Pinta.Gui.Widgets;
using Pinta.MacInterop;

namespace Pinta
{
	public class MainWindow
	{
		WindowShell window_shell;
		DockFrame dock;
		Menu show_pad;
        DockNotebookContainer dock_container;

		CanvasPad canvas_pad;

        bool suppress_active_notebook_change = false;

        public MainWindow ()
		{
            // Build our window
			CreateWindow ();

			// Initialize interface things
			window_shell.AddAccelGroup (PintaCore.Actions.AccelGroup);
			new ActionHandlers ();

			PintaCore.Chrome.InitializeProgessDialog (new ProgressDialog ());
			PintaCore.Chrome.InitializeErrorDialogHandler ((parent, message, details) => {
				System.Console.Error.WriteLine ("Pinta: {0}", details);
				ErrorDialog errorDialog = new ErrorDialog (parent);				
				try {
					errorDialog.SetMessage (message);
					errorDialog.AddDetails (details);
					errorDialog.Run ();
				} finally {
					errorDialog.Destroy ();
				}
			}
			);

			PintaCore.Chrome.InitializeUnsupportedFormatDialog((parent, message, details) => {
				System.Console.Error.WriteLine("Pinta: {0}", details);
				FileUnsupportedFormatDialog unsupportedFormDialog = new FileUnsupportedFormatDialog(parent);
				try
				{
					unsupportedFormDialog.SetMessage(message);
					unsupportedFormDialog.Run();
				}
				finally
				{
					unsupportedFormDialog.Destroy();
				}
			}
			);

			PintaCore.Initialize ();

			// Initialize extensions
			AddinManager.Initialize ();
			AddinManager.Registry.Update ();
			AddinSetupService setupService = new AddinSetupService (AddinManager.Registry);
			if (!setupService.AreRepositoriesRegistered ())
				setupService.RegisterRepositories (true);

			//Look out for any changes in extensions
			AddinManager.AddExtensionNodeHandler (typeof (IExtension), OnExtensionChanged);

			// Try to set the default tool to the PaintBrush
			PintaCore.Tools.SetCurrentTool (Catalog.GetString ("Paintbrush"));

			// Load the user's previous settings
			LoadUserSettings ();

			// We support drag and drop for URIs
			window_shell.AddDragDropSupport (new Gtk.TargetEntry ("text/uri-list", 0, 100));
			
			// Handle a few main window specific actions
			PintaCore.Actions.File.BeforeQuit += delegate { SaveUserSettings (); };

			window_shell.DeleteEvent += MainWindow_DeleteEvent;
			window_shell.DragDataReceived += MainWindow_DragDataReceived;

			window_shell.KeyPressEvent += MainWindow_KeyPressEvent;
			window_shell.KeyReleaseEvent += MainWindow_KeyReleaseEvent;

			// TODO: These need to be [re]moved when we redo zoom support
			PintaCore.Actions.View.ZoomToWindow.Activated += new EventHandler (ZoomToWindow_Activated);
			PintaCore.Actions.View.ZoomToSelection.Activated += new EventHandler (ZoomToSelection_Activated);
			PintaCore.Workspace.ActiveDocumentChanged += ActiveDocumentChanged;

            PintaCore.Workspace.DocumentCreated += Workspace_DocumentCreated;
            PintaCore.Workspace.DocumentClosed += Workspace_DocumentClosed;

            DockNotebookManager.ActiveNotebookChanged += DockNotebook_ActiveNotebookChanged;
            DockNotebookManager.ActiveTabChanged += DockNotebook_ActiveTabChanged;
            DockNotebookManager.TabClosed += DockNotebook_TabClosed;
            DockNotebookManager.NotebookDragDataReceived += MainWindow_DragDataReceived;
        }

        private void Workspace_DocumentClosed (object sender, DocumentEventArgs e)
        {
            var tab = FindTabWithCanvas ((PintaCanvas)e.Document.Workspace.Canvas);

            if (tab != null)
                dock_container.CloseTab (tab);
        }

        private void DockNotebook_TabClosed (object sender, TabClosedEventArgs e)
        {
            if (e.Tab == null || e.Tab.Content == null)
                return;

            var content = (SdiWorkspaceWindow)e.Tab.Content;
            var view = (DocumentViewContent)content.ViewContent;

            if (PintaCore.Workspace.OpenDocuments.IndexOf (view.Document) > -1) {
                PintaCore.Workspace.SetActiveDocument (view.Document);
                PintaCore.Actions.File.Close.Activate ();

                // User must have canceled the close
                if (PintaCore.Workspace.OpenDocuments.IndexOf (view.Document) > -1)
                    e.Cancel = true;
            }
        }

        private void DockNotebook_ActiveNotebookChanged (object sender, EventArgs e)
        {
            if (suppress_active_notebook_change)
                return;

            var tab = DockNotebookManager.ActiveTab;

            if (tab == null || tab.Content == null)
                return;

            var content = (SdiWorkspaceWindow)tab.Content;
            var view = (DocumentViewContent)content.ViewContent;

            PintaCore.Workspace.SetActiveDocument (view.Document);

            ((CanvasWindow)view.Control).Canvas.GdkWindow.Cursor = PintaCore.Tools.CurrentTool.CurrentCursor;
        }

        private void DockNotebook_ActiveTabChanged (object sender, EventArgs e)
        {
            var tab = DockNotebookManager.ActiveTab;

            if (tab == null || tab.Content == null)
                return;

            var content = (SdiWorkspaceWindow)tab.Content;
            var view = (DocumentViewContent)content.ViewContent;

            PintaCore.Workspace.SetActiveDocument (view.Document);

            ((CanvasWindow)view.Control).Canvas.GdkWindow.Cursor = PintaCore.Tools.CurrentTool.CurrentCursor;
        }

        private void Workspace_DocumentCreated (object sender, DocumentEventArgs e)
        {
            var doc = e.Document;

            // Find the currently active container for our new tab
            var container = DockNotebookManager.ActiveNotebookContainer ?? dock_container;
            var selected_index = container.TabControl.CurrentTabIndex;

            var canvas = new CanvasWindow (doc) {
                RulersVisible = PintaCore.Actions.View.Rulers.Active,
                RulerMetric = GetCurrentRulerMetric ()
            };

            var my_content = new DocumentViewContent (doc, canvas);

            // Insert our tab to the right of the currently selected tab
            container.TabControl.InsertTab (my_content, selected_index + 1);

            doc.Workspace.Canvas = canvas.Canvas;

            // Zoom to window only on first show (if we do it always, it will be called on every resize)
            canvas.SizeAllocated += (o, e2) => {
                if (!canvas.HasBeenShown)
                    ZoomToWindow_Activated (o, e);

                canvas.HasBeenShown = true;
            };

            PintaCore.Actions.View.Rulers.Toggled += (o, e2) => { canvas.RulersVisible = ((ToggleAction)o).Active; };
            PintaCore.Actions.View.Pixels.Activated += (o, e2) => { canvas.RulerMetric = MetricType.Pixels; };
            PintaCore.Actions.View.Inches.Activated += (o, e2) => { canvas.RulerMetric = MetricType.Inches; };
            PintaCore.Actions.View.Centimeters.Activated += (o, e2) => { canvas.RulerMetric = MetricType.Centimeters; };
        }

        private MetricType GetCurrentRulerMetric ()
        {
            if (PintaCore.Actions.View.Inches.Active)
                return MetricType.Inches;
            else if (PintaCore.Actions.View.Centimeters.Active)
                return MetricType.Centimeters;

            return MetricType.Pixels;
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
                var canvas_window = ((PintaCanvas)PintaCore.Workspace.ActiveWorkspace.Canvas).CanvasWindow;

                if (canvas_window.Canvas.HasFocus || canvas_window.IsMouseOnCanvas)
                    canvas_window.Canvas.DoKeyPressEvent (o, e);
            }

			// If the canvas/tool didn't consume it, see if its a toolbox shortcut
            if (e.RetVal == null || !(bool)e.RetVal) {
				if (e.Event.State.FilterModifierKeys () == Gdk.ModifierType.None)
					PintaCore.Tools.SetCurrentTool (e.Event.Key);
			}

			// Finally, see if the palette widget wants it.
            if (e.RetVal == null || !(bool)e.RetVal) {
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
            var canvas_window = ((PintaCanvas)PintaCore.Workspace.ActiveWorkspace.Canvas).CanvasWindow;

            if (canvas_window.Canvas.HasFocus || canvas_window.IsMouseOnCanvas)
                canvas_window.Canvas.DoKeyReleaseEvent (o, e);
		}

        private bool SendToFocusWidget (GLib.SignalArgs args, Gdk.EventKey e)
        {
            var widget = window_shell.Focus;
            if (widget != null && widget.ProcessEvent (e))
            {
                args.RetVal = true;
                return true;
            }

            return false;
        }

		// Called when an extension node is added or removed
		private void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			IExtension extension = (IExtension) args.ExtensionObject;
			if (args.Change == ExtensionChange.Add)
				extension.Initialize ();
			else
				extension.Uninitialize ();
		}

		#region GUI Construction
		private void CreateWindow ()
		{
			// Check for stored window settings
			var width = PintaCore.Settings.GetSetting<int> ("window-size-width", 1100);
			var height = PintaCore.Settings.GetSetting<int> ("window-size-height", 750);
			var maximize = PintaCore.Settings.GetSetting<bool> ("window-maximized", false);

			window_shell = new WindowShell ("Pinta.GenericWindow", "Pinta", width, height, maximize);

			CreateMainMenu (window_shell);
			CreateMainToolBar (window_shell);
			CreateToolToolBar (window_shell);

			CreatePanels (window_shell);

			window_shell.ShowAll ();

			PintaCore.Chrome.InitializeWindowShell (window_shell);
		}

		private void CreateMainMenu (WindowShell shell)
		{
			var main_menu = window_shell.CreateMainMenu ("main_menu");

			main_menu.Append (new Gtk.Action ("file", Catalog.GetString ("_File")).CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("edit", Catalog.GetString ("_Edit")).CreateMenuItem ());

			MenuItem view_menu = (MenuItem)new Gtk.Action ("view", Catalog.GetString ("_View")).CreateMenuItem ();
			main_menu.Append (view_menu);
			
			main_menu.Append (new Gtk.Action ("image", Catalog.GetString ("_Image")).CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("layers", Catalog.GetString ("_Layers")).CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("adjustments", Catalog.GetString ("_Adjustments")).CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("effects", Catalog.GetString ("Effe_cts")).CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("addins", Catalog.GetString ("A_dd-ins")).CreateMenuItem ());

			MenuItem window_menu = (MenuItem)new Gtk.Action ("window", Catalog.GetString ("_Window")).CreateMenuItem ();
			window_menu.Submenu = new Menu ();
			main_menu.Append (window_menu);

			Gtk.Action pads = new Gtk.Action ("pads", Mono.Unix.Catalog.GetString ("Tool Windows"), null, null);
			view_menu.Submenu = new Menu ();
			show_pad = (Menu)((Menu)(view_menu.Submenu)).AppendItem (pads.CreateSubMenuItem ()).Submenu;

			main_menu.Append (new Gtk.Action ("help", Catalog.GetString ("_Help")).CreateMenuItem ());

			PintaCore.Actions.CreateMainMenu (main_menu);

			if (PintaCore.System.OperatingSystem == OS.Mac) {
				try {
					//enable the global key handler for keyboard shortcuts
					IgeMacMenu.GlobalKeyHandlerEnabled = true;

					//Tell the IGE library to use your GTK menu as the Mac main menu
					IgeMacMenu.MenuBar = main_menu;

					//tell IGE which menu item should be used for the app menu's quit item
					IgeMacMenu.QuitMenuItem = (MenuItem)PintaCore.Actions.File.Exit.CreateMenuItem ();

					//add a new group to the app menu, and add some items to it
					var appGroup = IgeMacMenu.AddAppMenuGroup ();
					MenuItem aboutItem = (MenuItem)PintaCore.Actions.Help.About.CreateMenuItem ();
					appGroup.AddMenuItem (aboutItem, Catalog.GetString ("About"));

					main_menu.Hide ();
				} catch {
					// If things don't work out, just use a normal menu.
				}
			}

			PintaCore.Chrome.InitializeMainMenu (main_menu);
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
		
		private void CreatePanels (WindowShell shell)
		{
			HBox panel_container = shell.CreateWorkspace ();

			CreateDockAndPads (panel_container);
			panel_container.ShowAll ();
		}
		
		private void CreateDockAndPads (HBox container)
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Tools.Pencil.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Tools.Pencil.png")));
			fact.Add ("Pinta.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Pinta.png")));
			fact.AddDefault ();

			// Dock widget
			dock = new DockFrame ();
			dock.CompactGuiLevel = 5;

            var style = new DockVisualStyle ();
            style.PadTitleLabelColor = Styles.PadLabelColor;
            style.PadBackgroundColor = Styles.PadBackground;
            style.InactivePadBackgroundColor = Styles.InactivePadBackground;
            style.TabStyle = DockTabStyle.Normal;
            style.ShowPadTitleIcon = false;
            dock.DefaultVisualStyle = style;

			// Toolbox pad
			var toolboxpad = new ToolBoxPad ();
			toolboxpad.Initialize (dock, show_pad);
		
			// Palette pad
			var palettepad = new ColorPalettePad ();
			palettepad.Initialize (dock, show_pad);

			// Canvas pad
			canvas_pad = new CanvasPad ();
			canvas_pad.Initialize (dock, show_pad);

            dock_container = canvas_pad.NotebookContainer;

			// Layer pad
			var layers_pad = new LayersPad ();
			layers_pad.Initialize (dock, show_pad);

			// Open Images pad
			var open_images_pad = new OpenImagesPad ();
			open_images_pad.Initialize (dock, show_pad);

			// History pad
			var history_pad = new HistoryPad ();
			history_pad.Initialize (dock, show_pad);

			container.PackStart (dock, true, true, 0);
			
			string layout_file = PintaCore.Settings.LayoutFilePath;

            if (System.IO.File.Exists(layout_file))
            {
                try
                {
                    dock.LoadLayouts(layout_file);
                }
                // If parsing layouts.xml fails for some reason, proceed to create the default layout.
                catch (Exception e)
                {
                    System.Console.Error.WriteLine ("Error reading " + PintaCore.Settings.LayoutFile + ": " + e.ToString());
                }
            }
			
			if (!dock.HasLayout ("Default"))
				dock.CreateLayout ("Default", false);
				
			dock.CurrentLayout = "Default";
		}
		#endregion

		#region User Settings
		private const string LastDialogDirSettingKey = "last-dialog-directory";

		private void LoadUserSettings ()
		{
			PintaCore.Actions.View.Rulers.Active = PintaCore.Settings.GetSetting ("ruler-shown", false);
            PintaCore.Actions.View.ToolBar.Active = PintaCore.Settings.GetSetting ("toolbar-shown", true);
            PintaCore.Actions.View.ImageTabs.Active = PintaCore.Settings.GetSetting ("image-tabs-shown", true);
            PintaCore.Actions.View.PixelGrid.Active = PintaCore.Settings.GetSetting ("pixel-grid-shown", false);
			PintaCore.System.LastDialogDirectory = PintaCore.Settings.GetSetting (LastDialogDirSettingKey,
			                                                                      PintaCore.System.DefaultDialogDirectory);

			var ruler_metric = (MetricType) PintaCore.Settings.GetSetting ("ruler-metric", (int) MetricType.Pixels);

			switch (ruler_metric) {
				case MetricType.Pixels:
					PintaCore.Actions.View.Pixels.Activate ();
					break;
				case MetricType.Centimeters:
					PintaCore.Actions.View.Centimeters.Activate ();
					break;
				case MetricType.Inches:
					PintaCore.Actions.View.Inches.Activate ();
					break;
			}
		}

		private void SaveUserSettings ()
		{
			dock.SaveLayouts (PintaCore.Settings.LayoutFilePath);

			// Don't store the maximized height if the window is maximized
			if ((window_shell.GdkWindow.State & Gdk.WindowState.Maximized) == 0) {
				PintaCore.Settings.PutSetting ("window-size-width", window_shell.GdkWindow.GetSize ().Width);
				PintaCore.Settings.PutSetting ("window-size-height", window_shell.GdkWindow.GetSize ().Height);
			}

			var ruler_metric = MetricType.Pixels;

			if (PintaCore.Actions.View.Inches.Active)
				ruler_metric = MetricType.Inches;
			else if (PintaCore.Actions.View.Centimeters.Active)
				ruler_metric = MetricType.Centimeters;

			PintaCore.Settings.PutSetting ("ruler-metric", (int)ruler_metric);
			PintaCore.Settings.PutSetting ("window-maximized", (window_shell.GdkWindow.State & Gdk.WindowState.Maximized) != 0);
            PintaCore.Settings.PutSetting ("ruler-shown", PintaCore.Actions.View.Rulers.Active);
            PintaCore.Settings.PutSetting ("image-tabs-shown", PintaCore.Actions.View.ImageTabs.Active);
            PintaCore.Settings.PutSetting ("toolbar-shown", PintaCore.Actions.View.ToolBar.Active);
			PintaCore.Settings.PutSetting ("pixel-grid-shown", PintaCore.Actions.View.PixelGrid.Active);
			PintaCore.Settings.PutSetting (LastDialogDirSettingKey, PintaCore.System.LastDialogDirectory);

			PintaCore.Settings.SaveSettings ();
		}
		#endregion

		#region Action Handlers
		private void MainWindow_DeleteEvent (object o, DeleteEventArgs args)
		{
			// leave window open so user can cancel quitting
			args.RetVal = true;

			PintaCore.Actions.File.Exit.Activate ();
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

						progressDialog.Title = Catalog.GetString ("Downloading Image");
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
							Catalog.GetString ("Download failed"),
							string.Format (Catalog.GetString ("Unable to download image from {0}.\nDetails: {1}"), file, e.Message));
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
			if (PintaCore.Workspace.ImageFitsInWindow)
			{
				PintaCore.Actions.View.ActualSize.Activate ();
			}
			else
			{
				int image_x = PintaCore.Workspace.ImageSize.Width;
				int image_y = PintaCore.Workspace.ImageSize.Height;

                var canvas_window = PintaCore.Workspace.ActiveWorkspace.Canvas.Parent;

                var window_x = canvas_window.Allocation.Width;
                var window_y = canvas_window.Allocation.Height;

				double ratio;

				// The image is more constrained by width than height
				if ((double)image_x / (double)window_x >= (double)image_y / (double)window_y)
				{
					ratio = (double)(window_x - 20) / (double)image_x;
				}
				else
				{
					ratio = (double)(window_y - 20) / (double)image_y;					
				}

				PintaCore.Workspace.Scale = ratio;
				PintaCore.Actions.View.SuspendZoomUpdate ();
				(PintaCore.Actions.View.ZoomComboBox.ComboBox as ComboBoxEntry).Entry.Text = ViewActions.ToPercent (PintaCore.Workspace.Scale);
				PintaCore.Actions.View.ResumeZoomUpdate ();
			}

			PintaCore.Actions.View.ZoomToWindowActivated = true;
		}
		
		private void ActiveDocumentChanged (object sender, EventArgs e)
		{
			if (PintaCore.Workspace.HasOpenDocuments) {
				PintaCore.Actions.View.SuspendZoomUpdate ();
				(PintaCore.Actions.View.ZoomComboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = ViewActions.ToPercent (PintaCore.Workspace.Scale);
				PintaCore.Actions.View.ResumeZoomUpdate ();

                var doc = PintaCore.Workspace.ActiveDocument;
                var tab = FindTabWithCanvas ((PintaCanvas)doc.Workspace.Canvas);

                if (tab != null) {
                    // We need to suppress because ActivateTab changes both the notebook
                    // and the tab, and we handle both events, so when it fires the notebook
                    // changed event, the tab has not been changed yet.
                    suppress_active_notebook_change = true;
                    dock_container.ActivateTab (tab);
                    suppress_active_notebook_change = false;
                }

                doc.Workspace.Canvas.GrabFocus ();
			}
		}

        private DockNotebookTab FindTabWithCanvas (PintaCanvas canvas)
        {
            foreach (var tab in DockNotebookManager.AllTabs) {
                var window = (SdiWorkspaceWindow)tab.Content;
                var doc_content = (DocumentViewContent)window.ActiveViewContent;

                if (((CanvasWindow)doc_content.Control).Canvas == canvas)
                    return tab;
            }

            return null;
        }
		#endregion
	}
}
