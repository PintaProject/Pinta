// 
// PosterizeDialog.cs
//  
// Author:
//      Krzysztof Marecki <marecki.krzysztof@gmail.com>
// 
// Copyright (c) 2010 Krzysztof Marecki 
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
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class PosterizeDialog : Gtk.Dialog
{
	private readonly HScaleSpinButtonWidget red_spinbox;
	private readonly HScaleSpinButtonWidget green_spinbox;
	private readonly HScaleSpinButtonWidget blue_spinbox;
	private readonly Gtk.CheckButton link_button;

	public int Red => red_spinbox.ValueAsInt;
	public int Green => green_spinbox.ValueAsInt;
	public int Blue => blue_spinbox.ValueAsInt;

	public PosterizeData? EffectData { get; set; }

	public PosterizeDialog (IChromeService chrome)
	{
		DefaultWidth = 400;
		DefaultHeight = 300;

		Title = Translations.GetString ("Posterize");
		TransientFor = chrome.MainWindow;
		Modal = true;

		Resizable = false;

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		red_spinbox = CreateChannelSpinBox (Translations.GetString ("Red"));
		green_spinbox = CreateChannelSpinBox (Translations.GetString ("Green"));
		blue_spinbox = CreateChannelSpinBox (Translations.GetString ("Blue"));
		link_button = CreateLinkButton ();

		Gtk.Box content_area = this.GetContentAreaBox ();
		content_area.WidthRequest = 400;
		content_area.SetAllMargins (6);
		content_area.Spacing = 6;
		content_area.Append (red_spinbox);
		content_area.Append (green_spinbox);
		content_area.Append (blue_spinbox);
		content_area.Append (link_button);
	}

	private static Gtk.CheckButton CreateLinkButton ()
	{
		var result = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Linked"));
		result.Active = true;
		return result;
	}

	private HScaleSpinButtonWidget CreateChannelSpinBox (string label)
	{
		const int initial_channel_value = 16;
		HScaleSpinButtonWidget spinner = new (initial_channel_value) {
			Label = label,
			MaximumValue = 64,
			MinimumValue = 2,
		};
		spinner.ValueChanged += HandleValueChanged;
		return spinner;
	}

	private void HandleValueChanged (object? sender, EventArgs e)
	{
		if (sender is not HScaleSpinButtonWidget widget)
			return;

		if (link_button.Active)
			green_spinbox.Value = blue_spinbox.Value = red_spinbox.Value = widget.Value;

		UpdateEffectData ();
	}

	private void UpdateEffectData ()
	{
		if (EffectData == null)
			return;

		EffectData.Red = red_spinbox.ValueAsInt;
		EffectData.Green = green_spinbox.ValueAsInt;
		EffectData.Blue = blue_spinbox.ValueAsInt;

		// Only fire event once, even if all properties have changed.
		EffectData.FirePropertyChanged ("_all_");
	}
}
