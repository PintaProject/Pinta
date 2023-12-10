using System;

namespace Pinta.Core;

/// <summary>
/// The resampling mode to use when resizing an image.
/// </summary>
public enum ResamplingMode
{
	Bilinear,
	NearestNeighbor,
}

public static class ResamplingModeExtensions
{
	/// <summary>
	/// Returns a user-facing label for a resampling mode.
	/// </summary>
	public static string GetLabel (this ResamplingMode mode)
	{
		return mode switch {
			ResamplingMode.Bilinear => Translations.GetString ("Bilinear"),
			ResamplingMode.NearestNeighbor => Translations.GetString ("Nearest Neighbor"),
			_ => throw new ArgumentOutOfRangeException (nameof (mode))
		};
	}

	/// <summary>
	/// Translates a resampling mode to the equivalent Cairo filter.
	/// </summary>
	public static Cairo.Filter ToCairoFilter (this ResamplingMode mode)
	{
		return mode switch {
			ResamplingMode.Bilinear => Cairo.Filter.Bilinear,
			ResamplingMode.NearestNeighbor => Cairo.Filter.Nearest,
			_ => throw new ArgumentOutOfRangeException (nameof (mode))
		};
	}
}
