using Pinta.Core;

namespace PintaBenchmarks;

internal class MockSystemService : ISystemService
{
	public int RenderThreads => Environment.ProcessorCount;

	public OS OperatingSystem => throw new NotImplementedException ();
}
