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
using System.Globalization;
using Mono.Unix;
using Gtk;

namespace Pinta.Core
{
	public class ViewActions
	{
		public Gtk.Action ZoomIn { get; private set; }
		public Gtk.Action ZoomOut { get; private set; }
		public Gtk.Action ZoomToWindow { get; private set; }
		public Gtk.Action ZoomToSelection { get; private set; }
		public Gtk.Action ActualSize { get; private set; }
        public Gtk.ToggleAction ToolBar { get; private set; }
        public Gtk.ToggleAction ImageTabs { get; private set; }
        public Gtk.ToggleAction PixelGrid { get; private set; }
		public Gtk.ToggleAction Rulers { get; private set; }
		public Gtk.RadioAction Pixels { get; private set; }
		public Gtk.RadioAction Inches { get; private set; }
		public Gtk.RadioAction Centimeters { get; private set; }
		public Gtk.Action Fullscreen { get; private set; }

		public ToolBarComboBox ZoomComboBox { get; private set; }
		public string[] ZoomCollection { get; private set; }

		private string old_zoom_text = "";
		private bool zoom_to_window_activated = false;

		public bool ZoomToWindowActivated { 
			get { return zoom_to_window_activated; }
			set
			{
				zoom_to_window_activated = value;
				old_zoom_text = ZoomComboBox.ComboBox.ActiveText;
			}
		}
		
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
			
			ZoomIn = new Gtk.Action ("ZoomIn", Catalog.GetString ("Zoom In"), null, Stock.ZoomIn);
			ZoomOut = new Gtk.Action ("ZoomOut", Catalog.GetString ("Zoom Out"), null, Stock.ZoomOut);
			ZoomToWindow = new Gtk.Action ("ZoomToWindow", Catalog.GetString ("Best Fit"), null, Stock.ZoomFit);
			ZoomToSelection = new Gtk.Action ("ZoomToSelection", Catalog.GetString ("Zoom to Selection"), null, "Menu.View.ZoomToSelection.png");
			ActualSize = new Gtk.Action ("ActualSize", Catalog.GetString ("Normal Size"), null, Stock.Zoom100);
            ToolBar = new Gtk.ToggleAction ("Toolbar", Catalog.GetString ("Toolbar"), null, null);
            ImageTabs = new Gtk.ToggleAction ("ImageTabs", Catalog.GetString ("Image Tabs"), null, null);
            PixelGrid = new Gtk.ToggleAction ("PixelGrid", Catalog.GetString ("Pixel Grid"), null, "Menu.View.Grid.png");
			Rulers = new Gtk.ToggleAction ("Rulers", Catalog.GetString ("Rulers"), null, "Menu.View.Rulers.png");
			Pixels = new Gtk.RadioAction ("Pixels", Catalog.GetString ("Pixels"), null, null, 0);
			Inches = new Gtk.RadioAction ("Inches", Catalog.GetString ("Inches"), null, null, 1);
			Centimeters = new Gtk.RadioAction ("Centimeters", Catalog.GetString ("Centimeters"), null, null, 2);
			Fullscreen = new Gtk.Action ("Fullscreen", Catalog.GetString ("Fullscreen"), null, Stock.Fullscreen);

			ZoomCollection = new string[] {
				ToPercent (36),
				ToPercent (24),
				ToPercent (16),
				ToPercent (12),
				ToPercent (8),
				ToPercent (7),
				ToPercent (6),
				ToPercent (5),
				ToPercent (4),
				ToPercent (3),
				ToPercent (2),
				ToPercent (1.75),
				ToPercent (1.5),
				ToPercent (1.25),
				ToPercent (1),
				ToPercent (0.66),
				ToPercent (0.5),
				ToPercent (0.33),
				ToPercent (0.25),
				ToPercent (0.16),
				ToPercent (0.12),
				ToPercent (0.08),
				ToPercent (0.05),
				Catalog.GetString ("Window")
			};
			ZoomComboBox = new ToolBarComboBox (90, DefaultZoomIndex(), true, ZoomCollection);

			// Make sure these are the same group so only one will be selected at a time
			Inches.Group = Pixels.Group;
			Centimeters.Group = Pixels.Group;

            // The toolbar is shown by default.
            ToolBar.Active = true;
            ImageTabs.Active = true;
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			MenuItem show_pad = (MenuItem)menu.Children[0];
			menu.Remove (show_pad);
			
			menu.Append (ToolBar.CreateMenuItem ());
			menu.Append (PixelGrid.CreateMenuItem ());
            menu.Append (Rulers.CreateMenuItem ());
            menu.Append (ImageTabs.CreateMenuItem ());
            menu.AppendSeparator ();

