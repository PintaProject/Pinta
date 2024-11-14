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
		=> Resources.Icons.EffectsRenderVoronoiDiagram;

	public override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Voronoi Diagram");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Render");

	public VoronoiDiagramData Data
		=> (VoronoiDiagramData) EffectData!; // NRT - Set in constructor

	private readonly IChromeService chrome;
	private readonly ILivePreview live_preview;
	public VoronoiDiagramEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		live_preview = services.GetService<ILivePreview> ();
		EffectData = new VoronoiDiagramData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this);

	private sealed record VoronoiSettings (
		Size size,
		ImmutableArray<PointD> controlPoints,
		ImmutableArray<PointD> samplingLocations,
		ImmutableArray<ColorBgra> colors,
		Func<PointD, PointD, double> distanceCalculator);

	private VoronoiSettings CreateSettings (ImageSurface dst)
	{
		VoronoiDiagramData data = Data;

		RectangleI roi = live_preview.RenderBounds;

		ColorSorting colorSorting = data.ColorSorting;

		PointD locationOffset = new (0.5, 0.5);

		IEnumerable<PointI> basePoints = CreatePoints (roi, data.NumberOfCells, data.RandomPointLocations);
		IEnumerable<PointI> pointCorners = SortPoints (basePoints, colorSorting).ToImmutableArray ();
		ImmutableArray<PointD> controlPoints = pointCorners.Select (p => p.ToDouble () + locationOffset).ToImmutableArray ();

		IEnumerable<ColorBgra> baseColors = CreateColors (controlPoints.Length, data.RandomColors);
		IEnumerable<ColorBgra> positionSortedColors = SortColors (baseColors, colorSorting);
		IEnumerable<ColorBgra> reversedSortingColors = data.ReverseColorSorting ? positionSortedColors.Reverse () : positionSortedColors;

		return new (
			size: dst.GetSize (),
			controlPoints: controlPoints,
			samplingLocations: CreateSamplingLocations (data.Quality),
			colors: reversedSortingColors.ToImmutableArray (),
			distanceCalculator: GetDistanceCalculator (data.DistanceMetric)
		);
	}

	protected override void Render (
		ImageSurface src,
		ImageSurface dst,
		RectangleI roi)
	{
		VoronoiSettings settings = CreateSettings (dst);
		Span<ColorBgra> dst_data = dst.GetPixelData ();
		foreach (var kvp in roi.GeneratePixelOffsets (settings.size).Select (CreateColor))
			dst_data[kvp.Key] = kvp.Value;

		KeyValuePair<int, ColorBgra> CreateColor (PixelOffset pixel)
		{
			int sampleCount = settings.samplingLocations.Length;
			Span<ColorBgra> samples = stackalloc ColorBgra[sampleCount];
			for (int i = 0; i < sampleCount; i++) {
				PointD sampleLocation = pixel.coordinates.ToDouble () + settings.samplingLocations[i];
				ColorBgra sample = GetColorForLocation (sampleLocation);
				samples[i] = sample;
			}
			return KeyValuePair.Create (
				pixel.memoryOffset,
				ColorBgra.Blend (samples));
		}

		ColorBgra GetColorForLocation (PointD location)
		{
			double shortestDistance = double.MaxValue;
			int closestIndex = 0;
			for (var i = 0; i < settings.controlPoints.Length; i++) {
				// TODO: Acceleration structure that limits the search
				//       to a relevant subset of points, for better performance.
				//       Some ideas to consider: quadtree, spatial hashing
				PointD controlPoint = settings.controlPoints[i];
				double distance = settings.distanceCalculator (location, controlPoint);
				if (distance > shortestDistance) continue;
				shortestDistance = distance;
				closestIndex = i;
			}
			return settings.colors[closestIndex];
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

	private static Func<PointD, PointD, double> GetDistanceCalculator (DistanceMetric distanceCalculationMethod)
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

		static double Euclidean (PointD targetPoint, PointD pixelLocation)
		{
			PointD difference = pixelLocation - targetPoint;
			return difference.Magnitude ();
		}

		static double Manhattan (PointD targetPoint, PointD pixelLocation)
		{
			PointD difference = pixelLocation - targetPoint;
			return Math.Abs (difference.X) + Math.Abs (difference.Y);
		}

		static double Chebyshev (PointD targetPoint, PointD pixelLocation)
		{
			PointD difference = pixelLocation - targetPoint;
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

	/// <returns>
	/// Offsets, from top left corner of points,
	/// where samples should be taken.
	/// </returns>
	/// <remarks>
	/// The resulting colors are intended to be blended.
	/// </remarks>
	private static ImmutableArray<PointD> CreateSamplingLocations (int quality)
	{
		var builder = ImmutableArray.CreateBuilder<PointD> ();
		builder.Capacity = quality * quality;
		double sectionSize = 1.0 / quality;
		double initial = sectionSize / 2;

		for (int h = 0; h < quality; h++) {
			for (int v = 0; v < quality; v++) {
				double hOffset = sectionSize * h;
				double vOffset = sectionSize * v;
				PointD currentPoint = new (
					X: initial + hOffset,
					Y: initial + vOffset);
				builder.Add (currentPoint);
			}
		}

		return builder.MoveToImmutable ();
	}

	public sealed class VoronoiDiagramData : EffectData
	{
		[Caption ("Distance Metric")]
		public DistanceMetric DistanceMetric { get; set; } = DistanceMetric.Euclidean;

		[Caption ("Number of Cells"), MinimumValue (1), MaximumValue (1024)]
		public int NumberOfCells { get; set; } = 100;

		// TODO: Show points

		[Caption ("Color Sorting")]
		public ColorSorting ColorSorting { get; set; } = ColorSorting.Random;

		// Translators: In this context, "reverse" is a verb, and the user can choose whether or not they want to reverse the color sorting
		[Caption ("Reverse Color Sorting")]
		public bool ReverseColorSorting { get; set; } = false;

		[Caption ("Random Colors")]
		public RandomSeed RandomColors { get; set; } = new (0);

		[Caption ("Random Point Locations")]
		public RandomSeed RandomPointLocations { get; set; } = new (0);

		[Caption ("Quality")]
		[MinimumValue (1), MaximumValue (4)]
		public int Quality { get; set; } = 3;
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
