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
using Gtk;
using Mono.Addins;
using Mono.Unix;
using MonoDevelop.Components.Docking;
using Pinta.Core;

namespace Pinta
{
	public class MainWindow
	{
		WindowShell window_shell;
		ScrolledWindow sw;
		DockFrame dock;
		Menu show_pad;
		
		ActionHandlers dialog_handlers;
		
		public MainWindow ()
		{
			// Build our window
			CreateWindow ();

			// Initialize interface things
			window_shell.AddAccelGroup (PintaCore.Actions.AccelGroup);
			dialog_handlers = new ActionHandlers ();

			PintaCore.Chrome.InitializeProgessDialog (new ProgressDialog ());
			PintaCore.Initialize ();

			// Initialize extensions
			AddinManager.Initialize ();
			AddinManager.Registry.Update ();

			foreach (var extension in PintaCore.System.GetExtensions<IExtension> ())
				extension.Initialize ();

			// Try to set the default tool to the PaintBrush
			PintaCore.Tools.SetCurrentTool (Catalog.GetString ("Paintbrush"));

			// Load the user's previous settings
			LoadUserSettings ();

			// Give the canvas focus
			PintaCore.Chrome.Canvas.GrabFocus ();

			// We support drag and drop for URIs
			window_shell.AddDragDropSupport (new Gtk.TargetEntry ("text/uri-list", 0, 100));
			
			// Handle a few main window specific actions
			PintaCore.Actions.File.BeforeQuit += delegate { SaveUserSettings (); };

			window_shell.DeleteEvent += MainWindow_DeleteEvent;
			window_shell.DragDataReceived += MainWindow_DragDataReceived;

			// TODO: These need to be [re]moved when we redo zoom support
			PintaCore.Actions.View.ZoomToWindow.Activated += new EventHandler (ZoomToWindow_Activated);
			PintaCore.Actions.View.ZoomToSelection.Activated += new EventHandler (ZoomToSelection_Activated);
			PintaCore.Workspace.ActiveDocumentChanged += ActiveDocumentChanged;
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
					/*
					//tell IGE which menu item should be used for the app menu's quit item
					IgeMacMenu.QuitMenuItem = yourQuitMenuItem;
					*/
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

			// Toolbox pad
			var toolboxpad = new ToolBoxPad ();
			toolboxpad.Initialize (dock, show_pad);
		
			// Palette pad
			var palettepad = new ColorPalettePad ();
			palettepad.Initialize (dock, show_pad);

			// Canvas pad
			var canvas_pad = new CanvasPad ();
			canvas_pad.Initialize (dock, show_pad);

			sw = canvas_pad.ScrolledWindow;

			// Layer pad
			var layers_pad = new LayersPad ();
			layers_pad.Initialize (dock, show_pad);

			// History pad
			var history_pad = new HistoryPad ();
			history_pad.Initialize (dock, show_pad);

			// Open Images pad
			var open_images_pad = new OpenImagesPad ();
			open_images_pad.Initialize (dock, show_pad);

			container.PackStart (dock, true, true, 0);
			
			string layout_file = System.IO.Path.Combine (PintaCore.Settings.GetUserSettingsDirectory (), "layouts.xml");
			
			if (System.IO.File.Exists (layout_file))
				dock.LoadLayouts (layout_file);
			
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
			PintaCore.Actions.View.PixelGrid.Active = PintaCore.Settings.GetSetting ("pixel-grid-shown", false);
			PintaCore.System.LastDialogDirectory = PintaCore.Settings.GetSetting (LastDialogDirSettingKey,
			                                                                      PintaCore.System.DefaultDialogDirectory);

			var ruler_metric = (MetricType) PintaCore.Settings.GetSetting ("ruler-metric", (int) MetricType.Pixels);

			switch (ruler_metric) {
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
			dock.SaveLayouts (System.IO.Path.Combine (PintaCore.Settings.GetUserSettingsDirectory (), "layouts.xml"));

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
			// Only handle URIs
			if (args.Info != 100)
				return;

			string fullData = System.Text.Encoding.UTF8.GetString (args.SelectionData.Data);

			foreach (string individualFile in fullData.Split ('\n')) {
				string file = individualFile.Trim ();

				if (file.StartsWith ("file://"))
					PintaCore.Workspace.OpenFile (new Uri (file).LocalPath);
			}
		}

		private void ZoomToSelection_Activated (object sender, EventArgs e)
		{
			PintaCore.Workspace.ActiveWorkspace.ZoomToRectangle (PintaCore.Workspace.ActiveDocument.SelectionPath.GetBounds ().ToCairoRectangle ());
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

				int window_x = sw.Children[0].Allocation.Width;
				int window_y = sw.Children[0].Allocation.Height;

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
				(PintaCore.Actions.View.ZoomComboBox.ComboBox as ComboBoxEntry).Entry.Text = string.Format ("{0}%", (int)(PintaCore.Workspace.Scale * 100));
				PintaCore.Actions.View.ResumeZoomUpdate ();
			}

			PintaCore.Actions.View.ZoomToWindowActivated = true;
		}
		
		private void ActiveDocumentChanged (object sender, EventArgs e)
		{
			if (PintaCore.Workspace.HasOpenDocuments) {
				int zoom = (int)(PintaCore.Workspace.ActiveWorkspace.Scale * 100);
			
				PintaCore.Actions.View.SuspendZoomUpdate ();
				(PintaCore.Actions.View.ZoomComboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = string.Format ("{0}%", zoom);
				PintaCore.Actions.View.ResumeZoomUpdate ();

				PintaCore.Workspace.OnCanvasSizeChanged ();
			}
			
			PintaCore.Workspace.Invalidate ();
		}
		#endregion
	}
}
