using System;

namespace Pinta.Core;

partial class UserBlendOps
{
	[Serializable]
	public sealed class DifferenceBlendOp : UserBlendOp
	{
		public static string StaticName => "Difference";
		public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Abs (((rhs).B) - ((lhs).B)); }; { fG = Math.Abs (((rhs).G) - ((lhs).G)); }; { fR = Math.Abs (((rhs).R) - ((lhs).R)); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = mas_table[masIndex]; uint taA = mas_table[masIndex + 1]; uint taS = mas_table[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override void Apply (Span<ColorBgra> dst, ReadOnlySpan<ColorBgra> src)
		{
			for (int i = 0; i < src.Length; ++i)
				dst[i] = ApplyStatic (dst[i], src[i]);
		}
		public override void Apply (Span<ColorBgra> dst, ReadOnlySpan<ColorBgra> lhs, ReadOnlySpan<ColorBgra> rhs)
		{
			for (int i = 0; i < dst.Length; ++i)
				dst[i] = ApplyStatic (lhs[i], rhs[i]);
		}
		public static ColorBgra ApplyStatic (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Abs (((rhs).B) - ((lhs).B)); }; { fG = Math.Abs (((rhs).G) - ((lhs).G)); }; { fR = Math.Abs (((rhs).R) - ((lhs).R)); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = mas_table[masIndex]; uint taA = mas_table[masIndex + 1]; uint taS = mas_table[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
		public override UserBlendOp CreateWithOpacity (int opacity) { return new DifferenceBlendOpWithOpacity (opacity); }
		private sealed class DifferenceBlendOpWithOpacity : UserBlendOp
		{
			private readonly int opacity; private byte ApplyOpacity (byte a) { int r; { r = (a); }; { r = ((r) * (opacity) + 0x80); r = ((((r) >> 8) + (r)) >> 8); }; return (byte) r; }
			public static string StaticName => "UserBlendOps." + "Difference" + "BlendOp.Name";
			public override ColorBgra Apply (in ColorBgra lhs, in ColorBgra rhs) { int lhsA; { lhsA = ((lhs).A); }; int rhsA; { rhsA = ApplyOpacity ((rhs).A); }; int y; { y = ((lhsA) * (255 - rhsA) + 0x80); y = ((((y) >> 8) + (y)) >> 8); }; int totalA = y + rhsA; uint ret; if (totalA == 0) { ret = 0; } else { int fB; int fG; int fR; { fB = Math.Abs (((rhs).B) - ((lhs).B)); }; { fG = Math.Abs (((rhs).G) - ((lhs).G)); }; { fR = Math.Abs (((rhs).R) - ((lhs).R)); }; int x; { x = ((lhsA) * (rhsA) + 0x80); x = ((((x) >> 8) + (x)) >> 8); }; int z = rhsA - x; int masIndex = totalA * 3; uint taM = mas_table[masIndex]; uint taA = mas_table[masIndex + 1]; uint taS = mas_table[masIndex + 2]; uint b = (uint) (((((long) ((((lhs).B * y) + ((rhs).B * z) + (fB * x)))) * taM) + taA) >> (int) taS); uint g = (uint) (((((long) ((((lhs).G * y) + ((rhs).G * z) + (fG * x)))) * taM) + taA) >> (int) taS); uint r = (uint) (((((long) ((((lhs).R * y) + ((rhs).R * z) + (fR * x)))) * taM) + taA) >> (int) taS); int a; { { a = ((lhsA) * (255 - (rhsA)) + 0x80); a = ((((a) >> 8) + (a)) >> 8); }; a += (rhsA); }; ret = b + (g << 8) + (r << 16) + ((uint) a << 24); }; return ColorBgra.FromUInt32 (ret); }
			public override void Apply (Span<ColorBgra> dst, ReadOnlySpan<ColorBgra> src)
			{
				for (int i = 0; i < src.Length; ++i)
					dst[i] = ApplyStatic (dst[i], src[i]);
			}
			public override void Apply (Span<ColorBgra> dst, ReadOnlySpan<ColorBgra> lhs, ReadOnlySpan<ColorBgra> rhs)
			{
				for (int i = 0; i < dst.Length; ++i)
					dst[i] = ApplyStatic (lhs[i], rhs[i]);
			}
			public DifferenceBlendOpWithOpacity (int opacity) { if (this.opacity < 0 || this.opacity > 255) { throw new ArgumentOutOfRangeException (nameof (opacity)); } this.opacity = opacity; }
		}
	}
}
