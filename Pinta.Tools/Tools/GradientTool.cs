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

namespace Pinta.Tools
{
	public class GradientTool : BaseTool
	{
		private readonly IPaletteService palette;
		Cairo.PointD startpoint;
		bool tracking;
		protected ImageSurface? undo_surface;
		MouseButton button;

		private const string GRADIENT_TYPE_SETTING = "gradient-type";
		private const string GRADIENT_COLOR_MODE_SETTING = "gradient-color-mode";

		public GradientTool (IServiceManager services) : base (services)
		{
			palette = services.GetService<IPaletteService> ();
		}

		public override string Name => Translations.GetString ("Gradient");
		public override string Icon => Pinta.Resources.Icons.ToolGradient;
		public override string StatusBarText => Translations.GetString ("Click and drag to draw gradient from primary to secondary color.  Right click to reverse.");
		public override Gdk.Key ShortcutKey => Gdk.Key.G;
		public override Gdk.Cursor DefaultCursor => new Gdk.Cursor (Gdk.Display.Default, Resources.GetIcon ("Cursor.Gradient.png"), 9, 18);
		public override int Priority => 31;
		protected override bool ShowAlphaBlendingButton => true;
		private GradientType SelectedGradientType => GradientDropDown.SelectedItem.GetTagOrDefault (GradientType.Linear);
		private GradientColorMode SelectedGradientColorMode => ColorModeDropDown.SelectedItem.GetTagOrDefault (GradientColorMode.Color);

		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			tb.AppendItem (GradientLabel);
			tb.AppendItem (GradientDropDown);
			tb.AppendItem (new Gtk.SeparatorToolItem ());
			tb.AppendItem (ModeLabel);
			tb.AppendItem (ColorModeDropDown);
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

			if (undo_surface != null)
				document.History.PushNewItem (new SimpleHistoryItem (Icon, Name, undo_surface, document.Layers.CurrentUserLayerIndex));
		}

		protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
		{
			if (tracking) {
				var gr = CreateGradientRenderer ();

				if (button == MouseButton.Right) {
					gr.StartColor = palette.SecondaryColor.ToColorBgra ();
					gr.EndColor = palette.PrimaryColor.ToColorBgra ();
				} else {
					gr.StartColor = palette.PrimaryColor.ToColorBgra ();
					gr.EndColor = palette.SecondaryColor.ToColorBgra ();
				}

				gr.StartPoint = startpoint;
				gr.EndPoint = e.PointDouble;
				gr.AlphaBlending = UseAlphaBlending;

				gr.BeforeRender ();

				var selection_bounds = document.GetSelectedBounds (true);
				var scratch_layer = document.Layers.ToolLayer.Surface;

				// Initialize the scratch layer with the (original) current layer, if any blending is required.
				if (gr.AlphaOnly || (gr.AlphaBlending && (gr.StartColor.A != 255 || gr.EndColor.A != 255))) {
					using (var g = new Context (scratch_layer)) {
						document.Selection.Clip (g);
						g.SetSource (undo_surface);
						g.Operator = Operator.Source;
						g.Paint ();
					}
				}

				gr.Render (scratch_layer, new[] { selection_bounds });

				// Transfer the result back to the current layer.
				using (var g = document.CreateClippedContext ()) {
					g.SetSource (scratch_layer);
					g.Operator = Operator.Source;
					g.Paint ();
				}

				selection_bounds.Inflate (5, 5);
				document.Workspace.Invalidate (selection_bounds);
			}
		}

		protected override void OnSaveSettings (ISettingsService settings)
		{
			base.OnSaveSettings (settings);

			if (gradient_button is not null)
				settings.PutSetting (GRADIENT_TYPE_SETTING, gradient_button.SelectedIndex);
			if (color_mode_button is not null)
				settings.PutSetting (GRADIENT_COLOR_MODE_SETTING, color_mode_button.SelectedIndex);
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
				_ => throw new ArgumentOutOfRangeException ("Unknown gradient type."),
			};
		}

		private ToolBarLabel? gradient_label;
		private ToolBarDropDownButton? gradient_button;
		private ToolBarLabel? color_mode_label;
		private ToolBarDropDownButton? color_mode_button;

		private ToolBarLabel GradientLabel => gradient_label ??= new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Gradient")));
		private ToolBarDropDownButton GradientDropDown {
			get {
				if (gradient_button == null) {
					gradient_button = new ToolBarDropDownButton ();

					gradient_button.AddItem (Translations.GetString ("Linear Gradient"), Pinta.Resources.Icons.GradientLinear, GradientType.Linear);
					gradient_button.AddItem (Translations.GetString ("Linear Reflected Gradient"), Pinta.Resources.Icons.GradientLinearReflected, GradientType.LinearReflected);
					gradient_button.AddItem (Translations.GetString ("Linear Diamond Gradient"), Pinta.Resources.Icons.GradientDiamond, GradientType.Diamond);
					gradient_button.AddItem (Translations.GetString ("Radial Gradient"), Pinta.Resources.Icons.GradientRadial, GradientType.Radial);
					gradient_button.AddItem (Translations.GetString ("Conical Gradient"), Pinta.Resources.Icons.GradientConical, GradientType.Conical);

					gradient_button.SelectedIndex = Settings.GetSetting (GRADIENT_TYPE_SETTING, 0);
				}

				return gradient_button;
			}
		}
		
		private ToolBarLabel ModeLabel => color_mode_label ??= new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Mode")));
		private ToolBarDropDownButton ColorModeDropDown {
			get {
				if (color_mode_button == null) {
					color_mode_button = new ToolBarDropDownButton ();

					color_mode_button.AddItem (Translations.GetString ("Color Mode"), Pinta.Resources.Icons.ColorModeColor, GradientColorMode.Color);
					color_mode_button.AddItem (Translations.GetString ("Transparency Mode"), Pinta.Resources.Icons.ColorModeTransparency, GradientColorMode.Transparency);

					color_mode_button.SelectedIndex = Settings.GetSetting (GRADIENT_COLOR_MODE_SETTING, 0);
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
}
