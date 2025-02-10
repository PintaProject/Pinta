/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace Pinta.Core;

/// <summary>
/// This is our pixel format that we will work with. It is always 32-bits / 4-bytes and is
/// always laid out in BGRA order.
/// Generally used with the Surface class.
/// </summary>
[Serializable]
[StructLayout (LayoutKind.Explicit)]
public readonly partial struct ColorBgra
{
	[FieldOffset (0)]
	public readonly byte B;

	[FieldOffset (1)]
	public readonly byte G;

	[FieldOffset (2)]
	public readonly byte R;

	[FieldOffset (3)]
	public readonly byte A;

	/// <summary>
	/// Lets you change B, G, R, and A at the same time.
	/// </summary>
	[NonSerialized]
	[FieldOffset (0)]
	public readonly uint BGRA;

	private ColorBgra (byte b, byte g, byte r, byte a)
	{
		B = b;
		G = g;
		R = r;
		A = a;
	}

	private ColorBgra (uint bgra)
	{
		BGRA = bgra;
	}
}
