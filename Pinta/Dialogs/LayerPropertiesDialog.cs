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
using System.Collections.Immutable;
using Pinta.Core;

namespace Pinta;

public sealed class LayerPropertiesDialog : Gtk.Dialog
{
	private readonly LayerProperties initial_properties;

	private double current_layer_opacity;
	private bool current_layer_hidden;
	private string current_layer_name;
	private BlendMode current_layer_blend_mode;

	private readonly Gtk.Entry layer_name_entry;
	private readonly Gtk.CheckButton visibility_checkbox;
	private readonly Gtk.SpinButton opacity_spinner;
	private readonly Gtk.Scale opacity_slider;
	private readonly Gtk.ComboBoxText blend_combo_box;

	public LayerPropertiesDialog ()
	{
		const int spacing = 6;

		Document doc = PintaCore.Workspace.ActiveDocument;

		string currentLayerName = doc.Layers.CurrentUserLayer.Name;
		bool currentLayerHidden = doc.Layers.CurrentUserLayer.Hidden;
		double currentLayerOpacity = doc.Layers.CurrentUserLayer.Opacity;
		BlendMode currentLayerBlendMode = doc.Layers.CurrentUserLayer.BlendMode;

		LayerProperties initialProperties = new (
			currentLayerName,
			currentLayerHidden,
			currentLayerOpacity,
			currentLayerBlendMode);

		Gtk.Label nameLabel = Gtk.Label.New (Translations.GetString ("Name:"));
		nameLabel.Halign = Gtk.Align.End;

		Gtk.Entry layerNameEntry = new () {
			Hexpand = true,
			Halign = Gtk.Align.Fill,
		};
		layerNameEntry.SetText (initialProperties.Name);
		layerNameEntry.OnChanged (OnLayerNameChanged);
		layerNameEntry.SetActivatesDefault (true);

		Gtk.CheckButton visibilityCheckbox = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Visible"));
		visibilityCheckbox.Active = !initialProperties.Hidden;
		visibilityCheckbox.OnToggled += OnVisibilityToggled;

		Gtk.Label blendLabel = Gtk.Label.New (Translations.GetString ("Blend Mode") + ":");
		blendLabel.Halign = Gtk.Align.End;

		var allBlendmodes = UserBlendOps.GetAllBlendModeNames ().ToImmutableArray ();
		var index = allBlendmodes.IndexOf (UserBlendOps.GetBlendModeName (currentLayerBlendMode));

		Gtk.ComboBoxText blendComboBox = new ();

		foreach (string name in UserBlendOps.GetAllBlendModeNames ())
			blendComboBox.AppendText (name);

		blendComboBox.Hexpand = true;
		blendComboBox.Halign = Gtk.Align.Fill;
		blendComboBox.Active = index;
		blendComboBox.OnChanged += OnBlendModeChanged;

		Gtk.Label opacityLabel = Gtk.Label.New (Translations.GetString ("Opacity:"));
		opacityLabel.Halign = Gtk.Align.End;

		Gtk.SpinButton opacitySpinner = Gtk.SpinButton.NewWithRange (0, 100, 1);
		opacitySpinner.Adjustment!.PageIncrement = 10;
		opacitySpinner.ClimbRate = 1;
		opacitySpinner.Value = Math.Round (initialProperties.Opacity * 100);
		opacitySpinner.OnValueChanged += OnOpacitySpinnerChanged;
		opacitySpinner.SetActivatesDefaultImmediate (true);

		Gtk.Scale opacitySlider = Gtk.Scale.NewWithRange (Gtk.Orientation.Horizontal, 0, 100, 1);
		opacitySlider.Digits = 0;
		opacitySlider.Adjustment!.PageIncrement = 10;
		opacitySlider.Hexpand = true;
		opacitySlider.Halign = Gtk.Align.Fill;
		opacitySlider.SetValue (Math.Round (initialProperties.Opacity * 100));
		opacitySlider.OnValueChanged += OnOpacitySliderChanged;

		Gtk.Box opacityBox = new () { Spacing = spacing };
		opacityBox.SetOrientation (Gtk.Orientation.Horizontal);
		opacityBox.Append (opacitySpinner);
		opacityBox.Append (opacitySlider);

		Gtk.Grid grid = new () {
			RowSpacing = spacing,
			ColumnSpacing = spacing,
			ColumnHomogeneous = false,
		};
		grid.Attach (nameLabel, 0, 0, 1, 1);
		grid.Attach (layerNameEntry, 1, 0, 1, 1);
		grid.Attach (visibilityCheckbox, 1, 1, 1, 1);
		grid.Attach (blendLabel, 0, 2, 1, 1);
		grid.Attach (blendComboBox, 1, 2, 1, 1);
		grid.Attach (opacityLabel, 0, 3, 1, 1);
		grid.Attach (opacityBox, 1, 3, 1, 1);

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString ("Layer Properties");
		TransientFor = PintaCore.Chrome.MainWindow;
		Modal = true;
		DefaultWidth = 349;
		DefaultHeight = 224;
		IconName = Resources.Icons.LayerProperties;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- Initialization

		var contentArea = this.GetContentAreaBox ();
		contentArea.Spacing = spacing;
		contentArea.SetAllMargins (10);
		contentArea.Append (grid);

		// --- References to keep

		layer_name_entry = layerNameEntry;
		visibility_checkbox = visibilityCheckbox;
		blend_combo_box = blendComboBox;
		opacity_spinner = opacitySpinner;
		opacity_slider = opacitySlider;

		current_layer_name = currentLayerName;
		current_layer_hidden = currentLayerHidden;
		current_layer_opacity = currentLayerOpacity;
		current_layer_blend_mode = currentLayerBlendMode;

		initial_properties = initialProperties;
	}

