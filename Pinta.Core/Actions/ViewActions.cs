// 
// ViewActions.cs
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
using Mono.Unix;


namespace Pinta.Core
{
	public class ViewActions
	{
		public Gtk.Action ZoomIn { get; private set; }
		public Gtk.Action ZoomOut { get; private set; }
		public Gtk.Action ZoomToWindow { get; private set; }
		public Gtk.Action ZoomToSelection { get; private set; }
		public Gtk.Action ActualSize { get; private set; }
		public Gtk.Action PixelGrid { get; private set; }
		public Gtk.Action Rulers { get; private set; }
		public Gtk.Action Pixels { get; private set; }
		public Gtk.Action Inches { get; private set; }
		public Gtk.Action Centimeters { get; private set; }
		public Gtk.Action Fullscreen { get; private set; }

		public ToolBarComboBox ZoomComboBox { get; private set; }
		
		public ViewActions ()
		{
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add ("Menu.View.ActualSize.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.View.ActualSize.png")));
			fact.Add ("Menu.View.Grid.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.View.Grid.png")));
			fact.Add ("Menu.View.Rulers.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.View.Rulers.png")));
			fact.Add ("Menu.View.ZoomIn.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.View.ZoomIn.png")));
			fact.Add ("Menu.View.ZoomOut.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.View.ZoomOut.png")));
			fact.Add ("Menu.View.ZoomToSelection.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.View.ZoomToSelection.png")));
			fact.Add ("Menu.View.ZoomToWindow.png", new Gtk.IconSet (PintaCore.Resources.GetIcon ("Menu.View.ZoomToWindow.png")));
			fact.AddDefault ();
			
			ZoomIn = new Gtk.Action ("ZoomIn", Mono.Unix.Catalog.GetString ("Zoom In"), null, "Menu.View.ZoomIn.png");
			ZoomOut = new Gtk.Action ("ZoomOut", Mono.Unix.Catalog.GetString ("Zoom Out"), null, "Menu.View.ZoomOut.png");
			ZoomToWindow = new Gtk.Action ("ZoomToWindow", Mono.Unix.Catalog.GetString ("Zoom to Window"), null, "Menu.View.ZoomToWindow.png");
			ZoomToSelection = new Gtk.Action ("ZoomToSelection", Mono.Unix.Catalog.GetString ("Zoom to Selection"), null, "Menu.View.ZoomToSelection.png");
			ActualSize = new Gtk.Action ("ActualSize", Mono.Unix.Catalog.GetString ("Actual Size"), null, "Menu.View.ActualSize.png");
			PixelGrid = new Gtk.Action ("PixelGrid", Mono.Unix.Catalog.GetString ("Pixel Grid"), null, "Menu.View.Grid.png");
			Rulers = new Gtk.Action ("Rulers", Mono.Unix.Catalog.GetString ("Rulers"), null, "Menu.View.Rulers.png");
			Pixels = new Gtk.Action ("Pixels", Mono.Unix.Catalog.GetString ("Pixels"), null, null);
			Inches = new Gtk.Action ("Inches", Mono.Unix.Catalog.GetString ("Inches"), null, null);
			Centimeters = new Gtk.Action ("Centimeters", Mono.Unix.Catalog.GetString ("Centimeters"), null, null);
			Fullscreen = new Gtk.Action ("Fullscreen", Catalog.GetString ("Fullscreen"), null, Gtk.Stock.Fullscreen);

			ZoomComboBox = new ToolBarComboBox (75, 11, true, "3600%", "2400%", "1600%", "1200%", "800%", "700%", "600%", "500%", "400%", "300%", "200%", "100%", "66%", "50%", "33%", "25%", "16%", "12%", "8%", "5%", "Window");

			ZoomToSelection.Sensitive = false;
			PixelGrid.Sensitive = false;
			Rulers.Sensitive = false;
			Pixels.Sensitive = false;
			Inches.Sensitive = false;
			Centimeters.Sensitive = false;
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			menu.Remove (menu.Children[1]);
			
			menu.Append (ZoomIn.CreateAcceleratedMenuItem (Gdk.Key.plus, Gdk.ModifierType.ControlMask));
			menu.Append (ZoomOut.CreateAcceleratedMenuItem (Gdk.Key.minus, Gdk.ModifierType.ControlMask));
			menu.Append (ZoomToWindow.CreateAcceleratedMenuItem (Gdk.Key.B, Gdk.ModifierType.ControlMask));
			//menu.Append (ZoomToSelection.CreateAcceleratedMenuItem (Gdk.Key.B, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (ActualSize.CreateAcceleratedMenuItem (Gdk.Key.A, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (Fullscreen.CreateAcceleratedMenuItem (Gdk.Key.F11, Gdk.ModifierType.None));

			//menu.AppendSeparator ();
			//menu.Append (PixelGrid.CreateMenuItem ());
			//menu.Append (Rulers.CreateMenuItem ());
			//menu.AppendSeparator ();
			//menu.Append (Pixels.CreateMenuItem ());
			//menu.Append (Inches.CreateMenuItem ());
			//menu.Append (Centimeters.CreateMenuItem ());
		}
		
		public void CreateToolBar (Gtk.Toolbar toolbar)
		{
			toolbar.AppendItem (new Gtk.SeparatorToolItem ());
			toolbar.AppendItem (ZoomOut.CreateToolBarItem ());
			toolbar.AppendItem (ZoomComboBox);
			toolbar.AppendItem (ZoomIn.CreateToolBarItem ());
		}
		
		public void RegisterHandlers ()
		{
			ZoomIn.Activated += HandlePintaCoreActionsViewZoomInActivated;
			ZoomOut.Activated += HandlePintaCoreActionsViewZoomOutActivated;
			ZoomComboBox.ComboBox.Changed += HandlePintaCoreActionsViewZoomComboBoxComboBoxChanged;
			(ZoomComboBox.ComboBox as Gtk.ComboBoxEntry).Entry.FocusOutEvent += new Gtk.FocusOutEventHandler (ComboBox_FocusOutEvent);
			(ZoomComboBox.ComboBox as Gtk.ComboBoxEntry).Entry.FocusInEvent += new Gtk.FocusInEventHandler (Entry_FocusInEvent);
			ActualSize.Activated += HandlePintaCoreActionsViewActualSizeActivated;

			var isFullscreen = false;

			Fullscreen.Activated += (foo, bar) => {
				if (!isFullscreen) {
					PintaCore.Chrome.MainWindow.Fullscreen ();
				}
				else {
					PintaCore.Chrome.MainWindow.Unfullscreen ();
				}

				isFullscreen = !isFullscreen;
			};
		}

		private string temp_zoom;
		private bool suspend_zoom_change;
		
		private void Entry_FocusInEvent (object o, Gtk.FocusInEventArgs args)
		{
			temp_zoom = PintaCore.Actions.View.ZoomComboBox.ComboBox.ActiveText;
		}

		private void ComboBox_FocusOutEvent (object o, Gtk.FocusOutEventArgs args)
		{
			string text = PintaCore.Actions.View.ZoomComboBox.ComboBox.ActiveText;
			double percent;

			if (!double.TryParse (text, out percent)) {
				(PintaCore.Actions.View.ZoomComboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = temp_zoom;
				return;
			}
			
			if (percent > 3600)
				PintaCore.Actions.View.ZoomComboBox.ComboBox.Active = 0;
		}
		#endregion

		public void SuspendZoomUpdate ()
		{
			suspend_zoom_change = true;
		}

		public void ResumeZoomUpdate ()
		{
			suspend_zoom_change = false;
		}
		
		#region Action Handlers
		private void HandlePintaCoreActionsViewActualSizeActivated (object sender, EventArgs e)
		{
			PintaCore.Actions.View.ZoomComboBox.ComboBox.Active = 11;
		}

		private void HandlePintaCoreActionsViewZoomComboBoxComboBoxChanged (object sender, EventArgs e)
		{
			if (suspend_zoom_change)
				return;
				
			string text = PintaCore.Actions.View.ZoomComboBox.ComboBox.ActiveText;

			if (text == "Window") {
				PintaCore.Actions.View.ZoomToWindow.Activate ();
				return;
			}

			text = text.Trim ('%');

			double percent;

			if (!double.TryParse (text, out percent))
				return;

			percent = Math.Min (percent, 3600);
			percent = percent / 100.0;

			PintaCore.Workspace.Scale = percent;
	
		}

		private void HandlePintaCoreActionsViewZoomOutActivated (object sender, EventArgs e)
		{
			PintaCore.Workspace.ZoomOut ();
		}

		private void HandlePintaCoreActionsViewZoomInActivated (object sender, EventArgs e)
		{
			PintaCore.Workspace.ZoomIn ();
		}
		#endregion
	}
}
