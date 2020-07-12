//
// CoreExtensions.IEnumerable.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pinta.Docking
{
	public static partial class CoreExtensions
	{
		public static IEnumerable<T> Concat<T> (this IEnumerable<T> e, T item)
		{
			return e.Concat (Enumerable.Repeat (item, 1));
		}

		public static int FindIndex<T> (this IEnumerable<T> e, Func<T, bool> predicate)
		{
			bool found = false;
			int index = e.TakeWhile (i => {
				found = predicate (i);
				return !found;
			}).Count ();

			return found ? index : -1;
		}

		public static int IndexOf<T> (this IEnumerable<T> e, T item)
		{
			bool found = false;
			int index = e.TakeWhile (i => {
				found = EqualityComparer<T>.Default.Equals (i, item);
				return !found;
			}).Count ();

			return found ? index : -1;
		}

		static TSource MaxValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer, out bool hasValue)
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));

			TSource result = default;
			TCompare value = default;
			hasValue = false;
			foreach (TSource item in source) {
				var x = compareSelector (item);
				if (hasValue) {
					if (comparer.Compare (x, value) > 0) {
						value = x;
						result = item;
					}
				} else {
					value = x;
					result = item;
					hasValue = true;
				}
			}
			return result;
		}

		public static TSource MaxValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector) where TCompare : IComparable<TCompare>
		{
			TSource result = MaxValue (source, compareSelector, Comparer<TCompare>.Default, out bool hasValue);
			if (hasValue)
				return result;
			throw new InvalidOperationException (string.Format ("{0} contains no elements", nameof (source)));
		}

		public static TSource MaxValueOrDefault<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector) where TCompare : IComparable<TCompare>
		{
			return MaxValue (source, compareSelector, Comparer<TCompare>.Default, out _);
		}

		public static TSource MaxValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer)
		{
			TSource result = MaxValue (source, compareSelector, comparer, out bool hasValue);
			if (hasValue)
				return result;
			throw new InvalidOperationException (string.Format ("{0} contains no elements", nameof (source)));
		}

		public static TSource MaxValueOrDefault<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer)
		{
			return MaxValue (source, compareSelector, comparer, out _);
		}

		static TSource MinValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer, out bool hasValue)
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));

			TSource result = default;
			TCompare value = default;
			hasValue = false;
			foreach (TSource item in source) {
				var x = compareSelector (item);
				if (hasValue) {
					if (comparer.Compare (x, value) < 0) {
						value = x;
						result = item;
					}
				} else {
					value = x;
					result = item;
					hasValue = true;
				}
			}
			return result;
		}

		public static TSource MinValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector) where TCompare : IComparable<TCompare>
		{
			TSource result = MinValue (source, compareSelector, Comparer<TCompare>.Default, out bool hasValue);
			if (hasValue)
				return result;
			throw new InvalidOperationException (string.Format ("{0} contains no elements", nameof (source)));
		}

		public static TSource MinValueOrDefault<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector) where TCompare : IComparable<TCompare>
		{
			return MinValue (source, compareSelector, Comparer<TCompare>.Default, out _);
		}

		public static TSource MinValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer)
		{
			TSource result = MinValue (source, compareSelector, comparer, out bool hasValue);

			if (hasValue)
				return result;
			throw new InvalidOperationException (string.Format ("{0} contains no elements", nameof (source)));
		}

		public static TSource MinValueOrDefault<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer)
		{
			return MinValue (source, compareSelector, comparer, out _);
		}
	}
}
