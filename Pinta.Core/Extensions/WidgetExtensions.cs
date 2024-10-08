using Gtk;

namespace Pinta.Core.Extensions;

public static class WidgetExtensions
{
	/// <summary>
	/// Checks whether the mousePos (which is relative to topwidget) is within the area and returns its relative position to the area.
	/// </summary>
	/// <param name="widget">Drawing area where returns true if mouse inside.</param>
	/// <param name="topWidget">The top widget. This is what the mouse position is relative to.</param>
	/// <param name="mousePos">Position of the mouse relative to the top widget, usually obtained from Gtk.GestureClick</param>
	/// <param name="relPos">Position of the mouse relative to the drawing area.</param>
	/// <returns>Whether or not mouse position is within the drawing area.</returns>
	public static bool IsMouseInDrawingArea (this Widget widget, Widget topWidget, PointD mousePos, out PointD relPos)
	{
		widget.TranslateCoordinates (topWidget, 0, 0, out double x, out double y);
		relPos = new PointD ((mousePos.X - x), (mousePos.Y - y));
		if (relPos.X >= 0 && relPos.X <= widget.GetWidth () && relPos.Y >= 0 && relPos.Y <= widget.GetHeight ())
			return true;
		return false;
	}
}
