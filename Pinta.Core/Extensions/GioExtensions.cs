// 
// GioExtensions.cs
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
using System.Linq;
using Gio;

namespace Pinta.Core
{
	public static class GioExtensions
	{
		/// <summary>
		/// Return the display name for the file. Note that this can be very different from file.Basename,
		/// and should only be used for display purposes rather than identifying the file.
		/// </summary>
		public static string GetDisplayName (this Gio.File file)
		{
#if false // TODO-GTK4 - needs gir.core bindings
			// TODO-GTK4: use G_FILE_ATTRIBUTE_STANDARD_DISPLAY_NAME if there are bindings for it.
			var info = file.QueryInfo ("standard::display-name", GLib.FileQueryInfoFlags.None, cancellable: null);
			return info.DisplayName;
#else
			return file.GetBasename ()!;
#endif
		}

		/// <summary>
		/// Returns an output stream for creating or overwriting the file.
		/// NOTE: if you don't wrap this in a GLib.GioStream, you must call Close() !
		/// </summary>
		public static Gio.OutputStream Replace (this Gio.File file)
		{
#if false // TODO-GTK4 - needs gir.core bindings
			return file.Replace (null, false, Gio.FileCreateFlags.None, null);
#else
			throw new NotImplementedException ();
#endif
		}
	}
}
