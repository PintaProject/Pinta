using BenchmarkDotNet.Configs;
using Perfolizer.Horology;

namespace PintaBenchmarks;

internal class MillisecondConfig : ManualConfig
{
	public MillisecondConfig ()
	{
		SummaryStyle = BenchmarkDotNet.Reports.SummaryStyle.Default.WithTimeUnit (TimeUnit.Millisecond);
	}
}
