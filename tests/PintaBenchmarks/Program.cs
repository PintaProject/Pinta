using BenchmarkDotNet.Running;

namespace PintaBenchmarks;

public class Program
{
	public static void Main ()
	{
		// Run all benchmarks
		BenchmarkRunner.Run (typeof (Program).Assembly);

		// Run individual benchmark suites
		//BenchmarkRunner.Run<CanvasRendererBenchmarks> ();
		//BenchmarkRunner.Run<AdjustmentsBenchmarks> ();
		//BenchmarkRunner.Run<EffectsBenchmarks> ();
	}
}
