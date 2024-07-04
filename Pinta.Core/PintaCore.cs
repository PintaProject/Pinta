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

namespace Pinta.Core;

public static class PintaCore
{
	public static ActionManager Actions { get; }
	public static ChromeManager Chrome { get; }
	public static EffectsManager Effects { get; }
	public static ImageConverterManager ImageFormats { get; }
	public static IServiceManager Services { get; }
	public static LayerManager Layers { get; }
	public static LivePreviewManager LivePreview { get; }
	public static PaintBrushManager PaintBrushes { get; }
	public static PaletteFormatManager PaletteFormats { get; }
	public static PaletteManager Palette { get; }
	public static RecentFileManager RecentFiles { get; }
	public static ResourceManager Resources { get; }
	public static SettingsManager Settings { get; }
	public static SystemManager System { get; }
	public static ToolManager Tools { get; }
	public static WorkspaceManager Workspace { get; }

	public const string ApplicationVersion = "2.2";

	static PintaCore ()
	{
		// --- Services that don't depend on other services

		ResourceManager resources = new ();
		SystemManager system = new ();
		SettingsManager settings = new ();
		ChromeManager chrome = new ();
		PaintBrushManager paintBrushes = new ();
		PaletteFormatManager paletteFormats = new ();
		RecentFileManager recentFiles = new ();

		// --- Services that depend on other services

		ImageConverterManager imageFormats = new (settings);
		WorkspaceManager workspace = new (system, chrome, imageFormats);
		ToolManager tools = new (workspace, chrome);
		PaletteManager palette = new (settings, paletteFormats);
		LayerManager layers = new (workspace);
		ActionManager actions = new (chrome, imageFormats, layers, paletteFormats, palette, recentFiles, system, tools, workspace);
		LivePreviewManager livePreview = new (workspace, tools, system, chrome);
		EffectsManager effects = new (actions, chrome, livePreview);

		// --- Service manager

		ServiceManager services = new ();
		services.AddService<IResourceService> (resources);
		services.AddService<ISettingsService> (settings);
		services.AddService (actions);
		services.AddService<IWorkspaceService> (workspace);
		services.AddService (layers);
		services.AddService<IPaintBrushService> (paintBrushes);
		services.AddService<IToolService> (tools);
		services.AddService (imageFormats);
		services.AddService (paletteFormats);
		services.AddService (system);
		services.AddService (recentFiles);
		services.AddService (livePreview);
		services.AddService<IPaletteService> (palette);
		services.AddService<IChromeService> (chrome);
		services.AddService (effects);

		// --- References to expose

		Resources = resources;
		System = system;
		Settings = settings;
		Actions = actions;
		Workspace = workspace;
		Layers = layers;
		PaintBrushes = paintBrushes;
		Tools = tools;
		ImageFormats = imageFormats;
		PaletteFormats = paletteFormats;
		RecentFiles = recentFiles;
		LivePreview = livePreview;
		Palette = palette;
		Chrome = chrome;
		Effects = effects;

		Services = services;
	}

	public static void Initialize ()
	{
		Actions.RegisterHandlers ();
	}
}
