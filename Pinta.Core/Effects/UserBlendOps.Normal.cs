/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class NormalBlendOp : UserBlendOp
	{
		public static string StaticName => "Normal";

		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs)
			=> ApplyStatic (lhs, rhs);

		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs)
		{
			// These are the relevant mathematical formulae:
			// 
			// - C_out = C_a + C_b * (1 - A_a)
			// - A_out = A_a + A_b * (1 - A_a)
			// 
			// Where:
			// 
			// - C refers to the color channels: R, G, B
			// - A refers to the alpha channel
			// - a refers to the color on the top layer
			// - b refers to the color on the bottom layer
			// 
			// Integer arithmetic is used for efficiency.
			// 
			// If one reads about the theory behind the blending,
			// values in the operations usually range from 0 to 1,
			// but here they range from 0 to 255.
			// 
			// That is, the values are scaled by 255
			// with respect to their "theoretical" counterparts.
			// 
			// This 'ROUNDING_ADDEND' mechanism is a neat trick that
			// forces the truncation operator to function as a rounding operator.

			if (rhs.A == 255) return rhs; // Top layer is fully opaque
			if (rhs.A == 0) return lhs; // Top layer is fully transparent
			int inverseSourceAlpha = 255 - rhs.A;
			const int ROUNDING_ADDEND = 128;
			byte outA = Utility.ClampToByte (rhs.A + (lhs.A * inverseSourceAlpha + ROUNDING_ADDEND) / 255);
			byte outR = Utility.ClampToByte ((rhs.R * 255 + lhs.R * inverseSourceAlpha + ROUNDING_ADDEND) / 255);
			byte outG = Utility.ClampToByte ((rhs.G * 255 + lhs.G * inverseSourceAlpha + ROUNDING_ADDEND) / 255);
			byte outB = Utility.ClampToByte ((rhs.B * 255 + lhs.B * inverseSourceAlpha + ROUNDING_ADDEND) / 255);
			return ColorBgra.FromBgra (outB, outG, outR, outA);
		}
	}
}
