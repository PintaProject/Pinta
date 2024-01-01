//
// Maybe.cs
//
// Author:
//       Lehonti Ramos
//
// Copyright (c) 2024 Lehonti Ramos
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
//

using System;

namespace Pinta.Core;

/// <summary>
/// Utilities for creating and handling <see cref="Maybe{T}"/> values
/// </summary>
public static class Maybe
{
	public static Maybe<TValue>.Nothing Nothing<TValue> () => Maybe<TValue>.Nothing.Instance;

	public static Maybe<TValue> FromClass<TValue> (TValue? value) where TValue : class
	{
		if (value is null)
			return Maybe<TValue>.Nothing.Instance;
		else
			return new Maybe<TValue>.Defined (value);
	}

	public static Maybe<TValue> FromNullable<TValue> (TValue? nullable) where TValue : struct
	{
		if (nullable.HasValue)
			return new Maybe<TValue>.Defined (nullable.Value);
		else
			return Maybe<TValue>.Nothing.Instance;
	}

	public static Maybe<TValue>.Defined DefinedFromClass<TValue> (TValue value) where TValue : class
	{
		if (value is null) throw new ArgumentNullException (nameof (value)); // Just making extra-sure
		return new Maybe<TValue>.Defined (value);
	}

	public static Maybe<TValue>.Defined DefinedFromStruct<TValue> (TValue value) where TValue : struct
	{
		return new Maybe<TValue>.Defined (value);
	}

	/// <remarks>
	/// For immediate execution
	/// </remarks>
	public static TResult Match<TValue, TResult> (
		this Maybe<TValue> maybe,
		Func<Maybe<TValue>.Defined, TResult> defined,
		Func<Maybe<TValue>.Nothing, TResult> nothing)
	{
		var matcher = MatchingDelegate (defined, nothing);
		return matcher.Visit (maybe);
	}

	/// <remarks>
	/// For delegation
	/// </remarks>
	public static Maybe<TValue>.Matcher<TResult> MatchingDelegate<TValue, TResult> (
		Func<Maybe<TValue>.Defined, TResult> defined,
		Func<Maybe<TValue>.Nothing, TResult> nothing)
	{
		return new Maybe<TValue>.DelegateMatcher<TResult> (defined, nothing);
	}
}

/// <summary>
/// Represents a value that may of may not be present
/// </summary>
public abstract class Maybe<TValue>
{
	private Maybe () { }

	private protected abstract TResult DispatchSelf<TResult> (Matcher<TResult> visitor);

	public abstract class Matcher<TResult>
	{
		public TResult Visit (Maybe<TValue> maybe)
		{
			return maybe.DispatchSelf (this);
		}
		internal abstract TResult VisitDefined (Maybe<TValue>.Defined defined);
		internal abstract TResult VisitNothing (Maybe<TValue>.Nothing nothing);
	}

	internal sealed class DelegateMatcher<TResult> : Matcher<TResult>
	{
		private readonly Func<Maybe<TValue>.Defined, TResult> match_defined;
		private readonly Func<Maybe<TValue>.Nothing, TResult> match_nothing;

		public DelegateMatcher (
			Func<Maybe<TValue>.Defined, TResult> defined,
			Func<Maybe<TValue>.Nothing, TResult> nothing)
		{
			match_defined = defined;
			match_nothing = nothing;
		}

		internal override TResult VisitDefined (Maybe<TValue>.Defined defined)
		{
			return match_defined (defined);
		}

		internal override TResult VisitNothing (Maybe<TValue>.Nothing nothing)
		{
			return match_nothing (nothing);
		}
	}

	public sealed class Defined : Maybe<TValue>
	{
		public TValue Value { get; }
		internal Defined (TValue value)
		{
			Value = value;
		}
		private protected override TResult DispatchSelf<TResult> (Matcher<TResult> visitor)
		{
			return visitor.VisitDefined (this);
		}
		public override bool Equals (object? other)
		{
			if (other is not Defined defined) return false;
			return Value!.Equals (defined.Value);
		}

		public override int GetHashCode ()
		{
			return Value!.GetHashCode ();
		}
	}

	public sealed class Nothing : Maybe<TValue>
	{
		private Nothing () { }
		internal static Nothing Instance { get; } = new ();
		private protected override TResult DispatchSelf<TResult> (Matcher<TResult> visitor)
		{
			return visitor.VisitNothing (this);
		}
	}

	/// <remarks>
	/// For delegation
	/// </remarks>
	public static Matcher<TResult> MatchingDelegate<TResult> (
		Func<Maybe<TValue>.Defined, TResult> defined,
		Func<Maybe<TValue>.Nothing, TResult> nothing)
	{
		return new DelegateMatcher<TResult> (defined, nothing);
	}
}
