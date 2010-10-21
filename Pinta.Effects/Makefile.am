include $(top_srcdir)/Makefile.include

ASSEMBLY = Pinta.Effects
TARGET = library
ASSEMBLY_BUILD_FLAGS = -unsafe
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)
BUILD_SUBDIR = "/Extensions"

REFERENCES = \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(MONO_CAIRO_LIBS) \
	-r:../bin/Pinta.Core.dll \
	-r:../bin/Pinta.Gui.Widgets.dll \
	-r:../bin/System.Composition.ComponentModel.dll

SOURCES =  \
	Dialogs/Effects.LevelsDialog.cs \
	Dialogs/Effects.PosterizeDialog.cs \
	Adjustments/AutoLevelEffect.cs \
	Adjustments/BlackAndWhiteEffect.cs \
	Adjustments/BrightnessContrastEffect.cs \
	Adjustments/CurvesEffect.cs \
	Adjustments/HueSaturationEffect.cs \
	Adjustments/InvertColorsEffect.cs \
	Adjustments/LevelsEffect.cs \
	Adjustments/PosterizeEffect.cs \
	Adjustments/SepiaEffect.cs \
	Effects/AddNoiseEffect.cs \
	Effects/BulgeEffect.cs \
	Effects/CloudsEffect.cs \
	Effects/EdgeDetectEffect.cs \
	Effects/EmbossEffect.cs \
	Effects/FragmentEffect.cs \
	Effects/FrostedGlassEffect.cs \
	Effects/GaussianBlurEffect.cs \
	Effects/GlowEffect.cs \
	Effects/InkSketchEffect.cs \
	Effects/JuliaFractalEffect.cs \
	Effects/LocalHistogramEffect.cs \
	Effects/MandelbrotFractalEffect.cs \
	Effects/MedianEffect.cs \
	Effects/MotionBlurEffect.cs \
	Effects/OilPaintingEffect.cs \
	Effects/OutlineEffect.cs \
	Effects/PencilSketchEffect.cs \
	Effects/PixelateEffect.cs \
	Effects/PolarInversionEffect.cs \
	Effects/RadialBlurEffect.cs \
	Effects/RedEyeRemoveEffect.cs \
	Effects/ReduceNoiseEffect.cs \
	Effects/ReliefEffect.cs \
	Effects/SharpenEffect.cs \
	Effects/SoftenPortraitEffect.cs \
	Effects/TileEffect.cs \
	Effects/TwistEffect.cs \
	Effects/UnfocusEffect.cs \
	Effects/WarpEffect.cs \
	Effects/ZoomBlurEffect.cs \
	gtk-gui/generated.cs \
	gtk-gui/Pinta.Effects.PosterizeDialog.cs \
	Properties/AssemblyInfo.cs \
	Utilities/EffectHelper.cs \
	gtk-gui/Pinta.Effects.CurvesDialog.cs \
	Dialogs/Effects.CurvesDialog.cs \
	gtk-gui/Pinta.Effects.LevelsDialog.cs 

