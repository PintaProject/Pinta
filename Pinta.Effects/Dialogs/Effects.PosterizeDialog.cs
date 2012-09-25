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
	public partial class PosterizeDialog : Gtk.Dialog
	{
		public int Red {
			get { return hscalespinRed.ValueAsInt; }
		}
		
		public int Green { 
			get { return hscalespinGreen.ValueAsInt; }
		}
		
		public int Blue {
            		get { return hscalespinBlue.ValueAsInt; }
		}

		public PosterizeDialog () : base (Catalog.GetString ("Posterize"),
		                                  PintaCore.Chrome.MainWindow, DialogFlags.Modal)
		{
			Build ();
			
			hscalespinRed.ValueChanged += HandleValueChanged;
			hscalespinGreen.ValueChanged += HandleValueChanged;
			hscalespinBlue.ValueChanged += HandleValueChanged;

			AlternativeButtonOrder = new int[] { (int) Gtk.ResponseType.Ok, (int) Gtk.ResponseType.Cancel };
			DefaultResponse = Gtk.ResponseType.Ok;
		}
		
		public PosterizeData EffectData { get; set; }
		
		void HandleValueChanged (object sender, EventArgs e)
		{
			var widget = sender as HScaleSpinButtonWidget;
			
			if (checkLinked.Active)
				hscalespinGreen.Value = hscalespinBlue.Value = hscalespinRed.Value = widget.Value;
			
			UpdateEffectData ();
		}
		
		void UpdateEffectData ()
		{
			if (EffectData == null)
				return;
			
			EffectData.Red = hscalespinRed.ValueAsInt;
			EffectData.Green = hscalespinGreen.ValueAsInt;
			EffectData.Blue = hscalespinBlue.ValueAsInt;
			
			// Only fire event once, even if all properties have changed.
			EffectData.FirePropertyChanged ("_all_");
		}
	}
}
