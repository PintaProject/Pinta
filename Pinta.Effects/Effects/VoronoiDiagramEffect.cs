using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class VoronoiDiagramEffect : BaseEffect
{
	public override string Icon
		=> Pinta.Resources.Icons.EffectsRenderVoronoiDiagram;

	public override bool IsTileable
		=> false;

	public override string Name
		=> Translations.GetString ("Voronoi Diagram");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Render");

	public VoronoiDiagramData Data
		=> (VoronoiDiagramData) EffectData!; // NRT - Set in constructor

	private readonly IChromeService chrome;
	public VoronoiDiagramEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new VoronoiDiagramData ();
	}

	public override Task<Gtk.ResponseType> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	private sealed record VoronoiSettings (
		Size size,
		bool showPoints,
		ImmutableArray<PointI> points,
		ImmutableArray<ColorBgra> colors,
		Func<PointI, PointI, double> distanceCalculator);

	private VoronoiSettings CreateSettings (ImageSurface dst, RectangleI roi)
	{
		ColorSorting colorSorting = Data.ColorSorting;

		IEnumerable<PointI> basePoints = CreatePoints (roi, Data.NumberOfCells, Data.RandomPointLocations);
		ImmutableArray<PointI> points = SortPoints (basePoints, colorSorting).ToImmutableArray ();

		IEnumerable<ColorBgra> baseColors = CreateColors (points.Length, Data.RandomColors);
		IEnumerable<ColorBgra> positionSortedColors = SortColors (baseColors, colorSorting);
		IEnumerable<ColorBgra> reversedSortingColors = Data.ReverseColorSorting ? positionSortedColors.Reverse () : positionSortedColors;

		return new (
			size: dst.GetSize (),
			showPoints: Data.ShowPoints,
			points: points,
			colors: reversedSortingColors.ToImmutableArray (),
			distanceCalculator: GetDistanceCalculator (Data.DistanceMetric)
		);
	}

	protected override void Render (ImageSurface src, ImageSurface dst, RectangleI roi)
	{
		VoronoiSettings settings = CreateSettings (dst, roi);
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		foreach (var kvp in roi.GeneratePixelOffsets (settings.size).AsParallel ().Select (CreateColor))
			dst_data[kvp.Key] = kvp.Value;

		KeyValuePair<int, ColorBgra> CreateColor (PixelOffset pixel)
		{
			double shortestDistance = double.MaxValue;
			int closestIndex = 0;

			for (var i = 0; i < settings.points.Length; i++) {
				// TODO: Acceleration structure that limits the search
				//       to a relevant subset of points, for better performance.
				//       Some ideas to consider: quadtree, spatial hashing
				var point = settings.points[i];
				double distance = settings.distanceCalculator (point, pixel.coordinates);
				if (distance > shortestDistance) continue;
				shortestDistance = distance;
				closestIndex = i;
			}

			ColorBgra finalColor =
				settings.showPoints && shortestDistance == 0
				? ColorBgra.Black
				: settings.colors[closestIndex];

			return KeyValuePair.Create (pixel.memoryOffset, finalColor);
		}
	}

	private static IEnumerable<ColorBgra> SortColors (IEnumerable<ColorBgra> baseColors, ColorSorting colorSorting)

		=> colorSorting switch {

			ColorSorting.Random => baseColors,

			ColorSorting.HorizontalB or ColorSorting.VerticalB => baseColors.OrderBy (p => p.B),
			ColorSorting.HorizontalG or ColorSorting.VerticalG => baseColors.OrderBy (p => p.G),
			ColorSorting.HorizontalR or ColorSorting.VerticalR => baseColors.OrderBy (p => p.R),

			_ => throw new InvalidEnumArgumentException (
				nameof (baseColors),
				(int) colorSorting,
				typeof (ColorSorting)),
		};

	private static IEnumerable<PointI> SortPoints (IEnumerable<PointI> basePoints, ColorSorting colorSorting)

		=> colorSorting switch {

			ColorSorting.Random => basePoints,

			ColorSorting.HorizontalB
			or ColorSorting.HorizontalG
			or ColorSorting.HorizontalR => basePoints.OrderBy (p => p.X).ThenBy (p => p.Y),

			ColorSorting.VerticalB
			or ColorSorting.VerticalG
			or ColorSorting.VerticalR => basePoints.OrderBy (p => p.Y).ThenBy (p => p.X),

			_ => throw new InvalidEnumArgumentException (
				nameof (colorSorting),
				(int) colorSorting,
				typeof (ColorSorting)),
		};

	private static Func<PointI, PointI, double> GetDistanceCalculator (DistanceMetric distanceCalculationMethod)
	{
		return distanceCalculationMethod switch {
			DistanceMetric.Euclidean => Euclidean,
			DistanceMetric.Manhattan => Manhattan,
			DistanceMetric.Chebyshev => Chebyshev,
			_ => throw new InvalidEnumArgumentException (
				nameof (distanceCalculationMethod),
				(int) distanceCalculationMethod,
				typeof (DistanceMetric)),
		};

		static double Euclidean (PointI targetPoint, PointI pixelLocation)
		{
			PointI difference = pixelLocation - targetPoint;
			return difference.Magnitude ();
		}

		static double Manhattan (PointI targetPoint, PointI pixelLocation)
		{
			PointI difference = pixelLocation - targetPoint;
			return Math.Abs (difference.X) + Math.Abs (difference.Y);
		}

		static double Chebyshev (PointI targetPoint, PointI pixelLocation)
		{
			PointI difference = pixelLocation - targetPoint;
			return Math.Max (Math.Abs (difference.X), Math.Abs (difference.Y));
		}
	}

	private static ImmutableHashSet<PointI> CreatePoints (RectangleI roi, int pointCount, RandomSeed pointLocationsSeed)
	{
		int effectivePointCount = Math.Min (pointCount, roi.Width * roi.Height);

		Random randomPositioner = new (pointLocationsSeed.Value);
		var result = ImmutableHashSet.CreateBuilder<PointI> (); // Ensures points' uniqueness

		while (result.Count < effectivePointCount) {

			PointI point = new (
				X: randomPositioner.Next (roi.Left, roi.Right + 1),
				Y: randomPositioner.Next (roi.Top, roi.Bottom + 1)
			);

			result.Add (point);
		}

		return result.ToImmutable ();
	}

	private static IEnumerable<ColorBgra> CreateColors (int colorCount, RandomSeed colorsSeed)
	{
		Random randomColorizer = new (colorsSeed.Value);
		HashSet<ColorBgra> uniquenessTracker = new ();
		while (uniquenessTracker.Count < colorCount) {
			ColorBgra candidateColor = randomColorizer.RandomColorBgra ();
			if (uniquenessTracker.Contains (candidateColor)) continue;
			uniquenessTracker.Add (candidateColor);
			yield return candidateColor;
		}
	}

	public sealed class VoronoiDiagramData : EffectData
	{
		[Caption ("Distance Metric")]
		public DistanceMetric DistanceMetric { get; set; } = DistanceMetric.Euclidean;

		[Caption ("Number of Cells"), MinimumValue (1), MaximumValue (1024)]
		public int NumberOfCells { get; set; } = 100;

		// Translators: The user can choose whether or not to render the points used in the calculation of a Voronoi diagram
		[Caption ("Show Points")]
		public bool ShowPoints { get; set; } = false;

		[Caption ("Color Sorting")]
		public ColorSorting ColorSorting { get; set; } = ColorSorting.Random;

		// Translators: In this context, "reverse" is a verb, and the user can choose whether or not they want to reverse the color sorting
		[Caption ("Reverse Color Sorting")]
		public bool ReverseColorSorting { get; set; } = false;

		[Caption ("Random Colors")]
		public RandomSeed RandomColors { get; set; } = new (0);

		[Caption ("Random Point Locations")]
		public RandomSeed RandomPointLocations { get; set; } = new (0);
	}

	public enum DistanceMetric
	{
		Euclidean,
		Manhattan,
		Chebyshev,
	}

	public enum ColorSorting
	{
		[Caption ("Random")] Random,

		// Translators: Horizontal color sorting with blue (B) as the leading term
		[Caption ("Horizontal blue (B)")] HorizontalB,

		// Translators: Horizontal color sorting with green (G) as the leading term
		[Caption ("Horizontal green (G)")] HorizontalG,

		// Translators: Horizontal color sorting with red (R) as the leading term
		[Caption ("Horizontal red (R)")] HorizontalR,


		// Translators: Vertical color sorting with blue (B) as the leading term
		[Caption ("Vertical blue (B)")] VerticalB,

		// Translators: Vertical color sorting with green (G) as the leading term
		[Caption ("Vertical green (G)")] VerticalG,

		// Translators: Vertical color sorting with red (R) as the leading term
		[Caption ("Vertical red (R)")] VerticalR,
	}
}
