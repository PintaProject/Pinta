// 
// LineCurveEditEngine.cs
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
using Pinta.Core;

namespace Pinta.Tools;

public sealed class LineCurveEditEngine : ArrowedEditEngine
{
	protected override string ShapeName
		=> Translations.GetString ("Open Curve Shape");

	private readonly IWorkspaceService workspace;
	public LineCurveEditEngine (
		IServiceProvider services,
		ShapeTool passedOwner
	)
		: base (services, passedOwner)
	{
		workspace = services.GetService<IWorkspaceService> ();
	}

	protected override ShapeEngine CreateShape (
		bool ctrlKey,
		bool clickedOnControlPoint,
		PointD prevSelPoint)
	{
		Document doc = workspace.ActiveDocument;

		LineCurveSeriesEngine newEngine = new (
			doc.Layers.CurrentUserLayer,
			null,
			BaseEditEngine.ShapeTypes.OpenLineCurveSeries,
			owner.UseAntialiasing,
			false,
			BaseEditEngine.OutlineColor,
			BaseEditEngine.FillColor,
			owner.EditEngine.BrushWidth);

		AddLinePoints (ctrlKey, clickedOnControlPoint, newEngine, prevSelPoint);

		//Set the new shape's DashPattern option.
		newEngine.DashPattern = dash_pattern_box.ComboBox!.ComboBox.GetActiveText ()!; // NRT - Code assumes this is not-null

		//Set the new arrow's settings to be the same as what's in the toolbar settings.
		setNewArrowSettings (newEngine);

		return newEngine;
	}

	protected override void MovePoint (List<ControlPoint> controlPoints)
	{
		base.MovePoint (controlPoints);
	}
}
