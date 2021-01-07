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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinta.Tools
{
	public abstract class ArrowedEditEngine : BaseEditEngine
	{
		// NRT - These are all set by HandleBuildToolBar
		private Gtk.SeparatorToolItem arrowSep = null!;
		private ToolBarLabel arrowLabel = null!;
		private ToolBarWidget<Gtk.CheckButton> showArrowOneBox = null!, showArrowTwoBox = null!;
		private bool showOtherArrowOptions;

		private ToolBarWidget<Gtk.SpinButton> arrowSize = null!;
		private ToolBarLabel arrowSizeLabel = null!;

		private ToolBarWidget<Gtk.SpinButton> arrowAngleOffset = null!;
		private ToolBarLabel arrowAngleOffsetLabel = null!;

		private ToolBarWidget<Gtk.SpinButton> arrowLengthOffset = null!;
		private ToolBarLabel arrowLengthOffsetLabel = null!;

		private Arrow previousSettings1 = new Arrow();
		private Arrow previousSettings2 = new Arrow();

		private string ARROW1_SETTING (string prefix) => $"{prefix}-arrow1";
		private string ARROW2_SETTING (string prefix) => $"{prefix}-arrow2";
		private string ARROW_SIZE_SETTING (string prefix) => $"{prefix}-arrow-size";
		private string ARROW_ANGLE_SETTING (string prefix) => $"{prefix}-arrow-angle";
		private string ARROW_LENGTH_SETTING (string prefix) => $"{prefix}-arrow-length";

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

		public override void HandleBuildToolBar(Gtk.Toolbar tb, ISettingsService settings, string toolPrefix)
		{
			base.HandleBuildToolBar(tb, settings, toolPrefix);


			#region Show Arrows

			//Arrow separator.

			if (arrowSep == null)
			{
				arrowSep = new Gtk.SeparatorToolItem();

				showOtherArrowOptions = false;
			}

			tb.AppendItem(arrowSep);


			if (arrowLabel == null)
			{
				arrowLabel = new ToolBarLabel(string.Format(" {0}: ", Translations.GetString("Arrow")));
			}

			tb.AppendItem(arrowLabel);


			//Show arrow 1.

			if (showArrowOneBox == null) {
				showArrowOneBox = new (new Gtk.CheckButton ("1"));
				showArrowOneBox.Widget.Active = settings.GetSetting (ARROW1_SETTING (toolPrefix), previousSettings1.Show);

				showArrowOneBox.Widget.Toggled += (o, e) => {
					//Determine whether to change the visibility of Arrow options in the toolbar based on the updated Arrow showing/hiding.
					if (!showArrowOneBox.Widget.Active && !showArrowTwoBox.Widget.Active) {
						if (showOtherArrowOptions) {
							tb.Remove (arrowSizeLabel);
							tb.Remove (arrowSize);
							tb.Remove (arrowAngleOffsetLabel);
							tb.Remove (arrowAngleOffset);
							tb.Remove (arrowLengthOffsetLabel);
							tb.Remove (arrowLengthOffset);

							showOtherArrowOptions = false;
						}
					} else {
						if (!showOtherArrowOptions) {
							tb.Add (arrowSizeLabel);
							tb.Add (arrowSize);
							tb.Add (arrowAngleOffsetLabel);
							tb.Add (arrowAngleOffset);
							tb.Add (arrowLengthOffsetLabel);
							tb.Add (arrowLengthOffset);

							showOtherArrowOptions = true;
						}
					}

					LineCurveSeriesEngine? activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;

					if (activeEngine != null) {
						activeEngine.Arrow1.Show = showArrowOneBox.Widget.Active;

						DrawActiveShape (false, false, true, false, false);

						StorePreviousSettings ();
					}
				};
			}

			tb.AppendItem(showArrowOneBox);


			//Show arrow 2.
			if (showArrowTwoBox == null) {
				showArrowTwoBox = new (new Gtk.CheckButton ("2"));
				showArrowTwoBox.Widget.Active = settings.GetSetting (ARROW2_SETTING (toolPrefix), previousSettings2.Show);

				showArrowTwoBox.Widget.Toggled += (o, e) => {
					//Determine whether to change the visibility of Arrow options in the toolbar based on the updated Arrow showing/hiding.
					if (!showArrowOneBox.Widget.Active && !showArrowTwoBox.Widget.Active) {
						if (showOtherArrowOptions) {
							tb.Remove (arrowSizeLabel);
							tb.Remove (arrowSize);
							tb.Remove (arrowAngleOffsetLabel);
							tb.Remove (arrowAngleOffset);
							tb.Remove (arrowLengthOffsetLabel);
							tb.Remove (arrowLengthOffset);

							showOtherArrowOptions = false;
						}
					} else {
						if (!showOtherArrowOptions) {
							tb.Add (arrowSizeLabel);
							tb.Add (arrowSize);
							tb.Add (arrowAngleOffsetLabel);
							tb.Add (arrowAngleOffset);
							tb.Add (arrowLengthOffsetLabel);
							tb.Add (arrowLengthOffset);

							showOtherArrowOptions = true;
						}
					}

					LineCurveSeriesEngine? activeEngine = (LineCurveSeriesEngine?) ActiveShapeEngine;

					if (activeEngine != null) {
						activeEngine.Arrow2.Show = showArrowTwoBox.Widget.Active;

						DrawActiveShape (false, false, true, false, false);

						StorePreviousSettings ();
					}
				};
			}

			tb.AppendItem (showArrowTwoBox);

			#endregion Show Arrows


			#region Arrow Size

			if (arrowSizeLabel == null)
			{
				arrowSizeLabel = new ToolBarLabel(string.Format(" {0}: ", Translations.GetString("Size")));
			}

			if (arrowSize == null) {
				arrowSize = new (new Gtk.SpinButton (1, 100, 1) { Value = settings.GetSetting (ARROW_SIZE_SETTING (toolPrefix), 10) });

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

			#endregion Arrow Size


			#region Angle Offset

			if (arrowAngleOffsetLabel == null)
			{
				arrowAngleOffsetLabel = new ToolBarLabel(string.Format(" {0}: ", Translations.GetString("Angle")));
			}

			if (arrowAngleOffset == null) {
				arrowAngleOffset = new (new Gtk.SpinButton (-89, 89, 1) { Value = settings.GetSetting (ARROW_ANGLE_SETTING (toolPrefix), 15) });

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

			#endregion Angle Offset


			#region Length Offset

			if (arrowLengthOffsetLabel == null)
			{
				arrowLengthOffsetLabel = new ToolBarLabel(string.Format(" {0}: ", Translations.GetString("Length")));
			}

			if (arrowLengthOffset == null) {
				arrowLengthOffset = new (new Gtk.SpinButton (-100, 100, 1) { Value = settings.GetSetting (ARROW_LENGTH_SETTING (toolPrefix), 10) });

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

			#endregion Length Offset


			if (showOtherArrowOptions)
			{
				tb.Add(arrowSizeLabel);
				tb.Add(arrowSize);
				tb.Add(arrowAngleOffsetLabel);
				tb.Add(arrowAngleOffset);
				tb.Add(arrowLengthOffsetLabel);
				tb.Add(arrowLengthOffset);
			}
		}


		public ArrowedEditEngine(ShapeTool passedOwner): base(passedOwner)
		{
			
		}


		/// <summary>
		/// Set the new arrow's settings to be the same as what's in the toolbar settings.
		/// </summary>
		protected void setNewArrowSettings(LineCurveSeriesEngine newEngine)
		{
			if (showArrowOneBox != null)
			{
				newEngine.Arrow1.Show = showArrowOneBox.Widget.Active;
				newEngine.Arrow2.Show = showArrowTwoBox.Widget.Active;

				newEngine.Arrow1.ArrowSize = arrowSize.Widget.Value;
				newEngine.Arrow1.AngleOffset = arrowAngleOffset.Widget.Value;
				newEngine.Arrow1.LengthOffset = arrowLengthOffset.Widget.Value;

				newEngine.Arrow2.ArrowSize = newEngine.Arrow1.ArrowSize;
				newEngine.Arrow2.AngleOffset = newEngine.Arrow1.AngleOffset;
				newEngine.Arrow2.LengthOffset = newEngine.Arrow1.LengthOffset;
			}
		}


		public override void UpdateToolbarSettings(ShapeEngine engine)
		{
			if (engine != null && engine.ShapeType == ShapeTypes.OpenLineCurveSeries)
			{
				if (showArrowOneBox != null)
				{
					LineCurveSeriesEngine lCSEngine = (LineCurveSeriesEngine)engine;

					showArrowOneBox.Widget.Active = lCSEngine.Arrow1.Show;
					showArrowTwoBox.Widget.Active = lCSEngine.Arrow2.Show;
					
					if (showOtherArrowOptions)
					{
						arrowSize.Widget.Value = lCSEngine.Arrow1.ArrowSize;
						arrowAngleOffset.Widget.Value = lCSEngine.Arrow1.AngleOffset;
						arrowLengthOffset.Widget.Value = lCSEngine.Arrow1.LengthOffset;
					}
				}

				base.UpdateToolbarSettings(engine);
			}
		}

		protected override void RecallPreviousSettings()
		{
			if (showArrowOneBox != null)
			{
				showArrowOneBox.Widget.Active = previousSettings1.Show;
				showArrowTwoBox.Widget.Active = previousSettings2.Show;

				if (showOtherArrowOptions)
				{
					arrowSize.Widget.Value = previousSettings1.ArrowSize;
					arrowAngleOffset.Widget.Value = previousSettings1.AngleOffset;
					arrowLengthOffset.Widget.Value = previousSettings1.LengthOffset;
				}
			}

			base.RecallPreviousSettings();
		}

		protected override void StorePreviousSettings()
		{
			if (showArrowOneBox != null)
			{
				previousSettings1.Show = showArrowOneBox.Widget.Active;
				previousSettings2.Show = showArrowTwoBox.Widget.Active;

				previousSettings1.ArrowSize = arrowSize.Widget.Value;
				previousSettings1.AngleOffset = arrowAngleOffset.Widget.Value;
				previousSettings1.LengthOffset = arrowLengthOffset.Widget.Value;

				//Other Arrow2 settings are unnecessary since they are the same as Arrow1's.
			}

			base.StorePreviousSettings();
		}


		protected override void DrawExtras(ref Rectangle? dirty, Context g, ShapeEngine engine)
		{
            LineCurveSeriesEngine? lCSEngine = engine as LineCurveSeriesEngine;
			if (lCSEngine != null && engine.ControlPoints.Count > 0)
			{
				// Draw the arrows for the currently active shape.
				GeneratedPoint[] genPoints = engine.GeneratedPoints;

                if (lCSEngine.Arrow1.Show)
                {
                    if (genPoints.Length > 1)
                    {
                        dirty = dirty.UnionRectangles(lCSEngine.Arrow1.Draw(g, lCSEngine.OutlineColor,
                            genPoints[0].Position, genPoints[1].Position));
                    }
                }

                if (lCSEngine.Arrow2.Show)
                {
                    if (genPoints.Length > 1)
                    {
                        dirty = dirty.UnionRectangles(lCSEngine.Arrow2.Draw(g, lCSEngine.OutlineColor,
                            genPoints[genPoints.Length - 1].Position, genPoints[genPoints.Length - 2].Position));
                    }
                }
			}

			base.DrawExtras(ref dirty, g, engine);
		}
	}
}
