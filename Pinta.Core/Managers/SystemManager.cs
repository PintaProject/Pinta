// 
// SystemManager.cs
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
using System.Runtime.InteropServices;
using System.Text;
using Gtk;

namespace Pinta.Core
{
	public class SystemManager
	{
		private static OS operating_system;

		private string last_dialog_directory;
		private RecentData recent_data;

		public ImageConverterManager ImageFormats { get; private set; }
		public PaletteFormats PaletteFormats { get; private set; }
		public int RenderThreads { get; set; }
		public OS OperatingSystem { get { return operating_system; } }

		public SystemManager ()
		{
			ImageFormats = new ImageConverterManager ();
			PaletteFormats = new PaletteFormats ();
			RenderThreads = Environment.ProcessorCount;

			last_dialog_directory = DefaultDialogDirectory;

			recent_data = new RecentData ();
			recent_data.AppName = "Pinta";
			recent_data.AppExec = GetExecutablePathName ();
			recent_data.MimeType = "image/*";
		}

		static SystemManager ()
		{
			if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
				operating_system = OS.Windows;
			else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
				operating_system = OS.Mac;
			else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux) ||
				 RuntimeInformation.IsOSPlatform (OSPlatform.FreeBSD))
				operating_system = OS.X11;
			else
				operating_system = OS.Other;
		}

		#region Public Properties
		public string LastDialogDirectory {
			get { return last_dialog_directory; }
			set {
				// The file chooser dialog may return null for the current folder in certain cases,
				// such as the Recently Used pane in the Gnome file chooser.
				if (value != null)
					last_dialog_directory = value;
			}
		}

		public string DefaultDialogDirectory {
			get { return System.Environment.GetFolderPath (Environment.SpecialFolder.MyPictures); }
		}

		public RecentData RecentData { get { return recent_data; } }
		#endregion

		/// <summary>
		/// Returns a directory for use in a dialog. The last dialog directory is
		/// returned if it exists, otherwise the default directory is used.
		/// </summary>
		public string GetDialogDirectory ()
		{
			return Directory.Exists (LastDialogDirectory) ? LastDialogDirectory : DefaultDialogDirectory;
		}

		public static string GetExecutablePathName ()
		{
			string executablePathName = System.Environment.GetCommandLineArgs ()[0];
			executablePathName = System.IO.Path.GetFullPath (executablePathName);

			return executablePathName;
		}

		public static string GetExecutableDirectory ()
		{
			// NRT - We use Path.GetFullPath so it should contain a directory
			return Path.GetDirectoryName (GetExecutablePathName ())!;
		}

		/// <summary>
		/// Return the root directory to search under for translations, documentation, etc.
		/// </summary>
		public static string GetDataRootDirectory ()
		{
			string app_dir = SystemManager.GetExecutableDirectory ();

			// If Pinta is located at $prefix/lib/pinta, we want to use $prefix/share.
			if (GetOperatingSystem () == OS.X11) {
				var lib_dir = Directory.GetParent (app_dir);
				if (lib_dir?.Name == "lib") {
					var prefix = lib_dir.Parent;
					if (prefix is not null)
						return Path.Combine (prefix.FullName, "share");
				}
			}

			// If Pinta is in Pinta.app/Contents/MacOS, we want Pinta.app/Contents/Resources/share.
			if (GetOperatingSystem () == OS.Mac) {
				var contents_dir = Directory.GetParent (app_dir);
				if (contents_dir?.Name == "Contents") {
					return Path.Combine (contents_dir.FullName, "Resources", "share");
				}
			}

			// Otherwise, translations etc are contained under the executable's folder.
			return app_dir;
		}

		public static OS GetOperatingSystem ()
		{
			return operating_system;
		}

		public T[] GetExtensions<T> ()
		{
			// TODO-GTK3 (addins)
#if false
			return AddinManager.GetExtensionObjects<T> ();
#else
			return new T[0];
#endif
		}
	}

	public enum OS
	{
		Windows,
		Mac,
		X11,
		Other
	}
}
