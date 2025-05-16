using System;

namespace Pinta.Core;

public sealed class PaletteLoadException : Exception
{
	public string FileName { get; }
	internal PaletteLoadException (string fileName, string message)
		: base (message)
	{
		FileName = fileName;
	}
}
