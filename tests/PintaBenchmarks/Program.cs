using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace PintaBenchmarks;

public class Program
{
	public static void Main (string[] args)
	{
		// Disable the validator for optimized assemblies which complains about Mono.Addins
		var config = ManualConfig.Create (DefaultConfig.Instance).WithOptions (ConfigOptions.DisableOptimizationsValidator);
		// Run all benchmarks
		BenchmarkSwitcher.FromAssembly (typeof (Program).Assembly).Run (args, config);

		// Run individual benchmark suites
		//BenchmarkRunner.Run<CanvasRendererBenchmarks> ();
		//BenchmarkRunner.Run<AdjustmentsBenchmarks> ();
		//BenchmarkRunner.Run<EffectsBenchmarks> ();
	}
}
