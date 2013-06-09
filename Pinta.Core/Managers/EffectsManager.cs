// 
// EffectsManager.cs
//  
// Author:
//	Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2011 Jonathan Pobst
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
using System.Collections.Generic;
using Gtk;
using Mono.Unix;

namespace Pinta.Core
{
	/// <summary>
	/// Provides methods for registering and unregistering effects and adjustments.
	/// </summary>
	public class EffectsManager
	{
		private Dictionary<BaseEffect, Gtk.Action> adjustments;
		private Dictionary<BaseEffect, MenuItem> adjustment_menuitems;
		private Dictionary<BaseEffect, Gtk.Action> effects;

		internal EffectsManager ()
		{
			adjustments = new Dictionary<BaseEffect, Gtk.Action> ();
			adjustment_menuitems = new Dictionary<BaseEffect,MenuItem> ();
			effects = new Dictionary<BaseEffect, Gtk.Action> ();
		}

		/// <summary>
		/// Register a new adjustment with Pinta, causing it to be added to the Adjustments menu.
		/// </summary>
		/// <param name="adjustment">The adjustment to register</param>
		/// <returns>The action created for this adjustment</returns>
		public Gtk.Action RegisterAdjustment (BaseEffect adjustment)
		{
			// Add icon to IconFactory
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add (adjustment.Icon, new Gtk.IconSet (PintaCore.Resources.GetIcon (adjustment.Icon)));
			fact.AddDefault ();

			// Create a gtk action for each adjustment
			Gtk.Action act = new Gtk.Action (adjustment.GetType ().Name, adjustment.Name + (adjustment.IsConfigurable ? Catalog.GetString ("...") : ""), string.Empty, adjustment.Icon);
			act.Activated += delegate (object sender, EventArgs e) { PintaCore.LivePreview.Start (adjustment); };
			
			PintaCore.Actions.Adjustments.Actions.Add (act);

			// Create a menu item for each adjustment
			MenuItem menu_item;
			
			// If no key is specified, don't use an accelerated menu item
			if (adjustment.AdjustmentMenuKey == (Gdk.Key)0)
				menu_item = (MenuItem)act.CreateMenuItem ();
			else
				menu_item = act.CreateAcceleratedMenuItem (adjustment.AdjustmentMenuKey, adjustment.AdjustmentMenuKeyModifiers);

			((Menu)((ImageMenuItem)PintaCore.Chrome.MainMenu.Children[5]).Submenu).AppendMenuItemSorted (menu_item);

			adjustments.Add (adjustment, act);
			adjustment_menuitems.Add (adjustment, menu_item);

			return act;
		}

		/// <summary>
		/// Register a new effect with Pinta, causing it to be added to the Effects menu.
		/// </summary>
		/// <param name="effect">The effect to register</param>
		/// <returns>The action created for this effect</returns>
		public Gtk.Action RegisterEffect (BaseEffect effect)
		{
			// Add icon to IconFactory
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add (effect.Icon, new Gtk.IconSet (PintaCore.Resources.GetIcon (effect.Icon)));
			fact.AddDefault ();

			// Create a gtk action and menu item for each effect
			Gtk.Action act = new Gtk.Action (effect.GetType ().Name, effect.Name + (effect.IsConfigurable ? Catalog.GetString ("...") : ""), string.Empty, effect.Icon);
			act.Activated += delegate (object sender, EventArgs e) { PintaCore.LivePreview.Start (effect); };
			
			PintaCore.Actions.Effects.AddEffect (effect.EffectMenuCategory, act);
			
			effects.Add (effect, act);

			return act;
		}

		/// <summary>
		/// Unregister an effect with Pinta, causing it to be removed from the Effects menu.
		/// </summary>
		/// <param name="effect_type">The type of the effect to unregister</param>
		public void UnregisterInstanceOfEffect (System.Type effect_type)
		{
			foreach (BaseEffect effect in effects.Keys) {
				if (effect.GetType () == effect_type) {
					var action = effects[effect];

					effects.Remove (effect);
					PintaCore.Actions.Effects.RemoveEffect (effect.EffectMenuCategory, action);
					return;
				}
			}
		}

		/// <summary>
		/// Unregister an effect with Pinta, causing it to be removed from the Adjustments menu.
		/// </summary>
		/// <param name="adjustment_type">The type of the adjustment to unregister</param>
		public void UnregisterInstanceOfAdjustment (System.Type adjustment_type)
		{
			foreach (BaseEffect adjustment in adjustments.Keys) {
				if (adjustment.GetType () == adjustment_type) {

					var action = adjustments[adjustment];
					var menu_item = adjustment_menuitems[adjustment];

					adjustments.Remove (adjustment);
					PintaCore.Actions.Adjustments.Actions.Remove (action);

					((Menu)((ImageMenuItem)PintaCore.Chrome.MainMenu.Children[5]).Submenu).Remove (menu_item);
					return;
				}
			}
		}
	}
}
