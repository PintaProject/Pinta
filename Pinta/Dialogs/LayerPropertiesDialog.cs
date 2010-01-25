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
		public LayerPropertiesDialog ()
		{
			this.Build ();

			this.Icon = PintaCore.Resources.GetIcon ("Menu.Layers.LayerProperties.png");
			
			entry1.Text = PintaCore.Layers.CurrentLayer.Name;
			checkbutton1.Active = !PintaCore.Layers.CurrentLayer.Hidden;
			spinbutton1.Value = (int)(PintaCore.Layers.CurrentLayer.Opacity * 100);
			hscale1.Value = (int)(PintaCore.Layers.CurrentLayer.Opacity * 100);

			spinbutton1.ValueChanged += new EventHandler (spinbutton1_ValueChanged);
			hscale1.ValueChanged += new EventHandler (hscale1_ValueChanged);
		}

		#region Public Methods
		public void SaveChanges ()
		{
			PintaCore.Layers.CurrentLayer.Name = entry1.Text;
			PintaCore.Layers.CurrentLayer.Hidden = !checkbutton1.Active;
			PintaCore.Layers.CurrentLayer.Opacity = hscale1.Value / 100d;
		}
		#endregion

		#region Private Methods
		private void hscale1_ValueChanged (object sender, EventArgs e)
		{
			spinbutton1.Value = hscale1.Value;
		}

		private void spinbutton1_ValueChanged (object sender, EventArgs e)
		{
			hscale1.Value = spinbutton1.Value;
		}
		#endregion
	}
}

