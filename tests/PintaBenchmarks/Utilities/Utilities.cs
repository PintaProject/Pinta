using Pinta.Core;

namespace PintaBenchmarks;

internal static class Utilities
{
	public static IServiceManager CreateMockServices ()
		=> new ServiceManager ();
}
