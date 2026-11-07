// SmudgeTool.cs
//
// A brush tool that smears existing pixels along the stroke, like dragging a
// finger through wet paint. Built from the stroke-stepping and pixel-editing
// approach used by EraserTool (Smooth mode) and the toolbar pattern of RecolorTool.
//
// Permission is hereby granted under the MIT License; see license-mit.txt.

using System;
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class SmudgeTool : BaseBrushTool
{
	private PointI? last_point = null;

	// Pixels "carried" on the finger, stored premultiplied as floats,
	// laid out as a (2 * radius + 1) square centered on the brush.
	private float[]? carried;
	private int carried_radius;

	public SmudgeTool (IServiceProvider services) : base (services)
	{
	}

	public override string Name => Translations.GetString ("Smudge");
	public override string Icon => Pinta.Resources.Icons.ToolSmudge;
	public override string StatusBarText => Translations.GetString ("Click and drag to smudge the pixels under the brush.");
	public override Gdk.Cursor DefaultCursor => Gdk.Cursor.NewFromTexture (Resources.GetIcon ("Cursor.Recolor.png"), 9, 18, null);
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_U);
	public override int Priority => 51;

	// Strength is 0..1. Higher values smear further.
	private float Strength => (float) (StrengthSlider.GetValue () / 100.0);

	protected override void OnBuildToolBar (Box tb)
	{
		base.OnBuildToolBar (tb);

		tb.Append (Separator);
		tb.Append (StrengthLabel);
		tb.Append (StrengthSlider);
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (mouse_button == MouseButton.None) {
			last_point = null;
			carried = null;
			return;
		}

		ImageSurface surf = document.Layers.CurrentUserLayer.Surface;
		PointI newPoint = e.Point;

		// First event of the stroke: pick up what is under the brush, do not smear yet.
		if (!last_point.HasValue || carried is null) {
			PickUp (surf, newPoint);
			last_point = newPoint;
			return;
		}

		if (document.Workspace.PointInCanvas (e.PointDouble))
			surface_modified = true;

		using Context g = document.CreateClippedContext ();

		SmudgeSegment (surf, g, (PointD) last_point.Value, e.PointDouble);

		int dirtyPadding = BrushWidth + 2;
		RectangleI dirty = RectangleI.FromPoints (last_point.Value, newPoint).Inflated (dirtyPadding, dirtyPadding);

		if (document.Workspace.IsPartiallyOffscreen (dirty))
			document.Workspace.Invalidate ();
		else
			document.Workspace.Invalidate (document.ClampToImageSize (dirty));

		last_point = newPoint;
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		base.OnMouseUp (document, e);

		last_point = null;
		carried = null;
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		if (strength_slider is not null)
			settings.PutSetting (SettingNames.SMUDGE_STRENGTH, (int) strength_slider.GetValue ());
	}

	// Brush radius in pixels (at least 1).
	private int Radius => Math.Max (1, BrushWidth / 2);

	// Cosine falloff: 1 at the center of the brush, 0 at the edge.
	private static float Falloff (float normalizedDistance)
		=> 0.5f * (1f + MathF.Cos (MathF.PI * normalizedDistance));

	private void PickUp (ImageSurface surf, PointI center)
	{
		int rad = Radius;
		int size = 2 * rad + 1;
		carried_radius = rad;
		carried = new float[size * size * 4];

		ReadOnlySpan<ColorBgra> data = surf.GetReadOnlyPixelData ();
		int width = surf.Width;
		int height = surf.Height;

		for (int oy = -rad; oy <= rad; oy++) {
			for (int ox = -rad; ox <= rad; ox++) {
				int x = center.X + ox;
				int y = center.Y + oy;
				if (x < 0 || y < 0 || x >= width || y >= height)
					continue;

				ColorBgra p = data[y * width + x];
				int i = ((oy + rad) * size + (ox + rad)) * 4;
				carried[i] = p.B;
				carried[i + 1] = p.G;
				carried[i + 2] = p.R;
				carried[i + 3] = p.A;
			}
		}
	}

	private void SmudgeSegment (ImageSurface surf, Context g, PointD start, PointD end)
	{
		int rad = Radius;

		// If the brush size changed mid-stroke, restart the carried buffer.
		if (carried is null || carried_radius != rad) {
			PickUp (surf, new PointI ((int) start.X, (int) start.Y));
		}

		// Small spacing between dabs keeps the smear continuous.
		double spacing = Math.Max (1.0, rad / 4.0);
		int steps = Math.Max (1, (int) Math.Ceiling (start.Distance (end) / spacing));

		for (int step = 1; step <= steps; step++) {
			PointD pt = Utility.Lerp (start, end, (float) step / steps);
			ApplyDab (surf, g, (int) pt.X, (int) pt.Y, rad);
		}
	}

	private void ApplyDab (ImageSurface surf, Context g, int cx, int cy, int rad)
	{
		float[] carry = carried!;
		int size = 2 * rad + 1;
		float strength = Strength;

		RectangleI surfaceBounds = new (0, 0, surf.Width, surf.Height);
		RectangleI brushBounds = new (cx - rad, cy - rad, size, size);
		RectangleI dest = RectangleI.Intersect (surfaceBounds, brushBounds);

		if (dest.Width <= 0 || dest.Height <= 0)
			return;

		// Work on a temporary copy so the selection clip is honored when pasting back.
		ImageSurface temp = CopySurfacePart (surf, dest);
		Span<ColorBgra> data = temp.GetPixelData ();

		for (int iy = dest.Top; iy < dest.Bottom; iy++) {
			int oy = iy - cy;
			for (int ix = dest.Left; ix < dest.Right; ix++) {
				int ox = ix - cx;

				float dist = MathF.Sqrt (ox * ox + oy * oy) / rad;
				if (dist >= 1f)
					continue;

				float f = Falloff (dist);
				float deposit = strength * f; // how much carried paint lands here
				float pickup = (1f - strength) * f; // how much of the canvas the finger picks up

				int ti = (iy - dest.Top) * temp.Width + (ix - dest.Left);
				int ci = ((oy + rad) * size + (ox + rad)) * 4;
				ColorBgra under = data[ti];

				float nb = under.B + (carry[ci] - under.B) * deposit;
				float ng = under.G + (carry[ci + 1] - under.G) * deposit;
				float nr = under.R + (carry[ci + 2] - under.R) * deposit;
				float na = under.A + (carry[ci + 3] - under.A) * deposit;

				// Update the carried pixel with a bit of what was underneath.
				carry[ci] += (under.B - carry[ci]) * pickup;
				carry[ci + 1] += (under.G - carry[ci + 1]) * pickup;
				carry[ci + 2] += (under.R - carry[ci + 2]) * pickup;
				carry[ci + 3] += (under.A - carry[ci + 3]) * pickup;

				// Premultiplied alpha: color channels must never exceed alpha.
				byte a = ToByte (na);
				data[ti] = ColorBgra.FromBgra (
					b: Math.Min (ToByte (nb), a),
					g: Math.Min (ToByte (ng), a),
					r: Math.Min (ToByte (nr), a),
					a: a);
			}
		}

		PasteSurfacePart (g, temp, dest);
	}

	private static byte ToByte (float v)
		=> (byte) Math.Clamp ((int) (v + 0.5f), 0, 255);

	private static ImageSurface CopySurfacePart (ImageSurface surface, RectangleI bounds)
	{
		ImageSurface temp = CairoExtensions.CreateImageSurface (Format.Argb32, bounds.Width, bounds.Height);

		using Context g = new (temp) { Operator = Operator.Source };
		g.SetSourceSurface (surface, -bounds.Left, -bounds.Top);
		g.Rectangle (new RectangleD (0, 0, bounds.Width, bounds.Height));
		g.Fill ();

		temp.Flush ();
		return temp;
	}

	private static void PasteSurfacePart (Context g, ImageSurface temp, RectangleI bounds)
	{
		g.Operator = Operator.Source;
		g.SetSourceSurface (temp, bounds.Left, bounds.Top);
		g.Rectangle (new RectangleD (bounds.Left, bounds.Top, bounds.Width, bounds.Height));
		g.Fill ();
	}

	private Label? strength_label;
	private Scale? strength_slider;
	private Separator? separator;

	private Label StrengthLabel => strength_label ??= Label.New (string.Format ("  {0}: ", Translations.GetString ("Strength")));
	private Scale StrengthSlider => strength_slider ??= GtkExtensions.CreateToolBarSlider (0, 100, 1, Settings.GetSetting (SettingNames.SMUDGE_STRENGTH, 50));
	private Separator Separator => separator ??= GtkExtensions.CreateToolBarSeparator ();
}
