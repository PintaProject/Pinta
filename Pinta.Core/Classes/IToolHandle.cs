namespace Pinta.Core;

/// <summary>
/// Defines a handle that a tool can draw in the canvas window (for example,
/// a handle for resizing a selection).
/// This is suitable for drawing interactive elements at a constant size in
/// the window (drawing on the ToolLayer is limited to the image's resolution
/// and is affected by the canvas zoom).
/// </summary>
public interface IToolHandle
{
	/// <summary>
	/// Inactive handles are hidden when the canvas is drawn.
	/// </summary>
	public bool Active { get; }

	/// <summary>
	/// Draw the handle onto the canvas widget.
	/// This is in window space, so CanvasPointToWindow() should be used to transform a canvas position
	/// into the correct space.
	/// Use InvalidateWindowRect() to trigger a repaint when the handle's position / size / etc. changes.
	/// </summary>
	public void Draw (Gtk.Snapshot snapshot);
}

