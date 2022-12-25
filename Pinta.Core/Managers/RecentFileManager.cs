// 
// RecentFileManager.cs
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
	public class RecentFileManager
	{
		private GLib.IFile last_dialog_directory;
		private RecentData recent_data;

		public RecentFileManager ()
		{
			last_dialog_directory = DefaultDialogDirectory;

			recent_data = new RecentData ();
			recent_data.AppName = "Pinta";
			recent_data.AppExec = SystemManager.GetExecutablePathName ();
			recent_data.MimeType = "image/*";
		}

		public GLib.IFile LastDialogDirectory {
			get { return last_dialog_directory; }
			set {
				// The file chooser dialog may return null for the current folder in certain cases,
				// such as the Recently Used pane in the Gnome file chooser.
				if (value != null)
					last_dialog_directory = value;
			}
		}

		public GLib.IFile DefaultDialogDirectory {
			get { return GLib.FileFactory.NewForPath (System.Environment.GetFolderPath (Environment.SpecialFolder.MyPictures)); }
		}

		public RecentData RecentData { get { return recent_data; } }

		/// <summary>
		/// Returns a directory for use in a dialog. The last dialog directory is
		/// returned if it exists, otherwise the default directory is used.
		/// </summary>
		public GLib.IFile GetDialogDirectory ()
		{
			return last_dialog_directory.Exists ? last_dialog_directory : DefaultDialogDirectory;
		}
	}
}
