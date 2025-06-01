using Cairo;

namespace Pinta.Gui.Widgets;

public enum ColorPickType
{
	RegularSingle,
	MainColors,
}

public abstract record ColorPick
{
	public abstract ColorPickType Type { get; }
	protected ColorPick () { }
}

public sealed record RegularSingleColorPick (Color Color)
	: ColorPick
{
	public override ColorPickType Type => ColorPickType.RegularSingle;
}

public sealed record MainColorsPick (Color Primary, Color Secondary)
	: ColorPick
{
	public override ColorPickType Type
		=> ColorPickType.MainColors;

	public MainColorsPick Swapped ()
		=> new (Secondary, Primary);
}
