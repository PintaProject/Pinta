/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
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
			public override double ComputeUnboundedLerp (int x, int y)
			{
				double dx = x - StartPoint.X;
				double dy = y - StartPoint.Y;
				
				double lerp = (dx * this.dtdx) + (dy * this.dtdy);
				
				return lerp;
			}

			protected internal LinearStraight (bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}
		}

		public sealed class LinearReflected : LinearStraight
		{
			public override double BoundLerp (double t)
			{
				return Utility.Clamp (Math.Abs (t), 0, 1);
			}

			public LinearReflected (bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}
		}

		public sealed class LinearClamped : LinearStraight
		{
			public override double BoundLerp (double t)
			{
				return Utility.Clamp (t, 0, 1);
			}

			public LinearClamped (bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}
		}

		public sealed class LinearDiamond : LinearStraight
		{
			public override double ComputeUnboundedLerp (int x, int y)
			{
				double dx = x - StartPoint.X;
				double dy = y - StartPoint.Y;
				
				double lerp1 = (dx * this.dtdx) + (dy * this.dtdy);
				double lerp2 = (dx * this.dtdy) - (dy * this.dtdx);
				
				double absLerp1 = Math.Abs (lerp1);
				double absLerp2 = Math.Abs (lerp2);
				
				return absLerp1 + absLerp2;
			}

			public override double BoundLerp (double t)
			{
				return Utility.Clamp (t, 0, 1);
			}

			public LinearDiamond (bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}
		}

		public sealed class Radial : GradientRenderer
		{
			private double invDistanceScale;

			public override void BeforeRender ()
			{
				double distanceScale = this.StartPoint.Distance (this.EndPoint);
				
				if (distanceScale == 0) {
					this.invDistanceScale = 0;
				} else {
					this.invDistanceScale = 1f / distanceScale;
				}
				
				base.BeforeRender ();
			}

			public override double ComputeUnboundedLerp (int x, int y)
			{
				double dx = x - StartPoint.X;
				double dy = y - StartPoint.Y;
				
				double distance = Math.Sqrt (dx * dx + dy * dy);
				
				return distance * this.invDistanceScale;
			}

			public override double BoundLerp (double t)
			{
				return Utility.Clamp (t, 0, 1);
			}

			public Radial (bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}
		}

		public sealed class Conical : GradientRenderer
		{
			private double tOffset;
			private const double invPi = 1.0 / Math.PI;

			public override void BeforeRender ()
			{
				this.tOffset = -ComputeUnboundedLerp ((int)EndPoint.X, (int)EndPoint.Y);
				base.BeforeRender ();
			}

			public override double ComputeUnboundedLerp (int x, int y)
			{
				double ax = x - StartPoint.X;
				double ay = y - StartPoint.Y;
				
				double theta = Math.Atan2 (ay, ax);
				
				double t = theta * invPi;
				
				return t + this.tOffset;
			}

			public override double BoundLerp (double t)
			{
				if (t > 1) {
					t -= 2;
				} else if (t < -1) {
					t += 2;
				}
				
				return Utility.Clamp (Math.Abs (t), 0, 1);
			}

			public Conical (bool alphaOnly, BinaryPixelOp normalBlendOp) : base(alphaOnly, normalBlendOp)
			{
			}
		}
	}
}
