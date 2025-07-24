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
using Pinta.Core;

namespace Pinta.Tools;

public abstract class ArrowedEditEngine : BaseEditEngine
{
	private Gtk.Separator? arrow_sep;
	private Gtk.Label? arrow_label;
	private Gtk.CheckButton? show_arrow_one_box, show_arrow_two_box;

	private Gtk.SpinButton? arrow_size;
	private Gtk.Label? arrow_size_label;

	private Gtk.SpinButton? arrow_angle_offset;
	private Gtk.Label? arrow_angle_offset_label;

	private Gtk.SpinButton? arrow_length_offset;
	private Gtk.Label? arrow_length_offset_label;

	private Arrow previous_settings_1 = new ();
	private Arrow previous_settings_2 = new ();

	// NRT - These are all set by HandleBuildToolBar
	private ISettingsService settings = null!;
	private string tool_prefix = null!;
	private Gtk.Box toolbar = null!;
	private bool extra_toolbar_items_added = false;

	private bool ArrowOneEnabled => ArrowOneEnabledCheckBox.Active;
	private bool ArrowTwoEnabled => ArrowTwoEnabledCheckBox.Active;

	public ArrowedEditEngine (
		IServiceProvider services,
		ShapeTool passedOwner
	) : base (services, passedOwner) { }

	public override void OnSaveSettings (ISettingsService settings, string toolPrefix)
	{
		base.OnSaveSettings (settings, toolPrefix);

		if (show_arrow_one_box is not null)
			settings.PutSetting (SettingNames.Arrow1 (toolPrefix), show_arrow_one_box.Active);

		if (show_arrow_two_box is not null)
			settings.PutSetting (SettingNames.Arrow2 (toolPrefix), show_arrow_two_box.Active);

		if (arrow_size is not null)
			settings.PutSetting (SettingNames.ArrowSize (toolPrefix), arrow_size.GetValueAsInt ());

		if (arrow_angle_offset is not null)
			settings.PutSetting (SettingNames.ArrowAngle (toolPrefix), arrow_angle_offset.GetValueAsInt ());

		if (arrow_length_offset is not null)
			settings.PutSetting (SettingNames.ArrowLength (toolPrefix), arrow_length_offset.GetValueAsInt ());
	}

	public override void HandleBuildToolBar (Gtk.Box tb, ISettingsService settings, string toolPrefix)
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

		if (activeEngine == null)
			return;

		if (arrow1)
			activeEngine.Arrow1 = activeEngine.Arrow1 with { Show = ArrowOneEnabled };
		else
			activeEngine.Arrow2 = activeEngine.Arrow2 with { Show = ArrowTwoEnabled };

		DrawActiveShape (false, false, true, false, false);