	public bool AreLayerPropertiesUpdated =>
		initial_properties.Opacity != current_layer_opacity
		|| initial_properties.Hidden != current_layer_hidden
		|| initial_properties.Name != current_layer_name
		|| initial_properties.BlendMode != current_layer_blend_mode;

	public LayerProperties InitialLayerProperties
		=> initial_properties;

	public LayerProperties UpdatedLayerProperties
		=> new (
			current_layer_name,
			current_layer_hidden,
			current_layer_opacity,
			current_layer_blend_mode);

	private void OnLayerNameChanged (object? sender, EventArgs e)
	{
		Document doc = PintaCore.Workspace.ActiveDocument;
		current_layer_name = layer_name_entry.GetText ();
		doc.Layers.CurrentUserLayer.Name = current_layer_name;
	}

	private void OnVisibilityToggled (object? sender, EventArgs e)
	{
		Document doc = PintaCore.Workspace.ActiveDocument;

		current_layer_hidden = !visibility_checkbox.Active;

		doc.Layers.CurrentUserLayer.Hidden = current_layer_hidden;

		if (doc.Layers.SelectionLayer != null)
			doc.Layers.SelectionLayer.Hidden = doc.Layers.CurrentUserLayer.Hidden; // Update Visibility for SelectionLayer and force redraw

		PintaCore.Workspace.Invalidate ();
	}

	private void OnOpacitySliderChanged (object? sender, EventArgs e)
	{
		opacity_spinner.Value = opacity_slider.GetValue ();
		UpdateOpacity ();
	}

	private void OnOpacitySpinnerChanged (object? sender, EventArgs e)
	{
		opacity_slider.SetValue (opacity_spinner.Value);
		UpdateOpacity ();
	}

	private void UpdateOpacity ()
	{
		Document doc = PintaCore.Workspace.ActiveDocument;

		//TODO check redraws are being throttled.
		current_layer_opacity = opacity_spinner.Value / 100d;

		doc.Layers.CurrentUserLayer.Opacity = current_layer_opacity;

		if (doc.Layers.SelectionLayer != null)
			doc.Layers.SelectionLayer.Opacity = doc.Layers.CurrentUserLayer.Opacity; // Update Opacity for SelectionLayer and force redraw

		PintaCore.Workspace.Invalidate ();
	}

	private void OnBlendModeChanged (object? sender, EventArgs e)
	{
		Document doc = PintaCore.Workspace.ActiveDocument;

		current_layer_blend_mode = UserBlendOps.GetBlendModeByName (blend_combo_box.GetActiveText ()!);

		doc.Layers.CurrentUserLayer.BlendMode = current_layer_blend_mode;

		if (doc.Layers.SelectionLayer != null)
			doc.Layers.SelectionLayer.BlendMode = doc.Layers.CurrentUserLayer.BlendMode; //Update BlendMode for SelectionLayer and force redraw

		PintaCore.Workspace.Invalidate ();
	}
}

