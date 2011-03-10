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
			PintaCore.Effects.RegisterEffect (new TileEffect ());
			PintaCore.Effects.RegisterEffect (new TwistEffect ());
			PintaCore.Effects.RegisterEffect (new UnfocusEffect ());
			PintaCore.Effects.RegisterEffect (new ZoomBlurEffect ());
		}

		public void Uninitialize ()
		{
		}
		#endregion
	}
}
