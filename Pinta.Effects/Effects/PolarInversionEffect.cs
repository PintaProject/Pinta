/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects
{
	public class PolarInversionEffect : WarpEffect
	{
		public override string Icon => Pinta.Resources.Icons.EffectsDistortPolarInversion;

		public override string Name => Translations.GetString ("Polar Inversion");

		public override bool IsConfigurable => true;

		public new PolarInversionData Data => (PolarInversionData) EffectData!;

		public override string EffectMenuCategory => Translations.GetString ("Distort");

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
			[MinimumValue (-4), MaximumValue (4)]
			public double Amount = 0;

			public PolarInversionData () : base ()
			{
				EdgeBehavior = WarpEdgeBehavior.Reflect;
			}

		}
	}
}
