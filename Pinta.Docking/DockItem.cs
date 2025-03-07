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
using Pinta.Core;
using Pinta.Resources;

namespace Pinta.Docking;

/// <summary>
/// A dock item contains a single child widget, and can be docked at
/// various locations.
/// </summary>
public sealed class DockItem : Gtk.Box
{
	private readonly Gtk.Label label_widget;
	private readonly Gtk.Stack button_stack;
	private readonly Gtk.Button minimize_button;
	private readonly Gtk.Button maximize_button;

	/// <summary>
	/// Unique identifier for the dock item. Used e.g. when saving the dock layout to disk.
	/// </summary>
	public string UniqueName { get; }

	/// <summary>
	/// Icon name for the dock item, used when minimized.
	/// </summary>
	public string IconName { get; }

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

	public DockItem (
		Gtk.Widget child,
		string uniqueName,
		string iconName,
		bool locked = false)
	{
		Gtk.Button minimizeButton = CreateMinimizeButton (locked);
		Gtk.Button maximizeButton = CreateMaximizeButton (locked);

		Gtk.Stack buttonStack = new ();
		buttonStack.AddChild (minimizeButton);
		buttonStack.AddChild (maximizeButton);

		Gtk.Label labelWidget = CreateLabelWidget (locked);

		// --- Initialization (Gtk.Box)

		SetOrientation (Gtk.Orientation.Vertical);

		// --- Initialization

		UniqueName = uniqueName;
		IconName = iconName;

		child.Valign = Gtk.Align.Fill;
		child.Vexpand = true;

		if (!locked) {

			Gtk.Box titleLayout = GtkExtensions.BoxHorizontal ([
				labelWidget,
				buttonStack]);

			Append (titleLayout);
		}

		Append (child);

		// TODO - support dragging into floating panel?

		// --- References to keep

		minimize_button = minimizeButton;
		maximize_button = maximizeButton;

		button_stack = buttonStack;

		label_widget = labelWidget;
	}

	private Gtk.Button CreateMinimizeButton (bool locked)
	{
		Gtk.Button result = Gtk.Button.NewFromIconName (StandardIcons.WindowMinimize);
		result.AddCssClass (AdwaitaStyles.Flat);

		if (!locked)
			result.OnClicked += (o, args) => Minimize ();

		return result;
	}

	private Gtk.Button CreateMaximizeButton (bool locked)
	{
		Gtk.Button result = Gtk.Button.NewFromIconName (StandardIcons.WindowMaximize);
		result.AddCssClass (AdwaitaStyles.Flat);
		if (!locked)
			result.OnClicked += (o, args) => Maximize ();

		return result;
	}

	private static Gtk.Label CreateLabelWidget (bool locked)
	{
		if (locked)
			return new ();

		const int padding = 8;

		Gtk.Label result = new ();
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
