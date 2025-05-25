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
using System.IO;
using System.Runtime.InteropServices;

namespace Pinta.Core;

public interface ISystemService
{
	int RenderThreads { get; set; }
}

public sealed class SystemManager : ISystemService
{
	private static readonly OS operating_system;

	public int RenderThreads { get; set; }
	public OS OperatingSystem => operating_system;

	public SystemManager ()
	{
		RenderThreads = Environment.ProcessorCount;
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
		if (GetOperatingSystem () == OS.Mac && IsExecutableInMacBundle ()) {
			var contents_dir = Directory.GetParent (app_dir);
			return Path.Combine (contents_dir!.FullName, "Resources", "share");
		}

		// On Windows, the installed executable is under Pinta/bin and data is under Pinta/share.
		if (GetOperatingSystem () == OS.Windows) {
			string parentDir = Path.Combine (app_dir, "..");
			bool develMode = Path.Exists (Path.Combine (parentDir, "Pinta.sln"));
			if (!develMode)
				return Path.Combine (parentDir, "share");
		}

		// Otherwise, translations etc are contained under the executable's folder.
		// (e.g. for local development builds)
		return app_dir;
	}

	/// <summary>
	/// Returns true if Pinta is executing in a .app bundle (macOS).
	/// This requires some different paths to locate resources, GTK libraries, etc
	/// </summary>
	public static bool IsExecutableInMacBundle ()
	{
		if (GetOperatingSystem () != OS.Mac) {
			return false;
		}

		string app_dir = SystemManager.GetExecutableDirectory ();

		// For a bundle, the executable would be Pinta.app/Contents/MacOS/Pinta
		var contents_dir = Directory.GetParent (app_dir);
		return contents_dir?.Name == "Contents";
	}

	public static OS GetOperatingSystem ()
	{
		return operating_system;
	}

	public T[] GetExtensions<T> ()
	{
		return Mono.Addins.AddinManager.GetExtensionObjects<T> ();
	}
}

public enum OS
{
	Windows,
	Mac,
	X11,
	Other
}
