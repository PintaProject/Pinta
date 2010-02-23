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
			
			entry1.Text = initial_properties.Name;
			checkbutton1.Active = !initial_properties.Hidden;
			spinbutton1.Value = (int)(initial_properties.Opacity * 100);
			hscale1.Value = (int)(initial_properties.Opacity * 100);

			entry1.Changed += entry1_Changed;
			checkbutton1.Toggled += checkbutton1_Toggled;
			spinbutton1.ValueChanged += new EventHandler (spinbutton1_ValueChanged);
			hscale1.ValueChanged += new EventHandler (hscale1_ValueChanged);			
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
		private void entry1_Changed (object sender, EventArgs e)
		{
			name = entry1.Text;
			PintaCore.Layers.CurrentLayer.Name = name;
		}
		
		private void checkbutton1_Toggled (object sender, EventArgs e)
		{
			hidden = !checkbutton1.Active;
			PintaCore.Layers.CurrentLayer.Hidden = hidden;
		}
		
		private void hscale1_ValueChanged (object sender, EventArgs e)
		{
			spinbutton1.Value = hscale1.Value;
			UpdateOpacity ();
		}

		private void spinbutton1_ValueChanged (object sender, EventArgs e)
		{
			hscale1.Value = spinbutton1.Value;			
			UpdateOpacity ();
		}
		
		private void UpdateOpacity ()
		{
			//TODO check redraws are being throttled.
			opacity = spinbutton1.Value / 100d;
			PintaCore.Layers.CurrentLayer.Opacity = opacity;
		}
		#endregion
	}
}

