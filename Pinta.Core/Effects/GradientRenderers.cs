/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core
{
	public static class GradientRenderers
	{
		public abstract class LinearBase : GradientRenderer
		{
			protected double dtdx;
			protected double dtdy;

			public override void BeforeRender ()
			{
				PointD vec = new PointD (EndPoint.X - StartPoint.X, EndPoint.Y - StartPoint.Y);
				double mag = vec.Magnitude ();
				
				if (EndPoint.X == StartPoint.X) {
					this.dtdx = 0;
				} else {
					this.dtdx = vec.X / (mag * mag);
				}
				
				if (EndPoint.Y == StartPoint.Y) {
					this.dtdy = 0;
				} else {
					this.dtdy = vec.Y / (mag * mag);
				}
				
				base.BeforeRender ();
			}

			protected internal LinearBase (bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}
		}

		public abstract class LinearStraight : LinearBase
		{
			private int _startY;
			private int _startX;

			protected internal LinearStraight(bool alphaOnly, BinaryPixelOp normalBlendOp)
				: base(alphaOnly, normalBlendOp)
			{
			}

			private static byte BoundLerp(double t)
			{
				return (byte)(Utility.Clamp(Math.Abs(t), 0, 1) * 255f);
			}

			public override void BeforeRender()
			{
				base.BeforeRender();

				_startX = (int)StartPoint.X;
				_startY = (int)StartPoint.Y;

			}

			public override byte ComputeByteLerp(int x, int y)
			{
				var dx = x - _startX;
				var dy = y - _startY;

				var lerp = (dx*dtdx) + (dy*dtdy);

				return BoundLerp(lerp);
			}
		}

		public sealed class LinearReflected : LinearStraight
		{
			public LinearReflected(bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}
		}

		public sealed class LinearClamped : LinearStraight
		{
			public LinearClamped(bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}
		}

		public sealed class LinearDiamond : LinearStraight
		{
			public LinearDiamond(bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}

			public override byte ComputeByteLerp(int x, int y)
			{
				var dx = x - StartPoint.X;
				var dy = y - StartPoint.Y;

				var lerp1 = (dx*dtdx) + (dy*dtdy);
				var lerp2 = (dx*dtdy) - (dy*dtdx);

				var absLerp1 = Math.Abs(lerp1);
				var absLerp2 = Math.Abs(lerp2);

				return BoundLerp(absLerp1 + absLerp2);
			}

			private byte BoundLerp(double t)
			{
				return (byte)(Utility.Clamp(t, 0, 1)*255f);
			}
		}

		public sealed class Radial : GradientRenderer
		{
			private double invDistanceScale;

			public Radial(bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}

			int _startX, _startY;

			public override void BeforeRender()
			{
				var distanceScale = StartPoint.Distance(EndPoint);

				_startX = (int)StartPoint.X;
				_startY = (int)StartPoint.Y;

				if (distanceScale == 0)
					invDistanceScale = 0;
				else
					invDistanceScale = 1f / distanceScale;

				base.BeforeRender();
			}

			public override byte ComputeByteLerp(int x, int y)
			{
				var dx = x - _startX;
				var dy = y - _startY;

				var distance = Math.Sqrt(dx*dx + dy*dy);

				var result = distance*invDistanceScale;
				if (result < 0.0)
					return 0;
				return result > 1.0 ? (byte)255 : (byte)(result*255f);
			}
		}

		public sealed class Conical : GradientRenderer
		{
			private const double invPi = 1.0 / Math.PI;
			private double tOffset;

			public Conical(bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}

			public override void BeforeRender()
			{
				var ax = EndPoint.X - StartPoint.X;
				var ay = EndPoint.Y - StartPoint.Y;

				var theta = Math.Atan2(ay, ax);

				var t = theta * invPi;

				tOffset = -t;
				base.BeforeRender();
			}

			public override byte ComputeByteLerp(int x, int y)
			{
				var ax = x - StartPoint.X;
				var ay = y - StartPoint.Y;

				var theta = Math.Atan2(ay, ax);

				var t = theta*invPi;

				return (byte)(BoundLerp(t + tOffset)*255f);
			}

			public double BoundLerp(double t)
			{
				if (t > 1)
					t -= 2;
				else if (t < -1)
					t += 2;

				return Utility.Clamp(Math.Abs(t), 0, 1);
			}
		}
	}
}
