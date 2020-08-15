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
using Gtk;

namespace Pinta.Core
{
	public class ViewActions
	{
		public Command ZoomIn { get; private set; }
		public Command ZoomOut { get; private set; }
		public Command ZoomToWindow { get; private set; }
		public Command ZoomToSelection { get; private set; }
		public Command ActualSize { get; private set; }
        public ToggleCommand ToolBar { get; private set; }
        public ToggleCommand ImageTabs { get; private set; }
        public ToggleCommand PixelGrid { get; private set; }
		public ToggleCommand Rulers { get; private set; }
		public Gtk.RadioAction Pixels { get; private set; }
		public Gtk.RadioAction Inches { get; private set; }
		public Gtk.RadioAction Centimeters { get; private set; }
		public Command Fullscreen { get; private set; }

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
			
			ZoomIn = new Command ("ZoomIn", Translations.GetString ("Zoom In"), null, Stock.ZoomIn);
			ZoomOut = new Command ("ZoomOut", Translations.GetString ("Zoom Out"), null, Stock.ZoomOut);
			ZoomToWindow = new Command ("ZoomToWindow", Translations.GetString ("Best Fit"), null, Stock.ZoomFit);
			ZoomToSelection = new Command ("ZoomToSelection", Translations.GetString ("Zoom to Selection"), null, "Menu.View.ZoomToSelection.png");
			ActualSize = new Command ("ActualSize", Translations.GetString ("Normal Size"), null, Stock.Zoom100);
            ToolBar = new ToggleCommand ("Toolbar", Translations.GetString ("Toolbar"), null, null);
            ImageTabs = new ToggleCommand ("ImageTabs", Translations.GetString ("Image Tabs"), null, null);
            PixelGrid = new ToggleCommand ("PixelGrid", Translations.GetString ("Pixel Grid"), null, "Menu.View.Grid.png");
			Rulers = new ToggleCommand ("Rulers", Translations.GetString ("Rulers"), null, "Menu.View.Rulers.png");
			Pixels = new Gtk.RadioAction ("Pixels", Translations.GetString ("Pixels"), null, null, 0);
			Inches = new Gtk.RadioAction ("Inches", Translations.GetString ("Inches"), null, null, 1);
			Centimeters = new Gtk.RadioAction ("Centimeters", Translations.GetString ("Centimeters"), null, null, 2);
			Fullscreen = new Command ("Fullscreen", Translations.GetString ("Fullscreen"), null, Stock.Fullscreen);

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
				Translations.GetString ("Window")
			};
			ZoomComboBox = new ToolBarComboBox (90, DefaultZoomIndex(), true, ZoomCollection);

			// Make sure these are the same group so only one will be selected at a time
			Inches.Group = Pixels.Group;
			Centimeters.Group = Pixels.Group;

            // The toolbar is shown by default.
            ToolBar.Value = true;
            ImageTabs.Value = true;
		}

		#region Initialization
		public void RegisterActions(Gtk.Application app, GLib.Menu menu)
		{
			app.AddAction(ToolBar);
			menu.AppendItem(ToolBar.CreateMenuItem());

			app.AddAction(PixelGrid);
			menu.AppendItem(PixelGrid.CreateMenuItem());

			app.AddAction(Rulers);
			menu.AppendItem(Rulers.CreateMenuItem());

			app.AddAction(ImageTabs);
			menu.AppendItem(ImageTabs.CreateMenuItem());

			var zoom_section = new GLib.Menu();
			menu.AppendSection(null, zoom_section);

			app.AddAccelAction(ZoomIn, new[] { "<Primary>plus", "<Primary>equal", "<Primary>KP_Add" });
			zoom_section.AppendItem(ZoomIn.CreateMenuItem());

			app.AddAccelAction(ZoomOut, new[] { "<Primary>minus", "<Primary>underscore", "<Primary>KP_Subtract" });
			zoom_section.AppendItem(ZoomOut.CreateMenuItem());

			app.AddAccelAction(ActualSize, new[] { "<Primary>0", "<Primary><Shift>A" });
			zoom_section.AppendItem(ActualSize.CreateMenuItem());

			app.AddAccelAction(ZoomToWindow, "<Primary>B");
			zoom_section.AppendItem(ZoomToWindow.CreateMenuItem());

			app.AddAccelAction(Fullscreen, "F11");
			zoom_section.AppendItem(Fullscreen.CreateMenuItem());

			// TODO-GTK3 (rulers)
#if false
			Gtk.Action unit_action = new Gtk.Action ("RulerUnits", Translations.GetString ("Ruler Units"), null, null);
			Menu unit_menu = (Menu)menu.AppendItem (unit_action.CreateSubMenuItem ()).Submenu;
			unit_menu.Append (Pixels.CreateMenuItem ());
			unit_menu.Append (Inches.CreateMenuItem ());
			unit_menu.Append (Centimeters.CreateMenuItem ());
#endif
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
			ZoomComboBox.ComboBox.Entry.FocusOutEvent += new Gtk.FocusOutEventHandler (ComboBox_FocusOutEvent);
			ZoomComboBox.ComboBox.Entry.FocusInEvent += new Gtk.FocusInEventHandler (Entry_FocusInEvent);
			ActualSize.Activated += HandlePintaCoreActionsViewActualSizeActivated;

			PixelGrid.Toggled += (_) => {
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
				PintaCore.Actions.View.ZoomComboBox.ComboBox.Entry.Text = temp_zoom;
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
			return Translations.GetString("{0}%", percent);
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
			if (text == Translations.GetString ("Window") || (ZoomToWindowActivated && old_zoom_text == text))
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
