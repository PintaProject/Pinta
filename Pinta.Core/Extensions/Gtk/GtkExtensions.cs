//
// GtkExtensions.cs
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
using System.Runtime.InteropServices;
using GObject;

namespace Pinta.Core;

public static partial class GtkExtensions
{
	private const string GtkLibraryName = "Gtk";

	static GtkExtensions ()
	{
		NativeImportResolver.RegisterLibrary (

			library: GtkLibraryName,

			windowsLibraryName: "libgtk-4-1.dll",
			linuxLibraryName: "libgtk-4.so.1",
			osxLibraryName: "libgtk-4.1.dylib"
		);
	}

	public const uint MouseLeftButton = 1;
	public const uint MouseMiddleButton = 2;
	public const uint MouseRightButton = 3;

	/// <summary>
	/// Convert from GetCurrentButton to the MouseButton enum.
	/// </summary>
	public static MouseButton GetCurrentMouseButton (this Gtk.GestureClick gesture)
	{
		uint button = gesture.GetCurrentButton ();
		return button switch {
			MouseLeftButton => MouseButton.Left,
			MouseMiddleButton => MouseButton.Middle,
			MouseRightButton => MouseButton.Right,
			_ => MouseButton.None
		};
	}

	// TODO-GTK4 (bindings) - add pre-defined VariantType's to gir.core (https://github.com/gircore/gir.core/issues/843)
	public static readonly GLib.VariantType IntVariantType = GLib.VariantType.New ("i");

	/// <summary>
	/// Convert the "<Primary>" accelerator to the Ctrl or Command key, depending on the platform.
	/// This was done automatically in GTK3, but does not happen in GTK4.
	/// </summary>
	private static string ConvertPrimaryKey (this SystemManager system, string accel) =>
		accel.Replace ("<Primary>", system.OperatingSystem == OS.Mac ? "<Meta>" : "<Control>");

	/// <summary>
	/// Returns the platform-specific label for the "Primary" (Ctrl) key.
	/// For example, this is the Cmd key on macOS.
	/// </summary>
	public static string CtrlLabel (this SystemManager system)
	{
		AcceleratorParse (
			system.ConvertPrimaryKey ("<Primary>"),
			out var key,
			out var mods);

		return Gtk.Functions.AcceleratorGetLabel (key, mods);
	}

	/// <summary>
	/// Returns the platform-specific label for the Alt key.
	/// For example, this is the Option key on macOS.
	/// </summary>
	public static string AltLabel ()
	{
		AcceleratorParse ("<Alt>", out var key, out var mods);
		return Gtk.Functions.AcceleratorGetLabel (key, mods);
	}

	// TODO-GTK4 (bindings, unsubmitted) - need support for 'out' enum parameters.
	[LibraryImport (GtkLibraryName, EntryPoint = "gtk_accelerator_parse")]
	[return: MarshalAs (UnmanagedType.Bool)]
	private static partial bool AcceleratorParse (
		[MarshalAs (UnmanagedType.LPUTF8Str)] string accelerator,
		out uint accelerator_key,
		out Gdk.ModifierType accelerator_mods);

	/// <summary>
	/// Helper function to return the icon theme for the default display.
	/// </summary>
	public static Gtk.IconTheme GetDefaultIconTheme ()
		=> Gtk.IconTheme.GetForDisplay (Gdk.Display.GetDefault ()!);

	/// <summary>
	/// Find the index of a string in a Gtk.StringList.
	/// </summary>
	public static bool FindString (this Gtk.StringList list, string s, out uint index)
	{
		for (uint i = 0, n = list.GetNItems (); i < n; ++i) {
			if (list.GetString (i) == s) {
				index = i;
				return true;
			}
		}

		index = 0;
		return false;
	}

	/// <summary>
	/// Provides convenient access to the Gdk.Key of the key being pressed.
	/// </summary>
	public static Gdk.Key GetKey (this Gtk.EventControllerKey.KeyPressedSignalArgs args)
		=> (Gdk.Key) args.Keyval;

	/// <summary>
	/// Provides convenient access to the Gdk.Key of the key being released.
	/// </summary>
	public static Gdk.Key GetKey (this Gtk.EventControllerKey.KeyReleasedSignalArgs args)
		=> (Gdk.Key) args.Keyval;

