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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cairo;

namespace Pinta.Core;

public interface IPaletteService
{
	Color PrimaryColor { get; set; }
	Color SecondaryColor { get; set; }
	void SetColor (bool setPrimary, Color color, bool addToRecent = true);

	public event EventHandler? PrimaryColorChanged;
	public event EventHandler? SecondaryColorChanged;
}

public sealed class PaletteManager : IPaletteService
{
	private Color primary;
	private Color secondary;

	private const int MAX_RECENT_COLORS = 10;
	private const string PALETTE_FILE = "palette.txt";
	private const string PRIMARY_COLOR_SETTINGS_KEY = "primary-color";
	private const string SECONDARY_COLOR_SETTINGS_KEY = "secondary-color";
	private const string RECENT_COLORS_SETTINGS_KEY = "recently-used-colors";

	private readonly List<Color> recently_used;

	public Color PrimaryColor {
		get => primary;
		set => SetColor (true, value, true);
	}

	public int MaxRecentlyUsedColor => MAX_RECENT_COLORS;

	public ReadOnlyCollection<Color> RecentlyUsedColors { get; }

	public Color SecondaryColor {
		get => secondary;
		set => SetColor (false, value, true);
	}

	public Palette CurrentPalette { get; }

	private readonly ChromeManager chrome;
	private readonly SettingsManager settings;
	private readonly PaletteFormatManager palette_formats;
	public PaletteManager (
		ChromeManager chrome,
		SettingsManager settings,
		PaletteFormatManager paletteFormats)
	{
		List<Color> recentlyUsed = new (MAX_RECENT_COLORS);

		recently_used = recentlyUsed;
		RecentlyUsedColors = new ReadOnlyCollection<Color> (recentlyUsed);

		this.chrome = chrome;
		this.settings = settings;
		palette_formats = paletteFormats;

		CurrentPalette = Palette.GetDefault ();

		// This is an async method.
		// Furthermore, it depends on `chrome` and `palette_formats` having a value
		// Can this call be moved out of this constructor?
		PopulateSavedPalette (paletteFormats);

		PopulateRecentlyUsedColors ();

		settings.SaveSettingsBeforeQuit += (_, _) => {
			SaveCurrentPalette ();
			SaveRecentlyUsedColors ();
		};
	}

	public bool DoKeyPress (Gtk.EventControllerKey.KeyPressedSignalArgs args)
	{
		if (args.State.HasModifierKey () || args.GetKey ().ToUpper ().Value != Gdk.Constants.KEY_X)
			return false;

		Color temp = PrimaryColor;
		PrimaryColor = SecondaryColor;
		SecondaryColor = temp;

		return true;
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

	private async void PopulateSavedPalette (PaletteFormatManager paletteFormats)
	{
		try {
			string paletteFile = System.IO.Path.Combine (settings.GetUserSettingsDirectory (), PALETTE_FILE);
			if (!System.IO.File.Exists (paletteFile)) return;
			CurrentPalette.Load (paletteFormats, Gio.FileHelper.NewForPath (paletteFile));
		} catch (PaletteLoadException ex) {

			var parent = chrome.MainWindow;

			await chrome.ShowUnsupportedFormatDialog (
				parent,
				palette_formats.Formats,
				ex.FileName,
				Translations.GetString ("Unsupported palette format"),
				(ex.InnerException?.Message) ?? ex.Message);
		}
	}

	private void PopulateRecentlyUsedColors ()
	{
		// Primary / Secondary colors
		string primaryColor = settings.GetSetting (PRIMARY_COLOR_SETTINGS_KEY, ColorBgra.Black.ToHexString ());
		string secondaryColor = settings.GetSetting (SECONDARY_COLOR_SETTINGS_KEY, ColorBgra.White.ToHexString ());

		SetColor (
			true,
			ColorBgra.TryParseHexString (primaryColor, out ColorBgra primary) ? primary.ToCairoColor () : new Color (0, 0, 0),
			false);

		SetColor (
			false,
			ColorBgra.TryParseHexString (secondaryColor, out ColorBgra secondary) ? secondary.ToCairoColor () : new Color (1, 0, 0),
			false);

		// Recently used palette
		string saved_colors = settings.GetSetting (RECENT_COLORS_SETTINGS_KEY, string.Empty);

		foreach (string hex_color in saved_colors.Split (',')) {
			if (ColorBgra.TryParseHexString (hex_color, out ColorBgra color))
				recently_used.Add (color.ToCairoColor ());
		}

		// Fill in with default color if not enough saved
		int more_colors = MAX_RECENT_COLORS - recently_used.Count;

		if (more_colors > 0)
			recently_used.AddRange (Enumerable.Repeat (new Color (.9, .9, .9), more_colors));
	}

	private void SaveCurrentPalette ()
	{
		string palette_file = System.IO.Path.Combine (settings.GetUserSettingsDirectory (), PALETTE_FILE);
		var palette_saver = palette_formats.Formats.FirstOrDefault (p => p.Extensions.Contains ("txt"))?.Saver;
		if (palette_saver is not null)
			CurrentPalette.Save (Gio.FileHelper.NewForPath (palette_file), palette_saver);
	}

	private void SaveRecentlyUsedColors ()
	{
		// Primary / Secondary colors
		settings.PutSetting (PRIMARY_COLOR_SETTINGS_KEY, PrimaryColor.ToColorBgra ().ToHexString ());
		settings.PutSetting (SECONDARY_COLOR_SETTINGS_KEY, SecondaryColor.ToColorBgra ().ToHexString ());

		// Recently used palette
		string colors = string.Join (",", recently_used.Select (c => c.ToColorBgra ().ToHexString ()));
		settings.PutSetting (RECENT_COLORS_SETTINGS_KEY, colors);
	}

	private void OnPrimaryColorChanged ()
	{
		PrimaryColorChanged?.Invoke (this, EventArgs.Empty);
	}

	private void OnRecentColorsChanged () => RecentColorsChanged?.Invoke (this, EventArgs.Empty);

	private void OnSecondaryColorChanged ()
	{
		SecondaryColorChanged?.Invoke (this, EventArgs.Empty);
	}

	public event EventHandler? PrimaryColorChanged;
	public event EventHandler? SecondaryColorChanged;
	public event EventHandler? RecentColorsChanged;
}
