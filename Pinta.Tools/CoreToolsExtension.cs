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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pinta.Core;

namespace Pinta.Tools
{
	// TODO-GTK3 (addins)
#if false
	[Mono.Addins.Extension]
	class CoreToolsExtension : IExtension
#else
	public class CoreToolsExtension : IExtension
#endif
	{
		#region IExtension Members
		public void Initialize ()
		{
			PintaCore.PaintBrushes.AddPaintBrush (new Brushes.CircleBrush ());
			PintaCore.PaintBrushes.AddPaintBrush (new Brushes.GridBrush ());
			PintaCore.PaintBrushes.AddPaintBrush (new Brushes.PlainBrush ());
			PintaCore.PaintBrushes.AddPaintBrush (new Brushes.SplatterBrush ());
			PintaCore.PaintBrushes.AddPaintBrush (new Brushes.SquaresBrush ());

			PintaCore.Tools.AddTool (new MoveSelectedTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new MoveSelectionTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new ZoomTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new PanTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new RectangleSelectTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new EllipseSelectTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new LassoSelectTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new MagicWandTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new PaintBrushTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new PencilTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new EraserTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new PaintBucketTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new GradientTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new ColorPickerTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new TextTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new LineCurveTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new RectangleTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new RoundedRectangleTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new EllipseTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new FreeformShapeTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new CloneStampTool (PintaCore.Services));
			PintaCore.Tools.AddTool (new RecolorTool (PintaCore.Services));
		}

		public void Uninitialize ()
		{
			PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.CircleBrush));
			PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.GridBrush));
			PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.PlainBrush));
			PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.SplatterBrush));
			PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.SquaresBrush));

			PintaCore.Tools.RemoveInstanceOfTool<RectangleSelectTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<MoveSelectedTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<LassoSelectTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<MoveSelectionTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<EllipseSelectTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<ZoomTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<MagicWandTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<PanTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<PaintBucketTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<GradientTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<PaintBrushTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<EraserTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<PencilTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<ColorPickerTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<CloneStampTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<RecolorTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<TextTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<LineCurveTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<RectangleTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<RoundedRectangleTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<EllipseTool> ();
			PintaCore.Tools.RemoveInstanceOfTool<FreeformShapeTool> ();
		}
		#endregion
	}

	public delegate void MouseHandler (double x, double y, Gdk.ModifierType state);
}
