//
// Command.cs
//
// Author:
//       Cameron White <cameronwhite91@gmail.com>
//
// Copyright (c) 2010 Cameron White
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
using Gio;
using GObject;

namespace Pinta.Core;

/// <summary>
/// Wrapper around a Glib.SimpleAction to store additional data such as the label.
/// </summary>
public class Command
{
	public Gio.SimpleAction Action { get; }
	public string Name => Action.Name!;
	public SignalHandler<SimpleAction, SimpleAction.ActivateSignalArgs>? Activated;
	public void Activate ()
	{
		Action.Activate (null);
	}

	public string Label { get; }
	public string? ShortLabel { get; set; }
	public string? Tooltip { get; }
	public string? IconName { get; }
	public string FullName => $"app.{Name}";
	public bool IsImportant { get; set; } = false;
	public bool Sensitive { get => Action.Enabled; set => Action.Enabled = value; }

	public Command (string name, string label, string? tooltip, string? icon_name, GLib.Variant? state = null)
	{
		if (state is not null)
			Action = Gio.SimpleAction.NewStateful (name, null, state);
		else
			Action = Gio.SimpleAction.New (name, null);

		Action.OnActivate += (o, args) => {
			Activated?.Invoke (o, args);
		};

		Label = label;
		Tooltip = tooltip;
		IconName = icon_name;
	}

	public Gio.MenuItem CreateMenuItem ()
	{
		return Gio.MenuItem.New (Label, FullName);
	}
}

public sealed class ToggleCommand : Command
{
	public ToggleCommand (string name, string label, string? tooltip, string? stock_id)
	    : base (name, label, tooltip, stock_id, GLib.Variant.NewBoolean (false))
	{
		Activated += (o, args) => {
			bool active = !State.GetBoolean ();
			Toggled?.Invoke (active, interactive: true);
			Action.ChangeState (GLib.Variant.NewBoolean (active));
		};
	}

	public bool Value {
		get => State.GetBoolean ();
		set {
			if (value != Value) {
				Toggled?.Invoke (value, interactive: false);
				Action.ChangeState (GLib.Variant.NewBoolean (value));
			}
		}
	}

	public delegate void ToggledHandler (bool value, bool interactive);
	public ToggledHandler? Toggled;

	private GLib.Variant State => Action.GetState () ?? throw new InvalidOperationException ("Action should not be stateless!");
}
