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
using System.Threading.Tasks;
using GObject;

namespace Pinta.Core;

/// <summary>
/// Style classes from libadwaita.
/// https://gnome.pages.gitlab.gnome.org/libadwaita/doc/1-latest/style-classes.html
/// </summary>
public static class AdwaitaStyles
{
	public const string Body = "body";
	public const string Compact = "compact";
	public const string DestructiveAction = "destructive-action";
	public const string DimLabel = "dim-label";
	public const string Error = "error";
	public const string Flat = "flat";
	public const string Heading = "heading";
	public const string Inline = "inline";
	public const string Linked = "linked";
	public const string Osd = "osd";
	public const string Spacer = "spacer";
	public const string SuggestedAction = "suggested-action";
	public const string Title4 = "title-4";
	public const string Toolbar = "toolbar";
	public const string Warning = "warning";
};

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
	/// In GTK4, toolbars are just a Box with a different CSS style class.
	/// </summary>
	public static Gtk.Box CreateToolBar ()
	{
		Gtk.Box toolbar = new () { Spacing = 0 };
		toolbar.SetOrientation (Gtk.Orientation.Horizontal);
		toolbar.AddCssClass (AdwaitaStyles.Toolbar);
		return toolbar;
	}

	/// <summary>
	/// Remove all child widgets from a box.
	/// </summary>
	public static void RemoveAll (this Gtk.Box box)
	{
		while (box.GetFirstChild () is Gtk.Widget child)
			box.Remove (child);
	}

	public static Gtk.Button CreateToolBarItem (this Command action, bool force_icon_only = false)
	{
		string label = action.ShortLabel ?? action.Label;
		Gtk.Button button = new () {
			ActionName = action.FullName,
			TooltipText = action.Tooltip ?? action.Label,
		};

		if (action.IsImportant && !force_icon_only) {
			button.Child = new Adw.ButtonContent () {
				IconName = action.IconName,
				Label = label
			};
		} else {
			button.Label = label;
			button.IconName = action.IconName;
		}

		return button;
	}

	public static Gtk.Button CreateDockToolBarItem (this Command action)
	{
		return action.CreateToolBarItem (force_icon_only: false);
	}

	public static Gtk.Separator CreateToolBarSeparator ()
	{
		Gtk.Separator sep = new ();
		sep.AddCssClass (AdwaitaStyles.Spacer);
		return sep;
	}

	public static Gtk.SpinButton CreateToolBarSpinButton (
		double min,
		double max,
		double step,
		double init_value)
	{
		Gtk.SpinButton spin = Gtk.SpinButton.NewWithRange (min, max, step);
		spin.FocusOnClick = false;
		spin.Value = init_value;
		// After a spin button is edited, return focus to the canvas so that
		// tools can handle subsequent key events.
		spin.OnValueChanged += (o, e) => {
			if (PintaCore.Workspace.HasOpenDocuments)
				PintaCore.Workspace.ActiveWorkspace.Canvas.GrabFocus ();
		};
		return spin;
	}

	public static Gtk.Scale CreateToolBarSlider (
		int min,
		int max,
		int step,
		int val)
	{
		Gtk.Scale slider = Gtk.Scale.NewWithRange (Gtk.Orientation.Horizontal, min, max, step);
		slider.WidthRequest = 150;
		slider.DrawValue = true;
		slider.ValuePos = Gtk.PositionType.Left;
		slider.SetValue (val);
		return slider;
	}

	public static void Toggle (this Gtk.ToggleButton button)
	{
		button.Active = !button.Active;
	}

	public static void AddAction (
		this Gtk.Application app,
		Command action)
	{
		app.AddAction (action.Action);
	}

	public static void AddAccelAction (
		this Gtk.Application app,
		Command action,
		string accel)
	{
		app.AddAccelAction (action, new[] { accel });
	}

	public static void AddAccelAction (
		this Gtk.Application app,
		Command action,
		IEnumerable<string> accels)
	{
		app.AddAction (action);
		app.SetAccelsForAction (
			action.FullName,
			accels.Select (a => PintaCore.System.ConvertPrimaryKey (a)).ToArray ());
	}

	/// <summary>
	/// Convert the "<Primary>" accelerator to the Ctrl or Command key, depending on the platform.
	/// This was done automatically in GTK3, but does not happen in GTK4.
	/// </summary>
	private static string ConvertPrimaryKey (this SystemManager system, string accel) =>
		accel.Replace ("<Primary>", system.OperatingSystem == OS.Mac ? "<Meta>" : "<Control>");

	/// <summary>
	/// Returns the Cancel / Ok button pair in the correct order for the current platform.
	/// This can be used with the Gtk.Dialog constructor.
	/// </summary>
	public static void AddCancelOkButtons (this Gtk.Dialog dialog)
	{
		Gtk.Widget ok_button;
		if (PintaCore.System.OperatingSystem == OS.Windows) {
			ok_button = dialog.AddButton (Translations.GetString ("_OK"), (int) Gtk.ResponseType.Ok);
			dialog.AddButton (Translations.GetString ("_Cancel"), (int) Gtk.ResponseType.Cancel);
		} else {
			dialog.AddButton (Translations.GetString ("_Cancel"), (int) Gtk.ResponseType.Cancel);
			ok_button = dialog.AddButton (Translations.GetString ("_OK"), (int) Gtk.ResponseType.Ok);
		}

		ok_button.AddCssClass (AdwaitaStyles.SuggestedAction);
	}

	public static void SetDefaultResponse (
		this Gtk.Dialog dialog,
		Gtk.ResponseType response
	)
		=> dialog.SetDefaultResponse ((int) response);

	/// <summary>
	/// Helper function to avoid repeated casts. The dialog's content area is always a Box.
	/// </summary>
	public static Gtk.Box GetContentAreaBox (this Gtk.Dialog dialog)
		=> (Gtk.Box) dialog.GetContentArea ();

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
	/// Set all four margins of the widget to the same value.
	/// </summary>
	/// <param name="w"></param>
	/// <param name="margin"></param>
	public static void SetAllMargins (this Gtk.Widget w, int margin)
	{
		w.MarginTop = w.MarginBottom = w.MarginStart = w.MarginEnd = margin;
	}

	/// <summary>
	/// Helper function to return the icon theme for the default display.
	/// </summary>
	public static Gtk.IconTheme GetDefaultIconTheme ()
		=> Gtk.IconTheme.GetForDisplay (Gdk.Display.GetDefault ()!);

	/// <summary>
	/// For a combo box that has an entry, provides easy access to the child entry widget.
	/// </summary>
	public static Gtk.Entry GetEntry (this Gtk.ComboBox box)
	{
		if (!box.HasEntry)
			throw new InvalidOperationException ("Combobox does not have an entry");

		return (Gtk.Entry) box.Child!;
	}

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
	/// Similar to gtk_dialog_run() in GTK3, this runs the dialog in a blocking manner with a nested event loop.
	/// This can be useful for compatibility with old code that relies on this behaviour, but new code should be
	/// structured to use event handlers.
	/// </summary>
	public static string RunBlocking (this Adw.MessageDialog dialog)
	{
		string response = "";
		var loop = GLib.MainLoop.New (null, false);

		if (!dialog.Modal)
			dialog.Modal = true;

		dialog.OnResponse += (_, args) => {
			response = args.Response;
			if (loop.IsRunning ())
				loop.Quit ();
		};

		dialog.Show ();
		loop.Run ();

		return response;
	}

	/// <summary>
	/// Similar to gtk_dialog_run() in GTK3, this runs the dialog in a blocking manner with a nested event loop.
	/// This can be useful for compatibility with old code that relies on this behaviour, but new code should be
	/// structured to use event handlers.
	/// </summary>
	public static Gtk.ResponseType RunBlocking (this Gtk.NativeDialog dialog)
	{
		var response = Gtk.ResponseType.None;
		var loop = GLib.MainLoop.New (null, false);

		if (!dialog.Modal)
			dialog.Modal = true;

		dialog.OnResponse += (_, args) => {
			response = (Gtk.ResponseType) args.ResponseId;
			if (loop.IsRunning ())
				loop.Quit ();
		};

		dialog.Show ();
		loop.Run ();

		return response;
	}

	/// <summary>
	/// Similar to gtk_dialog_run() in GTK3, this runs the dialog in a blocking manner with a nested event loop.
	/// This can be useful for compatibility with old code that relies on this behaviour, but new code should be
	/// structured to use event handlers.
	/// </summary>
	public static Gtk.ResponseType RunBlocking (this Gtk.Dialog dialog)
	{
		var response = Gtk.ResponseType.None;
		var loop = GLib.MainLoop.New (null, false);

		if (!dialog.Modal)
			dialog.Modal = true;

		dialog.OnResponse += (_, args) => {
			response = (Gtk.ResponseType) args.ResponseId;
			if (loop.IsRunning ())
				loop.Quit ();
		};

		dialog.Show ();
		loop.Run ();

		return response;
	}

	// TODO-GTK4 (bindings) - replace with adw_message_dialog_choose() once adwaita 1.3 is available, like in v0.4 of gir.core
	public static Task<string> RunAsync (this Adw.MessageDialog dialog)
	{
		TaskCompletionSource<string> completionSource = new ();

		void ResponseCallback (
			Adw.MessageDialog sender,
			Adw.MessageDialog.ResponseSignalArgs args)
		{
			completionSource.SetResult (args.Response);
			dialog.OnResponse -= ResponseCallback;
		}

		dialog.OnResponse += ResponseCallback;
		dialog.Present ();

		return completionSource.Task;
	}

	/// <returns>Task whose result is the response ID</returns>
	public static Task<int> RunAsync (this Gtk.Dialog dialog)
	{
		TaskCompletionSource<int> completionSource = new ();

		void ResponseCallback (
			Gtk.Dialog sender,
			Gtk.Dialog.ResponseSignalArgs args)
		{
			completionSource.SetResult (args.ResponseId);
			dialog.OnResponse -= ResponseCallback;
		}

		dialog.OnResponse += ResponseCallback;
		dialog.Present ();

		return completionSource.Task;
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

	/// <summary>
	/// Configures a spin button to immediately activate the default widget after pressing Enter,
	/// by configuring the editable text field.
	/// In GTK4, Gtk.SpinButton.SetActivateDefault() requires a second Enter to activate.
	/// </summary>
	public static void SetActivatesDefaultImmediate (
		this Gtk.SpinButton spin_button,
		bool activates)
	{
		Gtk.Editable? editable = spin_button.GetDelegate ();

		if (editable is null)
			return;

		// TODO-GTK4 (bindings, unsubmitted) - should be able to cast to a Gtk.Text from Gtk.Editable
		TextWrapper text = new (editable.Handle, ownedRef: false);
		text.SetActivatesDefault (activates);
	}

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

	// TODO-GTK4 (bindings) - structs are not generated (https://github.com/gircore/gir.core/issues/622)
	public static void GetColor (this Gtk.StyleContext context, out Cairo.Color color)
	{
		StyleContextGetColor (context.Handle, out var gdk_color);
		color = new Cairo.Color (gdk_color.Red, gdk_color.Green, gdk_color.Blue, gdk_color.Alpha);
	}

	[LibraryImport (GtkLibraryName, EntryPoint = "gtk_style_context_get_color")]
	private static partial void StyleContextGetColor (IntPtr handle, out GdkRGBA color);

	public static void SetColor (this Gtk.ColorChooserDialog dialog, Cairo.Color color)
	{
		dialog.SetRgba (new Gdk.RGBA {
			Red = (float) color.R,
			Blue = (float) color.B,
			Green = (float) color.G,
			Alpha = (float) color.A
		});
	}

	// TODO-GTK4 (bindings) - structs are not generated (https://github.com/gircore/gir.core/issues/622)
	public static void GetColor (this Gtk.ColorChooserDialog dialog, out Cairo.Color color)
	{
		ColorChooserGetRgba (dialog.Handle, out var gdk_color);
		color = new Cairo.Color (gdk_color.Red, gdk_color.Green, gdk_color.Blue, gdk_color.Alpha);
	}

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

	/// <summary>
	/// Remove the widget if it is a child of the box.
	/// Calling Remove() produces warnings from GTK if the child isn't found.
	/// </summary>
	public static void RemoveIfChild (
		this Gtk.Box box,
		Gtk.Widget to_remove)
	{
		Gtk.Widget? child = box.GetFirstChild ();
		while (child != null) {

			if (child == to_remove) {
				box.Remove (child);
				return;
			}

			child = child.GetNextSibling ();
		}
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

	/// Wrapper around TranslateCoordinates which uses PointD instead of separate x/y parameters.
	public static bool TranslateCoordinates (
		this Gtk.Widget src,
		Gtk.Widget dest,
		PointD src_pos,
		out PointD dest_pos)
	{
		bool result = src.TranslateCoordinates (
			dest,
			src_pos.X,
			src_pos.Y,
			out double x,
			out double y);

		dest_pos = new PointD (x, y);

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
