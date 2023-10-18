namespace Pinta.Core;

public interface IColorMapping
{
	ColorBgra GetColor (double position);
	bool IsMapped (double position);
}

public interface IRangeColorMapping : IColorMapping
{
	double MinimumPosition { get; }
	double MaximumPosition { get; }
}
