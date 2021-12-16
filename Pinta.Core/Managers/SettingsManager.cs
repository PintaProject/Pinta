// 
// SettingsManager.cs
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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Pinta.Core
{
	public interface ISettingsService
	{
		/// <summary>
		/// Retrieves stored setting with the specified key. The specified default value is
		/// returned if the setting cannot be found or contains an invalid value.
		/// </summary>
		T GetSetting<T> (string key, T defaultValue);

		/// <summary>
		/// Returns the user settings directory.
		/// </summary>
		string GetUserSettingsDirectory ();

		/// <summary>
		/// Stores a setting with specified key and value for future application launches.
		/// </summary>
		void PutSetting (string key, object value);

		/// <summary>
		/// An event that is fired when the user quits the application, giving subscribers
		/// a chance to call PutSetting to store setting.
		/// </summary>
		event EventHandler? SaveSettingsBeforeQuit;
	}

	public class SettingsManager : ISettingsService
	{
		private const string SETTINGS_FILE = "settings.xml";

		private readonly Dictionary<string, object> settings = new ();

		/// <summary>
		/// Handle this event to be given a chance to save settings to disk
		/// when the user is closing the application.
		/// </summary>
		public event EventHandler? SaveSettingsBeforeQuit;

		public SettingsManager ()
		{
			LoadSettings ();
		}

		public string GetUserSettingsDirectory ()
		{
			var appdata_folder = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
			var settings_directory = Path.Combine (appdata_folder, "Pinta");

			// If someone is getting this, they probably are going to need
			// the directory created, so just handle that here.
			Directory.CreateDirectory (settings_directory);

			return settings_directory;
		}

		public T GetSetting<T> (string key, T defaultValue)
		{
			if (settings.TryGetValue (key, out var value))
				return (T) value;

			return defaultValue;
		}

		public void PutSetting (string key, object value)
		{
			settings[key] = value;
		}

		public void DoSaveSettingsBeforeQuit ()
		{
			SaveSettingsBeforeQuit?.Invoke (this, EventArgs.Empty);

			SaveSettings ();
		}

		private void LoadSettings ()
		{
			var settings_file = Path.Combine (GetUserSettingsDirectory (), SETTINGS_FILE);

			if (!File.Exists (settings_file))
				return;

			XDocument document;

			try {
				document = XDocument.Load (settings_file);
			} catch (Exception ex) {
				Console.Error.WriteLine (ex);
				return;
			}

			var nodes = document.Element ("settings")?.Elements ("setting") ?? Enumerable.Empty<XElement> (); ;

			foreach (var node in nodes) {
				if (node.Attribute ("name")?.Value is not string name)
					continue;

				// Kinda cheating because we know there are only a few types stored in here
				switch (node.Attribute ("type")?.Value) {
					case "System.Int32":
						if (int.TryParse (node.Value, out var i))
							PutSetting (name, i);
						break;
					case "System.Boolean":
						if (bool.TryParse (node.Value, out var b))
							PutSetting (name, b);
						break;
					case "System.String":
						if (node.Value is string s)
							PutSetting (name, s);
						break;
				}
			}
		}

		private void SaveSettings ()
		{
			try {
				var settings_dir = GetUserSettingsDirectory ();
				var settings_file = Path.Combine (settings_dir, SETTINGS_FILE);

				// Just in case the directory got deleted after the application started
				Directory.CreateDirectory (settings_dir);

				using (var xw = new XmlTextWriter (settings_file, Encoding.UTF8)) {
					xw.Formatting = Formatting.Indented;
					xw.WriteStartElement ("settings");

					foreach (var item in settings) {
						xw.WriteStartElement ("setting");
						xw.WriteAttributeString ("name", item.Key);
						xw.WriteAttributeString ("type", item.Value.GetType ().ToString ());
						xw.WriteValue (item.Value.ToString ());
						xw.WriteEndElement ();
					}

					xw.WriteEndElement ();
				}
			} catch (Exception ex) {
				// Not much we can do at this point since the application is exiting,
				// but I could imagine scenarios where the user doesn't have write permission.
				Console.Error.WriteLine (ex);
			}
		}
	}
}
