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
			PintaCore.Effects.AddAdjustment (new AutoLevelEffect ());
			PintaCore.Effects.AddAdjustment (new BlackAndWhiteEffect ());
			PintaCore.Effects.AddAdjustment (new BrightnessContrastEffect ());
			PintaCore.Effects.AddAdjustment (new CurvesEffect ());
			PintaCore.Effects.AddAdjustment (new HueSaturationEffect ());
			PintaCore.Effects.AddAdjustment (new InvertColorsEffect ());
			PintaCore.Effects.AddAdjustment (new LevelsEffect ());
			PintaCore.Effects.AddAdjustment (new PosterizeEffect ());
			PintaCore.Effects.AddAdjustment (new SepiaEffect ());

			// Add the effects
			// HACK: Have to add one from each submenu item in order
			// until EffectsManager can keep the menus sorted
			PintaCore.Effects.AddEffect (new InkSketchEffect ());
			PintaCore.Effects.AddEffect (new FragmentEffect ());
			PintaCore.Effects.AddEffect (new BulgeEffect ());
			PintaCore.Effects.AddEffect (new AddNoiseEffect ());
			PintaCore.Effects.AddEffect (new GlowEffect ());
			PintaCore.Effects.AddEffect (new CloudsEffect ());
			PintaCore.Effects.AddEffect (new EdgeDetectEffect ());

			PintaCore.Effects.AddEffect (new EmbossEffect ());
			PintaCore.Effects.AddEffect (new FrostedGlassEffect ());
			PintaCore.Effects.AddEffect (new GaussianBlurEffect ());
			PintaCore.Effects.AddEffect (new JuliaFractalEffect ());
			PintaCore.Effects.AddEffect (new MandelbrotFractalEffect ());
			PintaCore.Effects.AddEffect (new MedianEffect ());
			PintaCore.Effects.AddEffect (new MotionBlurEffect ());
			PintaCore.Effects.AddEffect (new OilPaintingEffect ());
			PintaCore.Effects.AddEffect (new OutlineEffect ());
			PintaCore.Effects.AddEffect (new PencilSketchEffect ());
			PintaCore.Effects.AddEffect (new PixelateEffect ());
			PintaCore.Effects.AddEffect (new PolarInversionEffect ());
			PintaCore.Effects.AddEffect (new RadialBlurEffect ());
			PintaCore.Effects.AddEffect (new RedEyeRemoveEffect ());
			PintaCore.Effects.AddEffect (new ReduceNoiseEffect ());
			PintaCore.Effects.AddEffect (new ReliefEffect ());
			PintaCore.Effects.AddEffect (new SharpenEffect ());
			PintaCore.Effects.AddEffect (new TileEffect ());
			PintaCore.Effects.AddEffect (new TwistEffect ());
			PintaCore.Effects.AddEffect (new UnfocusEffect ());
			PintaCore.Effects.AddEffect (new ZoomBlurEffect ());
		}

		public void Uninitialize ()
		{
		}
		#endregion
	}
}
