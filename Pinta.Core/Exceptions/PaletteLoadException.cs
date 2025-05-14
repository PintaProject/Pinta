using System;

namespace Pinta.Core;

internal sealed class PaletteLoadException (
	string fileName,
	Exception innerException
)
	: Exception (
		$"File at {fileName} could not load as palette",
		innerException
	)
{
	public string FileName { get; } = fileName;
}
