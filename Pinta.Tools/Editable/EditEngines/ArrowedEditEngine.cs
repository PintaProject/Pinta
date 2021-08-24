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
using Cairo;
using Pinta.Core;
using Gtk;

namespace Pinta.Tools
{
	public abstract class ArrowedEditEngine : BaseEditEngine
	{
		private SeparatorToolItem? arrowSep;
		private ToolBarLabel? arrowLabel;
		private ToolBarWidget<CheckButton>? showArrowOneBox, showArrowTwoBox;

		private ToolBarWidget<SpinButton>? arrowSize;
		private ToolBarLabel? arrowSizeLabel;

		private ToolBarWidget<SpinButton>? arrowAngleOffset;
		private ToolBarLabel? arrowAngleOffsetLabel;

		private ToolBarWidget<SpinButton>? arrowLengthOffset;
		private ToolBarLabel? arrowLengthOffsetLabel;

		private Arrow previousSettings1 = new Arrow ();
		private Arrow previousSettings2 = new Arrow ();

		// NRT - These are all set by HandleBuildToolBar
		private ISettingsService settings = null!;
		private string tool_prefix = null!;
		private Toolbar toolbar = null!;
		private bool extra_toolbar_items_added = false;

		private string ARROW1_SETTING (string prefix) => $"{prefix}-arrow1";
		private string ARROW2_SETTING (string prefix) => $"{prefix}-arrow2";
		private string ARROW_SIZE_SETTING (string prefix) => $"{prefix}-arrow-size";
		private string ARROW_ANGLE_SETTING (string prefix) => $"{prefix}-arrow-angle";
		private string ARROW_LENGTH_SETTING (string prefix) => $"{prefix}-arrow-length";

		private bool ArrowOneEnabled => ArrowOneEnabledCheckBox.Widget.Active;
		private bool ArrowTwoEnabled => ArrowTwoEnabledCheckBox.Widget.Active;

		public ArrowedEditEngine (ShapeTool passedOwner) : base (passedOwner)
		{
		}

		public override void OnSaveSettings (ISettingsService settings, string toolPrefix)
		{
			base.OnSaveSettings (settings, toolPrefix);

			if (showArrowOneBox is not null)
				settings.PutSetting (ARROW1_SETTING (toolPrefix), showArrowOneBox.Widget.Active);
			if (showArrowTwoBox is not null)
				settings.PutSetting (ARROW2_SETTING (toolPrefix), showArrowTwoBox.Widget.Active);
			if (arrowSize is not null)
				settings.PutSetting (ARROW_SIZE_SETTING (toolPrefix), arrowSize.Widget.ValueAsInt);
			if (arrowAngleOffset is not null)
				settings.PutSetting (ARROW_ANGLE_SETTING (toolPrefix), arrowAngleOffset.Widget.ValueAsInt);
			if (arrowLengthOffset is not null)
				settings.PutSetting (ARROW_LENGTH_SETTING (toolPrefix), arrowLengthOffset.Widget.ValueAsInt);
		}

		public override void HandleBuildToolBar (Toolbar tb, ISettingsService settings, string toolPrefix)
		{
			base.HandleBuildToolBar (tb, settings, toolPrefix);

			this.settings = settings;
			tool_prefix = toolPrefix;
			toolbar = tb;

			tb.AppendItem (ArrowSeparator);
			tb.AppendItem (ArrowLabel);
			tb.AppendItem (ArrowOneEnabledCheckBox);
			tb.AppendItem (ArrowTwoEnabledCheckBox);

			extra_toolbar_items_added = false;

			UpdateArrowOptionToolbarItems (true);
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

		private void UpdateArrowOptionToolbarItems (bool initial = false)
		{
			// We have to do some hackery to get around the fact that the Antialiasing
			// dropdown is added after our inital toolbar build. As we
			// add and remove these extra toolbar items we always want them to be before
			// the Antialiasing dropdown.
			var offset = initial ? 0 : 2;

			if (ArrowOneEnabled || ArrowTwoEnabled) {
				if (extra_toolbar_items_added)
					return;

				toolbar.Insert (ArrowSizeLabel, toolbar.NItems - offset);
				toolbar.Insert (ArrowSize, toolbar.NItems - offset);
				toolbar.Insert (ArrowAngleOffsetLabel, toolbar.NItems - offset);
				toolbar.Insert (ArrowAngleOffset, toolbar.NItems - offset);
				toolbar.Insert (ArrowLengthOffsetLabel, toolbar.NItems - offset);
				toolbar.Insert (ArrowLengthOffset, toolbar.NItems - offset);

				extra_toolbar_items_added = true;
			} else {
				toolbar.Remove (ArrowSizeLabel);
				toolbar.Remove (ArrowSize);
				toolbar.Remove (ArrowAngleOffsetLabel);
				toolbar.Remove (ArrowAngleOffset);
				toolbar.Remove (ArrowLengthOffsetLabel);
				toolbar.Remove (ArrowLengthOffset);

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

				newEngine.Arrow1.ArrowSize = ArrowSize.Widget.Value;
				newEngine.Arrow1.AngleOffset = ArrowAngleOffset.Widget.Value;
				newEngine.Arrow1.LengthOffset = ArrowLengthOffset.Widget.Value;

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

					ArrowOneEnabledCheckBox.Widget.Active = lCSEngine.Arrow1.Show;
					ArrowTwoEnabledCheckBox.Widget.Active = lCSEngine.Arrow2.Show;

					if (ArrowOneEnabled || ArrowTwoEnabled) {
						ArrowSize.Widget.Value = lCSEngine.Arrow1.ArrowSize;
						ArrowAngleOffset.Widget.Value = lCSEngine.Arrow1.AngleOffset;
						ArrowLengthOffset.Widget.Value = lCSEngine.Arrow1.LengthOffset;
					}
				}

				base.UpdateToolbarSettings (engine);
			}
		}

