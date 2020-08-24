// 
// PintaCore.cs
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
using Gtk;
using Pinta.Resources;

namespace Pinta.Core
{
	public static class PintaCore
	{
		public static LayerManager Layers { get; private set; }
		public static PaintBrushManager PaintBrushes { get; private set; }
		public static ToolManager Tools { get; private set; }
		public static ChromeManager Chrome { get; private set; }
		public static PaletteManager Palette { get; private set; }
		public static ResourceManager Resources { get; private set; }
		public static ActionManager Actions { get; private set; }
		public static WorkspaceManager Workspace { get; private set; }
		public static HistoryManager History { get; private set; }
		public static SystemManager System { get; private set; }
		public static LivePreviewManager LivePreview { get; private set; }
		public static SettingsManager Settings { get; private set; }
		public static EffectsManager Effects { get; private set; }

        public const string ApplicationVersion = "1.8";

		static PintaCore ()
		{
			Resources = new ResourceManager ();
			Actions = new ActionManager ();
			Workspace = new WorkspaceManager ();
			Layers = new LayerManager ();
			PaintBrushes = new PaintBrushManager ();
			Tools = new ToolManager ();
			History = new HistoryManager ();
			System = new SystemManager ();
			LivePreview = new LivePreviewManager ();
			Palette = new PaletteManager ();
			Settings = new SettingsManager ();
			Chrome = new ChromeManager ();
			Effects = new EffectsManager ();
		}
		
		public static void Initialize ()
		{
			Actions.RegisterHandlers ();
		}
	}
}
