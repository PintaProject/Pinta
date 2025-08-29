using Pinta.Core;

namespace Pinta.Effects;

// TODO: Remove when we have blend op classes for premultiplied alpha

/// <summary>
/// Provides methods for Porter-Duff compositing operations on premultiplied ColorBgra values.
/// </summary>
internal static class Blending
{
	/// <summary>
	/// Blends a source (top) color over a destination (bottom) color using the 'Normal' (Source Over) blend mode.
	/// </summary>
	/// <param name="bottom">The destination color (background layer).</param>
	/// <param name="top">The source color (foreground layer).</param>
	internal static ColorBgra Normal (in ColorBgra bottom, in ColorBgra top)
	{
		// These are the relevant mathematical formulae:
		// 
		// - C_out = C_a + C_b * (1 - A_a)
		// - A_out = A_a + A_b * (1 - A_a)
		// 
		// Where:
		// 
		// - C refers to the color channels: R, G, B
		// - A refers to the alpha channel
		// - a refers to the color on the top layer
		// - b refers to the color on the bottom layer
		// 
		// Integer arithmetic is used for efficiency.
		// 
		// If one reads about the theory behind the blending,
		// values in the operations usually range from 0 to 1,
		// but here they range from 0 to 255.
		// 
		// That is, the values are scaled by 255
		// with respect to their "theoretical" counterparts.
		// 
		// This is also why 'ROUNDING_ADDEND' can be either 127 or 128.
		// This 'ROUNDING_ADDEND' mechanism is a neat trick that
		// forces the truncation operator to function as a rounding operator.
		// This _addend_ would normally be 0.5,
		// and if it's scaled by a factor of 255, it's 127.5

		if (top.A == 255) return top; // Top layer is fully opaque
		if (top.A == 0) return bottom; // Top layer is fully transparent
		int inverseSourceAlpha = 255 - top.A;
		const bool ROUNDING_ERR_SIDE = true;
		const int ROUNDING_ADDEND = ROUNDING_ERR_SIDE ? 128 : 127;
		byte outA = Utility.ClampToByte (top.A + (bottom.A * inverseSourceAlpha + ROUNDING_ADDEND) / 255);
		byte outR = Utility.ClampToByte ((top.R * 255 + bottom.R * inverseSourceAlpha + ROUNDING_ADDEND) / 255);
		byte outG = Utility.ClampToByte ((top.G * 255 + bottom.G * inverseSourceAlpha + ROUNDING_ADDEND) / 255);
		byte outB = Utility.ClampToByte ((top.B * 255 + bottom.B * inverseSourceAlpha + ROUNDING_ADDEND) / 255);
		return ColorBgra.FromBgra (outB, outG, outR, outA);
	}
}
