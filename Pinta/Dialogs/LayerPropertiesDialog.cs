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
using System.Linq;
using Pinta.Core;

namespace Pinta;

public sealed class LayerPropertiesDialog : Gtk.Dialog
{
	private readonly LayerProperties initial_properties;

	private double opacity;
	private bool hidden;
	private string name;
	private BlendMode blendmode;

	private readonly Gtk.Entry layer_name_entry;
	private readonly Gtk.CheckButton visibility_checkbox;
	private readonly Gtk.SpinButton opacity_spinner;
	private readonly Gtk.Scale opacity_slider;
	private readonly Gtk.ComboBoxText blend_combo_box;

	public LayerPropertiesDialog ()
	{
		Title = Translations.GetString ("Layer Properties");
		TransientFor = PintaCore.Chrome.MainWindow;
		Modal = true;
		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		Document doc = PintaCore.Workspace.ActiveDocument;

		DefaultWidth = 349;
		DefaultHeight = 224;

		const int spacing = 6;

		var content_area = this.GetContentAreaBox ();
		content_area.Spacing = spacing;
		content_area.SetAllMargins (10);

		Gtk.Grid grid = new () { RowSpacing = spacing, ColumnSpacing = spacing, ColumnHomogeneous = false };

		// Layer name
		Gtk.Label name_label = Gtk.Label.New (Translations.GetString ("Name:"));
		name_label.Halign = Gtk.Align.End;
		grid.Attach (name_label, 0, 0, 1, 1);

		layer_name_entry = new Gtk.Entry () {
			Hexpand = true,
			Halign = Gtk.Align.Fill
		};
		grid.Attach (layer_name_entry, 1, 0, 1, 1);

		// Visible checkbox
		visibility_checkbox = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Visible"));

		grid.Attach (visibility_checkbox, 1, 1, 1, 1);

		// Blend mode
		Gtk.Label blend_label = Gtk.Label.New (Translations.GetString ("Blend Mode") + ":");
		blend_label.Halign = Gtk.Align.End;
		grid.Attach (blend_label, 0, 2, 1, 1);

		blend_combo_box = new Gtk.ComboBoxText ();
		foreach (string name in UserBlendOps.GetAllBlendModeNames ())
			blend_combo_box.AppendText (name);

		blend_combo_box.Hexpand = true;
		blend_combo_box.Halign = Gtk.Align.Fill;
		grid.Attach (blend_combo_box, 1, 2, 1, 1);

		// Opacity
		Gtk.Label opacity_label = Gtk.Label.New (Translations.GetString ("Opacity:"));
		opacity_label.Halign = Gtk.Align.End;
		grid.Attach (opacity_label, 0, 3, 1, 1);

		Gtk.Box opacity_box = new () { Spacing = spacing };
		opacity_box.SetOrientation (Gtk.Orientation.Horizontal);
		opacity_spinner = Gtk.SpinButton.NewWithRange (0, 100, 1);
		opacity_spinner.Adjustment!.PageIncrement = 10;
		opacity_spinner.ClimbRate = 1;
		opacity_box.Append (opacity_spinner);

		opacity_slider = Gtk.Scale.NewWithRange (Gtk.Orientation.Horizontal, 0, 100, 1);
		opacity_slider.Digits = 0;
		opacity_slider.Adjustment!.PageIncrement = 10;
		opacity_slider.Hexpand = true;
		opacity_slider.Halign = Gtk.Align.Fill;
		opacity_box.Append (opacity_slider);

		grid.Attach (opacity_box, 1, 3, 1, 1);

		content_area.Append (grid);

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

		layer_name_entry.SetText (initial_properties.Name);
		visibility_checkbox.Active = !initial_properties.Hidden;
		opacity_spinner.Value = Math.Round (initial_properties.Opacity * 100);
		opacity_slider.SetValue (Math.Round (initial_properties.Opacity * 100));

		var all_blendmodes = UserBlendOps.GetAllBlendModeNames ().ToList ();
		var index = all_blendmodes.IndexOf (UserBlendOps.GetBlendModeName (blendmode));
		blend_combo_box.Active = index;

		layer_name_entry.OnChanged ((o, e) => OnLayerNameChanged (o, e));

		visibility_checkbox.OnToggled += OnVisibilityToggled;
		opacity_spinner.OnValueChanged += OnOpacitySpinnerChanged;
		opacity_slider.OnValueChanged += OnOpacitySliderChanged;
		blend_combo_box.OnChanged += OnBlendModeChanged;

		layer_name_entry.SetActivatesDefault (true);
		opacity_spinner.SetActivatesDefault (true);
	}

	public bool AreLayerPropertiesUpdated =>
		initial_properties.Opacity != opacity
		|| initial_properties.Hidden != hidden
		|| initial_properties.Name != name
		|| initial_properties.BlendMode != blendmode;

	public LayerProperties InitialLayerProperties
		=> initial_properties;

	public LayerProperties UpdatedLayerProperties
		=> new (name, hidden, opacity, blendmode);

	private void OnLayerNameChanged (object? sender, EventArgs e)
	{
		Document doc = PintaCore.Workspace.ActiveDocument;

		name = layer_name_entry.GetText ();
		doc.Layers.CurrentUserLayer.Name = name;
	}

	private void OnVisibilityToggled (object? sender, EventArgs e)
	{
		Document doc = PintaCore.Workspace.ActiveDocument;

		hidden = !visibility_checkbox.Active;
		doc.Layers.CurrentUserLayer.Hidden = hidden;
		if (doc.Layers.SelectionLayer != null) {
			//Update Visibility for SelectionLayer and force redraw			
			doc.Layers.SelectionLayer.Hidden = doc.Layers.CurrentUserLayer.Hidden;
		}
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
		opacity = opacity_spinner.Value / 100d;
		doc.Layers.CurrentUserLayer.Opacity = opacity;
		if (doc.Layers.SelectionLayer != null) {
			//Update Opacity for SelectionLayer and force redraw			
			doc.Layers.SelectionLayer.Opacity = doc.Layers.CurrentUserLayer.Opacity;
		}
		PintaCore.Workspace.Invalidate ();
	}

	private void OnBlendModeChanged (object? sender, EventArgs e)
	{
		Document doc = PintaCore.Workspace.ActiveDocument;

		blendmode = UserBlendOps.GetBlendModeByName (blend_combo_box.GetActiveText ()!);
		doc.Layers.CurrentUserLayer.BlendMode = blendmode;
		if (doc.Layers.SelectionLayer != null) {
			//Update BlendMode for SelectionLayer and force redraw
			doc.Layers.SelectionLayer.BlendMode = doc.Layers.CurrentUserLayer.BlendMode;
		}
		PintaCore.Workspace.Invalidate ();
	}
}

