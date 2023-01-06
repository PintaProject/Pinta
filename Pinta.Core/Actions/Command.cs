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

namespace Pinta.Core
{
	/// <summary>
	/// Wrapper around a Glib.SimpleAction to store additional data such as the label.
	/// </summary>
	public class Command
	{
		public Gio.SimpleAction Action { get; private set; }
		public string Name {
			get { return Action.Name!; }
		}
		public SignalHandler<SimpleAction, SimpleAction.ActivateSignalArgs>? Activated;
		public void Activate ()
		{
			Action.Activate (null);
		}

		public string Label { get; private set; }
		public string? ShortLabel { get; set; }
		public string? Tooltip { get; private set; }
		public string? IconName { get; private set; }
		public string FullName { get { return string.Format ("app.{0}", Name); } }
		public bool IsImportant { get; set; } = false;

		// TODO-GTK4 - the Enabled properly should be get/set. This was a regression in gir.core 0.2
		public bool Sensitive { get { return Action.Enabled; } set { Action.SetEnabled(value); } }

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

	public class ToggleCommand : Command
	{
		public ToggleCommand (string name, string label, string? tooltip, string? stock_id)
		    : base (name, label, tooltip, stock_id, CreateBoolVariant (false))
		{
			Activated += (o, args) => {
				var active = !GetBoolValue (Action.GetState ());
				Toggled?.Invoke (active);
				Action.ChangeState (CreateBoolVariant (active));
			};
		}

		public bool Value {
			get { return GetBoolValue (Action.GetState ()); }
			set {
				if (value != Value) {
					Toggled?.Invoke (value);
					Action.ChangeState (CreateBoolVariant (value));
				}
			}
		}

		public delegate void ToggledHandler (bool value);
		public ToggledHandler? Toggled;

		// TODO-GTK4 - these should be in gir.core
		private static GLib.Variant CreateBoolVariant (bool v) => new GLib.Variant (GLib.Internal.Variant.NewBoolean (v));
		private static bool GetBoolValue (GLib.Variant val) => GLib.Internal.Variant.GetBoolean (val.Handle);
	}
}
