/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using Pinta.Gui.Widgets;
using Cairo;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class CloudsEffect : BaseEffect
	{
        // This is so that repetition of the effect with CTRL+F actually shows up differently.
        private byte instanceSeed = unchecked((byte)DateTime.Now.Ticks);
        private static object render_lock = new object ();		

		public override string Icon {
			get { return "Menu.Effects.Render.Clouds.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Clouds"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Render"); }
		}

		public CloudsData Data { get { return EffectData as CloudsData; } }

		public CloudsEffect ()
		{
			EffectData = new CloudsData ();
		}

		public override bool LaunchConfiguration ()
		{
			return EffectHelper.LaunchSimpleEffectDialog (this);
		}

		#region Algorithm Code Ported From PDN

		static CloudsEffect()
        {
            for (int i = 0; i < 256; i++)
            {
                permuteLookup[256 + i] = permutationTable[i];
                permuteLookup[i] = permutationTable[i];
            }
        }
		
		// Adapted to 2-D version in C# from 3-D version in Java from http://mrl.nyu.edu/~perlin/noise/
        static private int[] permuteLookup = new int[512];

        static private int[] permutationTable = new int[]
        {
            151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7,
            225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6,
            148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35,
            11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171,
            168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231,
            83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245,
            40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76,
            132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86,
            164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123,
            5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47,
            16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2,
            44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39,
            253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218,
            246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162,
            241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181,
            199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150,
            254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128,
            195, 78, 66, 215, 61, 156, 180
        };

        private static double Fade(double t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static double Grad(int hash, double x, double y)
        {
            int h = hash & 15;
            double u = h < 8 ? x : y;
            double v = h < 4 ? y : h == 12 || h == 14 ? x : 0;

            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        private static double Noise(byte ix, byte iy, double x, double y, byte seed)
        {
            double u = Fade(x);
            double v = Fade(y);

            int a = permuteLookup[ix + seed] + iy;
            int aa = permuteLookup[a];
            int ab = permuteLookup[a + 1];
            int b = permuteLookup[ix + 1 + seed] + iy;
            int ba = permuteLookup[b];
            int bb = permuteLookup[b + 1];

            double gradAA = Grad(permuteLookup[aa], x, y);
            double gradBA = Grad(permuteLookup[ba], x - 1, y);

            double edge1 = Utility.Lerp(gradAA, gradBA, u);

            double gradAB = Grad(permuteLookup[ab], x, y - 1);
            double gradBB = Grad(permuteLookup[bb], x - 1, y - 1);

            double edge2 = Utility.Lerp(gradAB, gradBB, u);

            return Utility.Lerp(edge1, edge2, v);
        }
		
		private unsafe static void RenderClouds(ImageSurface surface, Gdk.Rectangle rect, int scale, byte seed, double power, ColorBgra colorFrom, ColorBgra colorTo)
        {
            int w = surface.Width;
            int h = surface.Height;

            for (int y = rect.Top; y <= rect.GetBottom (); ++y)
            {
                ColorBgra* ptr = surface.GetPointAddressUnchecked(0, y - rect.Top);
                int dy = 2 * y - h;

                for (int x = rect.Left; x <= rect.GetRight (); ++x)
                {
                    int dx = 2 * x - w;
                    double val = 0;
                    double mult = 1;
                    int div = scale;

                    for (int i = 0; i < 12 && mult > 0.03 && div > 0; ++i)
                    {
                        double dxr = 65536 + (double)dx / (double)div;
                        double dyr = 65536 + (double)dy / (double)div;

                        int dxd = (int)dxr;
                        int dyd = (int)dyr;

                        dxr -= dxd;
                        dyr -= dyd;
                        
                        double noise = Noise(
                            unchecked((byte)dxd),
                            unchecked((byte)dyd),
                            dxr, //(double)dxr / div,
                            dyr, //(double)dyr / div,
                            (byte)(seed ^ i));

                        val += noise * mult;
                        div /= 2;
                        mult *= power;
                    }

                    *ptr = ColorBgra.Lerp(colorFrom, colorTo, (val + 1) / 2);
                    ++ptr;
                }
            }
        }

		protected override void Render (ImageSurface src, ImageSurface dst, Gdk.Rectangle roi)
		{
            var r = roi.ToCairoRectangle ();

            using (var temp = new ImageSurface (Format.Argb32, roi.Width, roi.Height)) {

                RenderClouds (temp, roi, Data.Scale, (byte)(Data.Seed ^ instanceSeed), Data.Power / 100.0, 
                              PintaCore.Palette.PrimaryColor.ToColorBgra (), PintaCore.Palette.SecondaryColor.ToColorBgra ());
                
                temp.MarkDirty ();

                // Have to lock because effect renderer is multithreaded
                lock (render_lock) {
                    using (var g = new Context (dst)) {
                        // - Clear any previous render from the destination
                        // - Copy the source to the destination
                        // - Blend the clouds over the source
                        g.Clear (r);
                        g.BlendSurface (src, r);
                        g.BlendSurface (temp, r.Location (), (BlendMode)CloudsData.BlendOps[Data.BlendMode]);
                    }
                }
            }
		}
		#endregion

		public class CloudsData : EffectData
		{
			[Skip]
			public override bool IsDefault { get { return Power == 0; } }

			[Caption ("Scale"), MinimumValue (2), MaximumValue (1000)]
			public int Scale = 250;

			[Caption ("Power"), MinimumValue (0), MaximumValue (100)]
			public int Power = 50;
			
			[Skip]
			public static Dictionary <string, object> BlendOps;

			[Skip]
			private static string defaultBlendOp;
			
			static CloudsData ()
			{
				BlendOps = new Dictionary<string, object> ();

				foreach (string name in UserBlendOps.GetAllBlendModeNames ())
				{
					BlendOps.Add (name, UserBlendOps.GetBlendModeByName (name));
				}
	            defaultBlendOp = UserBlendOps.GetBlendModeName (Pinta.Core.BlendMode.Normal);
			}
			
			[StaticList ("BlendOps")]
			public string BlendMode = defaultBlendOp;
			
			[Caption ("Seed"), MinimumValue (0), MaximumValue (255)]
			public int Seed = 0;

		}
	}
}
