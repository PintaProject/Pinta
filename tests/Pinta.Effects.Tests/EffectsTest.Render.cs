using NUnit.Framework;
using Pinta.Core;
using Pinta.Effects.Tests;

namespace Pinta.Effects;

partial class EffectsTest
{
	[Test]
	public void Cells1 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.ReverseColorScheme = false;
		Utilities.TestEffect (effect, "cells1.png");
	}

	[Test]
	public void Cells2 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.ColorScheme = PresetGradients.Electric;
		Utilities.TestEffect (effect, "cells2.png");
	}

	[Test]
	public void Cells3 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.DistanceMetric = DistanceMetric.Manhattan;
		Utilities.TestEffect (effect, "cells3.png");
	}

	[Test]
	public void Cells4 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.ReverseColorScheme = true;
		Utilities.TestEffect (effect, "cells4.png");
	}

	[Test]
	public void Cells5 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.CellRadius = 16;
		Utilities.TestEffect (effect, "cells5.png");
	}

	[Test]
	public void Cells6 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.NumberOfCells = 50;
		Utilities.TestEffect (effect, "cells6.png");
	}

	[Test]
	public void Cells7 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.CellRadius = 8;
		effect.Data.ColorSchemeEdgeBehavior = EdgeBehavior.Wrap;
		Utilities.TestEffect (effect, "cells7.png");
	}

	[Test]
	public void Cells8 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.CellRadius = 8;
		effect.Data.ColorSchemeEdgeBehavior = EdgeBehavior.Transparent;
		Utilities.TestEffect (effect, "cells8.png");
	}

	[Test]
	public void Cells9 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.CellRadius = 8;
		effect.Data.ColorSchemeEdgeBehavior = EdgeBehavior.Primary;
		Utilities.TestEffect (effect, "cells9.png");
	}

	[Test]
	public void Cells10 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.CellRadius = 8;
		effect.Data.ColorSchemeEdgeBehavior = EdgeBehavior.Original;
		Utilities.TestEffect (effect, "cells10.png");
	}

	[Test]
	public void Cells11 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.ShowPoints = true;
		Utilities.TestEffect (effect, "cells11.png");
	}

	[Test]
	public void Cells12 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.PointArrangement = PointArrangement.Phyllotaxis;
		Utilities.TestEffect (effect, "cells12.png");
	}

	[Test]
	public void Cells13 ()
	{
		CellsEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.PointArrangement = PointArrangement.Circular;
		Utilities.TestEffect (effect, "cells13.png");
	}

	[Test]
	public void Clouds1 ()
	{
		CloudsEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "clouds1.png");
	}

	[Test]
	public void JuliaFractal1 ()
	{
		JuliaFractalEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "juliafractal1.png");
	}

	[Test]
	public void JuliaFractal2 ()
	{
		JuliaFractalEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Factor = 6;
		effect.Data.Quality = 4;
		effect.Data.Zoom = 25;
		effect.Data.Angle = new (90);
		Utilities.TestEffect (effect, "juliafractal2.png");
	}

	[Test]
	public void MandelbrotFractal1 ()
	{
		MandelbrotFractalEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "mandelbrotfractal1.png");
	}

	[Test]
	public void MandelbrotFractal2 ()
	{
		MandelbrotFractalEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.Factor = 6;
		effect.Data.Quality = 4;
		effect.Data.Zoom = 25;
		effect.Data.Angle = new (90);
		effect.Data.InvertColors = true;
		Utilities.TestEffect (effect, "mandelbrotfractal2.png");
	}

	[Test]
	public void Voronoi1 ()
	{
		VoronoiDiagramEffect effect = new (Utilities.CreateMockServices ());
		Utilities.TestEffect (effect, "voronoi1.png");
	}

	[Test]
	public void Voronoi2 ()
	{
		VoronoiDiagramEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.NumberOfCells = 200;
		Utilities.TestEffect (effect, "voronoi2.png");
	}

	[Test]
	public void Voronoi3 ()
	{
		VoronoiDiagramEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.DistanceMetric = DistanceMetric.Manhattan;
		Utilities.TestEffect (effect, "voronoi3.png");
	}

	[Test]
	public void Voronoi4 ()
	{
		VoronoiDiagramEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.ColorSorting = VoronoiDiagramEffect.ColorSorting.HorizontalB;
		Utilities.TestEffect (effect, "voronoi4.png");
	}

	[Test]
	public void Voronoi5 ()
	{
		VoronoiDiagramEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.ColorSorting = VoronoiDiagramEffect.ColorSorting.VerticalB;
		Utilities.TestEffect (effect, "voronoi5.png");
	}

	[Test]
	public void Voronoi6 ()
	{
		VoronoiDiagramEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.ShowPoints = true;
		Utilities.TestEffect (effect, "voronoi6.png");
	}

	[Test]
	public void Voronoi7 ()
	{
		VoronoiDiagramEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.PointArrangement = PointArrangement.Phyllotaxis;
		Utilities.TestEffect (effect, "voronoi7.png");
	}

	[Test]
	[Ignore ("Produces different results on some platforms for unknown reasons")]
	public void Voronoi8 ()
	{
		VoronoiDiagramEffect effect = new (Utilities.CreateMockServices ());
		effect.Data.PointArrangement = PointArrangement.Circular;
		Utilities.TestEffect (effect, "voronoi8.png");
	}
}
