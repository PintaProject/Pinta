using System;
using Cairo;

using Pinta.Core;

namespace Pinta.Tools.Brushes;

internal sealed class SmoothBrush : BasePaintBrush
{
	const int LUT_Resolution = 256;
	byte[,] lut_factor = null;

	public override string Name {
		get { return Translations.GetString ("Smooth"); }
	}

	public override double StrokeAlphaMultiplier {
		get { return 1.0; }
	}

	protected override unsafe RectangleI OnMouseMove (Context g,
	ImageSurface surface,
	BrushStrokeArgs strokeArgs)
	{
		int rad = (int) (g.LineWidth / 2.0) + 1;
		int stroke_a = (int) (255.0 * strokeArgs.StrokeColor.A);
		int stroke_r = (int) (255.0 * strokeArgs.StrokeColor.R);
		int stroke_g = (int) (255.0 * strokeArgs.StrokeColor.G);
		int stroke_b = (int) (255.0 * strokeArgs.StrokeColor.B);

		RectangleI surface_rect = new RectangleI (0, 0, surface.Width, surface.Height);
		RectangleI brush_rect = new RectangleI (strokeArgs.CurrentPosition.X - rad, strokeArgs.CurrentPosition.Y - rad, 2 * rad, 2 * rad);
		RectangleI dest_rect = RectangleI.Intersect (surface_rect, brush_rect);

		//Initialize lookup table when first used (to prevent slower startup of the application)
		if (lut_factor == null) {
			lut_factor = new byte[LUT_Resolution + 1, LUT_Resolution + 1];

			for (int dy = 0; dy < LUT_Resolution + 1; dy++) {
				for (int dx = 0; dx < LUT_Resolution + 1; dx++) {
					double d = Math.Sqrt (dx * dx + dy * dy) / LUT_Resolution;
					if (d > 1.0)
						lut_factor[dx, dy] = 0;
					else
						lut_factor[dx, dy] = (byte) (Math.Cos (Math.Sqrt (d) * Math.PI / 2.0) * 255);
				}
			}
		}

		if ((dest_rect.Width > 0) && (dest_rect.Height > 0)) {

			//Allow Clipping through a temporary surface
			ImageSurface tmp_surface = new ImageSurface (Format.Argb32, dest_rect.Width, dest_rect.Height);

			using (Context g2 = new Context (tmp_surface)) {
				g2.Operator = Operator.Source;
				g2.SetSourceSurface (surface, -dest_rect.Left, -dest_rect.Top);
				g2.Rectangle (0, 0, dest_rect.Width, dest_rect.Height);
				g2.Fill ();
			}

			//Flush to make sure all drawing operations are finished
			tmp_surface.Flush ();

			unsafe {
				Span<byte> data = tmp_surface.GetData ();
				int stride = tmp_surface.Stride;

				fixed (byte* basePtr = data) {
					for (int iy = dest_rect.Top; iy < dest_rect.Bottom; iy++) {
						int localY = iy - dest_rect.Top;
						byte* rowPtr = basePtr + (localY * stride);

						int dy = ((iy - strokeArgs.CurrentPosition.Y) * LUT_Resolution) / rad;
						if (dy < 0) dy = -dy;

						for (int ix = dest_rect.Left; ix < dest_rect.Right; ix++) {
							int localX = ix - dest_rect.Left;

							byte* pixelPtr = rowPtr + (localX * 4);



							byte b = pixelPtr[0];
							byte gg = pixelPtr[1];
							byte r = pixelPtr[2];
							byte a = pixelPtr[3];

							int dx = ((ix - strokeArgs.CurrentPosition.X) * LUT_Resolution) / rad;
							if (dx < 0) dx = -dx;

							int force = lut_factor[dx, dy];

							// Premultiplied stroke color
							byte strokePremulR = (byte) (stroke_r * stroke_a / 255);
							byte strokePremulG = (byte) (stroke_g * stroke_a / 255);
							byte strokePremulB = (byte) (stroke_b * stroke_a / 255);

							// Blend premultiplied directly
							byte newA = (byte) ((a * (255 - force) + stroke_a * force) / 255);
							byte newR = (byte) ((r * (255 - force) + strokePremulR * force) / 255);
							byte newG = (byte) ((gg * (255 - force) + strokePremulG * force) / 255);
							byte newB = (byte) ((b * (255 - force) + strokePremulB * force) / 255);

							pixelPtr[0] = newB;
							pixelPtr[1] = newG;
							pixelPtr[2] = newR;
							pixelPtr[3] = newA;

						}
					}
				}

				tmp_surface.MarkDirty ();
			}
			//Draw the final result on the surface
			g.Operator = Operator.Source;
			g.SetSourceSurface (tmp_surface, dest_rect.Left, dest_rect.Top);
			g.Rectangle (dest_rect.Left, dest_rect.Top, dest_rect.Width, dest_rect.Height);
			g.Fill ();
		}
		return RectangleI.Zero;
	}


}
