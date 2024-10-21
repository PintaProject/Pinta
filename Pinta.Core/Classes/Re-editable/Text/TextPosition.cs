/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core;

public readonly struct TextPosition : IComparable<TextPosition>
{
	public int Line { get; }
	public int Offset { get; }

	public TextPosition (int line, int offset)
	{
		Line = line;
		Offset = offset;
	}

	public TextPosition WithLine (int line)
		=> new (line, Offset);

	public TextPosition WithOffset (int offset)
		=> new (Line, offset);

	public override readonly bool Equals (object? obj)
	{
		return obj is TextPosition && this == (TextPosition) obj;
	}

	public override readonly int GetHashCode ()
	{
		return new { Line, Offset }.GetHashCode ();
	}

	public override readonly string ToString ()
	{
		return $"({Line}, {Offset})";
	}

	public static bool operator == (TextPosition x, TextPosition y)
	{
		return x.CompareTo (y) == 0;
	}

	public static bool operator != (TextPosition x, TextPosition y)
	{
		return x.CompareTo (y) != 0;
	}

	public readonly int CompareTo (TextPosition other)
	{
		if (Line.CompareTo (other.Line) != 0)
			return Line.CompareTo (other.Line);
		else
			return Offset.CompareTo (other.Offset);
	}

	public static TextPosition Max (TextPosition p1, TextPosition p2)
	{
		return (p1.CompareTo (p2) > 0) ? p1 : p2;
	}

	public static TextPosition Min (TextPosition p1, TextPosition p2)
	{
		return (p1.CompareTo (p2) < 0) ? p1 : p2;
	}
}
