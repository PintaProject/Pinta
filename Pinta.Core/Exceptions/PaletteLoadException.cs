using System;

namespace Pinta.Core;

public sealed class PaletteLoadException : Exception
{
	public string FileName { get; }
	internal PaletteLoadException (string fileName)
		: base ($"File at {fileName} could not load as palette")
	{
		FileName = fileName;
	}
}
