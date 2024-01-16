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
	public static IChromeManager Chrome { get; }
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
	public static IWorkspaceService Workspace { get; }

	public const string ApplicationVersion = "2.2";

	static PintaCore ()
	{
		// Resources and Settings are initialized first so later
		// Managers can access them as needed.
		Resources = new ResourceManager ();
		System = new SystemManager ();
		Settings = new SettingsManager ();
		Actions = new ActionManager ();
		Workspace = new WorkspaceManager ();
		Layers = new LayerManager ();
		PaintBrushes = new PaintBrushManager ();
		Tools = new ToolManager ();
		ImageFormats = new ImageConverterManager ();
		PaletteFormats = new PaletteFormatManager ();
		RecentFiles = new RecentFileManager ();
		LivePreview = new LivePreviewManager ();
		Palette = new PaletteManager ();
		Chrome = new ChromeManager ();
		Effects = new EffectsManager ();

		Services = new ServiceManager ();

		Services.AddService<IResourceService> (Resources);
		Services.AddService<ISettingsService> (Settings);
		Services.AddService (Actions);
		Services.AddService (Workspace);
		Services.AddService (Layers);
		Services.AddService<IPaintBrushService> (PaintBrushes);
		Services.AddService<IToolService> (Tools);
		Services.AddService (ImageFormats);
		Services.AddService (PaletteFormats);
		Services.AddService (System);
		Services.AddService (RecentFiles);

		Services.AddService (LivePreview);
		Services.AddService<IPaletteService> (Palette);
		Services.AddService (Chrome);
		Services.AddService (Effects);
	}

	public static void Initialize ()
	{
		Actions.RegisterHandlers ();
	}
}
