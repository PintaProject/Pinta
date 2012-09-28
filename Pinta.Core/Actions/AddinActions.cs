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
using Mono.Unix;

namespace Pinta.Core
{
	public class AddinActions
	{
		private Menu addins_menu;

		public Gtk.Action AddinManager { get; private set; }

		public AddinActions ()
		{
			AddinManager = new Gtk.Action ("AddinManager", Catalog.GetString ("Add-in Manager"),
			                               null, "Menu.Edit.Addins.png");
		}

		/// <summary>
		/// Adds a new item to the Add-ins menu.
		/// </summary>
		public void AddMenuItem (Widget item)
		{
			addins_menu.Add (item);
		}

		/// <summary>
		/// Removes an item from the Add-ins menu.
		/// </summary>
		public void RemoveMenuItem (Widget item)
		{
			addins_menu.Remove (item);
		}

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			addins_menu = menu;

			menu.Append (AddinManager.CreateMenuItem ());
			menu.AppendSeparator ();
		}
		#endregion
	}
}