		protected override void RecallPreviousSettings ()
		{
			if (showArrowOneBox != null) {
				ArrowOneEnabledCheckBox.Widget.Active = previousSettings1.Show;
				ArrowTwoEnabledCheckBox.Widget.Active = previousSettings2.Show;

				if (ArrowOneEnabled || ArrowTwoEnabled) {
					ArrowSize.Widget.Value = previousSettings1.ArrowSize;
					ArrowAngleOffset.Widget.Value = previousSettings1.AngleOffset;
					ArrowLengthOffset.Widget.Value = previousSettings1.LengthOffset;
				}
			}

			base.RecallPreviousSettings ();
		}

		protected override void StorePreviousSettings ()
		{
			if (showArrowOneBox != null) {
				previousSettings1.Show = ArrowOneEnabled;
				previousSettings2.Show = ArrowTwoEnabled;

				previousSettings1.ArrowSize = ArrowSize.Widget.Value;
				previousSettings1.AngleOffset = ArrowAngleOffset.Widget.Value;
				previousSettings1.LengthOffset = ArrowLengthOffset.Widget.Value;

				//Other Arrow2 settings are unnecessary since they are the same as Arrow1's.
			}

			base.StorePreviousSettings ();
		}


		protected override void DrawExtras (ref Rectangle? dirty, Context g, ShapeEngine engine)
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

		private SeparatorToolItem ArrowSeparator => arrowSep ??= new SeparatorToolItem ();
		private ToolBarLabel ArrowLabel => arrowLabel ??= new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Arrow")));

		private ToolBarWidget<CheckButton> ArrowOneEnabledCheckBox {
			get {
				if (showArrowOneBox is null) {
					showArrowOneBox = new (new CheckButton ("1"));
					showArrowOneBox.Widget.Active = settings.GetSetting (ARROW1_SETTING (tool_prefix), previousSettings1.Show);
					showArrowOneBox.Widget.Toggled += (o, e) => ArrowEnabledToggled (true);
				}

				return showArrowOneBox;
			}
		}

		private ToolBarWidget<CheckButton> ArrowTwoEnabledCheckBox {
			get {
				if (showArrowTwoBox is null) {
					showArrowTwoBox = new (new CheckButton ("2"));
					showArrowTwoBox.Widget.Active = settings.GetSetting (ARROW2_SETTING (tool_prefix), previousSettings2.Show);
					showArrowTwoBox.Widget.Toggled += (o, e) => ArrowEnabledToggled (false);
				}

				return showArrowTwoBox;
			}
		}

		private ToolBarLabel ArrowSizeLabel => arrowSizeLabel ??= new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Size")));

		private ToolBarWidget<SpinButton> ArrowSize {
			get {
				if (arrowSize == null) {
					arrowSize = new (new SpinButton (1, 100, 1) { Value = settings.GetSetting (ARROW_SIZE_SETTING (tool_prefix), 10) });

					arrowSize.Widget.ValueChanged += (o, e) => {
						var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;

						if (activeEngine != null) {
							var size = arrowSize.Widget.Value;
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

		private ToolBarLabel ArrowAngleOffsetLabel => arrowAngleOffsetLabel ??= new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Angle")));

		private ToolBarWidget<SpinButton> ArrowAngleOffset {
			get {
				if (arrowAngleOffset == null) {
					arrowAngleOffset = new (new SpinButton (-89, 89, 1) { Value = settings.GetSetting (ARROW_ANGLE_SETTING (tool_prefix), 15) });

					arrowAngleOffset.Widget.ValueChanged += (o, e) => {

						var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;
						if (activeEngine != null) {
							var angle = arrowAngleOffset.Widget.Value;
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

		private ToolBarLabel ArrowLengthOffsetLabel => arrowLengthOffsetLabel ??= new ToolBarLabel (string.Format (" {0}: ", Translations.GetString ("Length")));

		private ToolBarWidget<SpinButton> ArrowLengthOffset {
			get {
				if (arrowLengthOffset == null) {
					arrowLengthOffset = new (new SpinButton (-100, 100, 1) { Value = settings.GetSetting (ARROW_LENGTH_SETTING (tool_prefix), 10) });

					arrowLengthOffset.Widget.ValueChanged += (o, e) => {

						var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;
						if (activeEngine != null) {
							var length = arrowLengthOffset.Widget.Value;
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
	}
}
