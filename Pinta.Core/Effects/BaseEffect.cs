// 
// BaseEffect.cs
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
using Cairo;
using Mono.Unix;
using Mono.Addins;
using Pinta.Core;

[assembly: AddinRoot ("Pinta", PintaCore.ApplicationVersion)]

namespace Pinta.Core
{
	/// <summary>
	/// The base class for all effects and adjustments.
	/// </summary>
	[TypeExtensionPoint]
	public abstract class BaseEffect
	{
		/// <summary>
		/// Returns the name of the effect, displayed to the user in the Adjustments/Effects menu and history pad.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Returns the icon to use for the effect in the Adjustments/Effects menu and history pad.
		/// </summary>
		public virtual string Icon { get { return "Menu.Effects.Default.png"; } }

		/// <summary>
		/// Returns whether this effect can display a configuration dialog to the user. (Implemented by LaunchConfiguration ().)
		/// </summary>
		public virtual bool IsConfigurable { get { return false; } }

		/// <summary>
		/// Returns the keyboard shortcut for this adjustment. Only affects adjustments, not effects. Default is no shortcut.
		/// </summary>
		public virtual Gdk.Key AdjustmentMenuKey { get { return (Gdk.Key)0; } }

		/// <summary>
		/// Returns the modifier(s) to the keyboard shortcut. Only affects adjustments, not effects. Default is Ctrl+Shift.
		/// </summary>
		public virtual Gdk.ModifierType AdjustmentMenuKeyModifiers { get { return Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask; } }

		/// <summary>
		/// Returns the menu category for an effect. Only affects effects, not adjustments. Default is "General".
		/// </summary>
		public virtual string EffectMenuCategory { get { return "General"; } }

		/// <summary>
		/// The user configurable data this effect uses.
		/// </summary>
		public EffectData EffectData { get; protected set; }

		/// <summary>
		/// Launches the configuration dialog for this effect/adjustment.
		/// </summary>
		/// <returns>Whether the user accepted or cancelled the configuration dialog. (true: accept, false: cancel)</returns>
		public virtual bool LaunchConfiguration ()
		{
			if (IsConfigurable)
				throw new NotImplementedException (string.Format ("{0} is marked as configurable, but has not implemented LaunchConfiguration", this.GetType ()));
				
			return false;
		}

		#region Overrideable Render Methods
		/// <summary>
		/// Performs the actual work of rendering an effect. Do not call base.Render ().
		/// </summary>
		/// <param name="src">The source surface. DO NOT MODIFY.</param>
		/// <param name="dst">The destination surface.</param>
		/// <param name="rois">An array of rectangles of interest (roi) specifying the area(s) to modify. Only these areas should be modified.</param>
		public virtual void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			foreach (var rect in rois)
				Render (src, dst, rect);
		}

		/// <summary>
		/// Performs the actual work of rendering an effect. Do not call base.Render ().
		/// </summary>
		/// <param name="src">The source surface. DO NOT MODIFY.</param>
		/// <param name="dst">The destination surface.</param>
		/// <param name="roi">A rectangle of interest (roi) specifying the area to modify. Only these areas should be modified</param>
		protected unsafe virtual void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle roi)
		{
			ColorBgra* src_data_ptr = (ColorBgra*)src.DataPtr;
			int src_width = src.Width;
			ColorBgra* dst_data_ptr = (ColorBgra*)dst.DataPtr;
			int dst_width = dst.Width;

			for (int y = roi.Y; y <= roi.GetBottom (); ++y) {
				ColorBgra* srcPtr = src.GetPointAddressUnchecked (src_data_ptr, src_width, roi.X, y);
				ColorBgra* dstPtr = dst.GetPointAddressUnchecked (dst_data_ptr, dst_width, roi.X, y);
				Render (srcPtr, dstPtr, roi.Width);
			}
		}

		/// <summary>
		/// Performs the actual work of rendering an effect. This overload represent a single line of the image. Do not call base.Render ().
		/// </summary>
		/// <param name="src">The source surface. DO NOT MODIFY.</param>
		/// <param name="dst">The destination surface.</param>
		/// <param name="length">The number of pixels to render.</param>
		protected unsafe virtual void Render (ColorBgra* src, ColorBgra* dst, int length)
		{
			while (length > 0) {
				*dst = Render (*src);
				++dst;
				++src;
				--length;
			}
		}

		/// <summary>
		/// Performs the actual work of rendering an effect. This overload represent a single pixel of the image.
		/// </summary>
		/// <param name="color">The color of the source surface pixel.</param>
		/// <returns>The color to be used for the destination pixel.</returns>
		protected virtual ColorBgra Render (ColorBgra color)
		{
			return color;
		}
		#endregion
				
		// Effects that have any configuration state which is changed
		// during live preview, and this this state is stored in
		// non-value-type fields should override this method.
		// Generally this state should be stored in the effect data
		// class, not in the effect.
		/// <summary>
		/// Clones this effect so the live preview system has a copy that won't change while it is working.  Only override this when a MemberwiseClone is not enough.
		/// </summary>
		/// <returns>An identical copy of this effect.</returns>
		public virtual BaseEffect Clone ()
		{
			var effect = (BaseEffect) this.MemberwiseClone ();

			if (effect.EffectData != null)
				effect.EffectData = EffectData.Clone ();

			return effect;
		}		
	}
	
	/// <summary>
	/// Holds the user configurable data used by this effect.
	/// </summary>
	public abstract class EffectData : ObservableObject
	{
		// EffectData classes that have any state stored in non-value-type
		// fields must override this method, and clone those members.
		/// <summary>
		/// Clones this EffectData so the live preview system has a copy that won't change while it is working.  Only override this when a MemberwiseClone is not enough.
		/// </summary>
		/// <returns>An identical copy of this EffectData.</returns>
		public virtual EffectData Clone ()
		{
			return (EffectData) this.MemberwiseClone ();
		}
		
		/// <summary>
		/// Fires the PropertyChanged event for this ObservableObject.
		/// </summary>
		/// <param name="propertyName">The name of the property that changed.</param>
		public new void FirePropertyChanged (string propertyName)
		{
			base.FirePropertyChanged (propertyName);
		}
		
		/// <summary>
		/// Returns true if the current values of this EffectData do not modify the image. Returns false if current values modify the image.
		/// </summary>
		public virtual bool IsDefault { get { return false; } }
	}
}
