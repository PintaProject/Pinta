/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Gui.Widgets;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class PolarInversionEffect : WarpEffect
	{
		public override string Icon {
			get { return "Menu.Effects.Distort.PolarInversion.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Polar Inversion"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public new PolarInversionData Data {
			get { return EffectData as PolarInversionData; }
		}

		public override string EffectMenuCategory
		{
			get { return Catalog.GetString ("Distort"); }
		}

		public PolarInversionEffect ()
		{
			EffectData = new PolarInversionData ();
		}

		#region Algorithm Code Ported From PDN
		protected override void InverseTransform (ref TransformData transData)
		{
			double x = transData.X;
			double y = transData.Y;
			
			// NOTE: when x and y are zero, this will divide by zero and return NaN
			double invertDistance = Utility.Lerp (1.0, DefaultRadius2 / ((x * x) + (y * y)), Data.Amount);
			
			transData.X = x * invertDistance;
			transData.Y = y * invertDistance;
		}
		#endregion

		public class PolarInversionData : WarpEffect.WarpData
		{
			[MinimumValue(-4), MaximumValue(4)]
			public double Amount = 0;

			public PolarInversionData () : base()
			{
				EdgeBehavior = WarpEdgeBehavior.Reflect;
			}
			
		}
	}
}
