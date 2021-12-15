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
	// Note about TransparentMode.  The core issue is we can't just paint it on top of the
	// current layer because it's transparent.  Will require significant effort to support.
	public class GradientTool : BaseTool
	{
		private readonly IPaletteService palette;
		Cairo.PointD startpoint;
		bool tracking;
		protected ImageSurface? undo_surface;
		MouseButton button;

		private const string GRADIENT_TYPE_SETTING = "gradient-type";

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
		private GradientType SelectedGradientType => GradientDropDown.SelectedItem.GetTagOrDefault (GradientType.Linear);

		protected override void OnBuildToolBar (Gtk.Toolbar tb)
		{
			base.OnBuildToolBar (tb);

			tb.AppendItem (GradientLabel);
			tb.AppendItem (GradientDropDown);
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

				gr.Render (scratch_layer, new[] { selection_bounds });

				using (var g = document.CreateClippedContext ()) {
					g.SetSource (scratch_layer);
					g.Paint ();
				}

				document.Layers.ToolLayer.Clear ();

				selection_bounds.Inflate (5, 5);
				document.Workspace.Invalidate (selection_bounds);
			}
		}

		protected override void OnSaveSettings (ISettingsService settings)
		{
			base.OnSaveSettings (settings);

			if (gradient_button is not null)
				settings.PutSetting (GRADIENT_TYPE_SETTING, gradient_button.SelectedIndex);
		}

		private GradientRenderer CreateGradientRenderer ()
		{
			var op = new UserBlendOps.NormalBlendOp ();

			return SelectedGradientType switch {
				GradientType.Linear => new GradientRenderers.LinearClamped (false, op),
				GradientType.LinearReflected => new GradientRenderers.LinearReflected (false, op),
				GradientType.Radial => new GradientRenderers.Radial (false, op),
				GradientType.Diamond => new GradientRenderers.LinearDiamond (false, op),
				GradientType.Conical => new GradientRenderers.Conical (false, op),
				_ => throw new ArgumentOutOfRangeException ("Unknown gradient type."),
			};
		}

		private ToolBarLabel? gradient_label;
		private ToolBarDropDownButton? gradient_button;

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
