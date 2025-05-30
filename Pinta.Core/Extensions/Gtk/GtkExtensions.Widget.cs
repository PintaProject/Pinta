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
using Pinta.Resources;

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


partial class GtkExtensions
{
	public static Gtk.Box BoxHorizontal (ReadOnlySpan<Gtk.Widget> children)
		=> Box (BoxStyle.Horizontal, children);

	public static Gtk.Box BoxVertical (ReadOnlySpan<Gtk.Widget> children)
		=> Box (BoxStyle.Vertical, children);

	public static Gtk.Box Box (
		BoxStyle style,
		ReadOnlySpan<Gtk.Widget> children) // TODO: Add 'params' keyword when updated to C#13
	{
		Gtk.Box stack = new ();

		// --- Mandatory
		stack.SetOrientation (style.Orientation);

		// --- Optional
		if (style.Spacing.HasValue) stack.Spacing = style.Spacing.Value;
		if (style.CssClass is not null) stack.AddCssClass (style.CssClass);

		stack.AppendMultiple (children);

		return stack;
	}

	public static void AppendMultiple (
		this Gtk.Box box,
		ReadOnlySpan<Gtk.Widget> children) // TODO: Add 'params' keyword when updated to C#13
	{
		foreach (var child in children)
			box.Append (child);
	}

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
			if (!PintaCore.Workspace.HasOpenDocuments) return;
			PintaCore.Workspace.ActiveWorkspace.GrabFocusToCanvas ();
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
		slider.SetCssClasses ([Styles.ToolBarScale]);
		return slider;
	}

	public static void Toggle (this Gtk.ToggleButton button)
	{
		button.Active = !button.Active;
	}


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

	/// <summary>
	/// Helper function to avoid repeated casts. The dialog's content area is always a Box.
	/// </summary>
	public static Gtk.Box GetContentAreaBox (this Gtk.Dialog dialog)
		=> (Gtk.Box) dialog.GetContentArea ();

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
	/// For a combo box that has an entry, provides easy access to the child entry widget.
	/// </summary>
	public static Gtk.Entry GetEntry (this Gtk.ComboBox box)
	{
		if (!box.HasEntry)
			throw new InvalidOperationException ("Combobox does not have an entry");

		return (Gtk.Entry) box.Child!;
	}

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
		Gtk.Text text = (Gtk.Text) GObject.Internal.InstanceWrapper.WrapHandle<Gtk.Text> (editable.Handle.DangerousGetHandle (), ownedRef: false);
		text.SetActivatesDefault (activates);
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

		dest_pos = new (x, y);

		return result;
	}

	// TODO-GTK4 (bindings) - structs are not generated (https://github.com/gircore/gir.core/issues/622)
	public static void GetColor (this Gtk.StyleContext context, out Cairo.Color color)
	{
		StyleContextGetColor (context.Handle.DangerousGetHandle (), out var gdk_color);
		color = new Cairo.Color (gdk_color.Red, gdk_color.Green, gdk_color.Blue, gdk_color.Alpha);
	}

	/// <summary>
	/// Checks whether the mousePos (which is relative to topwidget) is within the area and returns its relative position to the area.
	/// </summary>
	/// <param name="widget">Drawing area where returns true if mouse inside.</param>
	/// <param name="topWidget">The top widget. This is what the mouse position is relative to.</param>
	/// <param name="mousePos">Position of the mouse relative to the top widget, usually obtained from Gtk.GestureClick</param>
	/// <param name="relPos">Position of the mouse relative to the drawing area.</param>
	/// <returns>Whether or not mouse position is within the drawing area.</returns>
	public static bool IsMouseInDrawingArea (this Gtk.Widget widget, Gtk.Widget topWidget, PointD mousePos, out PointD relPos)
	{
		widget.TranslateCoordinates (topWidget, 0, 0, out double x, out double y);
		relPos = new PointD ((mousePos.X - x), (mousePos.Y - y));
		if (relPos.X >= 0 && relPos.X <= widget.GetWidth () && relPos.Y >= 0 && relPos.Y <= widget.GetHeight ())
			return true;
		return false;
	}
}

