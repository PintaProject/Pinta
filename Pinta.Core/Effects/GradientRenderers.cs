/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core;

public static class GradientRenderers
{
	public abstract class LinearBase : GradientRenderer
	{
		protected double dtdx;
		protected double dtdy;

		public override void BeforeRender ()
		{
			PointD vec = new (EndPoint.X - StartPoint.X, EndPoint.Y - StartPoint.Y);
			double mag = vec.Magnitude ();

			dtdx = EndPoint.X == StartPoint.X ? 0 : vec.X / (mag * mag);
			dtdy = EndPoint.Y == StartPoint.Y ? 0 : vec.Y / (mag * mag);
		}

		protected internal LinearBase (bool alphaOnly, BinaryPixelOp normalBlendOp) : base (alphaOnly, normalBlendOp)
		{
		}
	}

	public abstract class LinearStraight : LinearBase
	{
		private int start_y;
		private int start_x;

		protected internal LinearStraight (bool alphaOnly, BinaryPixelOp normalBlendOp)
			: base (alphaOnly, normalBlendOp)
		{
		}

		protected virtual byte BoundLerp (double t)
			=> (byte) (Math.Clamp (t, 0, 1) * 255f);

		public override void BeforeRender ()
		{
			base.BeforeRender ();

			start_x = (int) StartPoint.X;
			start_y = (int) StartPoint.Y;
		}

		public override byte ComputeByteLerp (int x, int y)
		{
			int dx = x - start_x;
			int dy = y - start_y;

			double lerp = (dx * dtdx) + (dy * dtdy);

			return BoundLerp (lerp);
		}
	}

	public sealed class LinearReflected : LinearStraight
	{
		public LinearReflected (bool alphaOnly, BinaryPixelOp normalBlendOp) : base (alphaOnly, normalBlendOp)
		{
		}

		protected override byte BoundLerp (double t)
			=> (byte) (Math.Clamp (Math.Abs (t), 0, 1) * 255f);
	}

	public sealed class LinearClamped : LinearStraight
	{
		public LinearClamped (bool alphaOnly, BinaryPixelOp normalBlendOp) : base (alphaOnly, normalBlendOp)
		{
		}
	}

	public sealed class LinearDiamond : LinearStraight
	{
		public LinearDiamond (bool alphaOnly, BinaryPixelOp normalBlendOp) : base (alphaOnly, normalBlendOp)
		{
		}

		public override byte ComputeByteLerp (int x, int y)
		{
			double dx = x - StartPoint.X;
			double dy = y - StartPoint.Y;

			double lerp1 = (dx * dtdx) + (dy * dtdy);
			double lerp2 = (dx * dtdy) - (dy * dtdx);

			double absLerp1 = Math.Abs (lerp1);
			double absLerp2 = Math.Abs (lerp2);

			return BoundLerp (absLerp1 + absLerp2);
		}
	}

	public sealed class Radial : GradientRenderer
	{
		private double inv_distance_scale;

		public Radial (bool alphaOnly, BinaryPixelOp normalBlendOp) : base (alphaOnly, normalBlendOp)
		{
		}

		int start_x, start_y;

		public override void BeforeRender ()
		{
			double distanceScale = StartPoint.Distance (EndPoint);

			start_x = (int) StartPoint.X;
			start_y = (int) StartPoint.Y;

			inv_distance_scale = distanceScale switch {
				0 => 0,
				_ => 1f / distanceScale,
			};
		}

		public override byte ComputeByteLerp (int x, int y)
		{
			int dx = x - start_x;
			int dy = y - start_y;

			double distance = Math.Sqrt (dx * dx + dy * dy);

			double result = distance * inv_distance_scale;

			if (result < 0.0)
				return 0;

			return result > 1.0 ? (byte) 255 : (byte) (result * 255f);
		}
	}

	public sealed class Conical : GradientRenderer
	{
		private const double InvPi = 1.0 / Math.PI;
		private double t_offset;

		public Conical (bool alphaOnly, BinaryPixelOp normalBlendOp) : base (alphaOnly, normalBlendOp)
		{
		}

		public override void BeforeRender ()
		{
			double ax = EndPoint.X - StartPoint.X;
			double ay = EndPoint.Y - StartPoint.Y;

			double theta = Math.Atan2 (ay, ax);

			double t = theta * InvPi;

			t_offset = -t;
		}

		public override byte ComputeByteLerp (int x, int y)
		{
			double ax = x - StartPoint.X;
			double ay = y - StartPoint.Y;

			double theta = Math.Atan2 (ay, ax);

			double t = theta * InvPi;

			return (byte) (BoundLerp (t + t_offset) * 255f);
		}

		public double BoundLerp (double t)
		{
			double effective = t switch {
				> 1 => t - 2,
				< -1 => t + 2,
				_ => t,
			};

			return Math.Clamp (Math.Abs (effective), 0, 1);
		}
	}
}
