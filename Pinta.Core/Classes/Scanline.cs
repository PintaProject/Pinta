/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core;

public readonly struct Scanline
{
	public int X { get; }
	public int Y { get; }
	public int Length { get; }

	public override readonly int GetHashCode () { unchecked { return Length.GetHashCode () + X.GetHashCode () + Y.GetHashCode (); } }
	public override readonly bool Equals (object? obj) => obj is Scanline rhs && X == rhs.X && Y == rhs.Y && Length == rhs.Length;
	public static bool operator == (Scanline lhs, Scanline rhs) => lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Length == rhs.Length;
	public static bool operator != (Scanline lhs, Scanline rhs) => !(lhs == rhs);
	public override readonly string ToString () => $"({X},{Y}):[{Length}]";

	public Scanline (int x, int y, int length)
	{
		if (length < 0)
			throw new ArgumentOutOfRangeException (nameof (length));

		X = x;
		Y = y;
		Length = length;
	}
}
