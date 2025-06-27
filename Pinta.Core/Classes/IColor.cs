namespace Pinta.Core;

public interface IColor<TColor>
{
	static abstract TColor Lerp (in TColor from, in TColor to, double frac);
}
