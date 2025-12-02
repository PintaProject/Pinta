//
// TranslationManager.cs
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

using System.Globalization;
using NGettext;

namespace Pinta.Core;

public static class Translations
{
	private static ICatalog? catalog;

	public static void Init (string domain, string locale_dir)
	{
		CultureInfo cultureInfo = CultureInfo.CurrentUICulture;
		catalog = new Catalog (domain, locale_dir, cultureInfo);

		// The dotnet UI culture controls which translations are loaded for Pinta above.
		// The GTK / libadwaita libraries use the native version of gettext for their translations
		// (e.g. the About dialog), so here we set the LANG environment variable to be consistent.
		// Note we need to initialize the GLib module since this is called very early in startup,
		// before GTK is initialized.
		GLib.Module.Initialize ();
		string lang = cultureInfo.Name.Replace ('-', '_'); // convert names like en-CA to en_CA
		GLib.Functions.Setenv ("LANG", lang, overwrite: true);
	}

	public static string GetString (string text)
	{
		return catalog?.GetString (text) ?? text;
	}

	public static string GetString (string text, params object[] args)
	{
		return catalog?.GetString (text, args) ?? text;
	}
}
