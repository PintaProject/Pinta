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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Pinta.Core;

public static class Translations
{
	private const string PintaTextDomain = "pinta";
	private static string locale_dir = "";

	public static void Init (string localeDir)
	{
		locale_dir = localeDir;

		// Note we need to initialize the GLib module since this is called very early in startup,
		// before GTK is initialized.
		GLib.Module.Initialize ();

		// Follow the dotnet UI culture to choose which language is used by default, since this
		// correctly picks up system language settings on macOS, for example.
		// Pinta (along with GTK / libadwaita) use the native version of gettext for translations
		// so here we set the LANGUAGE environment variable to make these consistent.
		if (GLib.Functions.Getenv ("LANGUAGE") is null) {
			CultureInfo cultureInfo = CultureInfo.CurrentUICulture;
			string lang = cultureInfo.Name.Replace ('-', '_'); // convert names like en-CA to en_CA

			GLib.Functions.Setenv ("LANGUAGE", lang, overwrite: true);
		}

		// Initialize gettext for Pinta's translations.
		IntlExtensions.BindTextDomain (PintaTextDomain, localeDir);
		IntlExtensions.BindTextDomainCodeset (PintaTextDomain, "UTF-8");
		IntlExtensions.TextDomain (PintaTextDomain);
	}

	public static string GetString (string text)
	{
		// Just use glib'c gettext wrapper for convenience instead of adding our own binding.
		return GLib.Functions.Dgettext (PintaTextDomain, text);
	}

	public static string GetString (string text, params object[] args)
	{
		return string.Format (GetString (text), args);
	}

	/// <summary>
	/// Returns a list of the language codes (e.g. 'fr', 'en_CA') that Pinta has translations for.
	/// The list is not in any particular order.
	/// </summary>
	public static IEnumerable<string> GetAvailableLanguages ()
	{
		string moFile = $"{PintaTextDomain}.mo";

		// List folders in the locale dir if they contain Pinta's translation file.
		// On Linux, the folder might e.g. be /usr/share/locale which contains additional languages.
		DirectoryInfo dirInfo = new (locale_dir);
		return dirInfo.EnumerateDirectories ()
			.Where (dir => File.Exists (Path.Combine (dir.FullName, "LC_MESSAGES", moFile)))
			.Select (dir => dir.Name);
	}

	/// <summary>
	/// Returns a suitable display name for the specified language code.
	/// </summary>
	public static string GetLanguageDisplayName (string languageCode)
	{
		// Map back from e.g. 'en_CA' to 'en-CA' for the dotnet APIs.
		// This ensures we get 'English (Canada)' rather than 'English (Sort Order=ca)'.
		string dotnetLanguageCode = languageCode.Replace ('_', '-');

		CultureInfo cultureInfo = CultureInfo.GetCultureInfo (dotnetLanguageCode);
		return cultureInfo.TextInfo.ToTitleCase (cultureInfo.NativeName);
	}
}
