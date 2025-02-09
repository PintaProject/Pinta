namespace Pinta.Core;

partial struct ColorBgra
{
	public static int ColorDifference (ColorBgra a, ColorBgra b)
	{
		int diffR = a.R - b.R;
		int diffG = a.G - b.G;
		int diffB = a.B - b.B;
		int diffA = a.A - b.A;

		int summandR = (1 + diffR * diffR) * a.A / 256;
		int summandG = (1 + diffG * diffG) * a.A / 256;
		int summandB = (1 + diffB * diffB) * a.A / 256;
		int summandA = diffA * diffA;

		int sum = summandR + summandG + summandB + summandA;
		return sum;
	}

	public static bool ColorsWithinTolerance (ColorBgra a, ColorBgra b, int tolerance)
		=> ColorDifference (a, b) <= tolerance * tolerance * 4;
}
