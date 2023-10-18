namespace Pinta.Core;

public interface IColorMapping
{
	ColorBgra GetColor (double position);
}

public interface IRangeColorMapping : IColorMapping
{
	double MinimumPosition { get; }
	double MaximumPosition { get; }
}
