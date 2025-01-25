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

using System.Collections.Generic;

namespace Pinta.Core;

/// <summary>
/// Provides methods for registering and unregistering effects and adjustments.
/// </summary>
public sealed class EffectsManager
{
	private readonly Dictionary<BaseEffect, Command> adjustments;
	private readonly Dictionary<BaseEffect, Command> effects;

	private readonly ActionManager action_manager;
	private readonly ChromeManager chrome_manager;
	private readonly LivePreviewManager live_preview_manager;
	internal EffectsManager (
		ActionManager actionManager,
		ChromeManager chromeManager,
		LivePreviewManager livePreviewManager)
	{
		adjustments = [];
		effects = [];

		action_manager = actionManager;
		chrome_manager = chromeManager;
		live_preview_manager = livePreviewManager;
	}

	/// <summary>
	/// Register a new adjustment with Pinta, causing it to be added to the Adjustments menu.
	/// </summary>
	/// <param name="adjustment">The adjustment to register</param>
	/// <returns>The action created for this adjustment</returns>
	public void RegisterAdjustment (BaseEffect adjustment)
	{
#if false // For testing purposes to detect any missing icons. This implies more disk accesses on startup so we may not want this on by default.
		if (!GtkExtensions.GetDefaultIconTheme ().HasIcon (adjustment.Icon))
			Console.Error.WriteLine ($"Icon {adjustment.Icon} for adjustment {adjustment.Name} not found");
#endif

		// Create a gtk action for each adjustment
		Command act = new (
			adjustment.GetType ().Name,
			adjustment.Name + (adjustment.IsConfigurable ? Translations.GetString ("...") : ""),
			string.Empty,
			adjustment.Icon);
		act.Activated += (o, args) => { live_preview_manager.Start (adjustment); };

		action_manager.Adjustments.Actions.Add (act);

		// If no key is specified, don't use an accelerated menu item
		if (adjustment.AdjustmentMenuKey is null)
			chrome_manager.Application.AddAction (act);
		else {
			chrome_manager.Application.AddAccelAction (act, adjustment.AdjustmentMenuKeyModifiers + adjustment.AdjustmentMenuKey);
		}

		chrome_manager.AdjustmentsMenu.AppendMenuItemSorted (act.CreateMenuItem ());

		adjustments.Add (adjustment, act);
	}

	/// <summary>
	/// Register a new effect with Pinta, causing it to be added to the Effects menu.
	/// </summary>
	/// <param name="effect">The effect to register</param>
	/// <returns>The action created for this effect</returns>
	public void RegisterEffect (BaseEffect effect)
	{
#if false // For testing purposes to detect any missing icons. This implies more disk accesses on startup so we may not want this on by default.
		if (!GtkExtensions.GetDefaultIconTheme ().HasIcon (effect.Icon))
			Console.Error.WriteLine ($"Icon {effect.Icon} for effect {effect.Name} not found");
#endif

		// Create a gtk action and menu item for each effect
		Command act = new (
			effect.GetType ().Name,
			effect.Name + (effect.IsConfigurable ? Translations.GetString ("...") : ""),
			string.Empty,
			effect.Icon);
		chrome_manager.Application.AddAction (act);
		act.Activated += (o, args) => live_preview_manager.Start (effect);

		action_manager.Effects.AddEffect (effect.EffectMenuCategory, act);

		effects.Add (effect, act);
	}

	/// <summary>
	/// Unregister an effect with Pinta, causing it to be removed from the Effects menu.
	/// </summary>
	/// <param name="effect_type">The type of the effect to unregister</param>
	public void UnregisterInstanceOfEffect (System.Type effect_type)
	{
		foreach (BaseEffect effect in effects.Keys) {

			if (effect.GetType () != effect_type)
				continue;

			var action = effects[effect];

			effects.Remove (effect);
			action_manager.Effects.RemoveEffect (effect.EffectMenuCategory, action);
			return;
		}
	}

	/// <summary>
	/// Unregister an effect with Pinta, causing it to be removed from the Adjustments menu.
	/// </summary>
	/// <param name="adjustment_type">The type of the adjustment to unregister</param>
	public void UnregisterInstanceOfAdjustment (System.Type adjustment_type)
	{
		foreach (BaseEffect adjustment in adjustments.Keys) {

			if (adjustment.GetType () != adjustment_type)
				continue;

			var action = adjustments[adjustment];

			adjustments.Remove (adjustment);
			action_manager.Adjustments.Actions.Remove (action);
			chrome_manager.AdjustmentsMenu.Remove (action);

			return;
		}
	}
}
