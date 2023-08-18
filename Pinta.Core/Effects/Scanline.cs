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
	private readonly int x;
	private readonly int y;
	private readonly int length;

	public readonly int X => x;

	public readonly int Y => y;

	public readonly int Length => length;

	public override readonly int GetHashCode ()
	{
		unchecked {
			return length.GetHashCode () + x.GetHashCode () + y.GetHashCode ();
		}
	}

	public override readonly bool Equals (object? obj)
	{
		if (obj is Scanline) {
			Scanline rhs = (Scanline) obj;
			return x == rhs.x && y == rhs.y && length == rhs.length;
		} else {
			return false;
		}
	}

	public static bool operator == (Scanline lhs, Scanline rhs)
	{
		return lhs.x == rhs.x && lhs.y == rhs.y && lhs.length == rhs.length;
	}

	public static bool operator != (Scanline lhs, Scanline rhs)
	{
		return !(lhs == rhs);
	}

	public override readonly string ToString ()
	{
		return "(" + x + "," + y + "):[" + length.ToString () + "]";
	}

	public Scanline (int x, int y, int length)
	{
		if (length < 0)
			throw new ArgumentOutOfRangeException (nameof (length));

		this.x = x;
		this.y = y;
		this.length = length;
	}
}
