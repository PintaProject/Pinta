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
		PintaCore.Effects.RegisterEffect (new AlignObjectEffect (services));
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
		PintaCore.Effects.RegisterEffect (new OutlineObjectEffect (services));
		PintaCore.Effects.RegisterEffect (new InkSketchEffect (services));
		PintaCore.Effects.RegisterEffect (new JuliaFractalEffect (services));
		PintaCore.Effects.RegisterEffect (new MandelbrotFractalEffect (services));
		PintaCore.Effects.RegisterEffect (new MedianEffect (services));
		PintaCore.Effects.RegisterEffect (new MotionBlurEffect (services));
		PintaCore.Effects.RegisterEffect (new OilPaintingEffect (services));
		PintaCore.Effects.RegisterEffect (new OutlineEdgeEffect (services));
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
		PintaCore.Effects.UnregisterInstanceOfAdjustment<AutoLevelEffect> ();
		PintaCore.Effects.UnregisterInstanceOfAdjustment<BlackAndWhiteEffect> ();
		PintaCore.Effects.UnregisterInstanceOfAdjustment<BrightnessContrastEffect> ();
		PintaCore.Effects.UnregisterInstanceOfAdjustment<CurvesEffect> ();
		PintaCore.Effects.UnregisterInstanceOfAdjustment<HueSaturationEffect> ();
		PintaCore.Effects.UnregisterInstanceOfAdjustment<InvertColorsEffect> ();
		PintaCore.Effects.UnregisterInstanceOfAdjustment<LevelsEffect> ();
		PintaCore.Effects.UnregisterInstanceOfAdjustment<PosterizeEffect> ();
		PintaCore.Effects.UnregisterInstanceOfAdjustment<SepiaEffect> ();

		// Remove the effects
		PintaCore.Effects.UnregisterInstanceOfEffect<AddNoiseEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<AlignObjectEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<BulgeEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<CloudsEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<DentsEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<DitheringEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<EdgeDetectEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<EmbossEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<FragmentEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<FrostedGlassEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<GaussianBlurEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<GlowEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<FeatherEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<OutlineObjectEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<InkSketchEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<JuliaFractalEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<MandelbrotFractalEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<MedianEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<MotionBlurEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<OilPaintingEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<OutlineEdgeEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<PencilSketchEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<PixelateEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<PolarInversionEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<RadialBlurEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<RedEyeRemoveEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<ReduceNoiseEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<ReliefEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<SharpenEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<SoftenPortraitEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<TileEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<TwistEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<UnfocusEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<VignetteEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<VoronoiDiagramEffect> ();
		PintaCore.Effects.UnregisterInstanceOfEffect<ZoomBlurEffect> ();
	}
	#endregion
}
