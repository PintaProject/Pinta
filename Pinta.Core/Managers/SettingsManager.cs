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
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace Pinta.Core
{
	public class SettingsManager
	{
		private Dictionary<string, object> settings;

        public string LayoutFile { get { return "layouts.xml"; } }
        public string LayoutFilePath { get { return Path.Combine (GetUserSettingsDirectory (), LayoutFile); } }

		public SettingsManager ()
		{
			LoadSettings ();
		}
		
		public string GetUserSettingsDirectory ()
		{
			var settings_dir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "Pinta");

			// If someone is getting this, they probably are going to need
			// the directory created, so just handle that here.
			if (!Directory.Exists (settings_dir))
				Directory.CreateDirectory (settings_dir);

			return settings_dir;
		}
		
		public T GetSetting<T> (string key, T defaultValue)
		{
			if (!settings.ContainsKey (key))
				return defaultValue;
				
			return (T)settings[key];
		}
		
		public void PutSetting (string key, object value)
		{
			settings[key] = value;
		}

		private static Dictionary<string, object> Deserialize (string filename)
		{
			Dictionary<string, object> properties = new Dictionary<string,object> ();

			if (!File.Exists (filename))
				return properties;
				
			XmlDocument doc = new XmlDocument ();
			doc.Load (filename);
			
			// Kinda cheating for now because I know there is only a few things stored in here
			foreach (XmlElement setting in doc.DocumentElement.ChildNodes) {
				switch (setting.GetAttribute ("type")) {
					case "System.Int32":
						properties[setting.GetAttribute ("name")] = int.Parse (setting.InnerText);
						break;
					case "System.Boolean":
						properties[setting.GetAttribute ("name")] = bool.Parse (setting.InnerText);
						break;
					case "System.String":
						properties[setting.GetAttribute ("name")] = setting.InnerText;
						break;
				}
			
			}

			return properties;
		}

		private static void Serialize (string filename, Dictionary<string, object> settings)
		{
			string path = Path.GetDirectoryName (filename);

			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);

			using (XmlTextWriter xw = new XmlTextWriter (filename, System.Text.Encoding.UTF8)) {
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
		}
		
		private void LoadSettings ()
		{
			string settings_file = Path.Combine (GetUserSettingsDirectory (), "settings.xml");

			try {
				settings = Deserialize (settings_file);
			} catch (Exception) {
				// Will load with default settings
				settings = new Dictionary<string,object> ();
			}
			
			string palette_file = Path.Combine (GetUserSettingsDirectory (), "palette.txt");
			
			try {
				PintaCore.Palette.CurrentPalette.Load (palette_file);
			} catch (Exception) {
				// Retain the default palette
			}
		}
		
		public void SaveSettings ()
		{
			string settings_file = Path.Combine (GetUserSettingsDirectory (), "settings.xml");
			Serialize (settings_file, settings);
			
			string palette_file = Path.Combine (GetUserSettingsDirectory (), "palette.txt");
			PintaCore.Palette.CurrentPalette.Save (palette_file,
				PintaCore.System.PaletteFormats.Formats.First(p => p.Extensions.Contains("txt")).Saver);
		}
	}
}
