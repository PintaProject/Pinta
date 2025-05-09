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

namespace Pinta.Core;

/// <summary>
/// Provides methods for registering and unregistering effects and adjustments.
/// </summary>
public sealed class EffectsManager
{
	private readonly Dictionary<Type, Command> adjustments;

	private readonly Dictionary<Type, Command> effects;
	private readonly Dictionary<Type, string> effects_categories;

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
		effects_categories = [];

		action_manager = actionManager;
		chrome_manager = chromeManager;
		live_preview_manager = livePreviewManager;
	}

	/// <summary>
	/// Register a new adjustment with Pinta, causing it to be added to the Adjustments menu.
	/// </summary>
	/// <param name="adjustment">The adjustment to register</param>
	/// <returns>The action created for this adjustment</returns>
	public void RegisterAdjustment<T> (T adjustment) where T : BaseEffect
	{
#if false // For testing purposes to detect any missing icons. This implies more disk accesses on startup so we may not want this on by default.
		if (!GtkExtensions.GetDefaultIconTheme ().HasIcon (adjustment.Icon))
			Console.Error.WriteLine ($"Icon {adjustment.Icon} for adjustment {adjustment.Name} not found");
#endif
		Type adjustmentType = typeof (T);
		RegisterAdjustmentInternal (adjustment, adjustmentType);
	}

	/// <remarks>
	/// Overload provided for compatibility with older add-ins
	/// </remarks>
	public void RegisterAdjustment (BaseEffect adjustment)
	{
#if false // For testing purposes to detect any missing icons. This implies more disk accesses on startup so we may not want this on by default.
		if (!GtkExtensions.GetDefaultIconTheme ().HasIcon (adjustment.Icon))
			Console.Error.WriteLine ($"Icon {adjustment.Icon} for adjustment {adjustment.Name} not found");
#endif
		Type adjustmentType = adjustment.GetType ();
		RegisterAdjustmentInternal (adjustment, adjustmentType);
	}

	private void RegisterAdjustmentInternal (BaseEffect adjustment, Type adjustmentType)
	{
		if (adjustments.ContainsKey (adjustmentType))
			throw new Exception ($"An adjustment of type {adjustmentType} is already registered");

		// Create a gtk action for each adjustment
		Command action = new (
			adjustmentType.Name,
			adjustment.Name + (adjustment.IsConfigurable ? Translations.GetString ("...") : ""),
			string.Empty,
			adjustment.Icon,
			shortcuts:
				adjustment.AdjustmentMenuKey is null
				? [] // If no key is specified, don't use an accelerated menu item
				: [adjustment.AdjustmentMenuKeyModifiers + adjustment.AdjustmentMenuKey]);

		action.Activated += (o, args) => { live_preview_manager.Start (adjustment); };

		action_manager.Adjustments.Actions.Add (action);

		chrome_manager.Application.AddCommand (action);

		chrome_manager.AdjustmentsMenu.AppendMenuItemSorted (action.CreateMenuItem ());

		adjustments.Add (adjustmentType, action);
	}

	/// <summary>
	/// Register a new effect with Pinta, causing it to be added to the Effects menu.
	/// </summary>
	/// <param name="effect">The effect to register</param>
	/// <returns>The action created for this effect</returns>
	public void RegisterEffect<T> (T effect) where T : BaseEffect
	{
#if false // For testing purposes to detect any missing icons. This implies more disk accesses on startup so we may not want this on by default.
		if (!GtkExtensions.GetDefaultIconTheme ().HasIcon (effect.Icon))
			Console.Error.WriteLine ($"Icon {effect.Icon} for effect {effect.Name} not found");
#endif
		Type effectType = typeof (T);
		RegisterEffectInternal (effect, effectType);
	}

	/// <remarks>
	/// Overload provided for compatibility with older add-ins
	/// </remarks>
	public void RegisterEffect (BaseEffect effect)
	{
#if false // For testing purposes to detect any missing icons. This implies more disk accesses on startup so we may not want this on by default.
		if (!GtkExtensions.GetDefaultIconTheme ().HasIcon (effect.Icon))
			Console.Error.WriteLine ($"Icon {effect.Icon} for effect {effect.Name} not found");
#endif
		Type effectType = effect.GetType ();
		RegisterEffectInternal (effect, effectType);
	}

	private void RegisterEffectInternal (BaseEffect effect, Type effectType)
	{
		if (effects.ContainsKey (effectType))
			throw new Exception ($"An effect of type {effectType} is already registered");

		// Create a gtk action and menu item for each effect
		Command action = new (
			effectType.Name,
			effect.Name + (effect.IsConfigurable ? Translations.GetString ("...") : ""),
			string.Empty,
			effect.Icon);

		chrome_manager.Application.AddCommand (action);
		action.Activated += (o, args) => live_preview_manager.Start (effect);

		action_manager.Effects.AddEffect (effect.EffectMenuCategory, action);

		effects.Add (effectType, action);
		effects_categories.Add (effectType, effect.EffectMenuCategory);
	}

	/// <summary>
	/// Unregister an effect with Pinta, causing it to be removed from the Effects menu.
	/// </summary>
	/// <param name="effect_type">The type of the effect to unregister</param>
	public void UnregisterInstanceOfEffect<T> () where T : BaseEffect
	{
		Type effectType = typeof (T);
		UnregisterInstanceOfEffect (effectType);
	}

	/// <remarks>
	/// Overload provided for compatibility with older add-ins
	/// </remarks>
	public void UnregisterInstanceOfEffect (Type effectType)
	{
		if (!effects.TryGetValue (effectType, out var action))
			return;

		string category = effects_categories[effectType];

		effects.Remove (effectType);
		action_manager.Effects.RemoveEffect (category, action);
		effects_categories.Remove (effectType);
	}

	/// <summary>
	/// Unregister an effect with Pinta, causing it to be removed from the Adjustments menu.
	/// </summary>
	/// <param name="adjustment_type">The type of the adjustment to unregister</param>
	public void UnregisterInstanceOfAdjustment<T> () where T : BaseEffect
	{
		Type adjustmentType = typeof (T);
		UnregisterInstamceOfAdjustment (adjustmentType);
	}

	/// <remarks>
	/// Overload provided for compatibility with older add-ins
	/// </remarks>
	public void UnregisterInstamceOfAdjustment (Type adjustmentType)
	{
		if (!adjustments.TryGetValue (adjustmentType, out var action))
			return;

		adjustments.Remove (adjustmentType);
		action_manager.Adjustments.Actions.Remove (action);
		chrome_manager.AdjustmentsMenu.Remove (action);
	}
}
