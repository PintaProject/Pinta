// 
// PaletteManager.cs
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
using Cairo;
using System.Collections.Generic;
using System.Linq;

namespace Pinta.Core
{
	public interface IPaletteService
	{
		Color PrimaryColor { get; set; }
		Color SecondaryColor { get; set; }
		void SetColor (bool setPrimary, Color color, bool addToRecent = true);
	}

	public class PaletteManager : IPaletteService
	{
		private Color primary;
		private Color secondary;
		private Palette? palette;

		private const int MAX_RECENT_COLORS = 10;
		private const string PALETTE_FILE = "palette.txt";
		private const string PRIMARY_COLOR_SETTINGS_KEY = "primary-color";
		private const string SECONDARY_COLOR_SETTINGS_KEY = "secondary-color";
		private const string RECENT_COLORS_SETTINGS_KEY = "recently-used-colors";

		private readonly List<Color> recently_used = new List<Color> (MAX_RECENT_COLORS);

		public Color PrimaryColor {
			get => primary;
			set => SetColor (true, value, true);
		}

		public int MaxRecentlyUsedColor => MAX_RECENT_COLORS;

		// Return an IEnumerable to prevent modification of the list
		public IEnumerable<Color> RecentlyUsedColors => recently_used;

		public Color SecondaryColor {
			get => secondary;
			set => SetColor (false, value, true);
		}
		
		public Palette CurrentPalette {
			get {
				if (palette == null) {
					palette = Palette.GetDefault ();
				}
				
				return palette;
			}
		}
		
		public PaletteManager ()
		{
			PopulateSavedPalette ();
			PopulateRecentlyUsedColors ();

			PintaCore.Settings.SaveSettingsBeforeQuit += (o, e) => {
				SaveCurrentPalette ();
				SaveRecentlyUsedColors ();
			};
		}

		public void DoKeyPress (object o, KeyPressEventArgs e)
		{
			if (e.Event.State.FilterModifierKeys() == Gdk.ModifierType.None && e.Event.Key.ToUpper() == Gdk.Key.X) {
				Color temp = PintaCore.Palette.PrimaryColor;
				PintaCore.Palette.PrimaryColor = PintaCore.Palette.SecondaryColor;
				PintaCore.Palette.SecondaryColor = temp;

				e.RetVal = true;
			}
		}

		// This allows callers to bypass affecting the recently used list
		public void SetColor (bool setPrimary, Color color, bool addToRecent = true)
		{
			if (setPrimary && !primary.Equals (color)) {
				primary = color;

				OnPrimaryColorChanged ();
			} else if (!setPrimary && !secondary.Equals (color)) {
				secondary = color;

				OnSecondaryColorChanged ();
			}

			if (addToRecent)
				AddRecentlyUsedColor (color);
		}

		// The most recently used color is at index 0.
		private void AddRecentlyUsedColor (Color color)
		{
			// The color is already in the recently used list
			if (recently_used.Contains (color)) {
				// If it's already at the back, nothing to do
				if (recently_used[0].Equals (color))
					return;

				// Move it to the front
				recently_used.Remove (color);
				recently_used.Insert (0, color);

				OnRecentColorsChanged ();
				return;
			}

			// Color needs to be added to the list
			if (recently_used.Count == MAX_RECENT_COLORS)
				recently_used.RemoveAt (MAX_RECENT_COLORS - 1);

			recently_used.Insert (0, color);

			OnRecentColorsChanged ();
		}

		private void PopulateSavedPalette ()
		{
			var palette_file = System.IO.Path.Combine (PintaCore.Settings.GetUserSettingsDirectory (), PALETTE_FILE);

			if (System.IO.File.Exists (palette_file))
				CurrentPalette.Load (palette_file);
		}

		private void PopulateRecentlyUsedColors ()
		{
			// Primary / Secondary colors
			var primary_color = PintaCore.Settings.GetSetting (PRIMARY_COLOR_SETTINGS_KEY, ColorBgra.Black.ToHexString ());
			var secondary_color = PintaCore.Settings.GetSetting (SECONDARY_COLOR_SETTINGS_KEY, ColorBgra.White.ToHexString ());

			SetColor (true, ColorBgra.TryParseHexString (primary_color, out var primary) ? primary.ToCairoColor () : new Color (0, 0, 0), false);
			SetColor (false, ColorBgra.TryParseHexString (secondary_color, out var secondary) ? secondary.ToCairoColor () : new Color (1, 0, 0), false);

			// Recently used palette
			var saved_colors = PintaCore.Settings.GetSetting (RECENT_COLORS_SETTINGS_KEY, string.Empty);

			foreach (var hex_color in saved_colors.Split (',')) {
				if (ColorBgra.TryParseHexString (hex_color, out var color))
					recently_used.Add (color.ToCairoColor ());
			}

			// Fill in with default color if not enough saved
			var more_colors = MAX_RECENT_COLORS - recently_used.Count;

			if (more_colors > 0)
				recently_used.AddRange (Enumerable.Repeat (new Color (.9, .9, .9), more_colors));
		}

		private void SaveCurrentPalette ()
		{
			var palette_file = System.IO.Path.Combine (PintaCore.Settings.GetUserSettingsDirectory (), PALETTE_FILE);
			var palette_saver = PintaCore.System.PaletteFormats.Formats.FirstOrDefault (p => p.Extensions.Contains ("txt"))?.Saver;

			if (palette_saver is not null)
				CurrentPalette.Save (palette_file, palette_saver);
		}

		private void SaveRecentlyUsedColors ()
		{
			// Primary / Secondary colors
			PintaCore.Settings.PutSetting (PRIMARY_COLOR_SETTINGS_KEY, PrimaryColor.ToColorBgra ().ToHexString ());
			PintaCore.Settings.PutSetting (SECONDARY_COLOR_SETTINGS_KEY, SecondaryColor.ToColorBgra ().ToHexString ());

			// Recently used palette
			var colors = string.Join (",", recently_used.Select (c => c.ToColorBgra ().ToHexString ()));
			PintaCore.Settings.PutSetting (RECENT_COLORS_SETTINGS_KEY, colors);
		}

		#region Protected Methods
		protected void OnPrimaryColorChanged ()
		{
			if (PrimaryColorChanged != null)
				PrimaryColorChanged.Invoke (this, EventArgs.Empty);
		}

		protected void OnRecentColorsChanged () => RecentColorsChanged?.Invoke (this, EventArgs.Empty);

		protected void OnSecondaryColorChanged ()
		{
			if (SecondaryColorChanged != null)
				SecondaryColorChanged.Invoke (this, EventArgs.Empty);
		}
		#endregion
		
		#region Events
		public event EventHandler? PrimaryColorChanged;
		public event EventHandler? SecondaryColorChanged;
		public event EventHandler? RecentColorsChanged;
		#endregion
	}
}
