using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class CellsEffect : BaseEffect
{
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
		Size size,
		ImmutableArray<PointD> controlPoints,
		ImmutableArray<PointD> samplingLocations,
		Func<PointD, PointD, double> distanceCalculator,
		ColorGradient<ColorBgra> colorGradient,
		EdgeBehavior gradientEdgeBehavior);

	private CellsSettings CreateSettings (ImageSurface destination)
	{
		CellsData data = Data;

		RectangleI roi = live_preview.RenderBounds;
		PointD locationOffset = new (0.5, 0.5);

		var baseGradient =
			GradientHelper
			.CreateBaseGradientForEffect (
				palette,
				data.ColorSchemeSource,
				data.ColorScheme,
				data.ColorSchemeSeed)
			.Resized (0, data.CellRadius);

		IEnumerable<PointI> basePoints = SpatialPartition.CreateControlPoints (
			roi,
			Math.Min (data.NumberOfCells, roi.Width * roi.Height),
			data.RandomPointLocations);
		ImmutableArray<PointD> controlPoints = [.. basePoints.Select (p => p.ToDouble () + locationOffset)];

		return new (
			size: destination.GetSize (),
			controlPoints: controlPoints,
			samplingLocations: Sampling.CreateSamplingLocations (data.Quality),
			distanceCalculator: SpatialPartition.GetDistanceCalculator (data.DistanceMetric),
			colorGradient: data.ReverseColorScheme ? baseGradient.Reversed () : baseGradient,
			gradientEdgeBehavior: data.ColorSchemeEdgeBehavior);
	}

	protected override void Render (ImageSurface source, ImageSurface destination, RectangleI roi)
	{
		CellsSettings settings = CreateSettings (destination);
		Span<ColorBgra> dst_data = destination.GetPixelData ();
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
			return
				settings
				.colorGradient
				.GetColorExtended (
					shortestDistance,
					settings.gradientEdgeBehavior,
					palette);
		}
	}

	public sealed class CellsData : EffectData
	{

		[Caption ("Distance Metric")]
		public DistanceMetric DistanceMetric { get; set; } = DistanceMetric.Euclidean;

		[Caption ("Number of Cells"), MinimumValue (1), MaximumValue (1024)]
		public int NumberOfCells { get; set; } = 100;

		[Caption ("Cell Radius")]
		[MinimumValue (4), MaximumValue (100)]
		public double CellRadius { get; set; } = 32;

		// TODO: Show points

		[Caption ("Color Scheme Source")]
		public ColorSchemeSource ColorSchemeSource { get; set; } = ColorSchemeSource.PresetGradient;

		[Caption ("Color Scheme")]
		public PresetGradients ColorScheme { get; set; } = PresetGradients.BlackAndWhite;

		[Caption ("Random Color Scheme Seed")]
		public RandomSeed ColorSchemeSeed { get; set; } = new (0);

		[Caption ("Reverse Color Scheme")]
		public bool ReverseColorScheme { get; set; } = true;

		[Caption ("Color Scheme Edge Behavior")]
		public EdgeBehavior ColorSchemeEdgeBehavior { get; set; } = EdgeBehavior.Clamp;

		[Caption ("Random Point Locations")]
		public RandomSeed RandomPointLocations { get; set; } = new (0);

		[Caption ("Quality")]
		[MinimumValue (1), MaximumValue (4)]
		public int Quality { get; set; } = 3;
	}
}
