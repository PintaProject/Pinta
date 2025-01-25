//
// NativeImportResolver.cs
//
// Author:
//       Cameron White <cameronwhite91@gmail.com>
//
// Copyright (c) 2023 
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
using System.Reflection;
using System.Runtime.InteropServices;

namespace Pinta.Core;

// Utility to simplify DllImport's with platform-specific library names, adapted from gir.core
// TODO-GTK4 (bindings) - remove once manual bindings are no longer needed.
internal static class NativeImportResolver
{
	private class LibraryInfo
	{
		public string WindowsName;
		public string LinuxName;
		public string OsxName;
		public IntPtr LibraryPointer = IntPtr.Zero;

		public LibraryInfo (string windowsLibraryName, string linuxLibraryName, string osxLibraryName)
		{
			WindowsName = windowsLibraryName;
			OsxName = osxLibraryName;
			LinuxName = linuxLibraryName;
		}
	};

	private static readonly Dictionary<string, LibraryInfo> library_infos = [];

	static NativeImportResolver ()
	{
		NativeLibrary.SetDllImportResolver (typeof (NativeImportResolver).Assembly, Resolve);
	}

	public static void RegisterLibrary (string library, string windowsLibraryName, string linuxLibraryName, string osxLibraryName)
	{
		library_infos.Add (library, new LibraryInfo (windowsLibraryName, linuxLibraryName, osxLibraryName));
	}

	public static IntPtr Resolve (string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		if (!library_infos.TryGetValue (libraryName, out var libraryInfo))
			return IntPtr.Zero;

		if (libraryInfo.LibraryPointer != IntPtr.Zero)
			return libraryInfo.LibraryPointer;

		var osDependentLibraryName = GetOsDependentLibraryName (libraryInfo);
		libraryInfo.LibraryPointer = NativeLibrary.Load (osDependentLibraryName, assembly, searchPath);

		return libraryInfo.LibraryPointer;
	}

	private static string GetOsDependentLibraryName (LibraryInfo libraryInfo)
	{
		if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
			return libraryInfo.WindowsName;

		if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
			return libraryInfo.OsxName;

		if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux))
			return libraryInfo.LinuxName;

		throw new System.Exception ("Unknown platform");
	}
}

