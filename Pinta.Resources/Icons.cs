// 
// Icons.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pinta.Resources
{
	public static class StandardIcons
	{
		public const string ApplicationExit = "application-exit-symbolic";

		public const string DialogError = "dialog-error-symbolic";

		public const string DocumentNew = "document-new-symbolic";
		public const string DocumentOpen = "document-open-symbolic";
		public const string DocumentPrint = "document-print-symbolic";
		public const string DocumentRevert = "document-revert-symbolic";
		public const string DocumentSave = "document-save-symbolic";
		public const string DocumentSaveAs = "document-save-as-symbolic";

		public const string FormatJustifyLeft = "format-justify-left-symbolic";
		public const string FormatJustifyCenter = "format-justify-center-symbolic";
		public const string FormatJustifyRight = "format-justify-right-symbolic";
		public const string FormatTextBold = "format-text-bold-symbolic";
		public const string FormatTextItalic = "format-text-italic-symbolic";
		public const string FormatTextUnderline = "format-text-underline-symbolic";

		public const string EditCopy = "edit-copy-symbolic";
		public const string EditCut = "edit-cut-symbolic";
		public const string EditPaste = "edit-paste-symbolic";
		public const string EditRedo = "edit-redo-symbolic";
		public const string EditSelectAll = "edit-select-all-symbolic";
		public const string EditUndo = "edit-undo-symbolic";

		public const string GoPrevious = "go-previous-symbolic";

		public const string HelpAbout = "help-about-symbolic";
		public const string HelpBrowser = "help-browser-symbolic";

		public const string ImageMissing = "image-missing-symbolic";

		public const string ValueDecrease = "value-decrease-symbolic";
		public const string ValueIncrease = "value-increase-symbolic";
		public const string ViewFullscreen = "view-fullscreen-symbolic";

		public const string WindowClose = "window-close-symbolic";
		public const string WindowMaximize = "window-maximize-symbolic";
		public const string WindowMinimize = "window-minimize-symbolic";

		public const string ZoomFitBest = "zoom-fit-best-symbolic";
		public const string ZoomIn = "zoom-in-symbolic";
		public const string ZoomOut = "zoom-out-symbolic";
		public const string ZoomOriginal = "zoom-original-symbolic";
	}

	public static class Icons
	{
		public const string AboutPinta = "about-pinta";

		public const string AddinsManage = "addins-manage";

		public const string AntiAliasingEnabled = "tool-antialiasing-enabled-symbolic";
		public const string AntiAliasingDisabled = "tool-antialiasing-disabled-symbolic";

		public const string BlendingNormal = "tool-blending-normal-symbolic";
		public const string BlendingOverwrite = "tool-blending-overwrite-symbolic";

		public const string ColorModeColor = "tool-gradient-colormode-color-symbolic";
		public const string ColorModeTransparency = "tool-gradient-colormode-transparency-symbolic";

		public const string ColorPaletteReset = "ui-colorpalette-reset-symbolic";
		public const string ColorPaletteSwap = "ui-colorpalette-swap-symbolic";

		public const string CursorPosition = "ui-cursor-location-symbolic";

		public const string EditSelectionErase = "edit-selection-erase";
		public const string EditSelectionFill = "edit-selection-fill";
		public const string EditSelectionInvert = "edit-selection-invert";
		public const string EditSelectionNone = "ui-deselect-symbolic";

		public const string GradientConical = "tool-gradient-conical-symbolic";
		public const string GradientDiamond = "tool-gradient-diamond-symbolic";
		public const string GradientLinear = "tool-gradient-linear-symbolic";
		public const string GradientLinearReflected = "tool-gradient-linear-reflected-symbolic";
		public const string GradientRadial = "tool-gradient-radial-symbolic";

		public const string FillStyleBackground = "tool-fillstyle-background-symbolic";
		public const string FillStyleFill = "tool-fillstyle-fill-symbolic";
		public const string FillStyleOutline = "tool-fillstyle-outline-symbolic";
		public const string FillStyleOutlineFill = "tool-fillstyle-outlinefill-symbolic";

		public const string HelpBug = "help-bug";
		public const string HelpTranslate = "help-translate";
		public const string HelpWebsite = "help-website-symbolic";

		public const string ImageCrop = "ui-crop-to-selection-symbolic";
		public const string ImageResize = "image-resize";
		public const string ImageResizeCanvas = "image-resize-canvas";
		public const string ImageFlipHorizontal = "image-flip-horizontal-symbolic";
		public const string ImageFlipVertical = "image-flip-vertical-symbolic";
		public const string ImageRotate90CW = "image-rotate-90cw-symbolic";
		public const string ImageRotate90CCW = "image-rotate-90ccw-symbolic";
		public const string ImageRotate180 = "image-rotate-180-symbolic";
		public const string ImageFlatten = "image-flatten";
		public const string OrientationPortrait = "image-orientation-portrait-symbolic";
		public const string OrientationLandscape = "image-orientation-landscape-symbolic";

		public const string LayerDelete = "layers-remove-layer-symbolic";
		public const string LayerDuplicate = "layers-duplicate-layer-symbolic";
		public const string LayerFlipHorizontal = ImageFlipHorizontal;
		public const string LayerFlipVertical = ImageFlipVertical;
		public const string LayerImport = "layer-import";
		public const string LayerMergeDown = "layers-merge-down-symbolic";
		public const string LayerMoveDown = "layers-move-layer-down-symbolic";
		public const string LayerMoveUp = "layers-move-layer-up-symbolic";
		public const string LayerNew = "layers-add-layer-symbolic";
		public const string LayerProperties = "document-properties-symbolic";
		public const string LayerRotateZoom = "layers-rotate-zoom-symbolic";

		public const string Pinta = "pinta";

		public const string ResizeCanvasBase = "image-resize-canvas-base-symbolic";
		public const string ResizeCanvasDown = "image-resize-canvas-down-symbolic";
		public const string ResizeCanvasLeft = "image-resize-canvas-left-symbolic";
		public const string ResizeCanvasNE = "image-resize-canvas-ne-symbolic";
		public const string ResizeCanvasNW = "image-resize-canvas-nw-symbolic";
		public const string ResizeCanvasRight = "image-resize-canvas-right-symbolic";
		public const string ResizeCanvasSE = "image-resize-canvas-se-symbolic";
		public const string ResizeCanvasSW = "image-resize-canvas-sw-symbolic";
		public const string ResizeCanvasUp = "image-resize-canvas-up-symbolic";

		public const string Sampling1 = "tool-colorpicker-sampling-1x1-symbolic";
		public const string Sampling3 = "tool-colorpicker-sampling-3x3-symbolic";
		public const string Sampling5 = "tool-colorpicker-sampling-5x5-symbolic";
		public const string Sampling7 = "tool-colorpicker-sampling-7x7-symbolic";
		public const string Sampling9 = "tool-colorpicker-sampling-9x9-symbolic";

		public const string ToolCloneStamp = "tool-clonestamp-symbolic";
		public const string ToolColorPicker = "tool-colorpicker-symbolic";
		public const string ToolColorPickerPreviousTool = "go-previous-symbolic";
		public const string ToolEllipse = "tool-ellipse-symbolic";
		public const string ToolEraser = "tool-eraser-symbolic";
		public const string ToolFreeformShape = "tool-freeformshape-symbolic";
		public const string ToolGradient = "tool-gradient-symbolic";
		public const string ToolLine = "tool-line-symbolic";
		public const string ToolMove = "tool-move-symbolic";
		public const string ToolMoveCursor = "tool-move-cursor-symbolic";
		public const string ToolMoveSelection = "tool-move-selection-symbolic";
		public const string ToolPaintBrush = "tool-paintbrush-symbolic";
		public const string ToolPaintBucket = "tool-paintbucket-symbolic";
		public const string ToolPan = "tool-pan-symbolic";
		public const string ToolPencil = "tool-pencil-symbolic";
		public const string ToolRecolor = "tool-recolor-symbolic";
		public const string ToolRectangle = "tool-rectangle-symbolic";
		public const string ToolRectangleRounded = "tool-rectangle-rounded-symbolic";
		public const string ToolSelectEllipse = "tool-select-ellipse-symbolic";
		public const string ToolSelectLasso = "tool-select-lasso-symbolic";
		public const string ToolSelectMagicWand = "tool-select-magicwand-symbolic";
		public const string ToolSelectRectangle = "tool-select-rectangle-symbolic";
		public const string ToolText = "tool-text-symbolic";
		public const string ToolZoom = "tool-zoom-symbolic";

		public const string ViewGrid = "view-grid";
		public const string ViewRulers = "view-rulers";
		public const string ViewZoom100 = "view-zoom-100";
		public const string ViewZoomIn = "view-zoom-in";
		public const string ViewZoomOut = "view-zoom-out";
		public const string ViewZoomSelection = "view-zoom-selection";
		public const string ViewZoomWindow = "view-zoom-window";
	}

	/// <summary>
	/// Standard CSS cursor names (see Gdk.Cursor.new_from_name docs for a complete list).
	/// </summary>
	public static class StandardCursors
	{
		public const string Grab = "grab";
		public const string Grabbing = "grabbing";
	}
}
