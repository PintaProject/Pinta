include $(top_srcdir)/Makefile.include

ASSEMBLY = Pinta
TARGET = exe

REFERENCES = \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(MONO_CAIRO_LIBS) \
	-r:../bin/Pinta.Core.dll \
	-r:../bin/Pinta.Gui.Widgets.dll \
	-r:../bin/System.Composition.ComponentModel.dll

SOURCES =  \
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

RESOURCES = \
	gtk-gui/gui.stetic \
	DockLibrary/stock-auto-hide.png \
	DockLibrary/stock-close-12.png \
	DockLibrary/stock-dock.png \
	DockLibrary/stock-menu-left-12.png \
	DockLibrary/stock-menu-right-12.png 

