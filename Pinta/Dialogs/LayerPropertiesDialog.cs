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
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta
{
	public class LayerPropertiesDialog : Dialog
	{		
		private LayerProperties initial_properties;

		private double opacity;
		private bool hidden;
		private string name;
		private BlendMode blendmode;

		private Entry layerNameEntry;
		private CheckButton visibilityCheckbox;
		private SpinButton opacitySpinner;
		private HScale opacitySlider;
		private ComboBox blendComboBox;

		public LayerPropertiesDialog () : base (Mono.Unix.Catalog.GetString ("Layer Properties"), PintaCore.Chrome.MainWindow, DialogFlags.Modal, Stock.Cancel, ResponseType.Cancel, Stock.Ok, ResponseType.Ok)
		{
			Build ();

			this.Icon = PintaCore.Resources.GetIcon ("Menu.Layers.LayerProperties.png");
			
			name = PintaCore.Layers.CurrentLayer.Name;
			hidden = PintaCore.Layers.CurrentLayer.Hidden;
			opacity = PintaCore.Layers.CurrentLayer.Opacity;
			blendmode = PintaCore.Layers.CurrentLayer.BlendMode;

			initial_properties = new LayerProperties(
				name,				
				hidden,
				opacity,
				blendmode);
			
			layerNameEntry.Text = initial_properties.Name;
			visibilityCheckbox.Active = !initial_properties.Hidden;
			opacitySpinner.Value = (int)(initial_properties.Opacity * 100);
			opacitySlider.Value = (int)(initial_properties.Opacity * 100);

			var all_blendmodes = UserBlendOps.GetAllBlendModeNames ().ToList ();
			var index = all_blendmodes.IndexOf (UserBlendOps.GetBlendModeName (blendmode));
			blendComboBox.Active = index;

			layerNameEntry.Changed += OnLayerNameChanged;
			visibilityCheckbox.Toggled += OnVisibilityToggled;
			opacitySpinner.ValueChanged += new EventHandler (OnOpacitySpinnerChanged);
			opacitySlider.ValueChanged += new EventHandler (OnOpacitySliderChanged);
			blendComboBox.Changed += OnBlendModeChanged;

			AlternativeButtonOrder = new int[] { (int) Gtk.ResponseType.Ok, (int) Gtk.ResponseType.Cancel };
			DefaultResponse = Gtk.ResponseType.Ok;

			layerNameEntry.ActivatesDefault = true;
			opacitySpinner.ActivatesDefault = true;
		}
		
		public bool AreLayerPropertiesUpdated {
			get {
				return initial_properties.Opacity != opacity
					|| initial_properties.Hidden != hidden
					|| initial_properties.Name != name
					|| initial_properties.BlendMode != blendmode;
			}
		}
		
		public LayerProperties InitialLayerProperties { 
			get {
				return initial_properties;
			}
		}		
		
		public LayerProperties UpdatedLayerProperties { 
			get {
				return new LayerProperties (name, hidden, opacity, blendmode);
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
			if (PintaCore.Layers.SelectionLayer != null) {
				//Update Visiblity for SelectionLayer and force redraw			
				PintaCore.Layers.SelectionLayer.Hidden = PintaCore.Layers.CurrentLayer.Hidden;
			}
			PintaCore.Workspace.Invalidate ();
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
			if (PintaCore.Layers.SelectionLayer != null) {
				//Update Opacity for SelectionLayer and force redraw			
				PintaCore.Layers.SelectionLayer.Opacity = PintaCore.Layers.CurrentLayer.Opacity;
			}
			PintaCore.Workspace.Invalidate ();		
		}

		private void OnBlendModeChanged (object sender, EventArgs e)
		{
			blendmode = UserBlendOps.GetBlendModeByName (blendComboBox.ActiveText);
			PintaCore.Layers.CurrentLayer.BlendMode = blendmode;
			if (PintaCore.Layers.SelectionLayer != null) {
				//Update BlendMode for SelectionLayer and force redraw
				PintaCore.Layers.SelectionLayer.BlendMode = PintaCore.Layers.CurrentLayer.BlendMode;	 
			}
			PintaCore.Workspace.Invalidate ();		
		}

		private void Build ()
		{
			DefaultWidth = 349;
			DefaultHeight = 224;
			BorderWidth = 6;
			VBox.Spacing = 10;
			
			// Layer name
			var box1 = new HBox ();

			box1.Spacing = 6;
			box1.PackStart (new Label (Mono.Unix.Catalog.GetString ("Name:")), false, false, 0);

			layerNameEntry = new Entry ();
			box1.PackStart (layerNameEntry);

			VBox.PackStart (box1, false, false, 0);

			// Visible checkbox
			visibilityCheckbox = new CheckButton (Mono.Unix.Catalog.GetString ("Visible"));

			VBox.PackStart (visibilityCheckbox, false, false, 0);

			// Horizontal separator
			VBox.PackStart (new HSeparator (), false, false, 0);

			// Blend mode
			var box2 = new HBox ();

			box2.Spacing = 6;
			box2.PackStart (new Label (Mono.Unix.Catalog.GetString ("Blend Mode") + ":"), false, false, 0);

			blendComboBox = new ComboBox (UserBlendOps.GetAllBlendModeNames ().ToArray ());
			box2.PackStart (blendComboBox);

			VBox.PackStart (box2, false, false, 0);

			// Opacity
			var box3 = new HBox ();

			box3.Spacing = 6;
			box3.PackStart (new Label (Mono.Unix.Catalog.GetString ("Opacity:")), false, false, 0);

			opacitySpinner = new SpinButton (0, 100, 1);
			opacitySpinner.Adjustment.PageIncrement = 10;
			opacitySpinner.ClimbRate = 1;

			box3.PackStart (opacitySpinner, false, false, 0);

			opacitySlider = new HScale (0, 100, 1);
			opacitySlider.Digits = 0;
			opacitySlider.Adjustment.PageIncrement = 10;
			box3.PackStart (opacitySlider, true, true, 0);

			VBox.PackStart (box3, false, false, 0);

			// Finish up
			VBox.ShowAll ();

			AlternativeButtonOrder = new int[] { (int)ResponseType.Ok, (int)ResponseType.Cancel };
			DefaultResponse = ResponseType.Ok;
		}
		#endregion
	}
}

