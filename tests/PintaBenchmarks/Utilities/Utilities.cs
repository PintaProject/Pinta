using Pinta.Core;

namespace PintaBenchmarks;

internal static class Utilities
{
	private static readonly IServiceManager mock_services;

	static Utilities ()
	{
		mock_services = CreateMockServices ();
	}

	private static IServiceManager CreateMockServices ()
		=> new ServiceManager ();

	public static IServiceManager GetMockServices ()
		=> mock_services;
}
