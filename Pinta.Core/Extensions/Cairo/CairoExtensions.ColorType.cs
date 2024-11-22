namespace Cairo;

// TODO-GTK4 (bindings, unsubmitted) - should this be added to gir.core?
public readonly record struct Color (
	double R,
	double G,
	double B,
	double A)
{
	public Color (double r, double g, double b)
		: this (r, g, b, 1.0)
	{ }
}