		StorePreviousSettings ();
	}

	private void UpdateArrowOptionToolbarItems ()
	{
		if (ArrowOneEnabled || ArrowTwoEnabled) {

			if (extra_toolbar_items_added)
				return;

			// Carefully insert after our last toolbar widget, since the Antialiasing dropdown may have been added already.
			Gtk.Widget after_widget = ArrowTwoEnabledCheckBox;
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
		if (show_arrow_one_box == null)
			return;

		double newArrowSize = ArrowSize.Value;
		double newAngleOffset = ArrowAngleOffset.Value;
		double newLengthOffset = ArrowLengthOffset.Value;

		newEngine.Arrow1 = new (
			Show: ArrowOneEnabled,
			ArrowSize: newArrowSize,
			AngleOffset: newAngleOffset,
			LengthOffset: newLengthOffset);

		newEngine.Arrow2 = new (
			Show: ArrowTwoEnabled,
			ArrowSize: newArrowSize,
			AngleOffset: newAngleOffset,
			LengthOffset: newLengthOffset);
	}


	public override void UpdateToolbarSettings (ShapeEngine engine)
	{
		if (engine.ShapeType != ShapeTypes.OpenLineCurveSeries)
			return;

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

			previous_settings_1 = new Arrow (
				Show: ArrowOneEnabled,
				ArrowSize: ArrowSize.Value,
				AngleOffset: ArrowAngleOffset.Value,
				LengthOffset: ArrowLengthOffset.Value);

			previous_settings_2 = previous_settings_2 with { Show = ArrowTwoEnabled };

			//Other Arrow2 settings are unnecessary since they are the same as Arrow1's.
		}

		base.StorePreviousSettings ();
	}


	protected override void DrawExtras (ref RectangleD? totalDirty, Context g, ShapeEngine engine)
	{
		if (engine is LineCurveSeriesEngine lCSEngine && engine.ControlPoints.Count > 0) {

			// Draw the arrows for the currently active shape.
			ReadOnlySpan<GeneratedPoint> genPoints = engine.GeneratedPoints;

			if (lCSEngine.Arrow1.Show && genPoints.Length > 1) {
				RectangleD dirty = lCSEngine.Arrow1.Draw (
					g,
					lCSEngine.OutlineColor,
					genPoints[0].Position,
					genPoints[1].Position);
				totalDirty = totalDirty?.Union (dirty) ?? dirty;
			}

			if (lCSEngine.Arrow2.Show && genPoints.Length > 1) {
				RectangleD dirty = lCSEngine.Arrow2.Draw (
					g,
					lCSEngine.OutlineColor,
					genPoints[^1].Position,
					genPoints[^2].Position);
				totalDirty = totalDirty?.Union (dirty) ?? dirty;
			}
		}

		base.DrawExtras (ref totalDirty, g, engine);
	}

	private Gtk.Separator ArrowSeparator
		=> arrow_sep ??= GtkExtensions.CreateToolBarSeparator ();

	private Gtk.Label ArrowLabel
		=> arrow_label ??= Gtk.Label.New (string.Format (" {0}: ", Translations.GetString ("Arrow")));

	private Gtk.CheckButton ArrowOneEnabledCheckBox
		=> show_arrow_one_box ??= CreateArrowOneEnabledCheckBox ();

	private Gtk.CheckButton CreateArrowOneEnabledCheckBox ()
	{
		Gtk.CheckButton result = Gtk.CheckButton.NewWithLabel ("1");
		result.FocusOnClick = false;
		result.Active = settings.GetSetting (SettingNames.Arrow1 (tool_prefix), previous_settings_1.Show);
		result.OnToggled += (o, e) => ArrowEnabledToggled (true);
		return result;
	}

	private Gtk.CheckButton ArrowTwoEnabledCheckBox
		=> show_arrow_two_box ??= CreateArrowTwoEnabledCheckBox ();

	private Gtk.CheckButton CreateArrowTwoEnabledCheckBox ()
	{
		Gtk.CheckButton result = Gtk.CheckButton.NewWithLabel ("2");
		result.FocusOnClick = false;
		result.Active = settings.GetSetting (SettingNames.Arrow2 (tool_prefix), previous_settings_2.Show);
		result.OnToggled += (o, e) => ArrowEnabledToggled (false);
		return result;
	}

	private Gtk.Label ArrowSizeLabel
		=> arrow_size_label ??= Gtk.Label.New (string.Format (" {0}: ", Translations.GetString ("Size")));

	private Gtk.SpinButton ArrowSize
		=> arrow_size ??= CreateArrowSize ();

	private Gtk.SpinButton CreateArrowSize ()
	{
		Gtk.SpinButton result = GtkExtensions.CreateToolBarSpinButton (1, 100, 1, settings.GetSetting (SettingNames.ArrowSize (tool_prefix), 10));
		result.OnValueChanged += (o, e) => {
			var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;
			if (activeEngine == null) return;
			var size = result.Value;
			activeEngine.Arrow1 = activeEngine.Arrow1 with { ArrowSize = size };
			activeEngine.Arrow2 = activeEngine.Arrow2 with { ArrowSize = size };
			DrawActiveShape (false, false, true, false, false);
			StorePreviousSettings ();
		};
		return result;
	}

	private Gtk.Label ArrowAngleOffsetLabel
		=> arrow_angle_offset_label ??= Gtk.Label.New (string.Format (" {0}: ", Translations.GetString ("Angle")));

	private Gtk.SpinButton ArrowAngleOffset
		=> arrow_angle_offset ??= CreateArrowAngleOffset ();

	private Gtk.SpinButton CreateArrowAngleOffset ()
	{
		Gtk.SpinButton result = GtkExtensions.CreateToolBarSpinButton (-89, 89, 1, settings.GetSetting (SettingNames.ArrowAngle (tool_prefix), 15));
		result.OnValueChanged += (o, e) => {
			var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;
			if (activeEngine == null) return;
			var angle = result.Value;
			activeEngine.Arrow1 = activeEngine.Arrow1 with { AngleOffset = angle };
			activeEngine.Arrow2 = activeEngine.Arrow2 with { AngleOffset = angle };
			DrawActiveShape (false, false, true, false, false);
			StorePreviousSettings ();
		};
		return result;
	}

	private Gtk.Label ArrowLengthOffsetLabel
		=> arrow_length_offset_label ??= Gtk.Label.New (string.Format (" {0}: ", Translations.GetString ("Length")));

	private Gtk.SpinButton ArrowLengthOffset
		=> arrow_length_offset ??= CreateArrowLengthOffset ();

	private Gtk.SpinButton CreateArrowLengthOffset ()
	{
		Gtk.SpinButton result = GtkExtensions.CreateToolBarSpinButton (-100, 100, 1, settings.GetSetting (SettingNames.ArrowLength (tool_prefix), 10));
		result.OnValueChanged += (o, e) => {
			var activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;
			if (activeEngine == null) return;
			var length = result.Value;
			activeEngine.Arrow1 = activeEngine.Arrow1 with { LengthOffset = length };
			activeEngine.Arrow2 = activeEngine.Arrow2 with { LengthOffset = length };
			DrawActiveShape (false, false, true, false, false);
			StorePreviousSettings ();
		};
		return result;
	}

	private IEnumerable<Gtk.Widget> GetArrowOptionToolbarItems ()
	{
		yield return ArrowSizeLabel;
		yield return ArrowSize;
		yield return ArrowAngleOffsetLabel;
		yield return ArrowAngleOffset;
		yield return ArrowLengthOffsetLabel;
		yield return ArrowLengthOffset;
	}
};
