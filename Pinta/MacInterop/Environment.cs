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
using System.Runtime.InteropServices;

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
			// XDG_DATA_DIRS is used to find {prefix}/share/glib-2.0/schemas
			// This either from the system GTK (during development), or the app bundle.
			SetEnvVar ("XDG_DATA_DIRS", string.Join (':',
				"/usr/local/share",
				Pinta.Core.SystemManager.GetDataRootDirectory ()));
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

