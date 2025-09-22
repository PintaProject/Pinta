using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class GradientTests
{
	private static readonly ColorBgra default_start_color = ColorBgra.Black;
	private static readonly ColorBgra default_end_color = ColorBgra.White;

	[Test]
	[TestCaseSource (nameof (cases_stops_at_same_position))]
	public void Factory_Rejects_Stops_At_Same_Position (double minPosition, double maxPosition, IEnumerable<KeyValuePair<double, ColorBgra>> stops)
	{
		NumberRange<double> range = new (minPosition, maxPosition);
		Assert.Throws<ArgumentException> (() => ColorGradient.Create (default_start_color, default_end_color, range, stops));
	}

	[Test]
	[TestCaseSource (nameof (cases_stops_at_different_positions))]
	public void Factory_Accepts_Stops_At_Different_Positions (double minPosition, double maxPosition, IReadOnlyDictionary<double, ColorBgra> stops)
	{
		NumberRange<double> range = new (minPosition, maxPosition);
		Assert.DoesNotThrow (() => ColorGradient.Create (default_start_color, default_end_color, range, stops));
	}

	[Test]
	[TestCaseSource (nameof (cases_stops_out_of_bounds))]
	public void Factory_Rejects_Stops_Out_Of_Bounds (double minPosition, double maxPosition, IReadOnlyDictionary<double, ColorBgra> stops)
	{
		NumberRange<double> range = new (minPosition, maxPosition);
		Assert.Throws<ArgumentException> (() => ColorGradient.Create (default_start_color, default_end_color, range, stops));
	}

	[Test]
	[TestCaseSource (nameof (stops_color_checks))]
	public void Gradient_Stop_Colors_Are_Same (double minPosition, double maxPosition, IReadOnlyDictionary<double, ColorBgra> checks)
	{
		NumberRange<double> range = new (minPosition, maxPosition);
		ColorGradient<ColorBgra> gradient = ColorGradient.Create (default_start_color, default_end_color, range, checks);
		using var _ = Assert.EnterMultipleScope ();
		foreach (var check in checks) {
			var returned = gradient.GetColor (check.Key);
			Assert.That (check.Value, Is.EqualTo (returned));
		}
	}

	static readonly IReadOnlyList<TestCaseData> stops_color_checks = [.. CreateStopsColorChecks ()];
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

	[Test]
	[TestCaseSource (nameof (interpolated_color_checks))]
	public void Gradient_Interpolated_Colors_Are_Correct (ColorGradient<ColorBgra> gradient, IReadOnlyDictionary<double, ColorBgra> checks)
	{
		using var _ = Assert.EnterMultipleScope ();
		foreach (var check in checks) {
			ColorBgra interpolated = gradient.GetColor (check.Key);
			ColorBgra expected = check.Value;
			Assert.That (expected, Is.EqualTo (interpolated));
		}
	}

	[Test]
	[TestCaseSource (nameof (reversal_test_cases))]
	public void Gradient_Reversal_Is_Correct (
		double startPosition,
		double endPosition,
		IReadOnlyDictionary<double, ColorBgra> originalStops,
		IReadOnlyDictionary<double, ColorBgra> expectedReversedStops)
	{
		NumberRange<double> range = new (startPosition, endPosition);

		ColorGradient<ColorBgra> gradient = ColorGradient.Create (default_start_color, default_end_color, range, originalStops);

		ColorGradient<ColorBgra> reversedOnce = gradient.Reversed ();
		ColorGradient<ColorBgra> reversedTwice = reversedOnce.Reversed ();

		using var _ = Assert.EnterMultipleScope ();

		Assert.That (reversedOnce.Range.Lower, Is.EqualTo (startPosition), "Start position did not remain the same after reversing");
		Assert.That (reversedOnce.Range.Upper, Is.EqualTo (endPosition), "End position did not remain the same after reversing");

		Assert.That (reversedOnce.StartColor, Is.EqualTo (gradient.EndColor), "Start color after reversal is not the same as end color before reversal");
		Assert.That (reversedOnce.EndColor, Is.EqualTo (gradient.StartColor), "End color after reversal is not the same as start color before reversal");

		Assert.That (reversedOnce.StopsCount, Is.EqualTo (expectedReversedStops.Count), "Number of stops is not the same after reversing");

		foreach (var colorStop in expectedReversedStops) {
			ColorBgra actualColor = reversedOnce.GetColor (colorStop.Key);
			Assert.That (actualColor, Is.EqualTo (colorStop.Value), $"Color mismatch at reversed position {colorStop.Key}");
		}

		Assert.That (gradient.Positions, Is.EqualTo (reversedTwice.Positions));
		Assert.That (gradient.Colors, Is.EqualTo (reversedTwice.Colors));
	}

	private static readonly IReadOnlyList<TestCaseData> reversal_test_cases = [.. CreateReversalTestCases ()];
	private static IEnumerable<TestCaseData> CreateReversalTestCases ()
	{
		// Start is 0, end is positive
		yield return new TestCaseData (
			0d,
			100d,
			new Dictionary<double, ColorBgra> {
				[20] = ColorBgra.Red,
				[60] = ColorBgra.Blue,
			},
			new Dictionary<double, ColorBgra> {
				[80] = ColorBgra.Red,
				[40] = ColorBgra.Blue,
			}
		).SetArgDisplayNames ("Reversed_0_to_100");

		// Start is positive, end is positive
		yield return new TestCaseData (
			100d,
			200d,
			new Dictionary<double, ColorBgra> {
				[110] = ColorBgra.Red,
				[170] = ColorBgra.Green,
			},
			new Dictionary<double, ColorBgra> {
				[190] = ColorBgra.Red,
				[130] = ColorBgra.Green,
			}
		).SetArgDisplayNames ("Reversed_100_to_200");

		// Start is negative, end is positive
		yield return new TestCaseData (
			-50d,
			50d,
			new Dictionary<double, ColorBgra> {
				[-30] = ColorBgra.Red,
				[10] = ColorBgra.Blue,
			},
			new Dictionary<double, ColorBgra> {
				[30] = ColorBgra.Red,
				[-10] = ColorBgra.Blue,
			}
		).SetArgDisplayNames ("Reversed_Neg50_to_50");

		// Start is negative, end is negative
		yield return new TestCaseData (
			-100d,
			-50d,
			new Dictionary<double, ColorBgra> {
				[-90] = ColorBgra.Red,
				[-60] = ColorBgra.Blue,
			},
			new Dictionary<double, ColorBgra> {
				[-60] = ColorBgra.Red,
				[-90] = ColorBgra.Blue,
			}
		).SetArgDisplayNames ("Reversed_Neg100_to_Neg50");

		// Start is negative, end is 0
		yield return new TestCaseData (
			-100d,
			0d,
			new Dictionary<double, ColorBgra> {
				[-90] = ColorBgra.Red,
				[-60] = ColorBgra.Blue,
			},
			new Dictionary<double, ColorBgra> {
				[-10] = ColorBgra.Red,
				[-40] = ColorBgra.Blue,
			}
		).SetArgDisplayNames ("Reversed_Neg100_to_0");
	}

	// Not adding tolerances nor checking for mappings that could be rounded up to the next byte,
	// because currently the ColorBgra.Lerp function always rounds down, never up
	private static readonly IReadOnlyList<TestCaseData> interpolated_color_checks = [.. CreateInterpolatedColorChecks ()];
	private static IEnumerable<TestCaseData> CreateInterpolatedColorChecks ()
	{
		ColorGradient<ColorBgra> blackToWhite255 = ColorGradient.Create (
			ColorBgra.Black,
			ColorBgra.White,
			NumberRange.Create<double> (byte.MinValue, byte.MaxValue));

		yield return new (
			blackToWhite255,
			new Dictionary<double, ColorBgra> {
				[32] = ColorBgra.FromBgr (32, 32, 32),
				[128] = ColorBgra.FromBgr (128, 128, 128),
			}
		);

		ColorGradient<ColorBgra> blackToWhite1 = ColorGradient.Create (
			ColorBgra.Black,
			ColorBgra.White,
			NumberRange.Create<double> (0, 1));

		yield return new (
			blackToWhite1,
			new Dictionary<double, ColorBgra> {
				[0.08] = ColorBgra.FromBgr (20, 20, 20),
				[0.20] = ColorBgra.FromBgr (51, 51, 51),
				[0.91] = ColorBgra.FromBgr (232, 232, 232),
			}
		);
	}

	private static readonly IReadOnlyList<TestCaseData> cases_stops_out_of_bounds = [.. CreateCasesForStopsOutOfBounds ()];

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

	private static readonly IReadOnlyList<TestCaseData> cases_stops_at_different_positions = [.. CreateCasesForStopsAtDifferentPositions ()];
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

	private static readonly IReadOnlyList<TestCaseData> cases_stops_at_same_position = [.. CreateCasesForStopsAtSamePosition ()];
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
