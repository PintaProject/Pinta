// 
// EffectsActions.cs
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
using System.Collections.Generic;

namespace Pinta.Core
{
	public class EffectsActions
	{
		private Menu effects_menu;
		
		public Dictionary<string, Gtk.Menu> Menus { get; private set; }
		public List<Gtk.Action> Actions { get; private set; }

		public EffectsActions ()
		{
			Actions = new List<Gtk.Action> ();
			Menus = new Dictionary<string,Menu> ();
		}
		
		#region Initialization
		public void CreateMainMenu (Gtk.Menu menu)
		{
			effects_menu = menu;
		}

		public void AddEffect (string category, Gtk.Action action)
		{
			if (!Menus.ContainsKey (category)) {
				Gtk.Action menu_action = new Gtk.Action (category, Mono.Unix.Catalog.GetString (category), null, null);
				Menu category_menu = (Menu)effects_menu.AppendItem (menu_action.CreateSubMenuItem ()).Submenu;
				
				Menus.Add (category, category_menu);
			}
			
			Menu m = Menus[category];
			
			Actions.Add (action);
			m.Append (action.CreateMenuItem ());
		}
		#endregion

		#region Public Methods
		public void ToggleActionsSensitive (bool sensitive)
		{
			foreach (Gtk.Action a in Actions)
				a.Sensitive = sensitive;
		}
		#endregion
	}
}
