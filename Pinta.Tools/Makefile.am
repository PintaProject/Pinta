include $(top_srcdir)/Makefile.include

ASSEMBLY = Pinta.Tools
TARGET = library
ASSEMBLY_BUILD_FLAGS = -unsafe
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)
BUILD_SUBDIR = "/Extensions"

REFERENCES = \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(MONO_CAIRO_LIBS) \
	-r:../bin/Pinta.Core.dll \
	-r:../bin/System.Composition.ComponentModel.dll

SOURCES =  \
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

