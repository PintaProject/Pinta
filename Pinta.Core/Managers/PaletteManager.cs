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
	public class PaletteManager
	{
		private Color primary;
		private Color secondary;
		private Palette palette;

		private const int MAX_RECENT_COLORS = 10;
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
			SetColor (true, new Color (0, 0, 0), false);
			SetColor (false, new Color (1, 1, 1), false);
		}

		public void Initialize ()
		{
			PopulateRecentlyUsedColors ();
		}

		public void DoKeyRelease (object o, KeyReleaseEventArgs e)
		{
			if (e.Event.Key.ToString().ToUpper() == "X") {
				Color temp = PintaCore.Palette.PrimaryColor;
				PintaCore.Palette.PrimaryColor = PintaCore.Palette.SecondaryColor;
				PintaCore.Palette.SecondaryColor = temp;
			}
		}

		// This allows callers to bypass affecting the recently used list
		public void SetColor (bool setPrimary, Color color, bool addToRecent = true)
		{
			if (setPrimary && !primary.Equals (color)) {
				primary = color;

				if (addToRecent)
					AddRecentlyUsedColor (color);

				OnPrimaryColorChanged ();
			} else if (!setPrimary && !secondary.Equals (color)) {
				secondary = color;

				if (addToRecent)
					AddRecentlyUsedColor (color);

				OnSecondaryColorChanged ();
			}
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

		private void PopulateRecentlyUsedColors ()
		{
			var saved_colors = PintaCore.Settings.GetSetting ("recently-used-colors", string.Empty);

			foreach (var hex_color in saved_colors.Split (',')) {
				try {
					var color = ColorBgra.ParseHexString (hex_color);
					recently_used.Add (color.ToCairoColor ());
				} catch (Exception) {
					// Ignore
				}
			}

			// Fill in with default color if not enough saved
			var more_colors = MAX_RECENT_COLORS - recently_used.Count;

			if (more_colors > 0)
				recently_used.AddRange (Enumerable.Repeat (new Color (.9, .9, .9), more_colors));
		}

		public void SaveRecentlyUsedColors ()
		{
			var colors = string.Join (",", recently_used.Select (c => c.ToColorBgra ().ToHexString ()));
			PintaCore.Settings.PutSetting ("recently-used-colors", colors);
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
		public event EventHandler PrimaryColorChanged;
		public event EventHandler SecondaryColorChanged;
		public event EventHandler RecentColorsChanged;
		#endregion
	}
}
