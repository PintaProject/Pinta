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

public sealed record SingleColor (Color Color)
	: ColorPick
{
	public override ColorPickType Type
 		=> ColorPickType.RegularSingle;
}

public sealed record PaletteColors (Color Primary, Color Secondary)
	: ColorPick
{
	public override ColorPickType Type
		=> ColorPickType.MainColors;

	public PaletteColors Swapped ()
		=> new (Secondary, Primary);
}
