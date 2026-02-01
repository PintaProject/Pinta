using System;
using Pinta.Core;

namespace Pinta.Effects;

public class MockSystemService : ISystemService
{
	public int RenderThreads { get; set; } = Environment.ProcessorCount;
	public OS OperatingSystem { get; } = OS.Other;
}
