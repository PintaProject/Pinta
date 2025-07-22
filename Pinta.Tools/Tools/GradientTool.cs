//
// GradientTool.cs
//
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
//
// Copyright (c) 2010 Olivier Dufour
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
using Pinta.Core;

namespace Pinta.Tools;

public sealed class GradientTool : BaseTool
{
	private readonly IPaletteService palette;
	PointD startpoint;
	bool tracking;
	private ImageSurface? undo_surface;
	MouseButton button;

	public GradientTool (IServiceProvider services) : base (services)
	{
		palette = services.GetService<IPaletteService> ();
	}

	public override string Name => Translations.GetString ("Gradient");
	public override string Icon => Pinta.Resources.Icons.ToolGradient;
	public override string StatusBarText => Translations.GetString ("Click and drag to draw gradient from primary to secondary color.\nRight click to reverse.");
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_G);
	public override Gdk.Cursor DefaultCursor => Gdk.Cursor.NewFromTexture (Resources.GetIcon ("Cursor.Gradient.png"), 9, 18, null);
	public override int Priority => 31;
	protected override bool ShowAlphaBlendingButton => true;
	private GradientType SelectedGradientType => GradientDropDown.SelectedItem.GetTagOrDefault (GradientType.Linear);
	private GradientColorMode SelectedGradientColorMode => ColorModeDropDown.SelectedItem.GetTagOrDefault (GradientColorMode.Color);

	protected override void OnBuildToolBar (Gtk.Box tb)
	{
		base.OnBuildToolBar (tb);

		tb.Append (GradientLabel);
		tb.Append (GradientDropDown);
		tb.Append (GtkExtensions.CreateToolBarSeparator ());
		tb.Append (ModeLabel);
		tb.Append (ColorModeDropDown);
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		// Protect against history corruption
		if (tracking)
			return;

		startpoint = e.PointDouble;

		if (!document.Workspace.PointInCanvas (e.PointDouble))
			return;

		tracking = true;
		button = e.MouseButton;
		undo_surface = document.Layers.CurrentUserLayer.Surface.Clone ();
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		if (!tracking || e.MouseButton != button)
			return;

		tracking = false;

		// Clear the temporary scratch surface, which we don't clear in OnMouseMove
		// because it's overwritten on subsequent moves
		document.Layers.ToolLayer.Clear ();

		if (undo_surface != null)
			document.History.PushNewItem (new SimpleHistoryItem (Icon, Name, undo_surface, document.Layers.CurrentUserLayerIndex));
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (!tracking)
			return;

		var gr = CreateGradientRenderer ();

		if (button == MouseButton.Right) {
			gr.StartColor = palette.SecondaryColor.ToColorBgra ().ToPremultipliedAlpha ();
			gr.EndColor = palette.PrimaryColor.ToColorBgra ().ToPremultipliedAlpha ();
		} else {
			gr.StartColor = palette.PrimaryColor.ToColorBgra ().ToPremultipliedAlpha ();
			gr.EndColor = palette.SecondaryColor.ToColorBgra ().ToPremultipliedAlpha ();
		}

		gr.StartPoint = startpoint;
		gr.EndPoint = e.PointDouble;
		gr.AlphaBlending = UseAlphaBlending;

		gr.BeforeRender ();

		var selection_bounds = document.GetSelectedBounds (true);
		var scratch_layer = document.Layers.ToolLayer.Surface;
		document.Layers.ToolLayer.Hidden = true;

		// Initialize the scratch layer with the (original) current layer, if any blending is required.
		if (gr.AlphaOnly || (gr.AlphaBlending && (gr.StartColor.A != 255 || gr.EndColor.A != 255))) {
			using Context g = new (scratch_layer);
			document.Selection.Clip (g);
			g.SetSourceSurface (undo_surface!, 0, 0);
			g.Operator = Operator.Source;
			g.Paint ();
		}

		ReadOnlySpan<RectangleI> selection_bounds_array = [selection_bounds];
		gr.Render (scratch_layer, selection_bounds_array);

		// Transfer the result back to the current layer.
		using Context context = document.CreateClippedContext ();
		context.SetSourceSurface (scratch_layer, 0, 0);
		context.Operator = Operator.Source;
		context.Paint ();

		selection_bounds = selection_bounds.Inflated (5, 5);
		document.Workspace.Invalidate (selection_bounds);
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		if (gradient_button is not null)
			settings.PutSetting (SettingNames.GRADIENT_TYPE, gradient_button.SelectedIndex);
		if (color_mode_button is not null)
			settings.PutSetting (SettingNames.GRADIENT_COLOR_MODE, color_mode_button.SelectedIndex);
	}

	private GradientRenderer CreateGradientRenderer ()
	{
		var op = new UserBlendOps.NormalBlendOp ();
		bool alpha_only = SelectedGradientColorMode == GradientColorMode.Transparency;

		return SelectedGradientType switch {
			GradientType.Linear => new GradientRenderers.LinearClamped (alpha_only, op),
			GradientType.LinearReflected => new GradientRenderers.LinearReflected (alpha_only, op),
			GradientType.Radial => new GradientRenderers.Radial (alpha_only, op),
			GradientType.Diamond => new GradientRenderers.LinearDiamond (alpha_only, op),
			GradientType.Conical => new GradientRenderers.Conical (alpha_only, op),
			_ => throw new InvalidOperationException ("Unknown gradient type."),
		};
	}

	private Gtk.Label? gradient_label;
	private ToolBarDropDownButton? gradient_button;
	private Gtk.Label? color_mode_label;
	private ToolBarDropDownButton? color_mode_button;

	private Gtk.Label GradientLabel => gradient_label ??= Gtk.Label.New (string.Format (" {0}: ", Translations.GetString ("Gradient")));
	private ToolBarDropDownButton GradientDropDown {
		get {
			if (gradient_button == null) {
				gradient_button = new ToolBarDropDownButton ();

				gradient_button.AddItem (Translations.GetString ("Linear Gradient"), Pinta.Resources.Icons.GradientLinear, GradientType.Linear);
				gradient_button.AddItem (Translations.GetString ("Linear Reflected Gradient"), Pinta.Resources.Icons.GradientLinearReflected, GradientType.LinearReflected);
				gradient_button.AddItem (Translations.GetString ("Linear Diamond Gradient"), Pinta.Resources.Icons.GradientDiamond, GradientType.Diamond);
				gradient_button.AddItem (Translations.GetString ("Radial Gradient"), Pinta.Resources.Icons.GradientRadial, GradientType.Radial);
				gradient_button.AddItem (Translations.GetString ("Conical Gradient"), Pinta.Resources.Icons.GradientConical, GradientType.Conical);

				gradient_button.SelectedIndex = Settings.GetSetting (SettingNames.GRADIENT_TYPE, 0);
			}

			return gradient_button;
		}
	}

	private Gtk.Label ModeLabel => color_mode_label ??= Gtk.Label.New (string.Format (" {0}: ", Translations.GetString ("Mode")));
	private ToolBarDropDownButton ColorModeDropDown {
		get {
			if (color_mode_button == null) {
				color_mode_button = new ToolBarDropDownButton ();

				color_mode_button.AddItem (Translations.GetString ("Color Mode"), Pinta.Resources.Icons.ColorModeColor, GradientColorMode.Color);
				color_mode_button.AddItem (Translations.GetString ("Transparency Mode"), Pinta.Resources.Icons.ColorModeTransparency, GradientColorMode.Transparency);

				color_mode_button.SelectedIndex = Settings.GetSetting (SettingNames.GRADIENT_COLOR_MODE, 0);
			}

			return color_mode_button;
		}
	}

	enum GradientType
	{
		Linear,
		LinearReflected,
		Diamond,
		Radial,
		Conical
	}
}
