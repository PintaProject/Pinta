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

using System.Collections.Generic;
using System.Linq;

namespace Pinta.Core;

partial class GtkExtensions
{
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
}
