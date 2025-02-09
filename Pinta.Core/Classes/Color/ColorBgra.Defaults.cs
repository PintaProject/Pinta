namespace Pinta.Core;

partial struct ColorBgra
{
	//// Colors copied from System.Drawing.Color's list
	///
	public static ColorBgra Transparent => Zero; // Note pre-multiplied alpha is used.
	public static ColorBgra Zero => (ColorBgra) 0;

	public static ColorBgra Black => FromBgra (0, 0, 0, 255);
	public static ColorBgra Blue => FromBgra (255, 0, 0, 255);
	public static ColorBgra Cyan => FromBgra (255, 255, 0, 255);
	public static ColorBgra Green => FromBgra (0, 128, 0, 255);
	public static ColorBgra Magenta => FromBgra (255, 0, 255, 255);
	public static ColorBgra Red => FromBgra (0, 0, 255, 255);
	public static ColorBgra White => FromBgra (255, 255, 255, 255);
	public static ColorBgra Yellow => FromBgra (0, 255, 255, 255);
}
