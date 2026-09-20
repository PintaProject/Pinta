namespace Pinta.Core;

/// <summary>
/// Extra data saved on the clipboard along with an image, to allow for
/// additional functionality when copy-pasting within Pinta.
/// </summary>
[GObject.Subclass<GObject.Object>]
public partial class ClipboardImageMetadata
{
	/// <summary>
	/// Position that the data was copied from in the source image.
	/// </summary>
	public PointI Position { get; set; } = PointI.Zero;
}
