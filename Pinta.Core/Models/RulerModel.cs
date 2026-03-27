namespace Pinta.Core;

public sealed class RulerModel (Gtk.Orientation orientation)
{
	/// <summary>The position of the mark along the ruler.</summary>
	public double Position { get; set; } = 0;

	/// <summary>Metric type used for the ruler.</summary>
	public MetricType Metric { get; set; } = MetricType.Pixels;

	public NumberRange<double>? SelectionBounds { get; set; } = null;

	public NumberRange<double> RulerRange { get; set; } = default;

	/// <summary>Whether the ruler is horizontal or vertical.</summary>
	public Gtk.Orientation Orientation { get; } = orientation;
}
