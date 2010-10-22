// 
// WindowActions.cs
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
using Gtk;
using Mono.Unix;

namespace Pinta.Core
{
	public class WindowActions
	{
		public Gtk.Action SaveAll { get; private set; }
		public Gtk.Action CloseAll { get; private set; }

		public WindowActions ()
		{
			SaveAll = new Gtk.Action ("SaveAll", Catalog.GetString ("Save All"), null, Stock.Save);
			CloseAll = new Gtk.Action ("CloseAll", Catalog.GetString ("Close All"), null, Stock.Close);
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			//menu.Append (SaveAll.CreateAcceleratedMenuItem (Gdk.Key.L, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			//menu.Append (CloseAll.CreateAcceleratedMenuItem (Gdk.Key.W, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			//menu.AppendSeparator ();
		}
		#endregion
	}
}
