using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class RectangleTests
{
	[TestCase (0, 0, 1, 1)]
	[TestCase (1, 2, 3, 4)]
	public void ConsistentConstructor (int x, int y, int width, int height)
	{
		RectangleI constructed = new (x, y, width, height);
		Assert.That (constructed.X, Is.EqualTo (x));
		Assert.That (constructed.Y, Is.EqualTo (y));
		Assert.That (constructed.Width, Is.EqualTo (width));
		Assert.That (constructed.Height, Is.EqualTo (height));
	}

	[TestCase (0, 0, 0, 0)]
	[TestCase (0, 0, 1, 1)]
	[TestCase (1, 1, 1, 1)]
	[TestCase (1, 1, 2, 2)]
	public void ConsistentLTRBFactory (int l, int t, int r, int b)
	{
		RectangleI built = RectangleI.FromLTRB (l, t, r, b);
		Assert.That (built.Left, Is.EqualTo (l));
		Assert.That (built.Top, Is.EqualTo (t));
		Assert.That (built.Right, Is.EqualTo (r));
		Assert.That (built.Bottom, Is.EqualTo (b));

		Assert.That (RectangleD.FromLTRB (l, t, r, b), Is.EqualTo (built.ToDouble ()));
	}

	[TestCaseSource (nameof (from_points_cases))]
	public void CorrectFromPoints (PointI a, PointI b, RectangleI expected, RectangleI expected_no_invert)
	{
		Assert.That (RectangleI.FromPoints (a, b), Is.EqualTo (expected));
		Assert.That (
			RectangleD.FromPoints (a.ToDouble (), b.ToDouble (), invertIfNegative: true),
			Is.EqualTo (expected.ToDouble ()));
		Assert.That (
			RectangleD.FromPoints (a.ToDouble (), b.ToDouble (), invertIfNegative: false),
			Is.EqualTo (expected_no_invert.ToDouble ()));
	}

	[TestCaseSource (nameof (not_equal_cases))]
	public void CorrectNotEqual (RectangleI a, RectangleI b)
		=> Assert.That (a, Is.Not.EqualTo (b));

	[TestCaseSource (nameof (union_cases))]
	public void CorrectUnion (RectangleI a, RectangleI b, RectangleI expected)
	{
		Assert.That (a.Union (b), Is.EqualTo (expected));
		Assert.That (a.ToDouble ().Union (b.ToDouble ()), Is.EqualTo (expected.ToDouble ()));
	}

	[TestCaseSource (nameof (intersect_cases))]
	public void CorrectIntersection (RectangleI a, RectangleI b, RectangleI expected)
		=> Assert.That (a.Intersect (b), Is.EqualTo (expected));

	[TestCaseSource (nameof (inflation_cases))]
	public void CorrectInflation (RectangleI a, int widthInflation, int heightInflation, RectangleI expected)
		=> Assert.That (a.Inflated (widthInflation, heightInflation), Is.EqualTo (expected));

	[TestCaseSource (nameof (vertical_slicing_cases))]
	public void CorrectVerticalSlicing (RectangleI original, IReadOnlyList<RectangleI> expectedSlices)
	{
		var actualSlices = original.ToRows ().ToArray ();
		Assert.That (actualSlices.Length, Is.EqualTo (expectedSlices.Count));
		for (int i = 0; i < expectedSlices.Count; i++)
			Assert.That (actualSlices[i], Is.EqualTo (expectedSlices[i]));
	}

	private static readonly IReadOnlyList<TestCaseData> vertical_slicing_cases = CreateVerticalSlicingCases ().ToArray ();
	private static IEnumerable<TestCaseData> CreateVerticalSlicingCases ()
	{
		yield return new (
			new RectangleI (1, 1, 5, 5),
			new[] {
				new RectangleI(1,1,5,1),
				new RectangleI(1,2,5,1),
				new RectangleI(1,3,5,1),
				new RectangleI(1,4,5,1),
				new RectangleI(1,5,5,1),
			});
	}

	private static readonly IReadOnlyList<TestCaseData> inflation_cases = CreateInflationCases ().ToArray ();
	private static IEnumerable<TestCaseData> CreateInflationCases ()
	{
		yield return new (
			new RectangleI (1, 1, 1, 1),
			1,
			1,
			new RectangleI (0, 0, 3, 3));

		yield return new (
			new RectangleI (2, 1, 2, 1),
			2,
			1,
			new RectangleI (0, 0, 6, 3));
	}

	private static readonly IReadOnlyList<TestCaseData> union_cases = CreateUnionCases ().ToArray ();
	private static IEnumerable<TestCaseData> CreateUnionCases ()
	{
		yield return new (
			RectangleI.FromLTRB (0, 0, 2, 2),
			RectangleI.FromLTRB (1, 1, 3, 3),
			RectangleI.FromLTRB (0, 0, 3, 3));

		yield return new (
			RectangleI.FromLTRB (0, 0, 1, 1),
			RectangleI.FromLTRB (2, 2, 3, 3),
			RectangleI.FromLTRB (0, 0, 3, 3));
	}

	private static readonly IReadOnlyList<TestCaseData> intersect_cases = CreateIntersectionCases ().ToArray ();
	private static IEnumerable<TestCaseData> CreateIntersectionCases ()
	{
		yield return new (
			RectangleI.FromLTRB (0, 0, 2, 2),
			RectangleI.FromLTRB (1, 1, 3, 3),
			RectangleI.FromLTRB (1, 1, 2, 2));

		yield return new (
			RectangleI.FromLTRB (0, 0, 1, 1),
			RectangleI.FromLTRB (2, 2, 3, 3),
			RectangleI.Zero);
	}

	private static readonly IReadOnlyList<TestCaseData> not_equal_cases = CreateNotEqualCases ().ToArray ();
	private static IEnumerable<TestCaseData> CreateNotEqualCases ()
	{
		yield return new (
			RectangleI.FromLTRB (0, 0, 0, 0),
			RectangleI.FromLTRB (0, 0, 1, 1));

		yield return new (
			RectangleI.FromLTRB (1, 1, 1, 1),
			RectangleI.FromLTRB (0, 0, 1, 1));
	}

	private static readonly IReadOnlyList<TestCaseData> from_points_cases = CreateFromPointsCases ().ToArray ();
	private static IEnumerable<TestCaseData> CreateFromPointsCases ()
	{
		yield return new (
			new PointI (5, 6),
			new PointI (3, 4),
			RectangleI.FromLTRB (3, 4, 4, 5),
			new RectangleI (5, 6, 0, 0));
	}
}
