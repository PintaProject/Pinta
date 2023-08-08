// 
// RoundedLineEditEngine.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
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
using System.Linq;
using System.Text;
using Cairo;
using Pinta.Core;

namespace Pinta.Tools
{
	public class RoundedLineEditEngine : BaseEditEngine
	{
		protected override string ShapeName => Translations.GetString ("Rounded Line Shape");

		public const double DefaultRadius = 20d;

		protected double previousRadius = DefaultRadius;

		// NRT - Created in HandleBuildToolBar
		protected Gtk.SpinButton radius = null!;
		protected Gtk.Label radius_label = null!;
		protected Gtk.Separator radius_sep = null!;

		public double Radius {
			get {
				if (radius != null)
					return radius.Value;
				else
					return BrushWidth;
			}

			set {
				if (radius != null) {
					radius.Value = value;

					ShapeEngine? selEngine = SelectedShapeEngine;

					if (selEngine != null && selEngine.ShapeType == ShapeTypes.RoundedLineSeries) {
						((RoundedLineEngine) selEngine).Radius = Radius;

						StorePreviousSettings ();

						DrawActiveShape (false, false, true, false, false);
					}
				}
			}
		}

		private static string RADIUS_SETTING (string prefix) => $"{prefix}-radius";

		public override void OnSaveSettings (ISettingsService settings, string toolPrefix)
		{
			base.OnSaveSettings (settings, toolPrefix);

			if (radius is not null)
				settings.PutSetting (RADIUS_SETTING (toolPrefix), (int) radius.Value);
		}

		public override void HandleBuildToolBar (Gtk.Box tb, ISettingsService settings, string toolPrefix)
		{
			base.HandleBuildToolBar (tb, settings, toolPrefix);


			if (radius_sep == null)
				radius_sep = GtkExtensions.CreateToolBarSeparator ();

			tb.Append (radius_sep);

			if (radius_label == null) {
				var radiusText = Translations.GetString ("Radius");
				radius_label = Gtk.Label.New ($"  {radiusText}: ");
			}

			tb.Append (radius_label);

			if (radius == null) {
				radius = GtkExtensions.CreateToolBarSpinButton (0, 1e5, 1, settings.GetSetting (RADIUS_SETTING (toolPrefix), 20));

				radius.OnValueChanged += (o, e) => {
					//Go through the Get/Set routine.
					Radius = Radius;
				};
			}

			tb.Append (radius);
		}


		public RoundedLineEditEngine (ShapeTool passedOwner) : base (passedOwner)
		{

		}

		protected override ShapeEngine CreateShape (bool ctrlKey, bool clickedOnControlPoint, PointD prevSelPoint)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			ShapeEngine newEngine = new RoundedLineEngine (doc.Layers.CurrentUserLayer, null, Radius, owner.UseAntialiasing,
				BaseEditEngine.OutlineColor, BaseEditEngine.FillColor, owner.EditEngine.BrushWidth);

			AddRectanglePoints (ctrlKey, clickedOnControlPoint, newEngine, prevSelPoint);

			//Set the new shape's DashPattern option.
			newEngine.DashPattern = dash_pattern_box.comboBox!.ComboBox.GetActiveText ()!; // NRT - Code assumes this is not-null

			return newEngine;
		}

		protected override void MovePoint (List<ControlPoint> controlPoints)
		{
			MoveRectangularPoint (controlPoints);

			base.MovePoint (controlPoints);
		}


		public override void UpdateToolbarSettings (ShapeEngine engine)
		{
			if (engine != null && engine.ShapeType == ShapeTypes.RoundedLineSeries) {
				RoundedLineEngine rLEngine = (RoundedLineEngine) engine;

				Radius = rLEngine.Radius;

				base.UpdateToolbarSettings (engine);
			}
		}

		protected override void RecallPreviousSettings ()
		{
			Radius = previousRadius;

			base.RecallPreviousSettings ();
		}

		protected override void StorePreviousSettings ()
		{
			previousRadius = Radius;

			base.StorePreviousSettings ();
		}
	}
}
