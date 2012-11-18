// 
// CoreFoundation.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//       Miguel de Icaza
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
	internal static class CoreFoundation
	{
		const string CFLib = "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation";
		const string LSLib = "/System/Library/Frameworks/ApplicationServices.framework/Versions/A/ApplicationServices";
		
		[DllImport (CFLib)]
		static extern IntPtr CFStringCreateWithCString (IntPtr alloc, string str, int encoding);
		
		public static IntPtr CreateString (string s)
		{
			// The magic value is "kCFStringENcodingUTF8"
			return CFStringCreateWithCString (IntPtr.Zero, s, 0x08000100);
		}
		
		[DllImport (CFLib, EntryPoint="CFRelease")]
		public static extern void Release (IntPtr cfRef);
		
		struct CFRange {
			public int Location, Length;
			public CFRange (int l, int len)
			{
				Location = l;
				Length = len;
			}
		}
		
		[DllImport (CFLib, CharSet=CharSet.Unicode)]
		extern static int CFStringGetLength (IntPtr handle);

		[DllImport (CFLib, CharSet=CharSet.Unicode)]
		extern static IntPtr CFStringGetCharactersPtr (IntPtr handle);
		
		[DllImport (CFLib, CharSet=CharSet.Unicode)]
		extern static IntPtr CFStringGetCharacters (IntPtr handle, CFRange range, IntPtr buffer);
		
		public static string FetchString (IntPtr handle)
		{
			if (handle == IntPtr.Zero)
				return null;
			
			string str;
			
			int l = CFStringGetLength (handle);
			IntPtr u = CFStringGetCharactersPtr (handle);
			IntPtr buffer = IntPtr.Zero;
			if (u == IntPtr.Zero){
				CFRange r = new CFRange (0, l);
				buffer = Marshal.AllocCoTaskMem (l * 2);
				CFStringGetCharacters (handle, r, buffer);
				u = buffer;
			}
			unsafe {
				str = new string ((char *) u, 0, l);
			}
			
			if (buffer != IntPtr.Zero)
				Marshal.FreeCoTaskMem (buffer);
			
			return str;
		}
		
		public static string FSRefToString (ref FSRef fsref)
		{
			IntPtr url = IntPtr.Zero;
			IntPtr str = IntPtr.Zero;
			try {
				url = CFURLCreateFromFSRef (IntPtr.Zero, ref fsref);
				if (url == IntPtr.Zero)
					return null;
				str = CFURLCopyFileSystemPath (url, CFUrlPathStyle.Posix);
				if (str == IntPtr.Zero)
					return null;
				return FetchString (str);
			} finally {
				if (url != IntPtr.Zero)
					Release (url);
				if (str != IntPtr.Zero)
					Release (str);
			}
		}
		
		[DllImport (CFLib)]
		extern static IntPtr CFURLCreateFromFSRef (IntPtr allocator, ref FSRef fsref);
		
		[DllImport (CFLib)]
		extern static IntPtr CFURLCopyFileSystemPath (IntPtr urlRef, CFUrlPathStyle pathStyle);
		
		enum CFUrlPathStyle
		{
			Posix = 0,
			Hfs = 1,
			Windows = 2
		};
		
		[DllImport (CFLib)]
		extern static IntPtr CFURLCreateWithFileSystemPath (IntPtr allocator, IntPtr filePathString, 
			CFUrlPathStyle pathStyle, bool isDirectory);
		
		[DllImport (LSLib)]
		extern static IntPtr LSCopyApplicationURLsForURL (IntPtr urlRef, LSRolesMask roleMask); //CFArrayRef
		
		[DllImport (LSLib)]
		extern static int LSGetApplicationForURL (IntPtr url, LSRolesMask roleMask, IntPtr fsRefZero,
			ref IntPtr  appUrl);
		
		[DllImport (CFLib)]
		extern static int CFArrayGetCount (IntPtr theArray);
		
		[DllImport (CFLib)]
		extern static IntPtr CFArrayGetValueAtIndex (IntPtr theArray, int idx);
		
		[Flags]
		public enum LSRolesMask : uint
		{
			None = 0x00000001,
			Viewer = 0x00000002,
			Editor = 0x00000004,
			Shell = 0x00000008,
			All = 0xFFFFFFFF
		}
		
		static IntPtr CreatePathUrl (string path)
		{
			IntPtr str = IntPtr.Zero;
			IntPtr url = IntPtr.Zero;
			try {
				str = CreateString (path);
				if (str == IntPtr.Zero)
					throw new Exception ("CreateString failed");
				url = CFURLCreateWithFileSystemPath (IntPtr.Zero, str, CFUrlPathStyle.Posix, false);
				if (url == IntPtr.Zero)
					throw new Exception ("CFURLCreateWithFileSystemPath failed");
				return url;
			} finally {
				if (str != IntPtr.Zero)
					Release (str);
			}
		}
		
		public static string UrlToPath (IntPtr url)
		{
			IntPtr str = IntPtr.Zero;
			try {
				str = CFURLCopyFileSystemPath (url, CFUrlPathStyle.Posix);
				return str == IntPtr.Zero? null : FetchString (str);
			} finally {
				if (str != IntPtr.Zero)
					Release (str);
			}
		}
		
		public static string GetApplicationUrl (string filePath, LSRolesMask roles)
		{
			IntPtr url = IntPtr.Zero;
			try {
				url = CreatePathUrl (filePath);
				IntPtr appUrl = IntPtr.Zero;
				if (LSGetApplicationForURL (url, roles, IntPtr.Zero, ref appUrl) == 0 && appUrl != IntPtr.Zero)
					return UrlToPath (appUrl);
				return null;
			} finally {
				if (url != IntPtr.Zero)
					Release (url);
			}
		}
		
		public static string[] GetApplicationUrls (string filePath, LSRolesMask roles)
		{
			IntPtr url = IntPtr.Zero;
			IntPtr arr = IntPtr.Zero;
			try {
				url = CreatePathUrl (filePath);
				arr = LSCopyApplicationURLsForURL (url, roles);
				if (arr == IntPtr.Zero)
					return new string[0];
				int count = CFArrayGetCount (arr);
				string[] values = new string [count];
				for (int i = 0; i < values.Length; i++ ) {
					var u = CFArrayGetValueAtIndex (arr, i);
					if (u != IntPtr.Zero)
						values[i] = UrlToPath (u);
				}
				return values;
			} finally {
				if (url != IntPtr.Zero)
					Release (url);
				if (arr != IntPtr.Zero)
					Release (arr);
			}
		}
	}
}
