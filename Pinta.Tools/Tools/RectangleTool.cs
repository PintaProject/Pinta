// 
// RectangleTool.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
using Pinta.Core;

namespace Pinta.Tools;

public sealed class RectangleTool : ShapeTool
{
	private readonly IServiceProvider services;
	public RectangleTool (IServiceProvider services) : base (services)
	{
		this.services = services;
		BaseEditEngine.CorrespondingTools[ShapeType] = this;
	}

	public override string Name => Translations.GetString ("Rectangle");
	public override string Icon => Pinta.Resources.Icons.ToolRectangle;
	public override Gdk.Cursor DefaultCursor => Gdk.Cursor.NewFromTexture (Resources.GetIcon ("Cursor.Rectangle.png"), 9, 18, null);
	public override int Priority => 39;

	public override BaseEditEngine.ShapeTypes ShapeType
		=> BaseEditEngine.ShapeTypes.ClosedLineCurveSeries;

	protected override RectangleEditEngine CreateEditEngine ()
		=> new (services, this);
}
