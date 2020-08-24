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
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Mono.Addins;
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
		public FontManager Fonts { get; private set; }
		public int RenderThreads { get; set; }
		public OS OperatingSystem { get { return operating_system; } }
		
		public SystemManager ()
		{
			ImageFormats = new ImageConverterManager ();
			PaletteFormats = new PaletteFormats ();
			RenderThreads = Environment.ProcessorCount;
			Fonts = new FontManager ();

			last_dialog_directory = DefaultDialogDirectory;

			recent_data = new RecentData ();
			recent_data.AppName = "Pinta";
			recent_data.AppExec = GetExecutablePathName ();
			recent_data.MimeType = "image/*";
		}

		static SystemManager ()
		{
			if (Path.DirectorySeparatorChar == '\\')
				operating_system = OS.Windows;
			else if (IsRunningOnMac ())
				operating_system = OS.Mac;
			else if (Environment.OSVersion.Platform == PlatformID.Unix)
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
			return Path.GetDirectoryName (GetExecutablePathName ());
		}

		/// <summary>
		/// Return the root directory to search under for translations, documentation, etc.
		/// </summary>
		public static string GetDataRootDirectory ()
		{
			string app_dir = SystemManager.GetExecutableDirectory ();
			bool devel_mode = File.Exists (Path.Combine (Path.Combine (app_dir, ".."), "Pinta.sln"));

			if (GetOperatingSystem () != OS.X11 || devel_mode)
				return app_dir;
			else {
				// From MonoDevelop:
				// Pinta is located at $prefix/lib/pinta
				// adding "../.." should give us $prefix
				string prefix = Path.Combine (Path.Combine (app_dir, ".."), "..");
				//normalise it
				prefix = Path.GetFullPath (prefix);
				return Path.Combine (prefix, "share");
			}
		}

		public static OS GetOperatingSystem ()
		{
			return operating_system;
		}

		public T[] GetExtensions<T> ()
		{
			return AddinManager.GetExtensionObjects<T> ();
		}

		//From Managed.Windows.Forms/XplatUI
		[DllImport ("libc")]
		static extern int uname (IntPtr buf);

		static bool IsRunningOnMac ()
		{
			IntPtr buf = IntPtr.Zero;
			try {
				buf = Marshal.AllocHGlobal (8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname (buf) == 0) {
					string os = Marshal.PtrToStringAnsi (buf);
					if (os == "Darwin")
						return true;
				}
			} catch {
			} finally {
				if (buf != IntPtr.Zero)
					Marshal.FreeHGlobal (buf);
			}
			return false;
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
