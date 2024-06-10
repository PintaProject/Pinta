using Cairo;
using Pinta.Core;

namespace PintaBenchmarks;

internal sealed class MockPalette : IPaletteService
{
	public Color PrimaryColor { get; set; } = new (0, 0, 0); // Black
	public Color SecondaryColor { get; set; } = new (1, 1, 1); // White

	public event EventHandler? PrimaryColorChanged;
	public event EventHandler? SecondaryColorChanged;

	public void SetColor (bool setPrimary, Color color, bool addToRecent = true)
	{
		if (setPrimary)
			PrimaryColor = color;
		else
			SecondaryColor = color;
	}
}
