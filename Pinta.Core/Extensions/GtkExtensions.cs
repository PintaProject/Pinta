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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Gtk;

namespace Pinta.Core
{
	/// <summary>
	/// Style classes from libadwaita.
	/// https://gnome.pages.gitlab.gnome.org/libadwaita/doc/1-latest/style-classes.html
	/// </summary>
	public static class AdwaitaStyles
	{
		public const string Flat = "flat";
		public const string Inline = "inline";
		public const string Linked = "linked";
		public const string Spacer = "spacer";
		public const string SuggestedAction = "suggested-action";
		public const string Title4 = "title-4";
		public const string Toolbar = "toolbar";
	};

	public static class GtkExtensions
	{
		private const string GtkLibraryName = "Gtk";

		static GtkExtensions ()
		{
			NativeImportResolver.RegisterLibrary (GtkLibraryName,
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

		/// <summary>
		/// In GTK4, toolbars are just a Box with a different CSS style class.
		/// </summary>
		public static Gtk.Box CreateToolBar ()
		{
			var toolbar = new Box () { Spacing = 0 };
			toolbar.SetOrientation (Orientation.Horizontal);
			toolbar.AddCssClass (AdwaitaStyles.Toolbar);
			return toolbar;
		}

		public static Gtk.Button CreateToolBarItem (this Command action)
		{
			var label = action.ShortLabel ?? action.Label;
			var button = new Button () {
				ActionName = action.FullName,
				TooltipText = action.Tooltip ?? action.Label,
			};

			if (action.IsImportant) {
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

		public static Gtk.Separator CreateToolBarSeparator ()
		{
			var sep = new Separator ();
			sep.AddCssClass (AdwaitaStyles.Spacer);
			return sep;
		}

		public static Gtk.SpinButton CreateToolBarSpinButton (double min, double max, double step, double init_value)
		{
			var spin = Gtk.SpinButton.NewWithRange (min, max, step);
			spin.Value = init_value;
			// After a spin button is edited, return focus to the canvas so that
			// tools can handle subsequent key events.
			spin.OnValueChanged += (o, e) => {
				if (PintaCore.Workspace.HasOpenDocuments)
					PintaCore.Workspace.ActiveWorkspace.Canvas.GrabFocus ();
			};
			return spin;
		}

		public static Scale CreateToolBarSlider (int min, int max, int step, int val)
		{
			var slider = Scale.NewWithRange (Orientation.Horizontal, min, max, step);
			slider.WidthRequest = 150;
			slider.ValuePos = PositionType.Left;
			slider.SetValue (val);
			return slider;
		}

		public static void Toggle (this Gtk.ToggleButton button)
		{
			button.Active = !button.Active;
		}

		public static void AddAction (this Gtk.Application app, Command action)
		{
			app.AddAction (action.Action);
		}

		public static void AddAccelAction (this Gtk.Application app, Command action, string accel)
		{
			app.AddAccelAction (action, new[] { accel });
		}

		public static void AddAccelAction (this Gtk.Application app, Command action, string[] accels)
		{
			app.AddAction (action);
			app.SetAccelsForAction (action.FullName, accels.Select (s => ConvertPrimaryKey (s)).ToArray ());
		}

		/// <summary>
		/// Convert the "<Primary>" accelerator to the Ctrl or Command key, depending on the platform.
		/// This was done automatically in GTK3, but does not happen in GTK4.
		/// </summary>
		private static string ConvertPrimaryKey (string accel) =>
			accel.Replace ("<Primary>", PintaCore.System.OperatingSystem == OS.Mac ? "<Meta>" : "<Control>");

		public static int GetItemCount (this ComboBox combo)
		{
#if false // TODO-GTK4
			return ((ListStore) combo.Model).IterNChildren ();
#else
			return -1;
#endif
		}

#if false // TODO-GTK4
		public static int FindValue<T> (this ComboBox combo, T value)
		{
			for (var i = 0; i < combo.GetItemCount (); i++)
				if (combo.GetValueAt<T> (i)?.Equals (value) == true)
					return i;

			return -1;
		}

		public static T GetValueAt<T> (this ComboBox combo, int index)
		{
			TreeIter iter;

			// Set the tree iter to the correct row
			((ListStore) combo.Model).IterNthChild (out iter, index);

			// Retrieve the value of the first column at that row
			return (T) combo.Model.GetValue (iter, 0);
		}

		public static void SetValueAt (this ComboBox combo, int index, object value)
		{
			TreeIter iter;

			// Set the tree iter to the correct row
			((ListStore) combo.Model).IterNthChild (out iter, index);

			// Set the value of the first column at that row
			combo.Model.SetValue (iter, 0, value);
		}

		/// <summary>
		/// Gets the value in the specified column in the first selected row in a TreeView.
		/// </summary>
		public static T? GetSelectedValueAt<T> (this TreeView treeView, int column) where T : class
		{
			var paths = treeView.Selection.GetSelectedRows ();

			if (paths != null && paths.Length > 0 && treeView.Model.GetIter (out var iter, paths[0]))
				return treeView.Model.GetValue (iter, column) as T;

			return null;
		}

		/// <summary>
		/// Gets the value in the specified column in the specified row in a TreeView.
		/// </summary>
		public static T? GetValueAt<T> (this TreeView treeView, string path, int column) where T : class
		{
			if (treeView.Model.GetIter (out var iter, new TreePath (path)))
				return treeView.Model.GetValue (iter, column) as T;

			return null;
		}

		/// <summary>
		/// Sets the specified row(s) as selected in a TreeView.
		/// </summary>
		public static void SetSelectedRows (this TreeView treeView, params int[] indices)
		{
			var path = new TreePath (indices);
			treeView.Selection.SelectPath (path);
		}

		public static Gdk.Pixbuf LoadIcon (this Gtk.IconTheme theme, string icon_name, int size)
		{
			// Simple wrapper to avoid the verbose IconLookupFlags parameter.
			return theme.LoadIcon (icon_name, size, Gtk.IconLookupFlags.ForceSize);
		}

		/// <summary>
		/// Returns the Cancel / Open button pair in the correct order for the current platform.
		/// This can be used with the Gtk.Dialog constructor.
		/// </summary>
		public static object[] DialogButtonsCancelOpen ()
		{
			if (PintaCore.System.OperatingSystem == OS.Windows) {
				return new object[] {
		    Gtk.Stock.Open,
		    Gtk.ResponseType.Ok,
		    Gtk.Stock.Cancel,
		    Gtk.ResponseType.Cancel
				};
			} else {
				return new object[] {
		    Gtk.Stock.Cancel,
		    Gtk.ResponseType.Cancel,
		    Gtk.Stock.Open,
		    Gtk.ResponseType.Ok
				};
			}
		}

		/// <summary>
		/// Returns the Cancel / Save button pair in the correct order for the current platform.
		/// This can be used with the Gtk.Dialog constructor.
		/// </summary>
		public static object[] DialogButtonsCancelSave ()
		{
			if (PintaCore.System.OperatingSystem == OS.Windows) {
				return new object[] {
		    Gtk.Stock.Save,
		    Gtk.ResponseType.Ok,
		    Gtk.Stock.Cancel,
		    Gtk.ResponseType.Cancel
				};
			} else {
				return new object[] {
		    Gtk.Stock.Cancel,
		    Gtk.ResponseType.Cancel,
		    Gtk.Stock.Save,
		    Gtk.ResponseType.Ok
				};
			}
		}
#endif

		/// <summary>
		/// Returns the Cancel / Ok button pair in the correct order for the current platform.
		/// This can be used with the Gtk.Dialog constructor.
		/// </summary>
		public static void AddCancelOkButtons (this Dialog dialog)
		{
			// TODO-GTK4 - can these use the translations from GTK?
			Widget ok_button;
			if (PintaCore.System.OperatingSystem == OS.Windows) {
				ok_button = dialog.AddButton ("_OK", (int) ResponseType.Ok);
				dialog.AddButton ("_Cancel", (int) ResponseType.Cancel);
			} else {
				dialog.AddButton ("_Cancel", (int) ResponseType.Cancel);
				ok_button = dialog.AddButton ("_OK", (int) ResponseType.Ok);
			}

			ok_button.AddCssClass (AdwaitaStyles.SuggestedAction);
		}

		public static void SetDefaultResponse (this Dialog dialog, ResponseType response)
			=> dialog.SetDefaultResponse ((int) response);

		/// <summary>
		/// Helper function to avoid repeated casts. The dialog's content area is always a Box.
		/// </summary>
		public static Box GetContentAreaBox (this Dialog dialog)
			=> (Box) dialog.GetContentArea ();

		/// <summary>
		/// Returns the platform-specific label for the "Primary" (Ctrl) key.
		/// For example, this is the Cmd key on macOS.
		/// </summary>
		public static string CtrlLabel ()
		{
			AcceleratorParse (ConvertPrimaryKey ("<Primary>"), out var key, out var mods);
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

		// TODO-GTK4 - need binding from gir.core
		[DllImport (GtkLibraryName, EntryPoint = "gtk_accelerator_parse")]
		private static extern bool AcceleratorParse ([MarshalAs (UnmanagedType.LPUTF8Str)] string accelerator, out uint accelerator_key, out Gdk.ModifierType accelerator_mods);

		/// <summary>
		/// Set all four margins of the widget to the same value.
		/// </summary>
		/// <param name="w"></param>
		/// <param name="margin"></param>
		public static void SetAllMargins (this Widget w, int margin)
		{
			w.MarginTop = w.MarginBottom = w.MarginStart = w.MarginEnd = margin;
		}

		/// <summary>
		/// Helper function to return the icon theme for the default display.
		/// </summary>
		public static Gtk.IconTheme GetDefaultIconTheme () => Gtk.IconTheme.GetForDisplay (Gdk.Display.GetDefault ()!);

		/// <summary>
		/// For a combo box that has an entry, provides easy access to the child entry widget.
		/// </summary>
		public static Entry GetEntry (this ComboBox box)
		{
			if (!box.HasEntry)
				throw new InvalidOperationException ("Combobox does not have an entry");

			return (Entry) box.Child!;
		}

		// TODO-GTK4 - need gir.core binding for gtk_file_chooser_set_current_folder
		public static bool SetCurrentFolder (this FileChooser chooser, Gio.File file)
		{
			GLib.Internal.ErrorOwnedHandle error;
			bool result = Gtk.Internal.FileChooser.SetCurrentFolder (chooser.Handle, file.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return result;
		}

		// TODO-GTK4 - need gir.core binding for gtk_file_chooser_set_file
		public static bool SetFile (this FileChooser chooser, Gio.File file)
		{
			GLib.Internal.ErrorOwnedHandle error;
			bool result = Gtk.Internal.FileChooser.SetFile (chooser.Handle, file.Handle, out error);
			GLib.Error.ThrowOnError (error);
			return result;
		}

		/// <summary>
		/// Similar to gtk_dialog_run() in GTK3, this runs the dialog in a blocking manner with a nested event loop.
		/// This can be useful for compability with old code that relies on this behaviour, but new code should be
		/// structured to use event handlers.
		/// </summary>
		public static string RunBlocking (this Adw.MessageDialog dialog)
		{
			string response = "";
			GLib.MainLoop loop = new ();

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
		/// This can be useful for compability with old code that relies on this behaviour, but new code should be
		/// structured to use event handlers.
		/// </summary>
		public static ResponseType RunBlocking (this Gtk.NativeDialog dialog)
		{
			var response = ResponseType.None;
			GLib.MainLoop loop = new ();

			if (!dialog.Modal)
				dialog.Modal = true;

			dialog.OnResponse += (_, args) => {
				response = (ResponseType) args.ResponseId;
				if (loop.IsRunning ())
					loop.Quit ();
			};

			dialog.Show ();
			loop.Run ();

			return response;
		}

		/// <summary>
		/// Similar to gtk_dialog_run() in GTK3, this runs the dialog in a blocking manner with a nested event loop.
		/// This can be useful for compability with old code that relies on this behaviour, but new code should be
		/// structured to use event handlers.
		/// </summary>
		public static ResponseType RunBlocking (this Gtk.Dialog dialog)
		{
			var response = ResponseType.None;
			GLib.MainLoop loop = new ();

			if (!dialog.Modal)
				dialog.Modal = true;

			dialog.OnResponse += (_, args) => {
				response = (ResponseType) args.ResponseId;
				if (loop.IsRunning ())
					loop.Quit ();
			};

			dialog.Show ();
			loop.Run ();

			return response;
		}

		// TODO-GTK4 replace with adw_message_dialog_choose() once adwaita 1.3 is available
		public static Task<string> RunAsync (this Adw.MessageDialog dialog)
		{
			var tcs = new TaskCompletionSource<string> ();

			dialog.OnResponse += (_, args) => {
				tcs.SetResult (args.Response);
			};

			dialog.Show ();

			return tcs.Task;
		}

		// TODO-GTK4 - gir.core does not yet generate bindings for record methods
		public static bool Iteration (this GLib.MainContext context, bool may_block)
		{
			return GLib.Internal.MainContext.Iteration (context.Handle, may_block);
		}

		/// <summary>
		/// Provides convenient access to the Gdk.Key of the key being pressed.
		/// </summary>
		public static Gdk.Key GetKey (this Gtk.EventControllerKey.KeyPressedSignalArgs args) => (Gdk.Key) args.Keyval;

		/// <summary>
		/// Provides convenient access to the Gdk.Key of the key being released.
		/// </summary>
		public static Gdk.Key GetKey (this Gtk.EventControllerKey.KeyReleasedSignalArgs args) => (Gdk.Key) args.Keyval;

		/// <summary>
		/// Sets the activates-default property for the editable text field of a spin button.
		/// </summary>
		public static void SetActivatesDefault (this Gtk.SpinButton spin_button, bool activates)
		{
			Editable? editable = spin_button.GetDelegate ();
			if (editable is null)
				return;

			// TODO-GTK4 - gir.core should be able to cast to a Gtk.Text from Gtk.Editable
			var text = new TextWrapper (editable.Handle, false);
			text.SetActivatesDefault (activates);
		}

		internal class TextWrapper : Gtk.Text
		{
			internal TextWrapper (IntPtr ptr, bool ownedRef) : base (ptr, ownedRef)
			{
			}
		}

		// TODO-GTK4 - remove once Gdk.RGBA has struct bindings from gir.core
		[StructLayout (LayoutKind.Sequential)]
		private struct GdkRGBA
		{
			public float Red;
			public float Green;
			public float Blue;
			public float Alpha;
		}

		// TODO-GTK4 - remove once Gdk.RGBA has struct bindings from gir.core
		public static void GetColor (this Gtk.StyleContext context, out Cairo.Color color)
		{
			StyleContextGetColor (context.Handle, out var gdk_color);
			color = new Cairo.Color (gdk_color.Red, gdk_color.Green, gdk_color.Blue, gdk_color.Alpha);
		}

		[DllImport (GtkLibraryName, EntryPoint = "gtk_style_context_get_color")]
		private static extern void StyleContextGetColor (IntPtr handle, out GdkRGBA color);

		// TODO-GTK4 - remove once Gdk.RGBA has struct bindings from gir.core
		public static void SetColor (this Gtk.ColorChooserDialog dialog, Cairo.Color color)
		{
			ColorChooserSetRgba (dialog.Handle, new GdkRGBA () {
				Red = (float) color.R,
				Blue = (float) color.B,
				Green = (float) color.G,
				Alpha = (float) color.A
			});
		}

		[DllImport (GtkLibraryName, EntryPoint = "gtk_color_chooser_set_rgba")]
		private static extern void ColorChooserSetRgba (IntPtr handle, GdkRGBA color);

		// TODO-GTK4 - remove once Gdk.RGBA has struct bindings from gir.core
		public static void GetColor (this Gtk.ColorChooserDialog dialog, out Cairo.Color color)
		{
			ColorChooserGetRgba (dialog.Handle, out var gdk_color);
			color = new Cairo.Color (gdk_color.Red, gdk_color.Green, gdk_color.Blue, gdk_color.Alpha);
		}

		[DllImport (GtkLibraryName, EntryPoint = "gtk_color_chooser_get_rgba")]
		private static extern void ColorChooserGetRgba (IntPtr handle, out GdkRGBA color);
	}
}
