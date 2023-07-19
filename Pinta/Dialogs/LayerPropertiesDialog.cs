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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta
{
	public class LayerPropertiesDialog : Dialog
	{
		private readonly LayerProperties initial_properties;

		private double opacity;
		private bool hidden;
		private string name;
		private BlendMode blendmode;

		private Entry layerNameEntry;
		private CheckButton visibilityCheckbox;
		private SpinButton opacitySpinner;
		private Scale opacitySlider;
		private ComboBoxText blendComboBox;

		public LayerPropertiesDialog ()
		{
			Title = Translations.GetString ("Layer Properties");
			TransientFor = PintaCore.Chrome.MainWindow;
			Modal = true;
			this.AddCancelOkButtons ();
			this.SetDefaultResponse (ResponseType.Ok);

			var doc = PintaCore.Workspace.ActiveDocument;

			Build ();

			IconName = Resources.Icons.LayerProperties;

			name = doc.Layers.CurrentUserLayer.Name;
			hidden = doc.Layers.CurrentUserLayer.Hidden;
			opacity = doc.Layers.CurrentUserLayer.Opacity;
			blendmode = doc.Layers.CurrentUserLayer.BlendMode;

			initial_properties = new LayerProperties (
				name,
				hidden,
				opacity,
				blendmode);

			layerNameEntry.SetText (initial_properties.Name);
			visibilityCheckbox.Active = !initial_properties.Hidden;
			opacitySpinner.Value = Math.Round (initial_properties.Opacity * 100);
			opacitySlider.SetValue (Math.Round (initial_properties.Opacity * 100));

			var all_blendmodes = UserBlendOps.GetAllBlendModeNames ().ToList ();
			var index = all_blendmodes.IndexOf (UserBlendOps.GetBlendModeName (blendmode));
			blendComboBox.Active = index;

			layerNameEntry.OnChanged ((o, e) => OnLayerNameChanged (o, e));

			visibilityCheckbox.OnToggled += OnVisibilityToggled;
			opacitySpinner.OnValueChanged += OnOpacitySpinnerChanged;
			opacitySlider.OnValueChanged += OnOpacitySliderChanged;
			blendComboBox.OnChanged += OnBlendModeChanged;

			layerNameEntry.SetActivatesDefault (true);
			opacitySpinner.SetActivatesDefault (true);
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

			name = layerNameEntry.GetText ();
			doc.Layers.CurrentUserLayer.Name = name;
		}

		private void OnVisibilityToggled (object? sender, EventArgs e)
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			hidden = !visibilityCheckbox.Active;
			doc.Layers.CurrentUserLayer.Hidden = hidden;
			if (doc.Layers.SelectionLayer != null) {
				//Update Visibility for SelectionLayer and force redraw			
				doc.Layers.SelectionLayer.Hidden = doc.Layers.CurrentUserLayer.Hidden;
			}
			PintaCore.Workspace.Invalidate ();
		}

		private void OnOpacitySliderChanged (object? sender, EventArgs e)
		{
			opacitySpinner.Value = opacitySlider.GetValue ();
			UpdateOpacity ();
		}

		private void OnOpacitySpinnerChanged (object? sender, EventArgs e)
		{
			opacitySlider.SetValue (opacitySpinner.Value);
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

			blendmode = UserBlendOps.GetBlendModeByName (blendComboBox.GetActiveText ()!);
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
			const int spacing = 6;

			var content_area = this.GetContentAreaBox ();
			content_area.Spacing = spacing;
			content_area.SetAllMargins (10);

			var grid = new Grid () { RowSpacing = spacing, ColumnSpacing = spacing, ColumnHomogeneous = false };

			// Layer name
			var name_label = Label.New (Translations.GetString ("Name:"));
			name_label.Halign = Align.End;
			grid.Attach (name_label, 0, 0, 1, 1);

			layerNameEntry = new Entry ();
			layerNameEntry.Hexpand = true;
			layerNameEntry.Halign = Align.Fill;
			grid.Attach (layerNameEntry, 1, 0, 1, 1);

			// Visible checkbox
			visibilityCheckbox = CheckButton.NewWithLabel (Translations.GetString ("Visible"));

			grid.Attach (visibilityCheckbox, 1, 1, 1, 1);

			// Blend mode
			var blend_label = Label.New (Translations.GetString ("Blend Mode") + ":");
			blend_label.Halign = Align.End;
			grid.Attach (blend_label, 0, 2, 1, 1);

			blendComboBox = new ComboBoxText ();
			foreach (string name in UserBlendOps.GetAllBlendModeNames ())
				blendComboBox.AppendText (name);

			blendComboBox.Hexpand = true;
			blendComboBox.Halign = Align.Fill;
			grid.Attach (blendComboBox, 1, 2, 1, 1);

			// Opacity
			var opacity_label = Label.New (Translations.GetString ("Opacity:"));
			opacity_label.Halign = Align.End;
			grid.Attach (opacity_label, 0, 3, 1, 1);

			var opacity_box = new Box () { Spacing = spacing };
			opacity_box.SetOrientation (Orientation.Horizontal);
			opacitySpinner = SpinButton.NewWithRange (0, 100, 1);
			opacitySpinner.Adjustment!.PageIncrement = 10;
			opacitySpinner.ClimbRate = 1;
			opacity_box.Append (opacitySpinner);

			opacitySlider = Scale.NewWithRange (Orientation.Horizontal, 0, 100, 1);
			opacitySlider.Digits = 0;
			opacitySlider.Adjustment!.PageIncrement = 10;
			opacitySlider.Hexpand = true;
			opacitySlider.Halign = Align.Fill;
			opacity_box.Append (opacitySlider);

			grid.Attach (opacity_box, 1, 3, 1, 1);

			content_area.Append (grid);
		}
		#endregion
	}
}

