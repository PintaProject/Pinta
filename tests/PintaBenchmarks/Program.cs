using BenchmarkDotNet.Running;

namespace PintaBenchmarks;

public class Program
{
	public static void Main (string[] args)
	{
		// Run all benchmarks
		BenchmarkSwitcher.FromAssembly (typeof (Program).Assembly).Run (args);

		// Run individual benchmark suites
		//BenchmarkRunner.Run<CanvasRendererBenchmarks> ();
		//BenchmarkRunner.Run<AdjustmentsBenchmarks> ();
		//BenchmarkRunner.Run<EffectsBenchmarks> ();
	}
}
