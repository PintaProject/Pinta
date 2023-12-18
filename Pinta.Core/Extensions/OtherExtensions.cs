/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Pinta.Core;

public static class OtherExtensions
{
	/// <summary>
	/// In most cases, it creates a wrapped read-only copy of the values generated by the
	/// <see cref="IEnumerable{T}"/> argument, except if the argument is of type
	/// <see cref="ImmutableArray{T}"/> or <see cref="ImmutableList{T}"/>, in which case only
	/// the wrapping (not the copying) is necessary; or if the argument is an object that has
	/// been previously returned from this method, in which case the reference is returned as is.
	/// </summary>
	/// <param name="values">Sequence of values to be materialized</param>
	/// <returns>
	/// Read-only collection wrapper, suitable for class interfaces,
	/// backed by an immutable collection type such as <see cref="ImmutableArray{T}"/>
	/// </returns>
	public static ReadOnlyCollection<T> ToReadOnlyCollection<T> (this IEnumerable<T> values)
	{
		return values switch {
			ImmutableBackedReadOnlyCollection<T> transparent => transparent,
			ImmutableArray<T> array => array.ToReadOnlyCollection (),
			ImmutableList<T> list => list.ToReadOnlyCollection (),
			_ => values.ToImmutableArray ().ToReadOnlyCollection (),
		};
	}

	/// <summary>
	/// Wraps the <see cref="ImmutableArray{T}"/> in a custom
	/// <see cref="ReadOnlyCollection{T}"/> and returns it.
	/// </summary>
	/// <param name="array">Immutable array to be wrapped</param>
	/// <returns>
	/// Read-only collection wrapper, suitable for class interfaces,
	/// backed by the argument that has been passed
	/// </returns>
	public static ReadOnlyCollection<T> ToReadOnlyCollection<T> (this ImmutableArray<T> array)
	{
		return new ImmutableBackedReadOnlyCollection<T> (array);
	}

	/// <summary>
	/// Wraps the <see cref="ImmutableList{T}"/> in a custom
	/// <see cref="ReadOnlyCollection{T}"/> and returns it.
	/// </summary>
	/// <param name="list">Immutable list to be wrapped</param>
	/// <returns>
	/// Read-only collection wrapper, suitable for class interfaces,
	/// backed by the argument that has been passed
	/// </returns>
	public static ReadOnlyCollection<T> ToReadOnlyCollection<T> (this ImmutableList<T> list)
	{
		return new ImmutableBackedReadOnlyCollection<T> (list);
	}

	private sealed class ImmutableBackedReadOnlyCollection<T> : ReadOnlyCollection<T>
	{
		internal ImmutableBackedReadOnlyCollection (ImmutableList<T> list) : base (list)
		{
		}
		internal ImmutableBackedReadOnlyCollection (ImmutableArray<T> array) : base (array)
		{
		}
	}

	public static bool In<T> (this T enumeration, params T[] values)
	{
		if (enumeration is null)
			return false;

		foreach (var en in values)
			if (enumeration.Equals (en))
				return true;

		return false;
	}

	public static IReadOnlyList<IReadOnlyList<PointI>> CreatePolygonSet (this BitMask stencil, RectangleD bounds, PointI translateOffset)
	{
		if (stencil.IsEmpty)
			return Array.Empty<PointI[]> ();

		var polygons = new List<IReadOnlyList<PointI>> ();

		PointI start = bounds.Location ().ToInt ();
		var pts = new List<PointI> ();
		int count = 0;

		// find all islands
		while (true) {
			bool startFound = false;

			while (true) {
				if (stencil[start]) {
					startFound = true;
					break;
				}

				start = start with { X = start.X + 1 };

				if (start.X >= bounds.Right) {
					start = start with { X = (int) bounds.X, Y = start.Y + 1 };
					if (start.Y >= bounds.Bottom)
						break;
				}
			}

			if (!startFound)
				break;

			pts.Clear ();

			PointI last = new (start.X, start.Y + 1);
			PointI curr = new (start.X, start.Y);
			PointI next = curr;

			// trace island outline
			while (true) {

				PointI currLastDelta = curr - last;

				PointI left = new (
					X: (currLastDelta.X + currLastDelta.Y + 2) / 2 + curr.X - 1,
					Y: (currLastDelta.Y - currLastDelta.X + 2) / 2 + curr.Y - 1
				);

				PointI right = new (
					X: (currLastDelta.X - currLastDelta.Y + 2) / 2 + curr.X - 1,
					Y: (currLastDelta.Y + currLastDelta.X + 2) / 2 + curr.Y - 1
				);

				if (bounds.ContainsPoint ((PointD) left) && stencil[left]) {
					// go left
					next -= currLastDelta.Rotated90 ();
				} else if (bounds.ContainsPoint ((PointD) right) && stencil[right]) {
					// go straight
					next += currLastDelta;
				} else {
					// turn right
					next += currLastDelta.Rotated90 ();
				}

				if (
					Math.Sign (next.X - curr.X) != Math.Sign (currLastDelta.X) ||
					Math.Sign (next.Y - curr.Y) != Math.Sign (currLastDelta.Y)) {
					pts.Add (curr);
					++count;
				}

				last = curr;
				curr = next;

				if (next == start)
					break;
			}

			PointI[] points = pts.ToArray ();
			var scans = CairoExtensions.GetScans (points);

			foreach (var scan in scans)
				stencil.Invert (scan);

			CairoExtensions.TranslatePointsInPlace (points, translateOffset);
			polygons.Add (points);
		}

		return polygons;
	}
}
