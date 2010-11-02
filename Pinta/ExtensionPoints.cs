// 
// ExtensionPoints.cs
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
using System.Collections.Generic;
//using System.ComponentModel.Composition;
using Pinta.Core;
using Pinta.Tools;
using Pinta.Effects;
using Pinta.Tools.Brushes;

namespace Pinta
{
	class ExtensionPoints
	{
		//[ImportMany]
		public IEnumerable<BaseTool> Tools { get; set; }
		//[ImportMany]
		public IEnumerable<BaseEffect> Effects { get; set; }
		//[ImportMany]
		public IEnumerable<BasePaintBrush> PaintBrushes { get; set; }

		public ExtensionPoints ()
		{
			Tools = new List<BaseTool> {
				new RectangleSelectTool (),
				new MoveSelectedTool (),
				new LassoSelectTool (),
				new MoveSelectionTool (),
				new EllipseSelectTool (),
				new ZoomTool (),
				new MagicWandTool (),
				new PanTool (),
				new PaintBucketTool (),
				new GradientTool (),
				new PaintBrushTool (),
				new EraserTool (),
				new PencilTool (),
				new ColorPickerTool (),
				new CloneStampTool (),
				new RecolorTool (),
				new TextTool (),
				new LineCurveTool (),
				new RectangleTool (),
				new RoundedRectangleTool (),
				new EllipseTool (),
				new FreeformShapeTool ()
			};

			Effects = new List<BaseEffect> () {
				new AutoLevelEffect (),
				new BlackAndWhiteEffect (),
				new BrightnessContrastEffect (),
				new CurvesEffect (),
				new HueSaturationEffect (),
				new InvertColorsEffect (),
				new LevelsEffect (),
				new PosterizeEffect (),
				new SepiaEffect (),
				new AddNoiseEffect (),
				new BulgeEffect (),
				new CloudsEffect (),
				new EdgeDetectEffect (),
				new EmbossEffect (),
				new FragmentEffect (),
				new FrostedGlassEffect (),
				new GaussianBlurEffect (),
				new GlowEffect (),
				new InkSketchEffect (),
				new JuliaFractalEffect (),
				new MandelbrotFractalEffect (),
				new MedianEffect (),
				new MotionBlurEffect (),
				new OilPaintingEffect (),
				new OutlineEffect (),
				new PencilSketchEffect (),
				new PixelateEffect (),
				new PolarInversionEffect (),
				new RadialBlurEffect (),
				new RedEyeRemoveEffect (),
				new ReduceNoiseEffect (),
				new ReliefEffect (),
				new SharpenEffect (),
				new SoftenPortraitEffect (),
				new TileEffect (),
				new TwistEffect (),
				new UnfocusEffect (),
				new ZoomBlurEffect ()
			};

			PaintBrushes = new List<BasePaintBrush> {
				new CircleBrush (),
				new GridBrush (),
				new PlainBrush (),
				new SplatterBrush (),
				new SquaresBrush ()
			};
		}

	}
}
