// 
// CoreEffectsExtension.cs
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

namespace Pinta.Effects
{
	[Mono.Addins.Extension]
	class CoreEffectsExtension : IExtension
	{
		#region IExtension Members
		public void Initialize ()
		{
			// Add the adjustments
			PintaCore.Effects.RegisterAdjustment (new AutoLevelEffect ());
			PintaCore.Effects.RegisterAdjustment (new BlackAndWhiteEffect ());
			PintaCore.Effects.RegisterAdjustment (new BrightnessContrastEffect ());
			PintaCore.Effects.RegisterAdjustment (new CurvesEffect ());
			PintaCore.Effects.RegisterAdjustment (new HueSaturationEffect ());
			PintaCore.Effects.RegisterAdjustment (new InvertColorsEffect ());
			PintaCore.Effects.RegisterAdjustment (new LevelsEffect ());
			PintaCore.Effects.RegisterAdjustment (new PosterizeEffect ());
			PintaCore.Effects.RegisterAdjustment (new SepiaEffect ());

			// Add the effects
			PintaCore.Effects.RegisterEffect (new AddNoiseEffect ());
			PintaCore.Effects.RegisterEffect (new BulgeEffect ());
			PintaCore.Effects.RegisterEffect (new CloudsEffect ());
			PintaCore.Effects.RegisterEffect (new EdgeDetectEffect ());
			PintaCore.Effects.RegisterEffect (new EmbossEffect ());
			PintaCore.Effects.RegisterEffect (new FragmentEffect ());
			PintaCore.Effects.RegisterEffect (new FrostedGlassEffect ());
			PintaCore.Effects.RegisterEffect (new GaussianBlurEffect ());
			PintaCore.Effects.RegisterEffect (new GlowEffect ());
			PintaCore.Effects.RegisterEffect (new InkSketchEffect ());
			PintaCore.Effects.RegisterEffect (new JuliaFractalEffect ());
			PintaCore.Effects.RegisterEffect (new MandelbrotFractalEffect ());
			PintaCore.Effects.RegisterEffect (new MedianEffect ());
			PintaCore.Effects.RegisterEffect (new MotionBlurEffect ());
			PintaCore.Effects.RegisterEffect (new OilPaintingEffect ());
			PintaCore.Effects.RegisterEffect (new OutlineEffect ());
			PintaCore.Effects.RegisterEffect (new PencilSketchEffect ());
			PintaCore.Effects.RegisterEffect (new PixelateEffect ());
			PintaCore.Effects.RegisterEffect (new PolarInversionEffect ());
			PintaCore.Effects.RegisterEffect (new RadialBlurEffect ());
			PintaCore.Effects.RegisterEffect (new RedEyeRemoveEffect ());
			PintaCore.Effects.RegisterEffect (new ReduceNoiseEffect ());
			PintaCore.Effects.RegisterEffect (new ReliefEffect ());
			PintaCore.Effects.RegisterEffect (new SharpenEffect ());
			PintaCore.Effects.RegisterEffect (new SoftenPortraitEffect ());
			PintaCore.Effects.RegisterEffect (new TileEffect ());
			PintaCore.Effects.RegisterEffect (new TwistEffect ());
			PintaCore.Effects.RegisterEffect (new UnfocusEffect ());
			PintaCore.Effects.RegisterEffect (new ZoomBlurEffect ());
		}

		public void Uninitialize ()
		{
			// Remove the adjustments
			PintaCore.Effects.UnregisterInstanceOfAdjustment (typeof (AutoLevelEffect));
			PintaCore.Effects.UnregisterInstanceOfAdjustment (typeof (BlackAndWhiteEffect));
			PintaCore.Effects.UnregisterInstanceOfAdjustment (typeof (BrightnessContrastEffect));
			PintaCore.Effects.UnregisterInstanceOfAdjustment (typeof (CurvesEffect));
			PintaCore.Effects.UnregisterInstanceOfAdjustment (typeof (HueSaturationEffect));
			PintaCore.Effects.UnregisterInstanceOfAdjustment (typeof (InvertColorsEffect));
			PintaCore.Effects.UnregisterInstanceOfAdjustment (typeof (LevelsEffect));
			PintaCore.Effects.UnregisterInstanceOfAdjustment (typeof (PosterizeEffect));
			PintaCore.Effects.UnregisterInstanceOfAdjustment (typeof (SepiaEffect));

			// Remove the effects
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (AddNoiseEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (BulgeEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (CloudsEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (EdgeDetectEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (EmbossEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (FragmentEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (FrostedGlassEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (GaussianBlurEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (GlowEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (InkSketchEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (JuliaFractalEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (MandelbrotFractalEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (MedianEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (MotionBlurEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (OilPaintingEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (OutlineEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (PencilSketchEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (PixelateEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (PolarInversionEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (RadialBlurEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (RedEyeRemoveEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (ReduceNoiseEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (ReliefEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (SharpenEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (SoftenPortraitEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (TileEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (TwistEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (UnfocusEffect));
			PintaCore.Effects.UnregisterInstanceOfEffect (typeof (ZoomBlurEffect));
		}
		#endregion
	}
}
