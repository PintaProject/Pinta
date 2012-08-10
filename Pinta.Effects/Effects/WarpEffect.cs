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

namespace Pinta.Effects
{
	public enum WarpEdgeBehavior
    {
        Clamp,
        Wrap,
        Reflect,
        Primary,
        Secondary,
        Transparent,
        Original,
    }
	
	public abstract class WarpEffect : BaseEffect
	{

		public WarpData Data {
			get { return EffectData as WarpData; }
		}
		
		public WarpEffect ()
		{
			EffectData = new WarpData ();
		}
		
		public override bool LaunchConfiguration ()
		{
			if (EffectHelper.LaunchSimpleEffectDialog (this)) {
				Data.PropertyChanged += delegate {
						//TODO
						this.defaultRadius = 2; //Math.Min(selection.Width, selection.Height) * 0.5;
						this.defaultRadius2 = this.defaultRadius * this.defaultRadius;
				};
				return true;
			}
			return false;
		}

		private double defaultRadius;
		private double defaultRadius2;
		
		protected double DefaultRadius { get { return this.defaultRadius; } }
		protected double DefaultRadius2 { get { return this.defaultRadius2; } }
		
		#region Algorithm Code Ported From PDN
		public unsafe override void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle[] rois)
		{
			
			ColorBgra colPrimary = PintaCore.Palette.PrimaryColor.ToColorBgra ();
			ColorBgra colSecondary = PintaCore.Palette.SecondaryColor.ToColorBgra ();
			ColorBgra colTransparent = ColorBgra.Transparent;
			
			int aaSampleCount = Data.Quality * Data.Quality;
			Cairo.PointD* aaPoints = stackalloc Cairo.PointD[aaSampleCount];
			Utility.GetRgssOffsets (aaPoints, aaSampleCount, Data.Quality);
			ColorBgra* samples = stackalloc ColorBgra[aaSampleCount];
			
			TransformData td;
			
			foreach (Gdk.Rectangle rect in rois) {
				
				for (int y = rect.Top; y <= rect.GetBottom (); y++) {
					ColorBgra* dstPtr = dst.GetPointAddressUnchecked (rect.Left, y);
					
					double relativeY = y - Data.CenterOffset.Y;
					
					for (int x = rect.Left; x <= rect.GetRight (); x++) {
						double relativeX = x - Data.CenterOffset.X;
						
						int sampleCount = 0;
						
						for (int p = 0; p < aaSampleCount; ++p) {
							td.X = relativeX + aaPoints[p].X;
							td.Y = relativeY - aaPoints[p].Y;
							
							InverseTransform (ref td);
							
							float sampleX = (float)(td.X + Data.CenterOffset.X);
							float sampleY = (float)(td.Y + Data.CenterOffset.Y);
							
							ColorBgra sample = colPrimary;
							
							if (IsOnSurface (src, sampleX, sampleY)) {
								sample = src.GetBilinearSample (sampleX, sampleY);
							} else {
								switch (Data.EdgeBehavior) {
								case WarpEdgeBehavior.Clamp:
									sample = src.GetBilinearSampleClamped (sampleX, sampleY);
									break;
								
								case WarpEdgeBehavior.Wrap:
									sample = src.GetBilinearSampleWrapped (sampleX, sampleY);
									break;
								
								case WarpEdgeBehavior.Reflect:
									sample = src.GetBilinearSampleClamped (ReflectCoord (sampleX, src.Width), ReflectCoord (sampleY, src.Height));
									
									break;
								
								case WarpEdgeBehavior.Primary:
									sample = colPrimary;
									break;
								
								case WarpEdgeBehavior.Secondary:
									sample = colSecondary;
									break;
								
								case WarpEdgeBehavior.Transparent:
									sample = colTransparent;
									break;
								
								case WarpEdgeBehavior.Original:
									sample = src.GetColorBgra (x, y);
									break;
								default:
									
									break;
								}
							}
							
							samples[sampleCount] = sample;
							++sampleCount;
						}
						
						*dstPtr = ColorBgra.Blend (samples, sampleCount);
						++dstPtr;
					}
				}
			}
		}

		protected abstract void InverseTransform (ref TransformData data);

		protected struct TransformData
		{
			public double X;
			public double Y;
		}

		private static bool IsOnSurface (ImageSurface src, float u, float v)
		{
			return (u >= 0 && u <= (src.Width - 1) && v >= 0 && v <= (src.Height - 1));
		}

		private static float ReflectCoord (float value, int max)
		{
			bool reflection = false;
			
			while (value < 0) {
				value += max;
				reflection = !reflection;
			}
			
			while (value > max) {
				value -= max;
				reflection = !reflection;
			}
			
			if (reflection) {
				value = max - value;
			}
			
			return value;
		}
		
		#endregion
		public class WarpData : EffectData
		{
			[MinimumValue(1), MaximumValue(5)]
			public int Quality = 2;
			
			public Gdk.Point CenterOffset;
			
			public WarpEdgeBehavior EdgeBehavior = WarpEdgeBehavior.Wrap;
		}
	}
}
