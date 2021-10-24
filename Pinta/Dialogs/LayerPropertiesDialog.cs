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
using System.Diagnostics.CodeAnalysis;
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
		private ComboBoxText blendComboBox;

		public LayerPropertiesDialog () : base (Translations.GetString ("Layer Properties"),
			PintaCore.Chrome.MainWindow, DialogFlags.Modal,
            Core.GtkExtensions.DialogButtonsCancelOk())
        {
 			var doc = PintaCore.Workspace.ActiveDocument;

           Build ();

			IconName = Resources.Icons.LayerProperties;
			
			name = doc.Layers.CurrentUserLayer.Name;
			hidden = doc.Layers.CurrentUserLayer.Hidden;
			opacity = doc.Layers.CurrentUserLayer.Opacity;
			blendmode = doc.Layers.CurrentUserLayer.BlendMode;

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
		private void OnLayerNameChanged (object? sender, EventArgs e)
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			name = layerNameEntry.Text;
			doc.Layers.CurrentUserLayer.Name = name;
		}
		
		private void OnVisibilityToggled (object? sender, EventArgs e)
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			hidden = !visibilityCheckbox.Active;
			doc.Layers.CurrentUserLayer.Hidden = hidden;
			if (doc.Layers.SelectionLayer != null) {
				//Update Visiblity for SelectionLayer and force redraw			
				doc.Layers.SelectionLayer.Hidden = doc.Layers.CurrentUserLayer.Hidden;
			}
			PintaCore.Workspace.Invalidate ();
		}
		
		private void OnOpacitySliderChanged (object? sender, EventArgs e)
		{
			opacitySpinner.Value = opacitySlider.Value;
			UpdateOpacity ();
		}

		private void OnOpacitySpinnerChanged (object? sender, EventArgs e)
		{
			opacitySlider.Value = opacitySpinner.Value;
			UpdateOpacity ();
		}
		
		private void UpdateOpacity ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			//TODO check redraws are being throttled.
			opacity = opacitySpinner.Value / 100d;
			doc.Layers.CurrentUserLayer.Opacity = opacity;
			if (doc.Layers.SelectionLayer != null) {
				//Update Opacity for SelectionLayer and force redraw			
				doc.Layers.SelectionLayer.Opacity = doc.Layers.CurrentUserLayer.Opacity;
			}
			PintaCore.Workspace.Invalidate ();		
		}

		private void OnBlendModeChanged (object? sender, EventArgs e)
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			blendmode = UserBlendOps.GetBlendModeByName (blendComboBox.ActiveText);
			doc.Layers.CurrentUserLayer.BlendMode = blendmode;
			if (doc.Layers.SelectionLayer != null) {
				//Update BlendMode for SelectionLayer and force redraw
				doc.Layers.SelectionLayer.BlendMode = doc.Layers.CurrentUserLayer.BlendMode;	 
			}
			PintaCore.Workspace.Invalidate ();		
		}

		[MemberNotNull (nameof (layerNameEntry), nameof (visibilityCheckbox), nameof (blendComboBox), nameof (opacitySpinner), nameof (opacitySlider))]
		private void Build ()
		{
			DefaultWidth = 349;
			DefaultHeight = 224;
			BorderWidth = 6;
			ContentArea.Spacing = 10;
			
			// Layer name
			var box1 = new HBox ();

			box1.Spacing = 6;
			box1.PackStart (new Label (Translations.GetString ("Name:")), false, false, 0);

			layerNameEntry = new Entry ();
			box1.PackStart (layerNameEntry, true, true, 0);

			ContentArea.PackStart (box1, false, false, 0);

			// Visible checkbox
			visibilityCheckbox = new CheckButton (Translations.GetString ("Visible"));

			ContentArea.PackStart (visibilityCheckbox, false, false, 0);

			// Horizontal separator
			ContentArea.PackStart (new HSeparator (), false, false, 0);

			// Blend mode
			var box2 = new HBox ();

			box2.Spacing = 6;
			box2.PackStart (new Label (Translations.GetString ("Blend Mode") + ":"), false, false, 0);

			blendComboBox = new ComboBoxText();
			foreach (string name in UserBlendOps.GetAllBlendModeNames())
				blendComboBox.AppendText(name);

			box2.PackStart (blendComboBox, true, true, 0);

			ContentArea.PackStart (box2, false, false, 0);

			// Opacity
			var box3 = new HBox ();

			box3.Spacing = 6;
			box3.PackStart (new Label (Translations.GetString ("Opacity:")), false, false, 0);

			opacitySpinner = new SpinButton (0, 100, 1);
			opacitySpinner.Adjustment.PageIncrement = 10;
			opacitySpinner.ClimbRate = 1;

			box3.PackStart (opacitySpinner, false, false, 0);

			opacitySlider = new HScale (0, 100, 1);
			opacitySlider.Digits = 0;
			opacitySlider.Adjustment.PageIncrement = 10;
			box3.PackStart (opacitySlider, true, true, 0);

			ContentArea.PackStart (box3, false, false, 0);

			// Finish up
			ContentArea.ShowAll ();

			DefaultResponse = ResponseType.Ok;
		}
		#endregion
	}
}

