using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class GradientTests
{
	private static ColorBgra DefaultStartColor () => ColorBgra.Black;
	private static ColorBgra DefaultEndColor () => ColorBgra.White;

	[TestCase (0, 0)]
	[TestCase (1, 0)]
	public void Factory_Rejects_Inconsistent_Bounds (double minPosition, double maxPosition)
	{
		Assert.Throws<ArgumentException> (() => ColorMapping.Gradient (DefaultStartColor (), DefaultEndColor (), minPosition, maxPosition));
	}

	[TestCase (0, 1)]
	[TestCase (-1, 0)]
	[TestCase (-1, 1)]
	[TestCase (1, 2)]
	public void Factory_Accepts_Consistent_Bounds (double minPosition, double maxPosition)
	{
		Assert.DoesNotThrow (() => ColorMapping.Gradient (DefaultStartColor (), DefaultEndColor (), minPosition, maxPosition));
	}

	[TestCaseSource (nameof (cases_stops_at_same_position))]
	public void Factory_Rejects_Stops_At_Same_Position (double minPosition, double maxPosition, IEnumerable<KeyValuePair<double, ColorBgra>> stops)
	{
		Assert.Throws<ArgumentException> (
			() => ColorMapping.Gradient (
				DefaultStartColor (),
				DefaultEndColor (),
				minPosition,
				maxPosition,
				stops
			)
		);
	}

	[TestCaseSource (nameof (cases_stops_at_different_positions))]
	public void Factory_Accepts_Stops_At_Different_Positions (double minPosition, double maxPosition, IEnumerable<KeyValuePair<double, ColorBgra>> stops)
	{
		Assert.DoesNotThrow (
			() => ColorMapping.Gradient (
				DefaultStartColor (),
				DefaultEndColor (),
				minPosition,
				maxPosition,
				stops
			)
		);
	}

	[TestCaseSource (nameof (cases_stops_out_of_bounds))]
	public void Factory_Rejects_Stops_Out_Of_Bounds (double minPosition, double maxPosition, IEnumerable<KeyValuePair<double, ColorBgra>> stops)
	{
		Assert.Throws<ArgumentException> (
			() => ColorMapping.Gradient (
				DefaultStartColor (),
				DefaultEndColor (),
				minPosition,
				maxPosition,
				stops
			)
		);
	}

	[TestCaseSource (nameof (interpolated_color_checks))]
	public void Gradient_Interpolated_Colors_Are_Correct (ColorGradient gradient, IReadOnlyDictionary<double, ColorBgra> checks)
	{
		foreach (var check in checks) {
			var interpolated = gradient.GetColor (check.Key);
			var expected = check.Value;
			Assert.AreEqual (expected, interpolated);
		}
	}

	// Not adding tolerances nor checking for mappings that could be rounded up to the next byte,
	// because currently the ColorBgra.Lerp function always rounds down, never up
	private static readonly IReadOnlyList<TestCaseData> interpolated_color_checks = CreateInterpolatedColorChecks ().ToArray ();
	private static IEnumerable<TestCaseData> CreateInterpolatedColorChecks ()
	{
		ColorGradient blackToWhite255 = ColorGradient.Gradient (
			ColorBgra.Black,
			ColorBgra.White,
			byte.MinValue,
			byte.MaxValue
		);

		yield return new (
			blackToWhite255,
			new Dictionary<double, ColorBgra> {
				[32] = ColorBgra.FromBgr (32, 32, 32),
				[128] = ColorBgra.FromBgr (128, 128, 128),
			}
		);

		ColorGradient blackToWhite1 = ColorGradient.Gradient (
			ColorBgra.Black,
			ColorBgra.White,
			0,
			1
		);

		yield return new (
			blackToWhite1,
			new Dictionary<double, ColorBgra> {
				[0.08] = ColorBgra.FromBgr (20, 20, 20),
				[0.20] = ColorBgra.FromBgr (51, 51, 51),
				[0.91] = ColorBgra.FromBgr (232, 232, 232),
			}
		);
	}

	private static readonly IReadOnlyList<TestCaseData> cases_stops_out_of_bounds = CreateCasesForStopsOutOfBounds ().ToArray ();

	private static IEnumerable<TestCaseData> CreateCasesForStopsOutOfBounds ()
	{
		// First, the obvious, either higher than max or lower than min

		yield return new (
			1,
			100,
			new Dictionary<double, ColorBgra> {
				[100.1] = ColorBgra.Green,
			}
		);

		yield return new (
			1,
			100,
			new Dictionary<double, ColorBgra> {
				[0.9] = ColorBgra.Green,
			}
		);

		// Then the ones right at the min and max

		yield return new (
			1,
			100,
			new Dictionary<double, ColorBgra> {
				[1] = ColorBgra.Green,
			}
		);

		yield return new (
			1,
			100,
			new Dictionary<double, ColorBgra> {
				[100] = ColorBgra.Green,
			}
		);
	}

	private static readonly IReadOnlyList<TestCaseData> cases_stops_at_different_positions = CreateCasesForStopsAtDifferentPositions ().ToArray ();
	private static IEnumerable<TestCaseData> CreateCasesForStopsAtDifferentPositions ()
	{
		yield return new (
			0,
			100,
			new Dictionary<double, ColorBgra> {
				[1] = ColorBgra.Green,
				[2] = ColorBgra.Yellow,
				[3] = ColorBgra.Black,
				[4] = ColorBgra.Red,
			}
		);
	}

	private static readonly IReadOnlyList<TestCaseData> cases_stops_at_same_position = CreateCasesForStopsAtSamePosition ().ToArray ();
	private static IEnumerable<TestCaseData> CreateCasesForStopsAtSamePosition ()
	{
		yield return new (
			0,
			100,
			new KeyValuePair<double, ColorBgra>[] {
				new(1, ColorBgra.Green),
				new(2, ColorBgra.Yellow),
				new(2, ColorBgra.Black),
				new(3, ColorBgra.Red),
			}
		);
	}
}
