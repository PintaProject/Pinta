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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Gtk;
using MonoDevelop.Components.Docking;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta
{
	public class MainWindow : Window
	{
		DialogHandlers dialog_handler;

		ProgressDialog progress_dialog;
		ExtensionPoints extensions = new ExtensionPoints ();
		
		Toolbar tool_toolbar;
		PintaCanvas canvas;
		ToolBoxWidget toolbox;
		ColorPaletteWidget color;
		MenuBar main_menu;
		ScrolledWindow sw;

		public MainWindow () : base (WindowType.Toplevel)
		{
			CreateWindow ();

			// Initialize interface things
			this.AddAccelGroup (PintaCore.Actions.AccelGroup);

			progress_dialog = new ProgressDialog ();

			PintaCore.Initialize (tool_toolbar, canvas, this, progress_dialog);
			color.Initialize ();

			Compose ();

			LoadToolBox ();
			LoadEffects ();
			//CreateStatusBar ();

			this.Icon = PintaCore.Resources.GetIcon ("Pinta.png");

			dialog_handler = new DialogHandlers (this);
			PintaCore.Actions.View.ZoomToWindow.Activated += new EventHandler (ZoomToWindow_Activated);

			// Create a blank document
			PintaCore.Actions.File.NewFile (new Gdk.Size (800, 600));
			
			DeleteEvent += new DeleteEventHandler (MainWindow_DeleteEvent);
			
			if (Platform.GetOS () == Platform.OS.Mac) {
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
					appGroup.AddMenuItem (aboutItem, Mono.Unix.Catalog.GetString ("About"));

					main_menu.Hide ();
				} catch {
					// If things don't work out, just use a normal menu.
				}
			}
		}

		#region Action Handlers
		private void MainWindow_DeleteEvent (object o, DeleteEventArgs args)
		{
			// leave window open so user can cancel quitting
			args.RetVal = true;

			PintaCore.Actions.File.Exit.Activate ();
		}

		private void ZoomToWindow_Activated (object sender, EventArgs e)
		{
			// The image is small enough to fit in the window
			if (PintaCore.Workspace.ImageFitsInWindow) {
				PintaCore.Actions.View.ActualSize.Activate ();
				return;
			}

			int image_x = PintaCore.Workspace.ImageSize.Width;
			int image_y = PintaCore.Workspace.ImageSize.Height;

			int window_x = sw.Children[0].Allocation.Width;
			int window_y = sw.Children[0].Allocation.Height;

			// The image is more constrained by width than height
			if ((double)image_x / (double)window_x >= (double)image_y / (double)window_y) {
				double ratio = (double)(window_x - 20) / (double)image_x;
				PintaCore.Workspace.Scale = ratio;
				PintaCore.Actions.View.SuspendZoomUpdate ();
				(PintaCore.Actions.View.ZoomComboBox.ComboBox as ComboBoxEntry).Entry.Text = string.Format ("{0}%", (int)(PintaCore.Workspace.Scale * 100));
				PintaCore.Actions.View.ResumeZoomUpdate ();
			} else {
				double ratio2 = (double)(window_y - 20) / (double)image_y;
				PintaCore.Workspace.Scale = ratio2;
				PintaCore.Actions.View.SuspendZoomUpdate ();
				(PintaCore.Actions.View.ZoomComboBox.ComboBox as ComboBoxEntry).Entry.Text = string.Format ("{0}%", (int)(PintaCore.Workspace.Scale * 100));
				PintaCore.Actions.View.ResumeZoomUpdate ();
			}
		}
		#endregion

		#region Extension Handlers
		private void Compose ()
		{
			Gtk.StatusIcon s = new StatusIcon ();

			string ext_dir = System.IO.Path.Combine (System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ().Location), "Extensions");

			var catalog = new DirectoryCatalog (ext_dir, "*.dll");
			var container = new CompositionContainer (catalog);

			container.ComposeParts (extensions);
		}

		private void LoadEffects ()
		{
			// Load our adjustments
			foreach (BaseEffect effect in extensions.Effects.Where (t => t.EffectOrAdjustment == EffectAdjustment.Adjustment).OrderBy (t => t.Text)) {
				// Add icon to IconFactory
				Gtk.IconFactory fact = new Gtk.IconFactory ();
				fact.Add (effect.Icon, new Gtk.IconSet (PintaCore.Resources.GetIcon (effect.Icon)));
				fact.AddDefault ();

				// Create a gtk action for each adjustment
				Gtk.Action act = new Gtk.Action (effect.GetType ().Name, effect.Text, string.Empty, effect.Icon);
				PintaCore.Actions.Adjustments.Actions.Add (act);
				act.Activated += delegate (object sender, EventArgs e) { PintaCore.LivePreview.Start (extensions.Effects.Where (t => t.GetType ().Name == (sender as Gtk.Action).Name).First ()); };

				// Create a menu item for each adjustment
				((Menu)((ImageMenuItem)main_menu.Children[5]).Submenu).Append (act.CreateAcceleratedMenuItem (effect.AdjustmentMenuKey, effect.AdjustmentMenuKeyModifiers));
			}

			// Load our effects
			foreach (BaseEffect effect in extensions.Effects.Where (t => t.EffectOrAdjustment == EffectAdjustment.Effect).OrderBy (t => string.Format ("{0}|{1}", t.EffectMenuCategory, t.Text))) {
				// Add icon to IconFactory
				Gtk.IconFactory fact = new Gtk.IconFactory ();
				fact.Add (effect.Icon, new Gtk.IconSet (PintaCore.Resources.GetIcon (effect.Icon)));
				fact.AddDefault ();

				// Create a gtk action and menu item for each effect
				Gtk.Action act = new Gtk.Action (effect.GetType ().Name, effect.Text, string.Empty, effect.Icon);
				PintaCore.Actions.Effects.AddEffect (effect.EffectMenuCategory, act);
				act.Activated += delegate (object sender, EventArgs e) { PintaCore.LivePreview.Start (extensions.Effects.Where (t => t.GetType ().Name == (sender as Gtk.Action).Name).First ()); };
			}
		}
		
		private void LoadToolBox ()
		{
			// Create our tools
			foreach (BaseTool tool in extensions.Tools.OrderBy (t => t.Priority))
				PintaCore.Tools.AddTool (tool);

			// Try to set the paint brush as the default tool, if that
			// fails, set the first thing we can find.
			if (!PintaCore.Tools.SetCurrentTool ("PaintBrush"))
				PintaCore.Tools.SetCurrentTool (extensions.Tools.First ());

			foreach (var tool in PintaCore.Tools)
				toolbox.AddItem (tool.ToolItem);
		}
		#endregion

		#region GUI Construction
		private void CreateWindow ()
		{
			// Window
			Name = "Pinta.MainWindow";
			Title = Mono.Unix.Catalog.GetString ("Pinta!");
			WindowPosition = WindowPosition.Center;
			AllowShrink = true;
			DefaultWidth = 1100;
			DefaultHeight = 750;

			// shell - contains mainmenu, 2 toolbars, hbox
			VBox shell = new VBox () {
				Name = "shell"
			};

			CreateMainMenu (shell);
			CreateMainToolBar (shell);
			CreateToolToolBar (shell);

			CreatePanels (shell);

			Add (shell);

			if (Child != null)
				Child.ShowAll ();

			Show ();
		}

		private void CreateMainMenu (VBox shell)
		{
			// Main menu
			main_menu = new MenuBar () {
				Name = "main_menu"
			};

			main_menu.Append (new Gtk.Action ("file", "File").CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("edit", "Edit").CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("view", "View").CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("image", "Image").CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("layers", "Layers").CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("adjustments", "Adjustments").CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("effects", "Effects").CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("window", "Window").CreateMenuItem ());
			main_menu.Append (new Gtk.Action ("help", "Help").CreateMenuItem ());

			PintaCore.Actions.CreateMainMenu (main_menu);
			shell.PackStart (main_menu, false, false, 0);
		}
		
		private void CreateMainToolBar (VBox shell)
		{
			// Main toolbar
			Toolbar main_toolbar = new Toolbar () {
				Name = "main_toolbar",
				ShowArrow = false,
				ToolbarStyle = ToolbarStyle.Icons,
				IconSize = IconSize.SmallToolbar
			};

			PintaCore.Actions.CreateToolBar (main_toolbar);

			shell.PackStart (main_toolbar, false, false, 0);
		}
		
		private void CreateToolToolBar (VBox shell)
		{
			// Tool toolbar
			tool_toolbar = new Toolbar () {
				Name = "tool_toolbar",
				ShowArrow = false,
				ToolbarStyle = ToolbarStyle.Icons,
				IconSize = IconSize.SmallToolbar,
				HeightRequest = 28
			};

			shell.PackStart (tool_toolbar, false, false, 0);
		}
		
		private void CreatePanels (VBox shell)
		{
			HBox panel_container = new HBox () {
				Name = "panel_container"
			};

			CreateToolBoxAndColorPalettePanel (panel_container);
			CreateDockAndPads (panel_container);
			
			shell.PackStart (panel_container, true, true, 0);
		}
		
		private void CreateToolBoxAndColorPalettePanel (HBox container)
		{
			VBox toolbox_panel = new VBox () {
				Name = "toolbox_panel"
			};
			
			container.PackStart (toolbox_panel, false, false, 0);
		
			// Toolbox
			toolbox = new ToolBoxWidget () {
				Name = "toolbox"
			};

			toolbox_panel.PackStart (toolbox, false, false, 0);
						
			// Color palette
			color = new ColorPaletteWidget () {
				Name = "color"
			};

			toolbox_panel.PackStart (color, true, true, 0);
		}
		
		private void CreateDockAndPads (HBox container)
		{
			// Create canvas
			sw = new ScrolledWindow () {
				Name = "sw",
				ShadowType = ShadowType.EtchedOut
			};
			
			Viewport vp = new Viewport () {
				ShadowType = ShadowType.None
			};
			
			canvas = new PintaCanvas () {
				Name = "canvas",
				CanDefault = true,
				CanFocus = true,
				Events = (Gdk.EventMask)16134
			};
			
			sw.Add (vp);
			vp.Add (canvas);
			
			canvas.Show ();
			vp.Show ();
			
			// Dock widget
			DockFrame dock = new DockFrame ();
			dock.CompactGuiLevel = 5;
		
			// Canvas pad
			DockItem documentDockItem = dock.AddItem ("Canvas");
			documentDockItem.Behavior = DockItemBehavior.Locked;
			documentDockItem.Expand = true;
			
			documentDockItem.DrawFrame = false;
			documentDockItem.Label = "Documents";
			documentDockItem.Content = sw;

			// Layer pad
			LayersListWidget layers = new LayersListWidget ();
			DockItem layers_item = dock.AddItem ("Layers");
			DockItemToolbar layers_tb = layers_item.GetToolbar (PositionType.Bottom);
			
			layers_item.Label = "Layers";
			layers_item.Content = layers;
			layers_item.Icon = PintaCore.Resources.GetIcon ("Menu.Layers.MergeLayerDown.png");

			layers_tb.Add (PintaCore.Actions.Layers.AddNewLayer.CreateDockToolBarItem ());
			layers_tb.Add (PintaCore.Actions.Layers.DeleteLayer.CreateDockToolBarItem ());
			layers_tb.Add (PintaCore.Actions.Layers.DuplicateLayer.CreateDockToolBarItem ());
			layers_tb.Add (PintaCore.Actions.Layers.MergeLayerDown.CreateDockToolBarItem ());
			layers_tb.Add (PintaCore.Actions.Layers.MoveLayerUp.CreateDockToolBarItem ());
			layers_tb.Add (PintaCore.Actions.Layers.MoveLayerDown.CreateDockToolBarItem ());

			// History pad
			HistoryTreeView history = new HistoryTreeView ();
			DockItem history_item = dock.AddItem ("History");
			DockItemToolbar history_tb = history_item.GetToolbar (PositionType.Bottom);
			
			history_item.Label = "History";
			history_item.DefaultLocation = "Layers/Bottom";
			history_item.Content = history;
			history_item.Icon = PintaCore.Resources.GetIcon ("Menu.Layers.DuplicateLayer.png");

			history_tb.Add (PintaCore.Actions.Edit.Undo.CreateDockToolBarItem ());
			history_tb.Add (PintaCore.Actions.Edit.Redo.CreateDockToolBarItem ());

			container.PackStart (dock, true, true, 0);
			
			dock.CreateLayout ("Default", false);
			dock.CurrentLayout = "Default";
		}
		
		//private void CreateStatusBar ()
		//{
		//        Gtk.Image i = new Gtk.Image (PintaCore.Resources.GetIcon ("StatusBar.CursorXY.png"));
		//        i.Show ();

		//        statusbar1.Add (i);
		//        Gtk.Box.BoxChild box = (Gtk.Box.BoxChild)statusbar1[i];
		//        box.Position = 2;
		//        box.Fill = false;
		//        box.Expand = false;

		//        PintaCore.Chrome.StatusBarTextChanged += delegate (object sender, TextChangedEventArgs e) { label5.Text = e.Text; };

		//        PintaCore.Chrome.LastCanvasCursorPointChanged += delegate {
		//                Point pt = PintaCore.Chrome.LastCanvasCursorPoint;
		//                CursorPositionLabel.Text = string.Format ("{0}, {1}", pt.X, pt.Y);
		//        };
		//}
		#endregion
	}
}
