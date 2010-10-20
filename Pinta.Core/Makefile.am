
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../bin/Pinta.Core.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../bin/Pinta.Resources.dll
BUILD_DIR = ../bin

ICSHARPCODE_SHARPZIPLIB_DLL_SOURCE=../lib/ICSharpCode.SharpZipLib.dll
PINTA_RESOURCES_DLL_SOURCE=../bin/Pinta.Resources.dll
PINTA_RESOURCES_DLL_MDB_SOURCE=../bin/Pinta.Resources.dll.mdb
PINTA_RESOURCES_DLL_MDB=$(BUILD_DIR)/Pinta.Resources.dll.mdb
PINTA_CORE_DLL_MDB_SOURCE=../bin/Pinta.Core.dll.mdb
PINTA_CORE_DLL_MDB=$(BUILD_DIR)/Pinta.Core.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize-
ASSEMBLY = ../bin/Pinta.Core.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../bin/Pinta.Resources.dll
BUILD_DIR = ../bin

ICSHARPCODE_SHARPZIPLIB_DLL_SOURCE=../lib/ICSharpCode.SharpZipLib.dll
PINTA_RESOURCES_DLL_SOURCE=../bin/Pinta.Resources.dll
PINTA_RESOURCES_DLL_MDB=
PINTA_CORE_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(ICSHARPCODE_SHARPZIPLIB_DLL) \
	$(PINTA_RESOURCES_DLL) \
	$(PINTA_RESOURCES_DLL_MDB) \
	$(PINTA_CORE_DLL_MDB)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) 

FILES = \
	Actions/EffectsActions.cs \
	AssemblyInfo.cs \
	Classes/Anchor.cs \
	Classes/DocumentWorkspaceHistory.cs \
	Classes/Document.cs \
	Classes/DocumentWorkspace.cs \
	Classes/ScaleFactor.cs \
	Classes/Palette.cs \
	Effects/BaseEffect.cs \
	Effects/BinaryPixelOp.cs \
	Effects/BitVector2DSurfaceAdapter.cs \
	Effects/Histogram.cs \
	Effects/HistogramRGB.cs \
	Effects/IBitVector2D.cs \
	Effects/Scanline.cs \
	Effects/UserBlendOp.cs \
	Effects/UserBlendOps.cs \
	Effects/UserBlendOps.Generated.cs \
	Enumerations/EffectAdjustment.cs \
	Enumerations/GradientColorMode.cs \
	EventArgs/CanvasInvalidatedEventArgs.cs \
	EventArgs/DocumentEventArgs.cs \
	EventArgs/HistoryItemAddedEventArgs.cs \
	EventArgs/HistoryItemRemovedEventArgs.cs \
	EventArgs/TextChangedEventArgs.cs \
	Extensions/ToolBarSlider.cs \
	HistoryItems/ResizeHistoryItem.cs \
	HistoryItems/FinishPixelsHistoryItem.cs \
	HistoryItems/MovePixelsHistoryItem.cs \
	HistoryItems/SelectionHistoryItem.cs \
	HistoryItems/BaseHistoryItem.cs \
	HistoryItems/CompoundHistoryItem.cs \
	HistoryItems/AddLayerHistoryItem.cs \
	HistoryItems/SwapLayersHistoryItem.cs \
	HistoryItems/DeleteLayerHistoryItem.cs \
	HistoryItems/InvertHistoryItem.cs \
	HistoryItems/SimpleHistoryItem.cs \
	Managers/ActionManager.cs \
	Managers/ChromeManager.cs \
	Managers/HistoryManager.cs \
	Managers/LayerManager.cs \
	Managers/PaletteManager.cs \
	Managers/ResourceManager.cs \
	Managers/SettingsManager.cs \
	Managers/SystemManager.cs \
	Managers/ToolManager.cs \
	Managers/WorkspaceManager.cs \
	PintaCore.cs \
	Extensions/CairoExtensions.cs \
	Extensions/GdkExtensions.cs \
	Extensions/GtkExtensions.cs \
	Classes/Layer.cs \
	Extensions/ToolBarButton.cs \
	Extensions/ToolBarComboBox.cs \
	Extensions/ToolBarImage.cs \
	Extensions/ToolBarLabel.cs \
	Actions/LayerActions.cs \
	Actions/FileActions.cs \
	Actions/EditActions.cs \
	Actions/ViewActions.cs \
	Actions/ImageActions.cs \
	Actions/AdjustmentsActions.cs \
	Actions/HelpActions.cs \
	Effects/ColorBgra.cs \
	Effects/UnaryPixelOps.cs \
	Effects/UnaryPixelOp.cs \
	Effects/PixelOp.cs \
	Effects/Utility.cs \
	Effects/HsvColor.cs \
	Effects/RgbColor.cs \
	Classes/BaseTool.cs \
	Extensions/ToolBarToggleButton.cs \
	HistoryItems/UpdateLayerPropertiesHistoryItem.cs \
	Classes/ObservableObject.cs \
	Classes/LayerProperties.cs \
	Effects/ColorTransferMode.cs \
	Effects/SplineInterpolator.cs \
	Extensions/OtherExtensions.cs \
	Effects/IrregularSurface.cs \
	Effects/ISurfaceDraw.cs \
	Effects/PlacedSurface.cs \
	HistoryItems/ClippedSurfaceHistoryItem.cs \
	Classes/IndexEventHandler.cs \
	Classes/IndexEventArgs.cs \
	Effects/LocalHistogramEffect.cs \
	Effects/ColorDifferenceEffect.cs \
	EventArgs/LivePreviewEndedEventArgs.cs \
	EventArgs/LivePreviewRenderUpdatedEventArgs.cs \
	EventArgs/LivePreviewStartedEventArgs.cs \
	EventArgs/ModifyCompressionEventArgs.cs \
	Managers/LivePreviewManager.cs \
	Classes/AsyncEffectRenderer.cs \
	Classes/GradientRenderer.cs \
	Classes/GradientRenderers.cs \
	ImageFormats/OraFormat.cs \
	ImageFormats/GdkPixbufFormat.cs \
	ImageFormats/JpegFormat.cs \
	ImageFormats/IImageExporter.cs \
	ImageFormats/IImageImporter.cs \
	ImageFormats/FormatDescriptor.cs \
	ImageFormats/TgaExporter.cs \
	Classes/BasePaintBrush.cs \
	Managers/PaintBrushManager.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = 

REFERENCES =  \
	System \
	$(GTK_SHARP_20_LIBS) \
	System.Core \
	$(GLIB_SHARP_20_LIBS) \
	Mono.Posix \
	System.Data \
	System.Xml \
	$(MONO_CAIRO_LIBS)

DLL_REFERENCES =  \
	../lib/ICSharpCode.SharpZipLib.dll

CLEANFILES = $(PROGRAMFILES) 

include $(top_srcdir)/Makefile.include

ICSHARPCODE_SHARPZIPLIB_DLL = $(BUILD_DIR)/ICSharpCode.SharpZipLib.dll
PINTA_RESOURCES_DLL = $(BUILD_DIR)/Pinta.Resources.dll

$(eval $(call emit-deploy-target,ICSHARPCODE_SHARPZIPLIB_DLL))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
