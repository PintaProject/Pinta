using Cairo;

namespace Pinta.Core;

public sealed class StrokeContext
{
	public Color PrimaryColor { get; }
	public Color SecondaryColor { get; }
	public MouseButton MouseButton { get; }
	public Color StrokeColor { get; }

	public StrokeContext (
		Color primaryColor,
		Color secondaryColor,
		MouseButton mouseButton)
	{
		PrimaryColor = primaryColor;
		SecondaryColor = secondaryColor;
		MouseButton = mouseButton;
		StrokeColor = mouseButton switch {
			MouseButton.Right => secondaryColor,
			MouseButton.Left or _ => primaryColor,
		};
	}
}
