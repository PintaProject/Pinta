//
// AddinActions.cs
//
// Author:
//       Cameron White <cameronwhite91@gmail.com>
//
// Copyright (c) 2012 Cameron White
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

namespace Pinta.Core
{
	public class AddinActions
	{
		private GLib.Menu addins_menu = null!; // NRT - Set by RegisterActions

		public Command AddinManager { get; private set; }

		public AddinActions ()
		{
			AddinManager = new Command ("AddinManager", Translations.GetString ("Add-in Manager"),
			                               null, Resources.Icons.AddinsManage);
		}

		/// <summary>
		/// Adds a new item to the Add-ins menu.
		/// </summary>
		public void AddMenuItem (GLib.MenuItem item)
		{
			addins_menu.AppendItem (item);
		}

		/// <summary>
		/// Removes an item from the Add-ins menu.
		/// </summary>
		public void RemoveMenuItem (GLib.MenuItem item)
		{
			// TODO-GTK3 (addins)
			throw new NotImplementedException();
#if false
			addins_menu.Remove (item);
#endif
		}

		#region Initialization
		public void RegisterActions(Gtk.Application app, GLib.Menu menu)
		{
			app.AddAction(AddinManager);
			menu.AppendItem(AddinManager.CreateMenuItem());

			addins_menu = new GLib.Menu();
			menu.AppendSection(null, addins_menu);
		}
		#endregion
	}
}

