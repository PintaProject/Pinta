// 
// ArrowedEditEngine.cs
//  
// Author:
//	   Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2014 Andrew Davis, GSoC 2014
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

using System.Collections.Generic;
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools
{
	public abstract class ArrowedEditEngine : BaseEditEngine
	{
		private Separator? arrow_sep;
		private Label? arrow_label;
		private CheckButton? show_arrow_one_box, show_arrow_two_box;

		private SpinButton? arrow_size;
		private Label? arrow_size_label;

		private SpinButton? arrow_angle_offset;
		private Label? arrow_angle_offset_label;

		private SpinButton? arrow_length_offset;
		private Label? arrow_length_offset_label;

		private readonly Arrow previous_settings_1 = new Arrow ();
		private readonly Arrow previous_settings_2 = new Arrow ();

		// NRT - These are all set by HandleBuildToolBar
		private ISettingsService settings = null!;
		private string tool_prefix = null!;
		private Box toolbar = null!;
		private bool extra_toolbar_items_added = false;

		private string ARROW1_SETTING (string prefix) => $"{prefix}-arrow1";
		private string ARROW2_SETTING (string prefix) => $"{prefix}-arrow2";
		private string ARROW_SIZE_SETTING (string prefix) => $"{prefix}-arrow-size";
		private string ARROW_ANGLE_SETTING (string prefix) => $"{prefix}-arrow-angle";
		private string ARROW_LENGTH_SETTING (string prefix) => $"{prefix}-arrow-length";

		private bool ArrowOneEnabled => ArrowOneEnabledCheckBox.Active;
		private bool ArrowTwoEnabled => ArrowTwoEnabledCheckBox.Active;

		public ArrowedEditEngine (ShapeTool passedOwner) : base (passedOwner)
		{
		}

		public override void OnSaveSettings (ISettingsService settings, string toolPrefix)
		{
			base.OnSaveSettings (settings, toolPrefix);

			if (show_arrow_one_box is not null)
				settings.PutSetting (ARROW1_SETTING (toolPrefix), show_arrow_one_box.Active);
			if (show_arrow_two_box is not null)
				settings.PutSetting (ARROW2_SETTING (toolPrefix), show_arrow_two_box.Active);
			if (arrow_size is not null)
				settings.PutSetting (ARROW_SIZE_SETTING (toolPrefix), arrow_size.GetValueAsInt ());
			if (arrow_angle_offset is not null)
				settings.PutSetting (ARROW_ANGLE_SETTING (toolPrefix), arrow_angle_offset.GetValueAsInt ());
			if (arrow_length_offset is not null)
				settings.PutSetting (ARROW_LENGTH_SETTING (toolPrefix), arrow_length_offset.GetValueAsInt ());
		}

		public override void HandleBuildToolBar (Box tb, ISettingsService settings, string toolPrefix)
		{
			base.HandleBuildToolBar (tb, settings, toolPrefix);

			this.settings = settings;
			tool_prefix = toolPrefix;
			toolbar = tb;

			tb.Append (ArrowSeparator);
			tb.Append (ArrowLabel);
			tb.Append (ArrowOneEnabledCheckBox);
			tb.Append (ArrowTwoEnabledCheckBox);

			extra_toolbar_items_added = false;

			UpdateArrowOptionToolbarItems ();
		}

		private void ArrowEnabledToggled (bool arrow1)
		{
			UpdateArrowOptionToolbarItems ();

			var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;

			if (activeEngine != null) {
				if (arrow1)
					activeEngine.Arrow1.Show = ArrowOneEnabled;
				else
					activeEngine.Arrow2.Show = ArrowTwoEnabled;

				DrawActiveShape (false, false, true, false, false);

				StorePreviousSettings ();
			}
		}

		private void UpdateArrowOptionToolbarItems ()
		{
			if (ArrowOneEnabled || ArrowTwoEnabled) {
				if (extra_toolbar_items_added)
					return;

				// Carefully insert after our last toolbar widget, since the Antialiasing dropdown may have been added already.
				Widget after_widget = ArrowTwoEnabledCheckBox;
				foreach (var widget in GetArrowOptionToolbarItems ()) {
					toolbar.InsertChildAfter (widget, after_widget);
					after_widget = widget;
				}

				extra_toolbar_items_added = true;
			} else if (extra_toolbar_items_added) {
				foreach (var widget in GetArrowOptionToolbarItems ())
					toolbar.Remove (widget);

				extra_toolbar_items_added = false;
			}
		}

		/// <summary>
		/// Set the new arrow's settings to be the same as what's in the toolbar settings.
		/// </summary>
		protected void setNewArrowSettings (LineCurveSeriesEngine newEngine)
		{
			if (show_arrow_one_box != null) {
				newEngine.Arrow1.Show = ArrowOneEnabled;
				newEngine.Arrow2.Show = ArrowTwoEnabled;

				newEngine.Arrow1.ArrowSize = ArrowSize.Value;
				newEngine.Arrow1.AngleOffset = ArrowAngleOffset.Value;
				newEngine.Arrow1.LengthOffset = ArrowLengthOffset.Value;

				newEngine.Arrow2.ArrowSize = newEngine.Arrow1.ArrowSize;
				newEngine.Arrow2.AngleOffset = newEngine.Arrow1.AngleOffset;
				newEngine.Arrow2.LengthOffset = newEngine.Arrow1.LengthOffset;
			}
		}


		public override void UpdateToolbarSettings (ShapeEngine engine)
		{
			if (engine != null && engine.ShapeType == ShapeTypes.OpenLineCurveSeries) {
				if (show_arrow_one_box != null) {
					LineCurveSeriesEngine lCSEngine = (LineCurveSeriesEngine) engine;

					ArrowOneEnabledCheckBox.Active = lCSEngine.Arrow1.Show;
					ArrowTwoEnabledCheckBox.Active = lCSEngine.Arrow2.Show;

					if (ArrowOneEnabled || ArrowTwoEnabled) {
						ArrowSize.Value = lCSEngine.Arrow1.ArrowSize;
						ArrowAngleOffset.Value = lCSEngine.Arrow1.AngleOffset;
						ArrowLengthOffset.Value = lCSEngine.Arrow1.LengthOffset;
					}
				}

				base.UpdateToolbarSettings (engine);
			}
		}

		protected override void RecallPreviousSettings ()
		{
			if (show_arrow_one_box != null) {
				ArrowOneEnabledCheckBox.Active = previous_settings_1.Show;
				ArrowTwoEnabledCheckBox.Active = previous_settings_2.Show;

				if (ArrowOneEnabled || ArrowTwoEnabled) {
					ArrowSize.Value = previous_settings_1.ArrowSize;
					ArrowAngleOffset.Value = previous_settings_1.AngleOffset;
					ArrowLengthOffset.Value = previous_settings_1.LengthOffset;
				}
			}

			base.RecallPreviousSettings ();
		}

		protected override void StorePreviousSettings ()
		{
			if (show_arrow_one_box != null) {
				previous_settings_1.Show = ArrowOneEnabled;
				previous_settings_2.Show = ArrowTwoEnabled;

				previous_settings_1.ArrowSize = ArrowSize.Value;
				previous_settings_1.AngleOffset = ArrowAngleOffset.Value;
				previous_settings_1.LengthOffset = ArrowLengthOffset.Value;

				//Other Arrow2 settings are unnecessary since they are the same as Arrow1's.
			}

			base.StorePreviousSettings ();
		}


		protected override void DrawExtras (ref RectangleD? dirty, Context g, ShapeEngine engine)
		{
			LineCurveSeriesEngine? lCSEngine = engine as LineCurveSeriesEngine;
			if (lCSEngine != null && engine.ControlPoints.Count > 0) {
				// Draw the arrows for the currently active shape.
				GeneratedPoint[] genPoints = engine.GeneratedPoints;

				if (lCSEngine.Arrow1.Show) {
					if (genPoints.Length > 1) {
						dirty = dirty.UnionRectangles (lCSEngine.Arrow1.Draw (g, lCSEngine.OutlineColor,
						    genPoints[0].Position, genPoints[1].Position));
					}
				}

				if (lCSEngine.Arrow2.Show) {
					if (genPoints.Length > 1) {
						dirty = dirty.UnionRectangles (lCSEngine.Arrow2.Draw (g, lCSEngine.OutlineColor,
						    genPoints[genPoints.Length - 1].Position, genPoints[genPoints.Length - 2].Position));
					}
				}
			}

			base.DrawExtras (ref dirty, g, engine);
		}

		private Separator ArrowSeparator => arrow_sep ??= GtkExtensions.CreateToolBarSeparator ();
		private Label ArrowLabel => arrow_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Arrow")));

		private CheckButton ArrowOneEnabledCheckBox {
			get {
				if (show_arrow_one_box is null) {
					show_arrow_one_box = CheckButton.NewWithLabel ("1");
					show_arrow_one_box.Active = settings.GetSetting (ARROW1_SETTING (tool_prefix), previous_settings_1.Show);
					show_arrow_one_box.OnToggled += (o, e) => ArrowEnabledToggled (true);
				}

				return show_arrow_one_box;
			}
		}

		private CheckButton ArrowTwoEnabledCheckBox {
			get {
				if (show_arrow_two_box is null) {
					show_arrow_two_box = CheckButton.NewWithLabel ("2");
					show_arrow_two_box.Active = settings.GetSetting (ARROW2_SETTING (tool_prefix), previous_settings_2.Show);
					show_arrow_two_box.OnToggled += (o, e) => ArrowEnabledToggled (false);
				}

				return show_arrow_two_box;
			}
		}

		private Label ArrowSizeLabel => arrow_size_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Size")));

		private SpinButton ArrowSize {
			get {
				if (arrow_size == null) {
					arrow_size = GtkExtensions.CreateToolBarSpinButton (1, 100, 1, settings.GetSetting (ARROW_SIZE_SETTING (tool_prefix), 10));

					arrow_size.OnValueChanged += (o, e) => {
						var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;

						if (activeEngine != null) {
							var size = arrow_size.Value;
							activeEngine.Arrow1.ArrowSize = size;
							activeEngine.Arrow2.ArrowSize = size;

							DrawActiveShape (false, false, true, false, false);

							StorePreviousSettings ();
						}
					};
				}

				return arrow_size;
			}
		}

		private Label ArrowAngleOffsetLabel => arrow_angle_offset_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Angle")));

		private SpinButton ArrowAngleOffset {
			get {
				if (arrow_angle_offset == null) {
					arrow_angle_offset = GtkExtensions.CreateToolBarSpinButton (-89, 89, 1, settings.GetSetting (ARROW_ANGLE_SETTING (tool_prefix), 15));

					arrow_angle_offset.OnValueChanged += (o, e) => {

						var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;
						if (activeEngine != null) {
							var angle = arrow_angle_offset.Value;
							activeEngine.Arrow1.AngleOffset = angle;
							activeEngine.Arrow2.AngleOffset = angle;

							DrawActiveShape (false, false, true, false, false);

							StorePreviousSettings ();
						}
					};
				}

				return arrow_angle_offset;
			}
		}

		private Label ArrowLengthOffsetLabel => arrow_length_offset_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Length")));

		private SpinButton ArrowLengthOffset {
			get {
				if (arrow_length_offset == null) {
					arrow_length_offset = GtkExtensions.CreateToolBarSpinButton (-100, 100, 1, settings.GetSetting (ARROW_LENGTH_SETTING (tool_prefix), 10));

					arrow_length_offset.OnValueChanged += (o, e) => {

						var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;
						if (activeEngine != null) {
							var length = arrow_length_offset.Value;
							activeEngine.Arrow1.LengthOffset = length;
							activeEngine.Arrow2.LengthOffset = length;

							DrawActiveShape (false, false, true, false, false);

							StorePreviousSettings ();
						}
					};
				}

				return arrow_length_offset;
			}
		}

		private IEnumerable<Widget> GetArrowOptionToolbarItems ()
		{
			yield return ArrowSizeLabel;
			yield return ArrowSize;
			yield return ArrowAngleOffsetLabel;
			yield return ArrowAngleOffset;
			yield return ArrowLengthOffsetLabel;
			yield return ArrowLengthOffset;
		}
	};
}
