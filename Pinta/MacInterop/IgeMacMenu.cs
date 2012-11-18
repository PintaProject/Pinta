// 
// IgeMacMenu.cs
//  
// Author:
//	 Eoin Hennessy <eoin@randomrules.org> 
//       jurgenobernolte
// 
// Copyright (c) 2010 Eoin Hennessy
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
//
// Ported by jurgenobernolte from:
// http://github.com/eoin/ige-mac-integration-sharp/tree/master/bindings/ige-mac-integration-sharp

using System;
using System.Runtime.InteropServices;

namespace Pinta.MacInterop
{
	public class IgeMacMenu
	{
		[DllImport ("libigemacintegration.dylib")]
		static extern void ige_mac_menu_connect_window_key_handler (IntPtr window);

		public static void ConnectWindowKeyHandler (Gtk.Window window)
		{
			ige_mac_menu_connect_window_key_handler (window.Handle);
		}

		[DllImport ("libigemacintegration.dylib")]
		static extern void ige_mac_menu_set_global_key_handler_enabled (bool enabled);

		public static bool GlobalKeyHandlerEnabled {
			set { ige_mac_menu_set_global_key_handler_enabled (value); }
		}

		[DllImport ("libigemacintegration.dylib")]
		static extern void ige_mac_menu_set_menu_bar (IntPtr menu_shell);

		public static Gtk.MenuShell MenuBar {
			set { ige_mac_menu_set_menu_bar (value == null ? IntPtr.Zero : value.Handle); }
		}

		[DllImport ("libigemacintegration.dylib")]
		static extern void ige_mac_menu_set_quit_menu_item (IntPtr quit_item);

		public static Gtk.MenuItem QuitMenuItem {
			set { ige_mac_menu_set_quit_menu_item (value == null ? IntPtr.Zero : value.Handle); }
		}

		[DllImport ("libigemacintegration.dylib")]
		static extern IntPtr ige_mac_menu_add_app_menu_group ();

		public static IgeMacMenuGroup AddAppMenuGroup ()
		{
			IntPtr raw_ret = ige_mac_menu_add_app_menu_group ();
			IgeMacMenuGroup ret = raw_ret == IntPtr.Zero ? null : (IgeMacMenuGroup)GLib.Opaque.GetOpaque (raw_ret, typeof(IgeMacMenuGroup), false);
			return ret;
		}
	}
	
	public class IgeMacMenuGroup : GLib.Opaque
	{
		[DllImport ("libigemacintegration.dylib")]
		static extern void ige_mac_menu_add_app_menu_item (IntPtr raw, IntPtr menu_item, IntPtr label);

		public void AddMenuItem (Gtk.MenuItem menu_item, string label)
		{
			IntPtr native_label = GLib.Marshaller.StringToPtrGStrdup (label);
			ige_mac_menu_add_app_menu_item (Handle, menu_item == null ? IntPtr.Zero : menu_item.Handle, native_label);
			GLib.Marshaller.Free (native_label);
		}

		public IgeMacMenuGroup (IntPtr raw) : base(raw)
		{
		}
	}
}
