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
using System.Linq;
using Gtk;
using Mono.Unix;
using System.Collections.Generic;

namespace Pinta.Core
{
	public class WindowActions
	{
		private Menu window_menu;
		private Dictionary<RadioAction, CheckMenuItem> action_menu_items;

		public Gtk.Action SaveAll { get; private set; }
		public Gtk.Action CloseAll { get; private set; }

		public WindowActions ()
		{
			SaveAll = new Gtk.Action ("SaveAll", Catalog.GetString ("Save All"), null, Stock.Save);
			CloseAll = new Gtk.Action ("CloseAll", Catalog.GetString ("Close All"), null, Stock.Close);

			OpenWindows = new List<RadioAction> ();
			action_menu_items = new Dictionary<RadioAction,CheckMenuItem> ();
		}

		public List<RadioAction> OpenWindows { get; private set; }

		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			window_menu = menu;

			menu.Append (SaveAll.CreateAcceleratedMenuItem (Gdk.Key.A, Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.ControlMask));
			menu.Append (CloseAll.CreateAcceleratedMenuItem (Gdk.Key.W, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask));
			menu.AppendSeparator ();
		}
		#endregion

		#region Public Methods
		public RadioAction AddDocument (Document doc)
		{
			RadioAction action = new RadioAction (doc.Guid.ToString (), doc.Filename, string.Empty, null, 0);
			
			// Tie these all together as a radio group
			if (OpenWindows.Count > 0)
				action.Group = OpenWindows[0].Group;

			action.Active = true;
			action.Activated += (o, e) => { if ((o as Gtk.ToggleAction).Active) PintaCore.Workspace.SetActiveDocumentInternal (doc); };
			
			OpenWindows.Add (action);
			CheckMenuItem menuitem;

			// We only assign accelerators up to Alt-9
			if (OpenWindows.Count < 10)
				menuitem = action.CreateAcceleratedMenuItem (IntegerToNumKey (OpenWindows.Count), Gdk.ModifierType.Mod1Mask);
			else
				menuitem = (CheckMenuItem)action.CreateMenuItem ();

			action_menu_items.Add (action, menuitem);
			window_menu.Add (menuitem);

			doc.Renamed += (o, e) => { UpdateMenuLabel (action, o as Document); };
			doc.IsDirtyChanged += (o, e) => { UpdateMenuLabel (action, o as Document); };

			return action;
		}

		public void RemoveDocument (Document doc)
		{
			// Remove from our list of actions
			RadioAction act = OpenWindows.Where (p => p.Name == doc.Guid.ToString ()).FirstOrDefault ();
			OpenWindows.Remove (act);
            		act.Dispose ();

			window_menu.HideAll ();

			// Remove all the menu items from the menu
			foreach (var item in action_menu_items.Values) {
				window_menu.Remove (item);
				item.Dispose ();
			}

			action_menu_items.Clear ();

			// Recreate all of our menu items
			// I tried simply changing the accelerators, but could
			// no get it to work.
			CheckMenuItem menuitem;

			for (int i = 0; i < OpenWindows.Count; i++) {
				RadioAction action = OpenWindows[i];

				if (i < 9)
					menuitem = action.CreateAcceleratedMenuItem (IntegerToNumKey (i + 1), Gdk.ModifierType.Mod1Mask);
				else
					menuitem = (CheckMenuItem)action.CreateMenuItem ();

				action_menu_items.Add (action, menuitem);
				window_menu.Add (menuitem);
			}

			window_menu.ShowAll ();
		}
		#endregion

		#region Private Methods
		private Gdk.Key IntegerToNumKey (int i)
		{
			switch (i) {
				case 1: return Gdk.Key.Key_1;
				case 2: return Gdk.Key.Key_2;
				case 3: return Gdk.Key.Key_3;
				case 4: return Gdk.Key.Key_4;
				case 5: return Gdk.Key.Key_5;
				case 6: return Gdk.Key.Key_6;
				case 7: return Gdk.Key.Key_7;
				case 8: return Gdk.Key.Key_8;
				case 9: return Gdk.Key.Key_9;
			}

			throw new ArgumentOutOfRangeException (string.Format ("IntegerToNumKey does not support: {0}", i));
		}

		private void UpdateMenuLabel (RadioAction action, Document doc)
		{
			action.Label = string.Format ("{0}{1}", doc.Filename, doc.IsDirty ? "*" : string.Empty);
			PintaCore.Workspace.ResetTitle ();
		}
		#endregion
	}
}
