// 
// ConfigurableEffectHelper.cs
//  
// Author:
//       Greg Lowe <greg@vis.net.nz>
// 
// Copyright (c) 2010 Greg Lowe
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
using Mono.Addins;
using Mono.Addins.Localization;
using Pinta.Core;
using Pinta.Gui.Widgets;
using System.ComponentModel;

namespace Pinta
{

	public static class EffectHelper
	{
		/// <summary>
		/// Launchs an effect dialog.
		/// </summary>
		/// <param name="localizer">
		/// The localizer for the effect add-in. This is used to fetch translations for the
		/// strings in the dialog.
		/// </param>
		public static bool LaunchSimpleEffectDialog (BaseEffect effect, AddinLocalizer localizer)
		{
			return LaunchSimpleEffectDialog (effect, new AddinLocalizerWrapper (localizer));
		}

		/// <summary>
		/// Launchs an effect dialog using Pinta's translation template.
		/// </summary>
		internal static bool LaunchSimpleEffectDialog (BaseEffect effect)
		{
			return LaunchSimpleEffectDialog (effect, new PintaLocalizer ());
		}

		/// <summary>
		/// Helper function for the above methods. The IAddinLocalizer provides a generic way to
		/// get translated strings both for Pinta's effects and for effect add-ins.
		/// </summary>
		private static bool LaunchSimpleEffectDialog (BaseEffect effect, IAddinLocalizer localizer)
		{
			if (effect == null)
				throw new ArgumentNullException ("effect");
			
			if (effect.EffectData == null)
				throw new ArgumentException ("effect.EffectData is null.");
			
			var dialog = new SimpleEffectDialog (effect.Name,
			                                     PintaCore.Resources.GetIcon (effect.Icon),
			                                     effect.EffectData, localizer);
			
			// Hookup event handling for live preview.
			dialog.EffectDataChanged += (o, e) => {
				if (effect.EffectData != null)
					effect.EffectData.FirePropertyChanged (e.PropertyName);
			};
			
			int response = dialog.Run ();

			bool ret = false;
			if (response == (int)Gtk.ResponseType.Ok && effect.EffectData != null)
				ret = !effect.EffectData.IsDefault;

			dialog.Destroy ();

			return ret;
		}

		/// <summary>
		/// Wrapper around the AddinLocalizer of an add-in.
		/// </summary>
		private class AddinLocalizerWrapper : IAddinLocalizer
		{
			private AddinLocalizer localizer;

			public AddinLocalizerWrapper (AddinLocalizer localizer)
			{
				this.localizer = localizer;
			}

			public string GetString (string msgid)
			{
				return localizer.GetString (msgid);
			}
		};
	}
}
