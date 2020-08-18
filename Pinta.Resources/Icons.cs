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
using Gdk;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pinta.Resources
{
	public static class StandardIcons
    {
		public const string ApplicationExit = "application-exit";

		public const string DialogError = "dialog-error";

		public const string DocumentNew = "document-new";
		public const string DocumentOpen = "document-open";
		public const string DocumentPrint = "document-print";
		public const string DocumentRevert = "document-revert";
		public const string DocumentSave = "document-save";
		public const string DocumentSaveAs = "document-save-as";

		public const string EditCopy = "edit-copy";
		public const string EditCut = "edit-cut";
		public const string EditPaste = "edit-paste";
		public const string EditRedo = "edit-redo";
		public const string EditSelectAll = "edit-select-all";
		public const string EditUndo = "edit-undo";

		public const string GoPrevious = "go-previous";

		public const string HelpAbout = "help-about";
		public const string HelpBrowser = "help-browser";

		public const string ViewFullscreen = "view-fullscreen";

		public const string WindowClose = "window-close";

		public const string ZoomFitBest = "zoom-fit-best";
		public const string ZoomIn = "zoom-in";
		public const string ZoomOut = "zoom-out";
		public const string ZoomOriginal = "zoom-original";
	}

	public static class Icons
	{
		public const string ImageCrop = "image-crop";
		public const string ImageResize = "image-resize";
		public const string ImageResizeCanvas = "image-resize-canvas";
		public const string ImageFlipHorizontal = "image-flip-horizontal";
		public const string ImageFlipVertical = "image-flip-vertical";
		public const string ImageRotate90CW = "image-rotate-90cw";
		public const string ImageRotate90CCW = "image-rotate-90ccw";
		public const string ImageRotate180 = "image-rotate-180";
		public const string ImageFlatten = "image-flatten";
	}
}
