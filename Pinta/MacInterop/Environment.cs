//  
// Author:
//       Cameron White <cameronwhite91@gmail.com>
// 
// Copyright (c) 2021 Cameron White
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
using System.IO;
using System.Runtime.InteropServices;
using Pinta.Core;

namespace Pinta.MacInterop
{
	public class Environment
	{
		[DllImport ("/usr/lib/system/libsystem_c.dylib")]
		static extern int setenv (string name, string value);

		/// <summary>
		/// Initialize any environment variables for macOS (e.g. for GTK).
		/// </summary>
		public static void Init ()
		{
			// Set environment variables used to locate the GTK installation in the .app bundle.
			if (SystemManager.IsExecutableInMacBundle ()) {
				// XDG_DATA_DIRS is used to find {prefix}/share/glib-2.0/schemas
				var share_dir = SystemManager.GetDataRootDirectory ();
				SetEnvVar ("XDG_DATA_DIRS", share_dir);

				// Set environment variables used for loading pixbuf modules and input method modules.
				// TODO-GTK4 - update when packaging GTK4 for macOS.
				var resources_dir = Directory.GetParent (share_dir)!.FullName;
				SetEnvVar ("GTK_IM_MODULE_FILE", Path.Combine (resources_dir, "lib/gtk-3.0/3.0.0/immodules.cache"));
				SetEnvVar ("GDK_PIXBUF_MODULE_FILE", Path.Combine (resources_dir, "lib/gdk-pixbuf-2.0/2.10.0/loaders.cache"));
			}
		}

		private static void SetEnvVar (string name, string value)
		{
			// We need to use setenv() for the GTK libraries, but also set using the
			// .NET apis for consistency.
			// See https://yizhang82.dev/set-environment-variable
			System.Environment.SetEnvironmentVariable (name, value);
			setenv (name, value);
		}
	}
}

