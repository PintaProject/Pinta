namespace Pinta.Core;

/// <summary>
///     <para>
///         Represents an offset from the center
///         as a proportion of the image's half-dimensions.
///     </para>
///     <para>
///         For example, if this offset is placed at the
///         image's left edge, its <see cref="Horizontal"/>
///         offset would be -1, and if it's placed at the
///         image's right edge it would be 1
///     </para>
/// </summary>
public readonly record struct CenterOffset (
	double Horizontal,
	double Vertical)
{
	public static CenterOffset FromPoint (PointD p)
		=> new (p.X, p.Y);
};
