using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class CellsEffect : BaseEffect
{
	public override string Icon
		=> Resources.Icons.EffectsRenderCells;

	public override bool IsTileable
		=> true;

	public override string Name
		=> Translations.GetString ("Cells");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Render");

	public CellsData Data
		=> (CellsData) EffectData!; // NRT - Set in constructor

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	private readonly IChromeService chrome;
	private readonly ILivePreview live_preview;
	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;
	public CellsEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		live_preview = services.GetService<ILivePreview> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new CellsData ();
	}

	private sealed record CellsSettings (
		Size CanvasSize,
		ImmutableArray<PointD> ControlPoints,
		ImmutableArray<PointD> SamplingLocations,
		Func<PointD, PointD, double> DistanceCalculator,
		Func<PointD, PointD, double> DistanceCalculatorFast,
		ColorGradient<ColorBgra> ColorGradient,
		EdgeBehavior GradientEdgeBehavior,
		bool ShowPoints,
		double PointRadius,
		ColorBgra PointColor);

	private CellsSettings CreateSettings (ImageSurface destination)
	{
		CellsData data = Data;

		RectangleI roi = live_preview.RenderBounds;

		var baseGradient =
			GradientHelper
			.CreateBaseGradientForEffect (
				palette,
				data.ColorSchemeSource,
				data.ColorScheme,
				data.ColorSchemeSeed)
			.Resized (NumberRange.Create (0, data.CellRadius));

		IEnumerable<PointD> basePoints = SpatialPartition.CreateCellControlPoints (
			roi,
			Math.Min (data.NumberOfCells, roi.Width * roi.Height),
			data.RandomPointLocations,
			data.PointArrangement);

		ImmutableArray<PointD> controlPoints = [.. basePoints];

		return new (
			CanvasSize: destination.GetSize (),
			ControlPoints: controlPoints,
			SamplingLocations: Sampling.CreateSamplingOffsets (data.Quality),
			DistanceCalculator: SpatialPartition.GetDistanceCalculator (data.DistanceMetric),
			DistanceCalculatorFast: SpatialPartition.GetFastDistanceCalculator (data.DistanceMetric),
			ColorGradient: data.ReverseColorScheme ? baseGradient.Reversed () : baseGradient,
			GradientEdgeBehavior: data.ColorSchemeEdgeBehavior,
			ShowPoints: data.ShowPoints,
			PointRadius: data.PointSize / 2.0,
			PointColor: data.PointColor.ToColorBgra ());
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		CellsSettings settings = CreateSettings (destination);
		ReadOnlySpan<ColorBgra> sourceData = source.GetReadOnlyPixelData ();
		Span<ColorBgra> destinationData = destination.GetPixelData ();
		foreach (var pixel in roi.GeneratePixelOffsets (settings.CanvasSize)) {
			ColorBgra original = sourceData[pixel.memoryOffset];
			destinationData[pixel.memoryOffset] = CreateColor (pixel, original);
		}

		ColorBgra CreateColor (PixelOffset pixel, ColorBgra original)
		{
			int sampleCount = settings.SamplingLocations.Length;
			ColorBgra.Blender aggregate = new ();
			for (int i = 0; i < sampleCount; i++) {
				PointD sampleLocation = pixel.coordinates.ToDouble () + settings.SamplingLocations[i];
				ColorBgra sample = GetColorForLocation (sampleLocation, original);
				aggregate += sample;
			}
			return aggregate.Blend ();
		}

		ColorBgra GetColorForLocation (PointD location, ColorBgra original)
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
			PointD closestControlPoint = settings.ControlPoints[closestIndex];
			double shortestDistance = settings.DistanceCalculator (location, closestControlPoint);
			ColorBgra locationColor =
				settings
				.ColorGradient
				.GetColorExtended (
					shortestDistance,
					settings.GradientEdgeBehavior,
					original,
					palette);
			if (settings.ShowPoints && shortestDistance <= settings.PointRadius)
				return UserBlendOps.NormalBlendOp.ApplyStatic (locationColor, settings.PointColor);
			else
				return locationColor;
		}
	}

	public sealed class CellsData : EffectData
	{
		[Caption ("Distance Metric")]
		public DistanceMetric DistanceMetric { get; set; } = DistanceMetric.Euclidean;

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

		[Caption ("Number of Cells")]
		[MinimumValue (1), MaximumValue (1024)]
		public int NumberOfCells { get; set; } = 100;

		[Caption ("Cell Radius")]
		[MinimumValue (4), MaximumValue (100), IncrementValue (1)]
		public double CellRadius { get; set; } = 32;

		[Caption ("Color Scheme Source")]
		public ColorSchemeSource ColorSchemeSource { get; set; } = ColorSchemeSource.PresetGradient;

		[Caption ("Color Scheme")]
		public PresetGradients ColorScheme { get; set; } = PresetGradients.BlackAndWhite;

		[Caption ("Random Color Scheme Seed")]
		public RandomSeed ColorSchemeSeed { get; set; } = new (0);

		[Caption ("Reverse Color Scheme")]
		public bool ReverseColorScheme { get; set; } = false;

		[Caption ("Color Scheme Edge Behavior")]
		public EdgeBehavior ColorSchemeEdgeBehavior { get; set; } = EdgeBehavior.Clamp;

		[Caption ("Quality")]
		[MinimumValue (1), MaximumValue (4)]
		public int Quality { get; set; } = 3;
	}
}
