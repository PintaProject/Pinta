/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

namespace Pinta.Core;

partial struct ColorBgra
{
	public static int ColorDifference (ColorBgra a, ColorBgra b)
	{
		int diffR = a.R - b.R;
		int diffG = a.G - b.G;
		int diffB = a.B - b.B;
		int diffA = a.A - b.A;

		int summandR = diffR * diffR;
		int summandG = diffG * diffG;
		int summandB = diffB * diffB;
		int summandA = diffA * diffA;

		int sum = summandR + summandG + summandB + summandA;
		return sum;
	}

	public static bool ColorsWithinTolerance (ColorBgra a, ColorBgra b, int tolerance)
		=> ColorDifference (a, b) <= tolerance * tolerance * 4;
}
