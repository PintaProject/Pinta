using Cairo;

namespace Pinta.Gui.Widgets;

public abstract record ColorPick
{
	protected ColorPick () { }
}

public sealed record SingleColor (Color Color)
	: ColorPick
{ }

public sealed record PaletteColors (Color Primary, Color Secondary)
	: ColorPick
{
	public PaletteColors Swapped ()
		=> new (Secondary, Primary);
}
