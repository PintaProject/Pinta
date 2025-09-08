using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

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
		Size CanvasSize,
		ImmutableArray<PointD> ControlPoints,
		ImmutableArray<PointD> SamplingOffsets,
		ImmutableArray<ColorBgra> Colors,
		Func<PointD, PointD, double> DistanceCalculatorFast,
		bool ShowPoints,
		double PointRadiusFast,
		ColorBgra PointColor);

	private VoronoiSettings CreateSettings (ImageSurface destination)
	{
		VoronoiDiagramData data = Data;

		RectangleI roi = live_preview.RenderBounds;

		ColorSorting colorSorting = data.ColorSorting;

		IEnumerable<PointD> basePoints = SpatialPartition.CreateCellControlPoints (
			roi,
			Math.Min (data.NumberOfCells, roi.Width * roi.Height),
			data.RandomPointLocations,
			data.PointArrangement);

		ImmutableArray<PointD> controlPoints = [.. SortPoints (basePoints, colorSorting)];

		IEnumerable<ColorBgra> baseColors = CreateColors (controlPoints.Length, data.RandomColors);
		IEnumerable<ColorBgra> positionSortedColors = SortColors (baseColors, colorSorting);
		IEnumerable<ColorBgra> reversedSortingColors = data.ReverseColorSorting ? positionSortedColors.Reverse () : positionSortedColors;

		return new (
			CanvasSize: destination.GetSize (),
			ControlPoints: controlPoints,
			SamplingOffsets: Sampling.CreateSamplingOffsets (data.Quality),
			Colors: [.. reversedSortingColors],
			DistanceCalculatorFast: SpatialPartition.GetFastDistanceCalculator (data.DistanceMetric),
			ShowPoints: data.ShowPoints,
			PointRadiusFast: SpatialPartition.AdjustDistanceThresholdFast (data.PointSize / 2.0, data.DistanceMetric),
			PointColor: data.PointColor.ToColorBgra ());
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		VoronoiSettings settings = CreateSettings (destination);
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var kvp in roi.GeneratePixelOffsets (settings.CanvasSize).Select (CreateColor))
			destinationData[kvp.Key] = kvp.Value;

		KeyValuePair<int, ColorBgra> CreateColor (PixelOffset pixel)
		{
			int sampleCount = settings.SamplingOffsets.Length;
			ColorBgra.Blender aggregate = new ();
			for (int i = 0; i < sampleCount; i++) {
				PointD sampleLocation = pixel.coordinates.ToDouble () + settings.SamplingOffsets[i];
				ColorBgra sample = GetColorForLocation (sampleLocation);
				aggregate += sample;
			}
			return KeyValuePair.Create (
				pixel.memoryOffset,
				aggregate.Blend ());
		}

		ColorBgra GetColorForLocation (PointD location)
		{
			// A note about the naming ("relative"):
			// We don't need to know the actual distance,
			// we just need to know which distances are larger
			// and which are smaller.
			double shortestRelativeDistance = double.MaxValue;
			int closestIndex = 0;
			for (var i = 0; i < settings.ControlPoints.Length; i++) {
				// TODO: Acceleration structure that limits the search
				//       to a relevant subset of points, for better performance.
				//       Some ideas to consider: quadtree, spatial hashing
				PointD controlPoint = settings.ControlPoints[i];
				double relativeDistance = settings.DistanceCalculatorFast (location, controlPoint);
				if (relativeDistance > shortestRelativeDistance) continue;
				shortestRelativeDistance = relativeDistance;
				closestIndex = i;
			}
			ColorBgra cellColor = settings.Colors[closestIndex];
			if (settings.ShowPoints && shortestRelativeDistance <= settings.PointRadiusFast)
				return UserBlendOps.NormalBlendOp.ApplyStatic (cellColor, settings.PointColor);
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

	private static IEnumerable<PointD> SortPoints (IEnumerable<PointD> basePoints, ColorSorting colorSorting)

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

		[Caption ("Number of Cells")]
		[MinimumValue (1), MaximumValue (1024)]
		public int NumberOfCells { get; set; } = 100;

		[Caption ("Color Sorting")]
		public ColorSorting ColorSorting { get; set; } = ColorSorting.Random;

		// Translators: In this context, "reverse" is a verb, and the user can choose whether or not they want to reverse the color sorting
		[Caption ("Reverse Color Sorting")]
		public bool ReverseColorSorting { get; set; } = false;

		[Caption ("Random Colors")]
		public RandomSeed RandomColors { get; set; } = new (0);

		[Caption ("Point Arrangement")]
		public PointArrangement PointArrangement { get; set; } = PointArrangement.Random;

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
