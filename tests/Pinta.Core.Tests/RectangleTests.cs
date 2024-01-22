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
	}

	[TestCaseSource (nameof (not_equal_cases))]
	public void CorrectNotEqual (RectangleI a, RectangleI b)
		=> Assert.That (a, Is.Not.EqualTo (b));

	[TestCaseSource (nameof (union_cases))]
	public void CorrectUnion (RectangleI a, RectangleI b, RectangleI expected)
		=> Assert.That (a.Union (b), Is.EqualTo (expected));

	[TestCaseSource (nameof (intersect_cases))]
	public void CorrectIntersection (RectangleI a, RectangleI b, RectangleI expected)
		=> Assert.That (a.Intersect (b), Is.EqualTo (expected));

	[TestCaseSource (nameof (inflation_cases))]
	public void CorrectInflation (RectangleI a, int widthInflation, int heightInflation, RectangleI expected)
		=> Assert.That (a.Inflated (widthInflation, heightInflation), Is.EqualTo (expected));

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
}
