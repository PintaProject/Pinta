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
	public class EffectsManager
	{
		private List<BaseEffect> adjustments;
		private List<BaseEffect> effects;

		public EffectsManager ()
		{
			adjustments = new List<BaseEffect> ();
			effects = new List<BaseEffect> ();
		}

		// TODO: Needs to keep menu sorted
		public Gtk.Action AddAdjustment (BaseEffect adjustment)
		{
			adjustments.Add (adjustment);

			// Add icon to IconFactory
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add (adjustment.Icon, new Gtk.IconSet (PintaCore.Resources.GetIcon (adjustment.Icon)));
			fact.AddDefault ();

			// Create a gtk action for each adjustment
			Gtk.Action act = new Gtk.Action (adjustment.GetType ().Name, adjustment.Text + (adjustment.IsConfigurable ? Catalog.GetString ("...") : ""), string.Empty, adjustment.Icon);
			act.Activated += delegate (object sender, EventArgs e) { PintaCore.LivePreview.Start (adjustment); };
			
			PintaCore.Actions.Adjustments.Actions.Add (act);

			// Create a menu item for each adjustment
			((Menu)((ImageMenuItem)PintaCore.Chrome.MainMenu.Children[5]).Submenu).AppendMenuItemSorted (act.CreateAcceleratedMenuItem (adjustment.AdjustmentMenuKey, adjustment.AdjustmentMenuKeyModifiers));

			return act;
		}

		// TODO: Needs to keep menu sorted
		public Gtk.Action AddEffect (BaseEffect effect)
		{
			effects.Add (effect);

			// Add icon to IconFactory
			Gtk.IconFactory fact = new Gtk.IconFactory ();
			fact.Add (effect.Icon, new Gtk.IconSet (PintaCore.Resources.GetIcon (effect.Icon)));
			fact.AddDefault ();

			// Create a gtk action and menu item for each effect
			Gtk.Action act = new Gtk.Action (effect.GetType ().Name, effect.Text + (effect.IsConfigurable ? Catalog.GetString ("...") : ""), string.Empty, effect.Icon);
			act.Activated += delegate (object sender, EventArgs e) { PintaCore.LivePreview.Start (effect); };
			
			PintaCore.Actions.Effects.AddEffect (effect.EffectMenuCategory, act);

			return act;
		}
	}
}
