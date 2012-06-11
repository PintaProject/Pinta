﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pinta.Core;

namespace Pinta.Tools
{
	[Mono.Addins.Extension]
	class CoreToolsExtension : IExtension
	{
		#region IExtension Members
		public void Initialize ()
		{
			PintaCore.PaintBrushes.AddPaintBrush (new Brushes.CircleBrush ());
			PintaCore.PaintBrushes.AddPaintBrush (new Brushes.GridBrush ());
			PintaCore.PaintBrushes.AddPaintBrush (new Brushes.PlainBrush ());
			PintaCore.PaintBrushes.AddPaintBrush (new Brushes.SplatterBrush ());
			PintaCore.PaintBrushes.AddPaintBrush (new Brushes.SquaresBrush ());

			PintaCore.Tools.AddTool (new RectangleSelectTool ());
			PintaCore.Tools.AddTool (new MoveSelectedTool ());
			PintaCore.Tools.AddTool (new LassoSelectTool ());
			PintaCore.Tools.AddTool (new MoveSelectionTool ());
			PintaCore.Tools.AddTool (new EllipseSelectTool ());
			PintaCore.Tools.AddTool (new ZoomTool ());
			PintaCore.Tools.AddTool (new MagicWandTool ());
			PintaCore.Tools.AddTool (new PanTool ());
			PintaCore.Tools.AddTool (new PaintBucketTool ());
			PintaCore.Tools.AddTool (new GradientTool ());
			PintaCore.Tools.AddTool (new PaintBrushTool ());
			PintaCore.Tools.AddTool (new EraserTool ());
			PintaCore.Tools.AddTool (new PencilTool ());
			PintaCore.Tools.AddTool (new ColorPickerTool ());
			PintaCore.Tools.AddTool (new CloneStampTool ());
			PintaCore.Tools.AddTool (new RecolorTool ());
			PintaCore.Tools.AddTool (new TextTool ());
			PintaCore.Tools.AddTool (new LineCurveTool ());
			PintaCore.Tools.AddTool (new RectangleTool ());
			PintaCore.Tools.AddTool (new RoundedRectangleTool ());
			PintaCore.Tools.AddTool (new EllipseTool ());
			PintaCore.Tools.AddTool (new FreeformShapeTool ());
		}

		public void Uninitialize ()
		{
			PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.CircleBrush));
			PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.GridBrush));
			PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.PlainBrush));
			PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.SplatterBrush));
			PintaCore.PaintBrushes.RemoveInstanceOfPaintBrush (typeof (Brushes.SquaresBrush));

			PintaCore.Tools.RemoveInstanceOfTool (typeof (RectangleSelectTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (MoveSelectedTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (LassoSelectTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (MoveSelectionTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (EllipseSelectTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (ZoomTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (MagicWandTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (PanTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (PaintBucketTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (GradientTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (PaintBrushTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (EraserTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (PencilTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (ColorPickerTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (CloneStampTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (RecolorTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (TextTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (LineCurveTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (RectangleTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (RoundedRectangleTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (EllipseTool));
			PintaCore.Tools.RemoveInstanceOfTool (typeof (FreeformShapeTool));
		}
		#endregion
	}
}
