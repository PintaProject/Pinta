using System;
using System.Collections.ObjectModel;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects.Tests;

internal sealed class MockPalette : IPaletteService
{
	public Color PrimaryColor { get; set; } = Color.Black;
	public Color SecondaryColor { get; set; } = Color.White;

	public Palette CurrentPalette
		=> throw new NotImplementedException ();

	public int MaxRecentlyUsedColor
		=> throw new NotImplementedException ();

	public ReadOnlyCollection<Color> RecentlyUsedColors
		=> throw new NotImplementedException ();

#pragma warning disable CS0067 // The event 'X' is never used
	public event EventHandler? PrimaryColorChanged;
	public event EventHandler? SecondaryColorChanged;
	public event EventHandler? RecentColorsChanged;
#pragma warning restore CS0067 // The event 'x' is never used

	public void SetColor (bool setPrimary, Color color, bool addToRecent = true)
	{
		if (setPrimary)
			PrimaryColor = color;
		else
			SecondaryColor = color;
	}
}
