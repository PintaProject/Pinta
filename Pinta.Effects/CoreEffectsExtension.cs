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
using Mono.Addins;
using Pinta.Core;

[assembly: Addin ("DefaultEffects", PintaCore.ApplicationVersion, Category = "Core")]
[assembly: AddinName ("Default Effects")]
[assembly: AddinDescription ("The default adjustments and effects that ship with Pinta")]
[assembly: AddinDependency ("Pinta", PintaCore.ApplicationVersion)]
[assembly: AddinFlags (Mono.Addins.Description.AddinFlags.Hidden | Mono.Addins.Description.AddinFlags.CantUninstall)]

namespace Pinta.Effects;

[Mono.Addins.Extension]
internal sealed class CoreEffectsExtension : IExtension
{
	#region IExtension Members
	public void Initialize ()
	{
		IServiceProvider services = PintaCore.Services;

		// Add the adjustments
		PintaCore.Effects.RegisterAdjustment (new AutoLevelEffect (services));
		PintaCore.Effects.RegisterAdjustment (new BlackAndWhiteEffect (services));
		PintaCore.Effects.RegisterAdjustment (new BrightnessContrastEffect (services));
		PintaCore.Effects.RegisterAdjustment (new CurvesEffect (services));
		PintaCore.Effects.RegisterAdjustment (new HueSaturationEffect (services));
		PintaCore.Effects.RegisterAdjustment (new InvertColorsEffect (services));
		PintaCore.Effects.RegisterAdjustment (new LevelsEffect (services));
		PintaCore.Effects.RegisterAdjustment (new PosterizeEffect (services));
		PintaCore.Effects.RegisterAdjustment (new SepiaEffect (services));

		// Add the effects
		PintaCore.Effects.RegisterEffect (new AddNoiseEffect (services));
		PintaCore.Effects.RegisterEffect (new BulgeEffect (services));
		PintaCore.Effects.RegisterEffect (new CloudsEffect (services));
		PintaCore.Effects.RegisterEffect (new DentsEffect (services));
		PintaCore.Effects.RegisterEffect (new DitheringEffect (services));
		PintaCore.Effects.RegisterEffect (new EdgeDetectEffect (services));
		PintaCore.Effects.RegisterEffect (new EmbossEffect (services));
		PintaCore.Effects.RegisterEffect (new FragmentEffect (services));
		PintaCore.Effects.RegisterEffect (new FrostedGlassEffect (services));
		PintaCore.Effects.RegisterEffect (new GaussianBlurEffect (services));
		PintaCore.Effects.RegisterEffect (new GlowEffect (services));
		PintaCore.Effects.RegisterEffect (new FeatherEffect (services));
		PintaCore.Effects.RegisterEffect (new InkSketchEffect (services));
		PintaCore.Effects.RegisterEffect (new JuliaFractalEffect (services));
		PintaCore.Effects.RegisterEffect (new MandelbrotFractalEffect (services));
		PintaCore.Effects.RegisterEffect (new MedianEffect (services));
		PintaCore.Effects.RegisterEffect (new MotionBlurEffect (services));
		PintaCore.Effects.RegisterEffect (new OilPaintingEffect (services));
		PintaCore.Effects.RegisterEffect (new OutlineEffect (services));
		PintaCore.Effects.RegisterEffect (new PencilSketchEffect (services));
		PintaCore.Effects.RegisterEffect (new PixelateEffect (services));
		PintaCore.Effects.RegisterEffect (new PolarInversionEffect (services));
		PintaCore.Effects.RegisterEffect (new RadialBlurEffect (services));
		PintaCore.Effects.RegisterEffect (new RedEyeRemoveEffect (services));
		PintaCore.Effects.RegisterEffect (new ReduceNoiseEffect (services));
		PintaCore.Effects.RegisterEffect (new ReliefEffect (services));
		PintaCore.Effects.RegisterEffect (new SharpenEffect (services));
		PintaCore.Effects.RegisterEffect (new SoftenPortraitEffect (services));
		PintaCore.Effects.RegisterEffect (new TileEffect (services));
		PintaCore.Effects.RegisterEffect (new TwistEffect (services));
		PintaCore.Effects.RegisterEffect (new UnfocusEffect (services));
		PintaCore.Effects.RegisterEffect (new VignetteEffect (services));
		PintaCore.Effects.RegisterEffect (new VoronoiDiagramEffect (services));
		PintaCore.Effects.RegisterEffect (new ZoomBlurEffect (services));
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
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (DentsEffect));
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (DitheringEffect));
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (EdgeDetectEffect));
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (EmbossEffect));
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (FragmentEffect));
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (FrostedGlassEffect));
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (GaussianBlurEffect));
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (GlowEffect));
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (FeatherEffect));
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
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (VignetteEffect));
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (VoronoiDiagramEffect));
		PintaCore.Effects.UnregisterInstanceOfEffect (typeof (ZoomBlurEffect));
	}
	#endregion
}
