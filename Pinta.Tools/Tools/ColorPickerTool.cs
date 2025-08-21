//
// ColorPickerTool.cs
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
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class ColorPickerTool : BaseTool
{
	private readonly IPaletteService palette;
	private readonly IToolService tools;

	private MouseButton button_down;

	public ColorPickerTool (IServiceProvider services) : base (services)
	{
		palette = services.GetService<IPaletteService> ();
		tools = services.GetService<IToolService> ();
	}

	public override string Name => Translations.GetString ("Color Picker");
	public override string Icon => Pinta.Resources.Icons.ToolColorPicker;
	public override string StatusBarText => Translations.GetString ("Left click to set primary color.\nRight click to set secondary color.");
	public override bool CursorChangesOnZoom => true;
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_K);
	public override int Priority => 33;
	private int SampleSize => SampleSizeDropDown.SelectedItem.GetTagOrDefault (1);
	private bool SampleLayerOnly => SampleTypeDropDown.SelectedItem.GetTagOrDefault (false);

	public override Gdk.Cursor DefaultCursor {
		get {
			var icon = GdkExtensions.CreateIconWithShape ("Cursor.ColorPicker.png",
							CursorShape.Rectangle, SampleSize, 7, 27,
							out var iconOffsetX, out var iconOffsetY);
			return Gdk.Cursor.NewFromTexture (icon, iconOffsetX, iconOffsetY, null);
		}
	}

	protected override void OnBuildToolBar (Box tb)
	{
		base.OnBuildToolBar (tb);

		tb.Append (SamplingLabel);
		tb.Append (SampleSizeDropDown);
		tb.Append (SampleTypeDropDown);

		tb.Append (Separator);

		tb.Append (ToolSelectionLabel);
		tb.Append (ToolSelectionDropDown);
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		if (e.MouseButton == MouseButton.Left)
			button_down = MouseButton.Left;
		else if (e.MouseButton == MouseButton.Right)
			button_down = MouseButton.Right;

		if (!document.Workspace.PointInCanvas (e.PointDouble))
			return;

		var color = GetColorFromPoint (document, e.Point);

		if (button_down == MouseButton.Left)
			palette.SetColor (true, color, false);
		else if (button_down == MouseButton.Right)
			palette.SetColor (false, color, false);
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (button_down == MouseButton.None)
			return;

		if (!document.Workspace.PointInCanvas (e.PointDouble))
			return;

		var color = GetColorFromPoint (document, e.Point);

		if (button_down == MouseButton.Left)
			palette.SetColor (true, color, false);
		else if (button_down == MouseButton.Right)
			palette.SetColor (false, color, false);
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		// Even though we've already set the color, we don't add it to the
		// recently used while we're moving the mouse around.  We only want
		// to set it now, when the user releasing the mouse button.
		if (button_down == MouseButton.Left)
			palette.SetColor (true, palette.PrimaryColor, true);
		else if (button_down == MouseButton.Right)
			palette.SetColor (false, palette.SecondaryColor, true);

		button_down = MouseButton.None;

		if (ToolSelectionDropDown.SelectedItem.GetTagOrDefault (0) == 1 && tools.PreviousTool is not null)
			tools.SetCurrentTool (tools.PreviousTool);
		else if (ToolSelectionDropDown.SelectedItem.GetTagOrDefault (0) == 2)
			tools.SetCurrentTool (nameof (PencilTool));
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		if (tool_select is not null)
			settings.PutSetting (SettingNames.COLOR_PICKER_TOOL_SELECTION, tool_select.SelectedIndex);
		if (sample_size is not null)
			settings.PutSetting (SettingNames.COLOR_PICKER_SAMPLE_SIZE, sample_size.SelectedIndex);
		if (sample_type is not null)
			settings.PutSetting (SettingNames.COLOR_PICKER_SAMPLE_TYPE, sample_type.SelectedIndex);
	}

	private Color GetColorFromPoint (Document document, PointI point)
	{
		ImmutableArray<ColorBgra> pixels = GetPixelsFromPoint (document, point);
		if (pixels.Length == 0)
			return Color.Transparent; // TODO: Check if this scenario is possible, otherwise remove condition
		else
			return ColorBgra.Blend (pixels.AsSpan ()).ToCairoColor ();
	}

	private ImmutableArray<ColorBgra> GetPixelsFromPoint (Document document, PointI point)
	{
		var size = SampleSize;
		var half = size / 2;

		// Short circuit for single pixel
		if (size == 1)
			return ImmutableArray.Create (GetPixel (document, point));

		// Find the pixels we need (clamp to the size of the image)
		var rect = new RectangleI (point.X - half, point.Y - half, size, size);
		var intersected = rect.Intersect (new RectangleI (PointI.Zero, document.ImageSize));

		var totalRectanglePixels = intersected.Size.Width * intersected.Size.Height;

		var pixels = ImmutableArray.CreateBuilder<ColorBgra> (totalRectanglePixels);

		for (var i = intersected.Left; i <= intersected.Right; i++)
			for (var j = intersected.Top; j <= intersected.Bottom; j++)
				pixels.Add (GetPixel (document, new (i, j)));

		return pixels.MoveToImmutable ();
	}

	private ColorBgra GetPixel (Document document, PointI position)
	{
		if (SampleLayerOnly)
			return document.Layers.CurrentUserLayer.Surface.GetColorBgra (position);
		else
			return document.GetComputedPixel (position);
	}

	private ToolBarDropDownButton? tool_select;
	private Label? tool_select_label;
	private Label? sampling_label;
	private ToolBarDropDownButton? sample_size;
	private ToolBarDropDownButton? sample_type;
	private Separator? sample_sep;

	private Label ToolSelectionLabel => tool_select_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("After select")));
	private Label SamplingLabel => sampling_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Sampling")));
	private Separator Separator => sample_sep ??= GtkExtensions.CreateToolBarSeparator ();

	private ToolBarDropDownButton ToolSelectionDropDown {
		get {
			if (tool_select is null) {
				tool_select = new ToolBarDropDownButton (true);

				tool_select.AddItem (Translations.GetString ("Do not switch tool"), Pinta.Resources.Icons.ToolColorPicker, 0);
				tool_select.AddItem (Translations.GetString ("Switch to previous tool"), Pinta.Resources.Icons.ToolColorPickerPreviousTool, 1);
				tool_select.AddItem (Translations.GetString ("Switch to Pencil tool"), Pinta.Resources.Icons.ToolPencil, 2);

				tool_select.SelectedIndex = Settings.GetSetting (SettingNames.COLOR_PICKER_TOOL_SELECTION, 0);
			}

			return tool_select;
		}
	}

	private ToolBarDropDownButton SampleSizeDropDown {
		get {
			if (sample_size is null) {
				sample_size = new ToolBarDropDownButton (true);

				// Change the cursor when the SampleSize is changed.
				sample_size.SelectedItemChanged += (sender, e) => SetCursor (DefaultCursor);

				sample_size.AddItem (Translations.GetString ("Single Pixel"), Pinta.Resources.Icons.Sampling1, 1);
				sample_size.AddItem (Translations.GetString ("3 x 3 Region"), Pinta.Resources.Icons.Sampling3, 3);
				sample_size.AddItem (Translations.GetString ("5 x 5 Region"), Pinta.Resources.Icons.Sampling5, 5);
				sample_size.AddItem (Translations.GetString ("7 x 7 Region"), Pinta.Resources.Icons.Sampling7, 7);
				sample_size.AddItem (Translations.GetString ("9 x 9 Region"), Pinta.Resources.Icons.Sampling9, 9);

				sample_size.SelectedIndex = Settings.GetSetting (SettingNames.COLOR_PICKER_SAMPLE_SIZE, 0);
			}

			return sample_size;
		}
	}

	private ToolBarDropDownButton SampleTypeDropDown {
		get {
			if (sample_type is null) {
				sample_type = new ToolBarDropDownButton (true);

				sample_type.AddItem (Translations.GetString ("Layer"), Pinta.Resources.Icons.LayerMergeDown, true);
				sample_type.AddItem (Translations.GetString ("Image"), Pinta.Resources.Icons.ResizeCanvasBase, false);

				sample_type.SelectedIndex = Settings.GetSetting (SettingNames.COLOR_PICKER_SAMPLE_TYPE, 0);
			}

			return sample_type;
		}
	}
}
