using Pinta.Core;

namespace PintaBenchmarks;

internal static class Utilities
{
	public static IServiceManager CreateMockServices ()
	{
		ServiceManager manager = new ();
		manager.AddService<IPaletteService> (new MockPalette ());
		manager.AddService<IChromeManager> (new MockChromeManager ());
		return manager;
	}
}
