using System;
using System.Runtime.InteropServices;

namespace Pinta.Core;

public static partial class GrapheneExtensions
{
	/// <summary>
	/// Convert from Pinta.Core.PointD to a Graphene.Point.
	/// Note: this converts from double to float.
	/// </summary>
	public static Graphene.Point ToGraphenePoint (this PointD point)
	{
		return new () { X = (float) point.X, Y = (float) point.Y };
	}
}
