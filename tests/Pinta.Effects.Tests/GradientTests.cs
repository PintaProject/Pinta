using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Pinta.Core;

namespace Pinta.Effects.Tests;

[TestFixture]
internal sealed class GradientTests
{
	private static readonly ColorBgra default_start_color = ColorBgra.Black;
	private static readonly ColorBgra default_end_color = ColorBgra.White;

	[TestCase (0, 0)]
	[TestCase (1, 0)]
	public void Factory_Rejects_Inconsistent_Bounds (double minPosition, double maxPosition)
	{
		Assert.Throws<ArgumentException> (() => ColorGradient.Create (default_start_color, default_end_color, minPosition, maxPosition));
	}

	[TestCase (0, 1)]
	[TestCase (-1, 0)]
	[TestCase (-1, 1)]
	[TestCase (1, 2)]
	public void Factory_Accepts_Consistent_Bounds (double minPosition, double maxPosition)
	{
		Assert.DoesNotThrow (() => ColorGradient.Create (default_start_color, default_end_color, minPosition, maxPosition));
	}

	[TestCaseSource (nameof (cases_stops_at_same_position))]
	public void Factory_Rejects_Stops_At_Same_Position (double minPosition, double maxPosition, IEnumerable<KeyValuePair<double, ColorBgra>> stops)
	{
		Assert.Throws<ArgumentException> (
			() => ColorGradient.Create (
				default_start_color,
				default_end_color,
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
			() => ColorGradient.Create (
				default_start_color,
				default_end_color,
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
			() => ColorGradient.Create (
				default_start_color,
				default_end_color,
				minPosition,
				maxPosition,
				stops
			)
		);
	}



	[TestCaseSource (nameof (stops_color_checks))]
	public void Gradient_Stop_Colors_Are_Same (double minPosition, double maxPosition, IReadOnlyDictionary<double, ColorBgra> checks)
	{
		var gradient = ColorGradient.Create (default_start_color, default_end_color, minPosition, maxPosition, checks);
		foreach (var check in checks) {
			var returned = gradient.GetColor (check.Key);
			Assert.That (check.Value, Is.EqualTo (returned));
		}
	}

	static readonly IReadOnlyList<TestCaseData> stops_color_checks = CreateStopsColorChecks ().ToArray ();
	private static IEnumerable<TestCaseData> CreateStopsColorChecks ()
	{
		yield return new (
			0,
			100,
			new Dictionary<double, ColorBgra> {
				[3] = ColorBgra.Red,
				[50] = ColorBgra.Cyan,
			}
		);

		yield return new (
			0,
			100,
			new Dictionary<double, ColorBgra> {
				[50] = ColorBgra.Cyan,
				[3] = ColorBgra.Red,
			}
		);
	}

	[TestCaseSource (nameof (interpolated_color_checks))]
	public void Gradient_Interpolated_Colors_Are_Correct (ColorGradient<ColorBgra> gradient, IReadOnlyDictionary<double, ColorBgra> checks)
	{
		foreach (var check in checks) {
			var interpolated = gradient.GetColor (check.Key);
			var expected = check.Value;
			Assert.That (expected, Is.EqualTo (interpolated));
		}
	}

	// Not adding tolerances nor checking for mappings that could be rounded up to the next byte,
	// because currently the ColorBgra.Lerp function always rounds down, never up
	private static readonly IReadOnlyList<TestCaseData> interpolated_color_checks = CreateInterpolatedColorChecks ().ToArray ();
	private static IEnumerable<TestCaseData> CreateInterpolatedColorChecks ()
	{
		var blackToWhite255 = ColorGradient.Create (
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

		var blackToWhite1 = ColorGradient.Create (
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
