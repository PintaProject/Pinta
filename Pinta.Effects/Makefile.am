
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize- -debug "-define:DEBUG;TRACE"
ASSEMBLY = ../bin/Extensions/Pinta.Effects.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../bin/Pinta.Core.dll \
	../bin/Pinta.Gui.Widgets.dll \
	../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll
BUILD_DIR = ../bin/Extensions/

SYSTEM_COMPONENTMODEL_COMPOSITION_DLL_SOURCE=../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll
PINTA_EFFECTS_DLL_MDB_SOURCE=../bin/Extensions/Pinta.Effects.dll.mdb
PINTA_EFFECTS_DLL_MDB=$(BUILD_DIR)/Pinta.Effects.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize+ "-define:TRACE"
ASSEMBLY = ../bin/Extensions/Pinta.Effects.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../bin/Pinta.Core.dll \
	../bin/Pinta.Gui.Widgets.dll \
	../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll
BUILD_DIR = ../bin/Extensions/

SYSTEM_COMPONENTMODEL_COMPOSITION_DLL_SOURCE=../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll
PINTA_EFFECTS_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(SYSTEM_COMPONENTMODEL_COMPOSITION_DLL) \
	$(PINTA_EFFECTS_DLL_MDB)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) 

FILES = \
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

DATA_FILES = 

RESOURCES = \
	gtk-gui/gui.stetic 

EXTRAS = 

REFERENCES =  \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(MONO_CAIRO_LIBS) \
	System \
	System.Core \
	Mono.Posix

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) 

include $(top_srcdir)/Makefile.include

SYSTEM_COMPONENTMODEL_COMPOSITION_DLL = $(BUILD_DIR)/System.ComponentModel.Composition.dll

$(eval $(call emit-deploy-target,SYSTEM_COMPONENTMODEL_COMPOSITION_DLL))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
