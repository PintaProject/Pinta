//
// Author:
//       Cameron White <cameronwhite91@gmail.com>
//
// Copyright (c) 2020 Cameron White
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
using System.Diagnostics.CodeAnalysis;
using Pinta.Core;
using Pinta.Resources;

namespace Pinta.Docking;

/// <summary>
/// A dock item contains a single child widget, and can be docked at
/// various locations.
/// </summary>
[GObject.Subclass<Gtk.Box>]
public sealed partial class DockItem
{
	private Gtk.Label label_widget;
	private Gtk.Stack button_stack;
	private Gtk.Button minimize_button;
	private Gtk.Button maximize_button;

	/// <summary>
	/// Unique identifier for the dock item. Used e.g. when saving the dock layout to disk.
	/// </summary>
	public string UniqueName { get; private set; } = string.Empty;

	/// <summary>
	/// Icon name for the dock item, used when minimized.
	/// </summary>
	public string IconName { get; private set; } = string.Empty;

	/// <summary>
	/// Visible label for the dock item.
	/// </summary>
	public string Label {
		get => label_widget.GetLabel ();
		set => label_widget.SetLabel (value);
	}

	/// <summary>
	/// Triggered when the minimize button is pressed.
	/// </summary>
	public event EventHandler? MinimizeClicked;

	/// <summary>
	/// Triggered when the maximize button is pressed.
	/// </summary>
	public event EventHandler? MaximizeClicked;

	[MemberNotNull (nameof (label_widget))]
	[MemberNotNull (nameof (button_stack))]
	[MemberNotNull (nameof (minimize_button))]
	[MemberNotNull (nameof (maximize_button))]
	partial void Initialize ()
	{
		Gtk.Button minimizeButton = CreateMinimizeButton ();
		Gtk.Button maximizeButton = CreateMaximizeButton ();

		Gtk.Stack buttonStack = Gtk.Stack.New ();
		buttonStack.AddChild (minimizeButton);
		buttonStack.AddChild (maximizeButton);

		Gtk.Label labelWidget = CreateLabelWidget ();

		// --- Initialization (Gtk.Box)

		SetOrientation (Gtk.Orientation.Vertical);

		// TODO - support dragging into floating panel?

		// --- References to keep

		minimize_button = minimizeButton;
		maximize_button = maximizeButton;

		button_stack = buttonStack;

		label_widget = labelWidget;
	}

	public static DockItem New (
		Gtk.Widget child,
		string uniqueName,
		string iconName,
		bool locked = false)
	{
		DockItem item = NewWithProperties ([]);

		item.UniqueName = uniqueName;
		item.IconName = iconName;

		if (!locked) {
			item.minimize_button.OnClicked += (o, args) => item.Minimize ();
			item.maximize_button.OnClicked += (o, args) => item.Maximize ();

			const int padding = 8;
			item.label_widget.MarginStart = item.label_widget.MarginEnd = padding;
			item.label_widget.Hexpand = true;
			item.label_widget.Halign = Gtk.Align.Start;

			Gtk.Box titleLayout = GtkExtensions.BoxHorizontal ([
				item.label_widget,
				item.button_stack]);

			item.Append (titleLayout);
		}

		child.Valign = Gtk.Align.Fill;
		child.Vexpand = true;
		item.Append (child);

		return item;
	}

	private Gtk.Button CreateMinimizeButton ()
	{
		Gtk.Button result = Gtk.Button.NewFromIconName (StandardIcons.WindowMinimize);
		result.AddCssClass (AdwaitaStyles.Flat);
		return result;
	}

	private Gtk.Button CreateMaximizeButton ()
	{
		Gtk.Button result = Gtk.Button.NewFromIconName (StandardIcons.WindowMaximize);
		result.AddCssClass (AdwaitaStyles.Flat);
		return result;
	}

	private static Gtk.Label CreateLabelWidget ()
	{
		const int padding = 8;

		Gtk.Label result = Gtk.Label.New (null);
		result.MarginStart = result.MarginEnd = padding;
		result.Hexpand = true;
		result.Halign = Gtk.Align.Start;

		return result;
	}

	/// <summary>
	/// Create a toolbar and add it to the bottom of the dock item.
	/// </summary>
	public Gtk.Box AddToolBar ()
	{
		Gtk.Box toolbar = GtkExtensions.CreateToolBar ();
		toolbar.Spacing = -4;
		Append (toolbar);
		return toolbar;
	}

	/// <summary>
	/// Minimize the dock item.
	/// </summary>
	public void Minimize ()
	{
		if (button_stack.VisibleChild == maximize_button)
			return;

		button_stack.VisibleChild = maximize_button;
		MinimizeClicked?.Invoke (this, new EventArgs ());
	}

	/// <summary>
	/// Maximize the dock item.
	/// </summary>
	public void Maximize ()
	{
		if (button_stack.VisibleChild == minimize_button)
			return;

		button_stack.VisibleChild = minimize_button;
		MaximizeClicked?.Invoke (this, new EventArgs ());
	}
}
