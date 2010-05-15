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
using Gdk;
using Gtk;
using Pinta.Core;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq;

namespace Pinta
{
	public partial class MainWindow : Gtk.Window
	{
		DialogHandlers dialog_handler;

		ProgressDialog progress_dialog;

		[ImportMany]
		public IEnumerable<BaseTool> Tools { get; set; }
		[ImportMany]
		public IEnumerable<BaseEffect> Effects { get; set; }

		public MainWindow () : base(Gtk.WindowType.Toplevel)
		{
			DateTime start = DateTime.Now;
			Build ();
			
			// Initialize interface things
			this.AddAccelGroup (PintaCore.Actions.AccelGroup);
			
			progress_dialog = new ProgressDialog ();
			
			PintaCore.Initialize (tooltoolbar, drawingarea1, this, progress_dialog);
			colorpalettewidget1.Initialize ();
			
			Compose ();
			
			PintaCore.Actions.CreateMainMenu (menubar1);
			PintaCore.Actions.CreateToolBar (toolbar1);
			PintaCore.Actions.Layers.CreateLayerWindowToolBar (toolbar4);
			PintaCore.Actions.Edit.CreateHistoryWindowToolBar (toolbar2);
			
			CreateToolBox ();
			LoadEffects ();
			CreateStatusBar ();
			
			this.Icon = PintaCore.Resources.GetIcon ("Pinta.png");
			
			dialog_handler = new DialogHandlers (this);
			PintaCore.Actions.View.ZoomToWindow.Activated += new EventHandler (ZoomToWindow_Activated);
			
			// Create a blank document
			PintaCore.Actions.File.NewFile (new Size (800, 600));
			
			DeleteEvent += new DeleteEventHandler (MainWindow_DeleteEvent);
			
			WindowAction.Visible = false;
			
			if (Platform.GetOS () == Platform.OS.Mac) {
				try {
					//enable the global key handler for keyboard shortcuts
					IgeMacMenu.GlobalKeyHandlerEnabled = true;
					
					//Tell the IGE library to use your GTK menu as the Mac main menu
					IgeMacMenu.MenuBar = menubar1;
					/*
					//tell IGE which menu item should be used for the app menu's quit item
					IgeMacMenu.QuitMenuItem = yourQuitMenuItem;
					*/					
					//add a new group to the app menu, and add some items to it
					var appGroup = IgeMacMenu.AddAppMenuGroup ();
					MenuItem aboutItem = (MenuItem)PintaCore.Actions.Help.About.CreateMenuItem ();
					appGroup.AddMenuItem (aboutItem, Mono.Unix.Catalog.GetString ("About"));
					
					menubar1.Hide ();
				} catch {
					// If things don't work out, just use a normal menu.
				}
			}
			
			Console.WriteLine ("Total: {0}", DateTime.Now - start);
		}

		private void Compose ()
		{
			DateTime start = DateTime.Now;
			string ext_dir = System.IO.Path.Combine (System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ().Location), "Extensions");
			var catalog = new DirectoryCatalog (ext_dir);
			var container = new CompositionContainer (catalog);
			container.ComposeParts (this);
			Console.WriteLine ("Compose: {0}", DateTime.Now - start);
		}

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
			
			int window_x = GtkScrolledWindow.Children[0].Allocation.Width;
			int window_y = GtkScrolledWindow.Children[0].Allocation.Height;
			
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

		private void CreateToolBox ()
		{
			// Create our tools
			foreach (BaseTool tool in Tools.OrderBy (t => t.Priority))
				PintaCore.Tools.AddTool (tool);
			
			// Try to set the paint brush as the default tool, if that
			// fails, set the first thing we can find.
			if (!PintaCore.Tools.SetCurrentTool ("PaintBrush"))
				PintaCore.Tools.SetCurrentTool (Tools.First ());
			
			bool even = true;
			
			foreach (var tool in PintaCore.Tools) {
				if (even)
					toolbox1.Insert (tool.ToolItem, toolbox1.NItems);
				else
					toolbox2.Insert (tool.ToolItem, toolbox2.NItems);
				
				even = !even;
			}
		}

		private void LoadEffects ()
		{
			// Load our adjustments
			foreach (BaseEffect effect in Effects.Where (t => t.EffectOrAdjustment == EffectAdjustment.Adjustment).OrderBy (t => t.Text)) {
				// Add icon to IconFactory
				Gtk.IconFactory fact = new Gtk.IconFactory ();
				fact.Add (effect.Icon, new Gtk.IconSet (PintaCore.Resources.GetIcon (effect.Icon)));
				fact.AddDefault ();

				// Create a gtk action for each adjustment
				Gtk.Action act = new Gtk.Action (effect.GetType ().Name, effect.Text, string.Empty, effect.Icon);
				PintaCore.Actions.Adjustments.Actions.Add (act);
				act.Activated += delegate (object sender, EventArgs e) { PintaCore.LivePreview.Start (Effects.Where (t => t.GetType ().Name == (sender as Gtk.Action).Name).First ()); };
				
				// Create a menu item for each adjustment
				((Menu)((ImageMenuItem)menubar1.Children[5]).Submenu).Append (act.CreateAcceleratedMenuItem (effect.AdjustmentMenuKey, effect.AdjustmentMenuKeyModifiers));
			}

			// Load our effects
			foreach (BaseEffect effect in Effects.Where (t => t.EffectOrAdjustment == EffectAdjustment.Effect).OrderBy (t => string.Format ("{0}|{1}", t.EffectMenuCategory, t.Text))) {
				// Add icon to IconFactory
				Gtk.IconFactory fact = new Gtk.IconFactory ();
				fact.Add (effect.Icon, new Gtk.IconSet (PintaCore.Resources.GetIcon (effect.Icon)));
				fact.AddDefault ();

				// Create a gtk action and menu item for each effect
				Gtk.Action act = new Gtk.Action (effect.GetType ().Name, effect.Text, string.Empty, effect.Icon);
				PintaCore.Actions.Effects.AddEffect (effect.EffectMenuCategory, act);
				act.Activated += delegate (object sender, EventArgs e) { PintaCore.LivePreview.Start (Effects.Where (t => t.GetType ().Name == (sender as Gtk.Action).Name).First ()); };
			}
		}
		
		private void CreateStatusBar ()
		{
			Gtk.Image i = new Gtk.Image (PintaCore.Resources.GetIcon ("StatusBar.CursorXY.png"));
			i.Show ();

			statusbar1.Add (i);
			Gtk.Box.BoxChild box = (Gtk.Box.BoxChild)statusbar1[i];
			box.Position = 2;
			box.Fill = false;
			box.Expand = false;

			PintaCore.Chrome.StatusBarTextChanged += delegate (object sender, TextChangedEventArgs e) { label5.Text = e.Text; };

			PintaCore.Chrome.LastCanvasCursorPointChanged += delegate {
				Point pt = PintaCore.Chrome.LastCanvasCursorPoint;
				CursorPositionLabel.Text = string.Format ("{0}, {1}", pt.X, pt.Y);
			};
		}
	}
}
