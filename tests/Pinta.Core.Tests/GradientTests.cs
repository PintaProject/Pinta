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
