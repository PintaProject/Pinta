// 
// CoreToolsExtension.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2011 Jonathan Pobst
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

[assembly: Mono.Addins.Addin ("DefaultTools", PintaCore.ApplicationVersion, Category = "Core")]
[assembly: Mono.Addins.AddinName ("Default Tools")]
[assembly: Mono.Addins.AddinDescription ("The default tools and brushes that ship with Pinta")]
[assembly: Mono.Addins.AddinDependency ("Pinta", PintaCore.ApplicationVersion)]
[assembly: Mono.Addins.AddinFlags (Mono.Addins.Description.AddinFlags.Hidden | Mono.Addins.Description.AddinFlags.CantUninstall)]

namespace Pinta.Tools;

[Mono.Addins.Extension]
public sealed class CoreToolsExtension : IExtension
{
	#region IExtension Members
	public void Initialize ()
	{
		IServiceProvider services = PintaCore.Services;

		PintaCore.PaintBrushes.AddPaintBrush (new Brushes.CircleBrush ());
		PintaCore.PaintBrushes.AddPaintBrush (new Brushes.GridBrush ());
		PintaCore.PaintBrushes.AddPaintBrush (new Brushes.PlainBrush (PintaCore.Workspace));
		PintaCore.PaintBrushes.AddPaintBrush (new Brushes.SplatterBrush ());
		PintaCore.PaintBrushes.AddPaintBrush (new Brushes.SquaresBrush ());

		PintaCore.Tools.AddTool (new MoveSelectedTool (services));
		PintaCore.Tools.AddTool (new MoveSelectionTool (services));
		PintaCore.Tools.AddTool (new ZoomTool (services));
		PintaCore.Tools.AddTool (new PanTool (services));
		PintaCore.Tools.AddTool (new RectangleSelectTool (services));
		PintaCore.Tools.AddTool (new EllipseSelectTool (services));
		PintaCore.Tools.AddTool (new LassoSelectTool (services));
		PintaCore.Tools.AddTool (new MagicWandTool (services));
		PintaCore.Tools.AddTool (new PaintBrushTool (services));
		PintaCore.Tools.AddTool (new PencilTool (services));
		PintaCore.Tools.AddTool (new EraserTool (services));
		PintaCore.Tools.AddTool (new PaintBucketTool (services));
		PintaCore.Tools.AddTool (new GradientTool (services));
		PintaCore.Tools.AddTool (new ColorPickerTool (services));
		PintaCore.Tools.AddTool (new TextTool (services));
		PintaCore.Tools.AddTool (new LineCurveTool (services));
		PintaCore.Tools.AddTool (new RectangleTool (services));
		PintaCore.Tools.AddTool (new RoundedRectangleTool (services));
		PintaCore.Tools.AddTool (new EllipseTool (services));
		PintaCore.Tools.AddTool (new FreeformShapeTool (services));
		PintaCore.Tools.AddTool (new CloneStampTool (services));
		PintaCore.Tools.AddTool (new RecolorTool (services));
	}

	public void Uninitialize ()
	{
		PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.CircleBrush));
		PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.GridBrush));
		PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.PlainBrush));
		PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.SplatterBrush));
		PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.SquaresBrush));

		PintaCore.Tools.RemoveInstanceOfTool<MoveSelectedTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<MoveSelectionTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<ZoomTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<PanTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<RectangleSelectTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<EllipseSelectTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<LassoSelectTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<MagicWandTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<PaintBrushTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<PencilTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<EraserTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<PaintBucketTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<GradientTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<ColorPickerTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<TextTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<LineCurveTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<RectangleTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<RoundedRectangleTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<EllipseTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<FreeformShapeTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<CloneStampTool> ();
		PintaCore.Tools.RemoveInstanceOfTool<RecolorTool> ();
	}
	#endregion
}
