/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Marco Rolappe <m_rolappe@gmx.net>                       //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Pinta.Gui.Widgets;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class ReliefEffect : ColorDifferenceEffect
	{
		public ReliefEffect () {
			EffectData = new ReliefData ();
		}

		public ReliefData Data {
			get { return EffectData as ReliefData; }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Stylize"); }
		}

		public override bool LaunchConfiguration () {
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		public override string Icon {
			get { return "Menu.Effects.Stylize.Relief.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Relief"); }
		}

		#region Algorithm Code Ported From PDN
		public override void Render (Cairo.ImageSurface src, Cairo.ImageSurface dst, Gdk.Rectangle[] rois) {
			base.RenderColorDifferenceEffect (Weights, src, dst, rois);
		}

		private double[][] Weights {
			get {
				// adjust and convert angle to radians
				double r = (double)Data.Angle * 2.0 * Math.PI / 360.0;
				
				// angle delta for each weight
				double dr = Math.PI / 4.0;
				
				// for r = 0 this builds an Relief filter pointing straight left
				double[][] weights = new double[3][];
				
				for (uint idx = 0; idx < 3; ++idx) {
					weights[idx] = new double[3];
				}
				
				weights[0][0] = Math.Cos (r + dr);
				weights[0][1] = Math.Cos (r + 2.0 * dr);
				weights[0][2] = Math.Cos (r + 3.0 * dr);
				
				weights[1][0] = Math.Cos (r);
				weights[1][1] = 1;
				weights[1][2] = Math.Cos (r + 4.0 * dr);
				
				weights[2][0] = Math.Cos (r - dr);
				weights[2][1] = Math.Cos (r - 2.0 * dr);
				weights[2][2] = Math.Cos (r - 3.0 * dr);
				
				return weights;
			}
		}
		#endregion
	}


	public class ReliefData : EffectData
	{
		[Caption ("Angle")]
		public double Angle = 45;
	}
}
