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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pinta.Core;

public sealed class EffectsActions
{
	public Dictionary<string, Gio.Menu> Menus { get; } = new ();
	public Collection<Command> Actions { get; } = new ();

	private readonly ChromeManager chrome;
	public EffectsActions (ChromeManager chrome)
	{
		this.chrome = chrome;
	}

	#region Initialization
	public void AddEffect (string category, Command action)
	{
		var effects_menu = chrome.EffectsMenu;

		if (!Menus.ContainsKey (category)) {
			var category_menu = Gio.Menu.New ();
			effects_menu.AppendMenuItemSorted (Gio.MenuItem.NewSubmenu (Translations.GetString (category), category_menu));
			Menus.Add (category, category_menu);
		}

		Actions.Add (action);

		Gio.Menu m = Menus[category];
		m.AppendMenuItemSorted (action.CreateMenuItem ());
	}

	// TODO: Remove menu category if empty
	internal void RemoveEffect (string category, Command action)
	{
		if (!Menus.ContainsKey (category))
			return;

		var menu = Menus[category];
		menu.Remove (action);
	}
	#endregion

	#region Public Methods
	public void ToggleActionsSensitive (bool sensitive)
	{
		foreach (Command a in Actions)
			a.Sensitive = sensitive;
	}
	#endregion
}
