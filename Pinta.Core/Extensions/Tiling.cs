namespace Pinta.Core;

public static class Tiling
{
	/// <returns>
	/// Offsets of pixels, if we consider all pixels in a canvas of
	/// size <paramref name="canvasSize"/> to be sequential in memory
	/// (from left to right, and top to bottom)
	/// </returns>
	public static PixelOffsetEnumerable GeneratePixelOffsets (this RectangleI bounds, Size canvasSize)
		=> new (bounds, canvasSize);
}
