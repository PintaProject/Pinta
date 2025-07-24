/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

// Additional code:
// 
// FloodTool.cs
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
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

public abstract class FloodTool : BaseTool
{
	protected Label? mode_label;
	protected ToolBarDropDownButton? mode_button;
	protected Separator? mode_sep;
	protected Label? tolerance_label;
	protected Scale? tolerance_slider;

	public FloodTool (IServiceProvider services) : base (services) { }

	protected bool IsContinguousMode => ModeDropDown.SelectedItem.GetTagOrDefault (true);
	protected float Tolerance => (float) (ToleranceSlider.GetValue () / 100);
	protected virtual bool CalculatePolygonSet => true;
	protected bool LimitToSelection { get; set; } = true;

	protected override void OnBuildToolBar (Gtk.Box tb)
	{
		base.OnBuildToolBar (tb);

		tb.Append (ModeLabel);
		tb.Append (ModeDropDown);
		tb.Append (Separator);
		tb.Append (ToleranceLabel);
		tb.Append (ToleranceSlider);
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		var pos = e.Point;

		// Don't do anything if we're outside the canvas
		if (pos.X < 0 || pos.X >= document.ImageSize.Width)
			return;
		if (pos.Y < 0 || pos.Y >= document.ImageSize.Height)
			return;

		base.OnMouseDown (document, e);

		var currentRegion = CairoExtensions.CreateRegion (document.GetSelectedBounds (true));
		// See if the mouse click is valid
		if (!currentRegion.ContainsPoint (pos.X, pos.Y) && LimitToSelection)
			return;

		var surface = document.Layers.CurrentUserLayer.Surface;
		var stencilBuffer = new BitMask (surface.Width, surface.Height);
		var tol = (int) (Tolerance * Tolerance * 256);

		RectangleD boundingBox;

		if (IsContinguousMode)
			CairoExtensions.FillStencilFromPoint (surface, stencilBuffer, pos, tol, out boundingBox, currentRegion, LimitToSelection);
		else
			CairoExtensions.FillStencilByColor (surface, stencilBuffer, surface.GetColorBgra (pos), tol, out boundingBox, currentRegion, LimitToSelection);

		OnFillRegionComputed (document, stencilBuffer);

		// If a derived tool is only going to use the stencil,
		// don't waste time building the polygon set
		if (CalculatePolygonSet) {
			var polygonSet = stencilBuffer.CreatePolygonSet (boundingBox, PointI.Zero);
			OnFillRegionComputed (document, polygonSet);
		}
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		if (mode_button is not null)
			settings.PutSetting (SettingNames.FloodToolFillMode (this), mode_button.SelectedIndex);
		if (tolerance_slider is not null)
			settings.PutSetting (SettingNames.FloodToolFillTolerance (this), (int) tolerance_slider.GetValue ());
	}

	protected virtual void OnFillRegionComputed (Document document, IReadOnlyList<IReadOnlyList<PointI>> polygonSet) { }
	protected virtual void OnFillRegionComputed (Document document, BitMask stencil) { }

	protected Label ModeLabel => mode_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Flood Mode")));
	protected Label ToleranceLabel => tolerance_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Tolerance")));
	protected Scale ToleranceSlider => tolerance_slider ??= GtkExtensions.CreateToolBarSlider (0, 100, 1, Settings.GetSetting (SettingNames.FloodToolFillTolerance (this), 0));
	protected Separator Separator => mode_sep ??= GtkExtensions.CreateToolBarSeparator ();

	protected ToolBarDropDownButton ModeDropDown {
		get {
			if (mode_button is null) {
				mode_button = new ToolBarDropDownButton ();

				mode_button.AddItem (Translations.GetString ("Contiguous"), Pinta.Resources.Icons.ToolFreeformShape, true);
				mode_button.AddItem (Translations.GetString ("Global"), Pinta.Resources.Icons.HelpWebsite, false);

				mode_button.SelectedIndex = Settings.GetSetting (SettingNames.FloodToolFillMode (this), 0);
			}

			return mode_button;
		}
	}
}
