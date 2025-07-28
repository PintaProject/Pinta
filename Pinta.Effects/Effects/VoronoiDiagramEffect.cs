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
	private readonly IWorkspaceService workspace;
	public VoronoiDiagramEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		live_preview = services.GetService<ILivePreview> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new VoronoiDiagramData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private sealed record VoronoiSettings (
		Size size,
		ImmutableArray<PointD> controlPoints,
		ImmutableArray<PointD> samplingLocations,
		ImmutableArray<ColorBgra> colors,
		Func<PointD, PointD, double> distanceCalculator,
		bool showPoints,
		double pointSize,
		ColorBgra pointColor); // Blend method assumes straight alpha!

	private VoronoiSettings CreateSettings (ImageSurface dst)
	{
		VoronoiDiagramData data = Data;

		RectangleI roi = live_preview.RenderBounds;

		ColorSorting colorSorting = data.ColorSorting;

		PointD locationOffset = new (0.5, 0.5);

		IEnumerable<PointI> basePoints = SpatialPartition.CreateCellControlPoints (
			roi,
			Math.Min (data.NumberOfCells, roi.Width * roi.Height),
			data.RandomPointLocations);
		IEnumerable<PointI> pointCorners = [.. SortPoints (basePoints, colorSorting)];
		ImmutableArray<PointD> controlPoints = [.. pointCorners.Select (p => p.ToDouble () + locationOffset)];

		IEnumerable<ColorBgra> baseColors = CreateColors (controlPoints.Length, data.RandomColors);
		IEnumerable<ColorBgra> positionSortedColors = SortColors (baseColors, colorSorting);
		IEnumerable<ColorBgra> reversedSortingColors = data.ReverseColorSorting ? positionSortedColors.Reverse () : positionSortedColors;

		return new (
			size: dst.GetSize (),
			controlPoints: controlPoints,
			samplingLocations: Sampling.CreateSamplingOffsets (data.Quality),
			colors: [.. reversedSortingColors],
			distanceCalculator: SpatialPartition.GetDistanceCalculator (data.DistanceMetric),
			showPoints: data.ShowPoints,
			pointSize: data.PointSize,
			pointColor: data.PointColor.ToColorBgra ()
		);
	}

	protected override void Render (ImageSurface src, ImageSurface dst, RectangleI roi)
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
			ColorBgra cellColor = settings.colors[closestIndex];
			if (settings.showPoints && shortestDistance * 2 <= settings.pointSize)
				return ColorBgra.Blend (cellColor, settings.pointColor, settings.pointColor.A).NewAlpha (255);
			else
				return cellColor;
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

	private static IEnumerable<ColorBgra> CreateColors (int colorCount, RandomSeed colorsSeed)
	{
		Random randomColorizer = new (colorsSeed.Value);
		HashSet<ColorBgra> uniquenessTracker = [];
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

		[Caption ("Color Sorting")]
		public ColorSorting ColorSorting { get; set; } = ColorSorting.Random;

		// Translators: In this context, "reverse" is a verb, and the user can choose whether or not they want to reverse the color sorting
		[Caption ("Reverse Color Sorting")]
		public bool ReverseColorSorting { get; set; } = false;

		[Caption ("Random Colors")]
		public RandomSeed RandomColors { get; set; } = new (0);

		[Caption ("Random Point Locations")]
		public RandomSeed RandomPointLocations { get; set; } = new (0);

		[Caption ("Show Points")]
		public bool ShowPoints { get; set; } = false;

		[Caption ("Point Size")]
		[MinimumValue (1), MaximumValue (16), IncrementValue (1)]
		public double PointSize { get; set; } = 4;

		[Caption ("Point Color")]
		public Color PointColor { get; set; } = Color.Black;

		[Caption ("Quality")]
		[MinimumValue (1), MaximumValue (4)]
		public int Quality { get; set; } = 3;
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
