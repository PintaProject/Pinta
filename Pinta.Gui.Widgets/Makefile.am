include $(top_srcdir)/Makefile.include

ASSEMBLY = Pinta.Gui.Widgets
TARGET = library
ASSEMBLY_BUILD_FLAGS = -unsafe

REFERENCES = \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Data \
	-r:System.Xml \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(MONO_CAIRO_LIBS) \
	-r:../bin/Pinta.Core.dll

SOURCES =  \
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

