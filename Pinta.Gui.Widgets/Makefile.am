
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../bin/Pinta.Gui.Widgets.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../bin/Pinta.Core.dll
BUILD_DIR = ../bin

PINTA_GUI_WIDGETS_DLL_MDB_SOURCE=../bin/Pinta.Gui.Widgets.dll.mdb
PINTA_GUI_WIDGETS_DLL_MDB=$(BUILD_DIR)/Pinta.Gui.Widgets.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize-
ASSEMBLY = ../bin/Pinta.Gui.Widgets.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../bin/Pinta.Core.dll
BUILD_DIR = ../bin

PINTA_GUI_WIDGETS_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(PINTA_GUI_WIDGETS_DLL_MDB)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) 

FILES = \
	DialogAttributes.cs \
	gtk-gui/generated.cs \
	AssemblyInfo.cs \
	gtk-gui/Pinta.Gui.Widgets.ColorPanelWidget.cs \
	Widgets/Canvas/CanvasRenderer.cs \
	Widgets/Canvas/GridRenderer.cs \
	Widgets/ColorPaletteWidget.cs \
	Widgets/ColorPanelWidget.cs \
	Widgets/History/HistoryTreeView.cs \
	Widgets/HScaleSpinButtonWidget.cs \
	Dialogs/SimpleEffectDialog.cs \
	Widgets/AnglePickerGraphic.cs \
	Widgets/AnglePickerWidget.cs \
	gtk-gui/Pinta.Gui.Widgets.AnglePickerWidget.cs \
	Widgets/Canvas/PintaCanvas.cs \
	Widgets/Layers/CellRendererSurface.cs \
	Widgets/Layers/LayersListWidget.cs \
	Widgets/PointPickerGraphic.cs \
	Widgets/PointPickerWidget.cs \
	gtk-gui/Pinta.Gui.Widgets.PointPickerWidget.cs \
	Widgets/ComboBoxWidget.cs \
	gtk-gui/Pinta.Gui.Widgets.ComboBoxWidget.cs \
	Widgets/ReseedButtonWidget.cs \
	gtk-gui/Pinta.Gui.Widgets.ReseedButtonWidget.cs \
	gtk-gui/Pinta.Gui.Widgets.HScaleSpinButtonWidget.cs \
	gtk-gui/Pinta.Gui.Widgets.ColorGradientWidget.cs \
	Widgets/ColorGradientWidget.cs \
	gtk-gui/Pinta.Gui.Widgets.HistogramWidget.cs \
	Widgets/HistogramWidget.cs \
	Widgets/ToolBoxWidget.cs 

DATA_FILES = 

RESOURCES = \
	gtk-gui/gui.stetic 

EXTRAS = 

REFERENCES =  \
	System \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GLADE_SHARP_20_LIBS) \
	Mono.Posix \
	System.Core \
	$(MONO_CAIRO_LIBS) \
	System.Data \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) 

include $(top_srcdir)/Makefile.include




$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