	internal sealed class TextWrapper : Gtk.Text
	{
		internal TextWrapper (IntPtr ptr, bool ownedRef)
			: base (ptr, ownedRef)
		{ }
	}

	// TODO-GTK4 (bindings) - structs are not generated (https://github.com/gircore/gir.core/issues/622)
	[StructLayout (LayoutKind.Sequential)]
	private struct GdkRGBA
	{
		public float Red;
		public float Green;
		public float Blue;
		public float Alpha;
	}

	[LibraryImport (GtkLibraryName, EntryPoint = "gtk_style_context_get_color")]
	private static partial void StyleContextGetColor (IntPtr handle, out GdkRGBA color);

	[LibraryImport (GtkLibraryName, EntryPoint = "gtk_color_chooser_get_rgba")]
	private static partial void ColorChooserGetRgba (
		IntPtr handle,
		out GdkRGBA color);

	private static readonly Signal<Gtk.Entry> EntryChangedSignal = new (
		unmanagedName: "changed",
		managedName: string.Empty
	);

	// TODO-GTK4 (bindings) - the Gtk.Editable::changed signal is not generated (https://github.com/gircore/gir.core/issues/831)
	public static void OnChanged (
		this Gtk.Entry o,
		SignalHandler<Gtk.Entry> handler)
	{
		EntryChangedSignal.Connect (o, handler);
	}

	public sealed class SelectionChangedSignalArgs : SignalArgs
	{
		public uint Position => Args[1].GetUint ();
		public uint NItems => Args[2].GetUint ();

	}

	private static readonly Signal<Gtk.SingleSelection, SelectionChangedSignalArgs> SelectionChangedSignal = new (
		unmanagedName: "selection-changed",
		managedName: string.Empty
	);

	// TODO-GTK4 (bindings) - the Gtk.SelectionModel::selection-changed signal is not generated (https://github.com/gircore/gir.core/issues/831)
	public static void OnSelectionChanged (
		this Gtk.SingleSelection o,
		SignalHandler<Gtk.SingleSelection, SelectionChangedSignalArgs> handler)
	{
		SelectionChangedSignal.Connect (o, handler);
	}

	// TODO-GTK4 (bindings) - wrapper for GetFiles() since Gio.ListModel.GetObject doesn't return a Gio.File instance (https://github.com/gircore/gir.core/issues/838)
	public static IReadOnlyList<Gio.File> GetFileList (this Gtk.FileChooser file_chooser)
	{
		List<Gio.File> result = new ();

		Gio.ListModel files = file_chooser.GetFiles ();
		for (uint i = 0, n = files.GetNItems (); i < n; ++i)
			result.Add (new Gio.FileHelper (files.GetItem (i), ownedRef: true));

		return result;
	}

	// Manual binding for GetPreeditString
	// TODO-GTK4 (bindings) - missing from gir.core: "opaque record parameter 'attrs' with direction != in not yet supported"
	[DllImport (GtkLibraryName, EntryPoint = "gtk_im_context_get_preedit_string")]
	private static extern void IMContextGetPreeditString (
		IntPtr handle,
		out GLib.Internal.NonNullableUtf8StringOwnedHandle str,
		out Pango.Internal.AttrListOwnedHandle attrs,
		out int cursor_pos);

	public static void GetPreeditString (
		this Gtk.IMContext context,
		out string str,
		out Pango.AttrList attrs,
		out int cursor_pos)
	{
		IMContextGetPreeditString (
			context.Handle,
			out var str_handle,
			out var attrs_handle,
			out cursor_pos);

		str = str_handle.ConvertToString ();
		str_handle.Dispose ();
		attrs = new Pango.AttrList (attrs_handle);
	}

	public static async void LaunchUri (
		this SystemManager system,
		string uri)
	{
		// Workaround for macOS, which produces an "unsupported on current backend" error (https://gitlab.gnome.org/GNOME/gtk/-/issues/6788)
		if (system.OperatingSystem == OS.Mac) {
			var process = System.Diagnostics.Process.Start ("open", uri);
			process.WaitForExit ();
		} else {
			Gtk.UriLauncher launcher = Gtk.UriLauncher.New (uri);
			await launcher.LaunchAsync (PintaCore.Chrome.MainWindow);
		}
	}
}