			ImageMenuItem zoomin = ZoomIn.CreateAcceleratedMenuItem (Gdk.Key.plus, Gdk.ModifierType.ControlMask);
			zoomin.AddAccelerator ("activate", PintaCore.Actions.AccelGroup, new AccelKey (Gdk.Key.equal, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
			zoomin.AddAccelerator ("activate", PintaCore.Actions.AccelGroup, new AccelKey (Gdk.Key.KP_Add, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
			menu.Append (zoomin);
			
			ImageMenuItem zoomout = ZoomOut.CreateAcceleratedMenuItem (Gdk.Key.minus, Gdk.ModifierType.ControlMask);
			zoomout.AddAccelerator ("activate", PintaCore.Actions.AccelGroup, new AccelKey (Gdk.Key.underscore, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
			zoomout.AddAccelerator ("activate", PintaCore.Actions.AccelGroup, new AccelKey (Gdk.Key.KP_Subtract, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
			menu.Append (zoomout);
			
			ImageMenuItem actualsize = ActualSize.CreateAcceleratedMenuItem (Gdk.Key.Key_0, Gdk.ModifierType.ControlMask);
			actualsize.AddAccelerator ("activate", PintaCore.Actions.AccelGroup, new AccelKey (Gdk.Key.A, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, AccelFlags.Visible));
			menu.Append (actualsize);
			menu.Append (ZoomToWindow.CreateAcceleratedMenuItem (Gdk.Key.B, Gdk.ModifierType.ControlMask));
			//menu.Append (ZoomToSelection.CreateAcceleratedMenuItem (Gdk.Key.B, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.Append (Fullscreen.CreateAcceleratedMenuItem (Gdk.Key.F11, Gdk.ModifierType.None));

			menu.AppendSeparator ();

			Gtk.Action unit_action = new Gtk.Action ("RulerUnits", Mono.Unix.Catalog.GetString ("Ruler Units"), null, null);
			Menu unit_menu = (Menu)menu.AppendItem (unit_action.CreateSubMenuItem ()).Submenu;
			unit_menu.Append (Pixels.CreateMenuItem ());
			unit_menu.Append (Inches.CreateMenuItem ());
			unit_menu.Append (Centimeters.CreateMenuItem ());

			menu.AppendSeparator ();
			menu.Append (show_pad);
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

			PixelGrid.Toggled += delegate (object sender, EventArgs e) {
				PintaCore.Workspace.Invalidate ();
			};

			var isFullscreen = false;

			Fullscreen.Activated += (foo, bar) => {
				if (!isFullscreen)
					PintaCore.Chrome.MainWindow.Fullscreen ();
				else
					PintaCore.Chrome.MainWindow.Unfullscreen ();

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

			if (!TryParsePercent (text, out percent)) {
				(PintaCore.Actions.View.ZoomComboBox.ComboBox as Gtk.ComboBoxEntry).Entry.Text = temp_zoom;
				return;
			}
			
			if (percent > 3600)
				PintaCore.Actions.View.ZoomComboBox.ComboBox.Active = 0;
		}
		#endregion

		/// <summary>
		/// Converts the string representation of a percent (with or without a '%' sign) to a numeric value
		/// </summary>
		public static bool TryParsePercent (string text, out double percent)
		{
			var culture = CultureInfo.CurrentCulture;
			var format = culture.NumberFormat;

			// In order to use double.TryParse, we must:
			// - replace the decimal separator for percents with the regular separator.
			// - remove the percent sign.
			// - remove the group separators, since they might be different from the regular
			//   group separator, and the group sizes could also be different.
			text = text.Replace (format.PercentGroupSeparator, string.Empty);
			text = text.Replace (format.PercentSymbol, string.Empty);
			text = text.Replace (format.PercentDecimalSeparator, format.NumberDecimalSeparator);
			text = text.Trim();

			return double.TryParse (text,
			                        NumberStyles.AllowDecimalPoint |
			                        NumberStyles.AllowLeadingWhite |
			                        NumberStyles.AllowTrailingWhite,
			                        culture, out percent);
		}

		/// <summary>
		/// Convert the given number to a percentage string using the current locale.
		/// </summary>
		public static string ToPercent (double n)
		{
			var percent = (n * 100).ToString ("N0", CultureInfo.CurrentCulture);
			// Translators: This specifies the format of the zoom percentage choices
			// in the toolbar.
			return string.Format (Catalog.GetString ("{0}%"), percent);
		}

		public void SuspendZoomUpdate ()
		{
			suspend_zoom_change = true;
		}

		public void ResumeZoomUpdate ()
		{
			suspend_zoom_change = false;
		}

		public void UpdateCanvasScale ()
		{
			string text = PintaCore.Actions.View.ZoomComboBox.ComboBox.ActiveText;

			// stay in "Zoom to Window" mode if this function was called without the zoom level being changed by the user (e.g. if the 
			// image was rotated or cropped) and "Zoom to Window" mode is active
			if (text == Catalog.GetString ("Window") || (ZoomToWindowActivated && old_zoom_text == text))
			{
				PintaCore.Actions.View.ZoomToWindow.Activate ();
				ZoomToWindowActivated = true;
				return;
			}
			else
			{
				ZoomToWindowActivated = false;
			}

			double percent;

			if (!TryParsePercent (text, out percent))
				return;

			percent = Math.Min (percent, 3600);
			percent = percent / 100.0;

			PintaCore.Workspace.Scale = percent;
		}
		
		#region Action Handlers
		private void HandlePintaCoreActionsViewActualSizeActivated (object sender, EventArgs e)
		{
			int default_zoom = DefaultZoomIndex ();
			if (ZoomComboBox.ComboBox.Active != default_zoom)
			{
				ZoomComboBox.ComboBox.Active = default_zoom;
				UpdateCanvasScale ();
			}
		}

		private void HandlePintaCoreActionsViewZoomComboBoxComboBoxChanged (object sender, EventArgs e)
		{
			if (suspend_zoom_change)
				return;

			PintaCore.Workspace.ActiveDocument.Workspace.ZoomManually ();
		}

		private void HandlePintaCoreActionsViewZoomOutActivated (object sender, EventArgs e)
		{
			PintaCore.Workspace.ActiveDocument.Workspace.ZoomOut ();
		}

		private void HandlePintaCoreActionsViewZoomInActivated (object sender, EventArgs e)
		{
			PintaCore.Workspace.ActiveDocument.Workspace.ZoomIn ();
		}
		#endregion

		/// <summary>
		/// Returns the index in the ZoomCollection of the default zoom level
		/// </summary>
		private int DefaultZoomIndex()
		{
			return Array.IndexOf (ZoomCollection, ToPercent (1));
		}
	}
}