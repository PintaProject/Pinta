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

using System;
using System.Collections.Generic;
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools
{
	public abstract class ArrowedEditEngine : BaseEditEngine
	{
		private Separator? arrowSep;
		private Label? arrowLabel;
		private CheckButton? showArrowOneBox, showArrowTwoBox;

		private SpinButton? arrowSize;
		private Label? arrowSizeLabel;

		private SpinButton? arrowAngleOffset;
		private Label? arrowAngleOffsetLabel;

		private SpinButton? arrowLengthOffset;
		private Label? arrowLengthOffsetLabel;

		private Arrow previousSettings1 = new Arrow ();
		private Arrow previousSettings2 = new Arrow ();

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

			if (showArrowOneBox is not null)
				settings.PutSetting (ARROW1_SETTING (toolPrefix), showArrowOneBox.Active);
			if (showArrowTwoBox is not null)
				settings.PutSetting (ARROW2_SETTING (toolPrefix), showArrowTwoBox.Active);
			if (arrowSize is not null)
				settings.PutSetting (ARROW_SIZE_SETTING (toolPrefix), arrowSize.GetValueAsInt ());
			if (arrowAngleOffset is not null)
				settings.PutSetting (ARROW_ANGLE_SETTING (toolPrefix), arrowAngleOffset.GetValueAsInt ());
			if (arrowLengthOffset is not null)
				settings.PutSetting (ARROW_LENGTH_SETTING (toolPrefix), arrowLengthOffset.GetValueAsInt ());
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
			if (showArrowOneBox != null) {
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
				if (showArrowOneBox != null) {
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
			if (showArrowOneBox != null) {
				ArrowOneEnabledCheckBox.Active = previousSettings1.Show;
				ArrowTwoEnabledCheckBox.Active = previousSettings2.Show;

				if (ArrowOneEnabled || ArrowTwoEnabled) {
					ArrowSize.Value = previousSettings1.ArrowSize;
					ArrowAngleOffset.Value = previousSettings1.AngleOffset;
					ArrowLengthOffset.Value = previousSettings1.LengthOffset;
				}
			}

			base.RecallPreviousSettings ();
		}

		protected override void StorePreviousSettings ()
		{
			if (showArrowOneBox != null) {
				previousSettings1.Show = ArrowOneEnabled;
				previousSettings2.Show = ArrowTwoEnabled;

				previousSettings1.ArrowSize = ArrowSize.Value;
				previousSettings1.AngleOffset = ArrowAngleOffset.Value;
				previousSettings1.LengthOffset = ArrowLengthOffset.Value;

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

		private Separator ArrowSeparator => arrowSep ??= GtkExtensions.CreateToolBarSeparator ();
		private Label ArrowLabel => arrowLabel ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Arrow")));

		private CheckButton ArrowOneEnabledCheckBox {
			get {
				if (showArrowOneBox is null) {
					showArrowOneBox = CheckButton.NewWithLabel ("1");
					showArrowOneBox.Active = settings.GetSetting (ARROW1_SETTING (tool_prefix), previousSettings1.Show);
					showArrowOneBox.OnToggled += (o, e) => ArrowEnabledToggled (true);
				}

				return showArrowOneBox;
			}
		}

		private CheckButton ArrowTwoEnabledCheckBox {
			get {
				if (showArrowTwoBox is null) {
					showArrowTwoBox = CheckButton.NewWithLabel ("2");
					showArrowTwoBox.Active = settings.GetSetting (ARROW2_SETTING (tool_prefix), previousSettings2.Show);
					showArrowTwoBox.OnToggled += (o, e) => ArrowEnabledToggled (false);
				}

				return showArrowTwoBox;
			}
		}

		private Label ArrowSizeLabel => arrowSizeLabel ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Size")));

		private SpinButton ArrowSize {
			get {
				if (arrowSize == null) {
					arrowSize = GtkExtensions.CreateToolBarSpinButton (1, 100, 1, settings.GetSetting (ARROW_SIZE_SETTING (tool_prefix), 10));

					arrowSize.OnValueChanged += (o, e) => {
						var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;

						if (activeEngine != null) {
							var size = arrowSize.Value;
							activeEngine.Arrow1.ArrowSize = size;
							activeEngine.Arrow2.ArrowSize = size;

							DrawActiveShape (false, false, true, false, false);

							StorePreviousSettings ();
						}
					};
				}

				return arrowSize;
			}
		}

		private Label ArrowAngleOffsetLabel => arrowAngleOffsetLabel ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Angle")));

		private SpinButton ArrowAngleOffset {
			get {
				if (arrowAngleOffset == null) {
					arrowAngleOffset = GtkExtensions.CreateToolBarSpinButton (-89, 89, 1, settings.GetSetting (ARROW_ANGLE_SETTING (tool_prefix), 15));

					arrowAngleOffset.OnValueChanged += (o, e) => {

						var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;
						if (activeEngine != null) {
							var angle = arrowAngleOffset.Value;
							activeEngine.Arrow1.AngleOffset = angle;
							activeEngine.Arrow2.AngleOffset = angle;

							DrawActiveShape (false, false, true, false, false);

							StorePreviousSettings ();
						}
					};
				}

				return arrowAngleOffset;
			}
		}

		private Label ArrowLengthOffsetLabel => arrowLengthOffsetLabel ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Length")));

		private SpinButton ArrowLengthOffset {
			get {
				if (arrowLengthOffset == null) {
					arrowLengthOffset = GtkExtensions.CreateToolBarSpinButton (-100, 100, 1, settings.GetSetting (ARROW_LENGTH_SETTING (tool_prefix), 10));

					arrowLengthOffset.OnValueChanged += (o, e) => {

						var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;
						if (activeEngine != null) {
							var length = arrowLengthOffset.Value;
							activeEngine.Arrow1.LengthOffset = length;
							activeEngine.Arrow2.LengthOffset = length;

							DrawActiveShape (false, false, true, false, false);

							StorePreviousSettings ();
						}
					};
				}

				return arrowLengthOffset;
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
