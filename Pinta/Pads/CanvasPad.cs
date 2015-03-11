// 
// CanvasPad.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
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
using Gtk;
using Mono.Unix;
using MonoDevelop.Components.Docking;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta
{
	public class CanvasPad : IDockPad
	{
        public CanvasWindow CanvasWindow { get; private set; }
		public PintaCanvas Canvas { get { return CanvasWindow.Canvas; } }
        
		public void Initialize (DockFrame workspace, Menu padMenu)
		{
			// Create canvas
            CanvasWindow = new CanvasWindow ();

            // Add canvas to the dock
            var dock_item = workspace.AddItem ("Canvas");
            dock_item.Behavior = DockItemBehavior.Locked;
            dock_item.Expand = true;

            dock_item.DrawFrame = false;
            dock_item.Label = Catalog.GetString ("Canvas");
            dock_item.Icon = PintaCore.Resources.GetIcon ("Menu.Effects.Artistic.OilPainting.png");
            dock_item.Content = CanvasWindow;

            PintaCore.Chrome.InitializeCanvas (Canvas);

			PintaCore.Actions.View.Rulers.Toggled += HandleRulersToggled;
			PintaCore.Actions.View.Pixels.Activated += (o, e) => { SetRulersUnit (MetricType.Pixels); };
			PintaCore.Actions.View.Inches.Activated += (o, e) => { SetRulersUnit (MetricType.Inches); };
			PintaCore.Actions.View.Centimeters.Activated += (o, e) => { SetRulersUnit (MetricType.Centimeters); };
		}

		private void HandleRulersToggled (object sender, EventArgs e)
		{
			var visible = ((ToggleAction)sender).Active;

            CanvasWindow.RulersVisible = visible;
		}

		private void SetRulersUnit (Gtk.MetricType metric)
		{
            CanvasWindow.RulerMetric = metric;
		}
	}
}
