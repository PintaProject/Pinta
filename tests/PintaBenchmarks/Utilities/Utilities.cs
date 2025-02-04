using Pinta.Core;

namespace PintaBenchmarks;

internal static class Utilities
{
	public static IServiceProvider CreateMockServices ()
	{
		Size imageSize = new (250, 250);

		ServiceManager manager = new ();
		manager.AddService<IPaletteService> (new MockPalette ());
		manager.AddService<IChromeService> (new MockChromeManager ());
		manager.AddService<IWorkspaceService> (new MockWorkspaceService (imageSize));
		manager.AddService<ILivePreview> (new MockLivePreview (new RectangleI (0, 0, imageSize.Width, imageSize.Height)));
		return manager;
	}
}
