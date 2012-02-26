// 
// LayerPropertiesDialog.cs
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
using Pinta.Core;

namespace Pinta
{
	public partial class LayerPropertiesDialog : Gtk.Dialog
	{		
		private LayerProperties initial_properties;
		
		private double opacity;
		private bool hidden;
		private string name;
		
		public LayerPropertiesDialog ()
		{
			this.Build ();

			this.Icon = PintaCore.Resources.GetIcon ("Menu.Layers.LayerProperties.png");
			
			name = PintaCore.Layers.CurrentLayer.Name;
			hidden = PintaCore.Layers.CurrentLayer.Hidden;
			opacity = PintaCore.Layers.CurrentLayer.Opacity;
			
			initial_properties = new LayerProperties(
				name,				
				hidden,
				opacity);
			
			layerNameEntry.Text = initial_properties.Name;
			visibilityCheckbox.Active = !initial_properties.Hidden;
			opacitySpinner.Value = (int)(initial_properties.Opacity * 100);
			opacitySlider.Value = (int)(initial_properties.Opacity * 100);

			layerNameEntry.Changed += OnLayerNameChanged;
			visibilityCheckbox.Toggled += OnVisibilityToggled;
			opacitySpinner.ValueChanged += new EventHandler (OnOpacitySpinnerChanged);
			opacitySlider.ValueChanged += new EventHandler (OnOpacitySliderChanged);
			
			AlternativeButtonOrder = new int[] { (int) Gtk.ResponseType.Ok, (int) Gtk.ResponseType.Cancel };
			DefaultResponse = Gtk.ResponseType.Ok;

			layerNameEntry.ActivatesDefault = true;
			opacitySpinner.ActivatesDefault = true;
		}
		
		public bool AreLayerPropertiesUpdated {
			get {
				return initial_properties.Opacity != opacity
					|| initial_properties.Hidden != hidden
					|| initial_properties.Name != name;
			}
		}
		
		public LayerProperties InitialLayerProperties { 
			get {
				return initial_properties;
			}
		}		
		
		public LayerProperties UpdatedLayerProperties { 
			get {
				return new LayerProperties (name, hidden, opacity);
			}
		}
		
		#region Private Methods
		private void OnLayerNameChanged (object sender, EventArgs e)
		{
			name = layerNameEntry.Text;
			PintaCore.Layers.CurrentLayer.Name = name;
		}
		
		private void OnVisibilityToggled (object sender, EventArgs e)
		{
			hidden = !visibilityCheckbox.Active;
			PintaCore.Layers.CurrentLayer.Hidden = hidden;
		}
		
		private void OnOpacitySliderChanged (object sender, EventArgs e)
		{
			opacitySpinner.Value = opacitySlider.Value;
			UpdateOpacity ();
		}

		private void OnOpacitySpinnerChanged (object sender, EventArgs e)
		{
			opacitySlider.Value = opacitySpinner.Value;
			UpdateOpacity ();
		}
		
		private void UpdateOpacity ()
		{
			//TODO check redraws are being throttled.
			opacity = opacitySpinner.Value / 100d;
			PintaCore.Layers.CurrentLayer.Opacity = opacity;
		}
		#endregion
	}
}

