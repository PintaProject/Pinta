namespace Pinta.Core;

public interface IColor<TColor> where TColor : IColor<TColor>
{
	static abstract TColor Black { get; }

	static abstract TColor Red { get; }
	static abstract TColor Green { get; }
	static abstract TColor Blue { get; }

	static abstract TColor Yellow { get; }
	static abstract TColor Magenta { get; }
	static abstract TColor Cyan { get; }

	static abstract TColor White { get; }
}

public interface IAlphaColor<TColor> : IColor<TColor> where TColor : IAlphaColor<TColor>
{
	static abstract TColor Transparent { get; }
}

public interface IInterpolableColor<TColor> : IColor<TColor> where TColor : IInterpolableColor<TColor>
{
	static abstract TColor Lerp (in TColor from, in TColor to, double frac);
}
