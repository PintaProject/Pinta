
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = ../bin/Pinta.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = winexe
PROJECT_REFERENCES =  \
	../bin/Pinta.Core.dll \
	../bin/Pinta.Gui.Widgets.dll \
	../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll
BUILD_DIR = ../bin/

PINTA_CORE_DLL_SOURCE=../bin/Pinta.Core.dll
ICSHARPCODE_SHARPZIPLIB_DLL_SOURCE=../lib/ICSharpCode.SharpZipLib.dll
PINTA_RESOURCES_DLL_SOURCE=../bin/Pinta.Resources.dll
PINTA_RESOURCES_DLL_MDB_SOURCE=../bin/Pinta.Resources.dll.mdb
PINTA_RESOURCES_DLL_MDB=$(BUILD_DIR)/Pinta.Resources.dll.mdb
PINTA_CORE_DLL_MDB_SOURCE=../bin/Pinta.Core.dll.mdb
PINTA_CORE_DLL_MDB=$(BUILD_DIR)/Pinta.Core.dll.mdb
PINTA_GUI_WIDGETS_DLL_SOURCE=../bin/Pinta.Gui.Widgets.dll
PINTA_GUI_WIDGETS_DLL_MDB_SOURCE=../bin/Pinta.Gui.Widgets.dll.mdb
PINTA_GUI_WIDGETS_DLL_MDB=$(BUILD_DIR)/Pinta.Gui.Widgets.dll.mdb
SYSTEM_COMPONENTMODEL_COMPOSITION_DLL_SOURCE=../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize-
ASSEMBLY = ../bin/Pinta.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = winexe
PROJECT_REFERENCES =  \
	../bin/Pinta.Core.dll \
	../bin/Pinta.Gui.Widgets.dll \
	../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll
BUILD_DIR = ../bin

PINTA_CORE_DLL_SOURCE=../bin/Pinta.Core.dll
ICSHARPCODE_SHARPZIPLIB_DLL_SOURCE=../lib/ICSharpCode.SharpZipLib.dll
PINTA_RESOURCES_DLL_SOURCE=../bin/Pinta.Resources.dll
PINTA_RESOURCES_DLL_MDB=
PINTA_CORE_DLL_MDB=
PINTA_GUI_WIDGETS_DLL_SOURCE=../bin/Pinta.Gui.Widgets.dll
PINTA_GUI_WIDGETS_DLL_MDB=
SYSTEM_COMPONENTMODEL_COMPOSITION_DLL_SOURCE=../System.Composition.ComponentModel/bin/Debug/System.ComponentModel.Composition.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(PINTA_CORE_DLL) \
	$(ICSHARPCODE_SHARPZIPLIB_DLL) \
	$(PINTA_RESOURCES_DLL) \
	$(PINTA_RESOURCES_DLL_MDB) \
	$(PINTA_CORE_DLL_MDB) \
	$(PINTA_GUI_WIDGETS_DLL) \
	$(PINTA_GUI_WIDGETS_DLL_MDB) \
	$(SYSTEM_COMPONENTMODEL_COMPOSITION_DLL)  

BINARIES = \
	$(PINTA)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES = \
	Dialogs/AboutPintaTabPage.cs \
	Dialogs/AboutDialog.cs \
	Dialogs/ErrorDialog.cs \
	Dialogs/JpegCompressionDialog.cs \
	Dialogs/SpinButtonEntryDialog.cs \
	Dialogs/VersionInformationTabPage.cs \
	DockLibrary/AutoHideBox.cs \
	DockLibrary/DockBar.cs \
	DockLibrary/DockBarItem.cs \
	DockLibrary/DockContainer.cs \
	DockLibrary/DockFrame.cs \
	DockLibrary/DockFrameTopLevel.cs \
	DockLibrary/DockGroup.cs \
	DockLibrary/DockGroupItem.cs \
	DockLibrary/DockGroupType.cs \
	DockLibrary/DockItem.cs \
	DockLibrary/DockItemBehavior.cs \
	DockLibrary/DockItemContainer.cs \
	DockLibrary/DockItemStatus.cs \
	DockLibrary/DockItemToolbar.cs \
	DockLibrary/DockLayout.cs \
	DockLibrary/DockObject.cs \
	DockLibrary/DockPosition.cs \
	DockLibrary/HslColor.cs \
	DockLibrary/PlaceholderWindow.cs \
	DockLibrary/ShadedContainer.cs \
	DockLibrary/TabStrip.cs \
	ExtensionPoints.cs \
	Extensions/GtkExtensions.cs \
	gtk-gui/generated.cs \
	Main.cs \
	AssemblyInfo.cs \
	Options.cs \
	Dialogs/NewImageDialog.cs \
	gtk-gui/Pinta.NewImageDialog.cs \
	DialogHandlers.cs \
	Dialogs/LayerPropertiesDialog.cs \
	gtk-gui/Pinta.LayerPropertiesDialog.cs \
	Dialogs/ResizeImageDialog.cs \
	gtk-gui/Pinta.ResizeImageDialog.cs \
	Dialogs/ResizeCanvasDialog.cs \
	gtk-gui/Pinta.ResizeCanvasDialog.cs \
	Platform.cs \
	IgeMacMenu.cs \
	gtk-gui/Pinta.ProgressDialog.cs \
	Dialogs/ProgressDialog.cs \
	MainWindow.cs 

DATA_FILES = 

RESOURCES = \
	gtk-gui/gui.stetic \
	DockLibrary/stock-auto-hide.png \
	DockLibrary/stock-close-12.png \
	DockLibrary/stock-dock.png \
	DockLibrary/stock-menu-left-12.png \
	DockLibrary/stock-menu-right-12.png 

EXTRAS = \
	pinta.in 

REFERENCES =  \
	$(GTK_SHARP_20_LIBS) \
	System \
	$(GLIB_SHARP_20_LIBS) \
	System.Core \
	Mono.Posix \
	$(MONO_CAIRO_LIBS) \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

PINTA_CORE_DLL = $(BUILD_DIR)/Pinta.Core.dll
ICSHARPCODE_SHARPZIPLIB_DLL = $(BUILD_DIR)/ICSharpCode.SharpZipLib.dll
PINTA_RESOURCES_DLL = $(BUILD_DIR)/Pinta.Resources.dll
PINTA_GUI_WIDGETS_DLL = $(BUILD_DIR)/Pinta.Gui.Widgets.dll
SYSTEM_COMPONENTMODEL_COMPOSITION_DLL = $(BUILD_DIR)/System.ComponentModel.Composition.dll
PINTA = $(BUILD_DIR)/pinta

$(eval $(call emit-deploy-target,ICSHARPCODE_SHARPZIPLIB_DLL))
$(eval $(call emit-deploy-target,SYSTEM_COMPONENTMODEL_COMPOSITION_DLL))
$(eval $(call emit-deploy-wrapper,PINTA,pinta,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
