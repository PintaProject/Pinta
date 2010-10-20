
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize- -debug "-define:DEBUG;TRACE"
ASSEMBLY = ../bin/Extensions/Pinta.Tools.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../bin/Pinta.Core.dll \
	../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll
BUILD_DIR = ../bin/Extensions/

SYSTEM_COMPONENTMODEL_COMPOSITION_DLL_SOURCE=../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll
PINTA_TOOLS_DLL_MDB_SOURCE=../bin/Extensions/Pinta.Tools.dll.mdb
PINTA_TOOLS_DLL_MDB=$(BUILD_DIR)/Pinta.Tools.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize+ "-define:TRACE"
ASSEMBLY = ../bin/Extensions/Pinta.Tools.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../bin/Pinta.Core.dll \
	../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll
BUILD_DIR = ../bin/Extensions/

SYSTEM_COMPONENTMODEL_COMPOSITION_DLL_SOURCE=../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll
PINTA_TOOLS_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(SYSTEM_COMPONENTMODEL_COMPOSITION_DLL) \
	$(PINTA_TOOLS_DLL_MDB)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) 

FILES = \
	BaseBrushTool.cs \
	Brushes/SplatterBrush.cs \
	ColorPickerTool.cs \
	EllipseSelectTool.cs \
	EllipseTool.cs \
	EraserTool.cs \
	FloodTool.cs \
	FreeformShapeTool.cs \
	GradientTool.cs \
	LassoSelectTool.cs \
	LineCurveTool.cs \
	MagicWandTool.cs \
	MoveSelectedTool.cs \
	MoveSelectionTool.cs \
	PaintBrushTool.cs \
	PaintBucketTool.cs \
	PanTool.cs \
	PencilTool.cs \
	Properties/AssemblyInfo.cs \
	RecolorTool.cs \
	RectangleSelectTool.cs \
	RectangleTool.cs \
	RoundedRectangleTool.cs \
	SelectTool.cs \
	ShapeTool.cs \
	TextTool.cs \
	CloneStampTool.cs \
	ZoomTool.cs \
	Brushes/PaintBrush.cs \
	Brushes/GridBrush.cs \
	Brushes/SquaresBrush.cs \
	Brushes/PlainBrush.cs \
	Brushes/CircleBrush.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = 

REFERENCES =  \
	System \
	System.Core \
	System.Xml \
	$(GTK_SHARP_20_LIBS) \
	$(MONO_CAIRO_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
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
