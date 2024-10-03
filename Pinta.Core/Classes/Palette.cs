// 
// Palette.cs
//  
// Author:
//       Maia Kozheva <sikon@ubuntu.com>
// 
// Copyright (c) 2010 Maia Kozheva <sikon@ubuntu.com>
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
using System.Text;
using Cairo;
using Gtk;

namespace Pinta.Core;

public sealed class Palette
{
	public event EventHandler? PaletteChanged;

	private List<Color> colors = new ();

	private void OnPaletteChanged ()
	{
		PaletteChanged?.Invoke (this, EventArgs.Empty);
	}

	public int Count => colors.Count;

	public Color this[int index] {
		get => colors[index];
		set {
			colors[index] = value;
			OnPaletteChanged ();
		}
	}

	public void Resize (int newSize)
	{
		int difference = newSize - Count;

		if (difference > 0) {
			for (int i = 0; i < difference; i++)
				colors.Add (new Color (1, 1, 1));
		} else {
			colors.RemoveRange (newSize, -difference);
		}

		colors.TrimExcess ();
		OnPaletteChanged ();
	}

	public static Palette GetDefault ()
	{
		Palette p = new Palette ();
		p.LoadDefault ();
		return p;
	}

	public void LoadDefault ()
	{
		colors.Clear ();

		colors.AddRange (EnumerateDefaultColors ());

		colors.TrimExcess ();
		OnPaletteChanged ();
	}

	private static IEnumerable<Color> EnumerateDefaultColors ()
	{
		yield return new (255 / 255f, 255 / 255f, 255 / 255f);
		yield return new (0 / 255f, 0 / 255f, 0 / 255f);

		yield return new (160 / 255f, 160 / 255f, 160 / 255f);
		yield return new (128 / 255f, 128 / 255f, 128 / 255f);

		yield return new (64 / 255f, 64 / 255f, 64 / 255f);
		yield return new (48 / 255f, 48 / 255f, 48 / 255f);

		yield return new (255 / 255f, 0 / 255f, 0 / 255f);
		yield return new (255 / 255f, 127 / 255f, 127 / 255f);

		yield return new (255 / 255f, 106 / 255f, 0 / 255f);
		yield return new (255 / 255f, 178 / 255f, 127 / 255f);

		yield return new (255 / 255f, 216 / 255f, 0 / 255f);
		yield return new (255 / 255f, 233 / 255f, 127 / 255f);

		yield return new (182 / 255f, 255 / 255f, 0 / 255f);
		yield return new (218 / 255f, 255 / 255f, 127 / 255f);

		yield return new (76 / 255f, 255 / 255f, 0 / 255f);
		yield return new (165 / 255f, 255 / 255f, 127 / 255f);

		yield return new (0 / 255f, 255 / 255f, 33 / 255f);
		yield return new (127 / 255f, 255 / 255f, 142 / 255f);

		yield return new (0 / 255f, 255 / 255f, 144 / 255f);
		yield return new (127 / 255f, 255 / 255f, 197 / 255f);

		yield return new (0 / 255f, 255 / 255f, 255 / 255f);
		yield return new (127 / 255f, 255 / 255f, 255 / 255f);

		yield return new (0 / 255f, 148 / 255f, 255 / 255f);
		yield return new (127 / 255f, 201 / 255f, 255 / 255f);

		yield return new (0 / 255f, 38 / 255f, 255 / 255f);
		yield return new (127 / 255f, 146 / 255f, 255 / 255f);

		yield return new (72 / 255f, 0 / 255f, 255 / 255f);
		yield return new (161 / 255f, 127 / 255f, 255 / 255f);

		yield return new (178 / 255f, 0 / 255f, 255 / 255f);
		yield return new (214 / 255f, 127 / 255f, 255 / 255f);

		yield return new (255 / 255f, 0 / 255f, 220 / 255f);
		yield return new (255 / 255f, 127 / 255f, 237 / 255f);

		yield return new (255 / 255f, 0 / 255f, 110 / 255f);
		yield return new (255 / 255f, 127 / 255f, 182 / 255f);
	}

	public void Load (PaletteFormatManager paletteFormats, Gio.File file)
	{
		(var loadedColors, var errors) = LoadColors (paletteFormats, file);

		if (loadedColors is not null) {
			colors = loadedColors;
			colors.TrimExcess ();
			OnPaletteChanged ();
		} else {
			var parent = PintaCore.Chrome.MainWindow;
			ShowUnsupportedFormatDialog (parent, file.GetParseName (), Translations.GetString ("Unsupported palette format"), errors);
		}
	}

	static (List<Color>? loadedColors, string errors) LoadColors (PaletteFormatManager paletteFormats, Gio.File file)
	{
		var loader = paletteFormats.GetFormatByFilename (file.GetDisplayName ())?.Loader;

		if (loader != null)
			return (loader.Load (file), string.Empty);

		StringBuilder errors = new ();

		// Not a recognized extension, so attempt all formats
		foreach (var format in paletteFormats.Formats.Where (f => !f.IsWriteOnly ())) {
			try {
				var loaded_colors = format.Loader.Load (file);
				if (loaded_colors != null)
					return (loaded_colors, errors.ToString ());
			} catch (Exception e) {
				// Record errors in case none of the formats work.
				errors.AppendLine ($"Failed to load palette as {format.Filter.Name}:");
				errors.Append (e.ToString ());
				errors.AppendLine ();
			}
		}

		return (null, errors.ToString ());
	}

	public void Save (Gio.File file, IPaletteSaver saver)
	{
		saver.Save (colors, file);
	}

	private static void ShowUnsupportedFormatDialog (Window parent, string filename, string message, string errors)
	{
		var details = new StringBuilder ();
		details.AppendLine (Translations.GetString ("Could not open file: {0}", filename));
		details.AppendLine (Translations.GetString ("Pinta supports the following palette formats:"));

		var extensions =
			from format in PintaCore.PaletteFormats.Formats
			where format.Loader != null
			from extension in format.Extensions
			where char.IsLower (extension.FirstOrDefault ())
			orderby extension
			select extension;

		details.AppendJoin (", ", extensions);
		details.AppendLine ();
		details.AppendLine (errors);

		PintaCore.Chrome.ShowMessageDialog (parent, message, details.ToString ());
	}
}
