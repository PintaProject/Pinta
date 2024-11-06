using Pinta.Core;

namespace PintaBenchmarks;

internal static class Utilities
{
	public static IServiceProvider CreateMockServices ()
	{
		ServiceManager manager = new ();
		manager.AddService<IPaletteService> (new MockPalette ());
		manager.AddService<IChromeService> (new MockChromeManager ());
		manager.AddService<IWorkspaceService> (new MockWorkspaceService ());
		manager.AddService<ILivePreview> (new MockLivePreview (new RectangleI (0, 0, 250, 250)));
		return manager;
	}
}
