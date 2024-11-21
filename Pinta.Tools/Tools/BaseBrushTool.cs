// 
// BaseBrushTool.cs
//  
// Author:
//       Joseph Hillenbrand <joehillen@gmail.com>
// 
// Copyright (c) 2010 Joseph Hillenbrand
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
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

// This is a base class for brush type tools (paintbrush, eraser, etc)
public abstract class BaseBrushTool : BaseTool
{
	protected IPaletteService Palette { get; }

	protected ImageSurface? undo_surface;
	protected bool surface_modified;
	protected MouseButton mouse_button;

	private string BRUSH_WIDTH_SETTING => $"{GetType ().Name.ToLowerInvariant ()}-brush-width";

	protected BaseBrushTool (IServiceProvider services) : base (services)
	{
		Palette = services.GetService<IPaletteService> ();

		BrushWidthSpinButton.TooltipText = Translations.GetString ("Change brush width. Shortcut keys: [ ]");
	}

	protected override bool ShowAntialiasingButton => true;

	protected int BrushWidth {
		get => brush_width?.GetValueAsInt () ?? DEFAULT_BRUSH_WIDTH;
		set {
			if (brush_width is not null)
				brush_width.Value = value;
		}
	}

	protected override void OnBuildToolBar (Box tb)
	{
		base.OnBuildToolBar (tb);

		tb.Append (BrushWidthLabel);
		tb.Append (BrushWidthSpinButton);

		// Change the cursor when the BrushWidth is changed.
		BrushWidthSpinButton.OnValueChanged += (sender, e) => SetCursor (DefaultCursor);
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		// If we are already drawing, ignore any additional mouse down events
		if (mouse_button != MouseButton.None)
			return;

		surface_modified = false;
		undo_surface = document.Layers.CurrentUserLayer.Surface.Clone ();
		mouse_button = e.MouseButton;

		OnMouseMove (document, e);
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		if (undo_surface != null && surface_modified) {
			document.History.PushNewItem (new SimpleHistoryItem (Icon, Name, undo_surface, document.Layers.CurrentUserLayerIndex));
		}

		surface_modified = false;
		undo_surface = null;
		mouse_button = MouseButton.None;
	}

	protected override bool OnKeyDown (Document document, ToolKeyEventArgs e)
	{
		Gdk.Key keyPressed = e.Key;
		switch (keyPressed) {
			case Gdk.Key.bracketleft:
				BrushWidth--;
				return true;
			case Gdk.Key.bracketright:
				BrushWidth++;
				return true;
		}

		return base.OnKeyDown (document, e);
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		if (brush_width is not null)
			settings.PutSetting (BRUSH_WIDTH_SETTING, brush_width.GetValueAsInt ());
	}

	private SpinButton? brush_width;
	private Label? brush_width_label;

	protected SpinButton BrushWidthSpinButton => brush_width ??= GtkExtensions.CreateToolBarSpinButton (1, 1e5, 1, Settings.GetSetting (BRUSH_WIDTH_SETTING, DEFAULT_BRUSH_WIDTH));
	protected Label BrushWidthLabel => brush_width_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Brush width")));
}
