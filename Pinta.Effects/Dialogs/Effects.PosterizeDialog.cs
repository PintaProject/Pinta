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
using Gtk;
using Mono.Unix;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public class PosterizeDialog : Gtk.Dialog
	{
		private HScaleSpinButtonWidget red_spinbox;
		private HScaleSpinButtonWidget green_spinbox;
		private HScaleSpinButtonWidget blue_spinbox;
		private CheckButton link_button;

		public int Red {
			get { return red_spinbox.ValueAsInt; }
		}
		
		public int Green { 
			get { return green_spinbox.ValueAsInt; }
		}
		
		public int Blue {
            get { return blue_spinbox.ValueAsInt; }
		}

		public PosterizeData EffectData { get; set; }

		public PosterizeDialog () : base (Catalog.GetString ("Posterize"),
		                                  PintaCore.Chrome.MainWindow, DialogFlags.Modal)
		{
			Build ();
			
			red_spinbox.ValueChanged += HandleValueChanged;
			green_spinbox.ValueChanged += HandleValueChanged;
			blue_spinbox.ValueChanged += HandleValueChanged;

			AlternativeButtonOrder = new int[] { (int) Gtk.ResponseType.Ok, (int) Gtk.ResponseType.Cancel };
			DefaultResponse = Gtk.ResponseType.Ok;
		}
		
		private void HandleValueChanged (object sender, EventArgs e)
		{
			var widget = sender as HScaleSpinButtonWidget;
			
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

		private void InitSpinBox (HScaleSpinButtonWidget spinbox)
		{
			spinbox.DefaultValue = 16;
			spinbox.MaximumValue = 64;
			spinbox.MinimumValue = 2;
			VBox.Add (spinbox);
		}

		private void Build ()
		{
			Resizable = false;

			VBox.WidthRequest = 400;
			VBox.BorderWidth = 6;
			VBox.Spacing = 6;

			red_spinbox = new HScaleSpinButtonWidget ();
			red_spinbox.Label = Catalog.GetString ("Red");
			InitSpinBox (red_spinbox);

			green_spinbox = new HScaleSpinButtonWidget ();
			green_spinbox.Label = Catalog.GetString ("Green");
			InitSpinBox (green_spinbox);

			blue_spinbox = new HScaleSpinButtonWidget ();
			blue_spinbox.Label = Catalog.GetString ("Blue");
			InitSpinBox (blue_spinbox);

			link_button = new CheckButton (Catalog.GetString ("Linked"));
			link_button.Active = true;
			VBox.Add (link_button);

			AddButton (Stock.Cancel, ResponseType.Cancel);
			AddButton (Stock.Ok, ResponseType.Ok);

			DefaultWidth = 400;
			DefaultHeight = 300;
			ShowAll ();
		}
	}
}
