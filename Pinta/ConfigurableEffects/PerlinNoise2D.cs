/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

// Copyright (c) 2006-2008 Ed Harvey 
//
// MIT License: http://www.opensource.org/licenses/mit-license.php
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions: 
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software. 
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE. 
//

// Parts adapted to 2D from perlin's reference 3D implementation
// http://mrl.nyu.edu/~perlin/noise/


using System;
using System.Collections.Generic;
using System.Text;

namespace Pinta.Core
{
    internal static class PerlinNoise2D
    {
        // precalculated rotation matrix coefficients
        private static readonly double rot_11;
        private static readonly double rot_12;
        private static readonly double rot_21;
        private static readonly double rot_22;

        private static readonly int[] permuteLookup;

        private static readonly int[] permutationTable = 
            new int[]
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

        static PerlinNoise2D()
        {
            permuteLookup = new int[512];

            for (int i = 0; i < 256; i++)
            {
                permuteLookup[256 + i] = permutationTable[i];
                permuteLookup[i] = permutationTable[i];
            }

            // precalculate a rotation matrix - arbitary angle... 
            double angle = 137.2 / 180.0 * Math.PI;

            rot_11 = Math.Cos(angle);
            rot_12 = -Math.Sin(angle);
            rot_21 = Math.Sin(angle);
            rot_22 = Math.Cos(angle);
        }

        public static double Noise(double x, double y, double detail, double roughness, byte seed)
        {
            double total = 0.0;
            double frequency = 1;
            double amplitude = 1;

            double partialOctaveFactor = detail;
            int octaves = (int)Math.Ceiling(detail);

            for (int i = 0; i < octaves; i++)
            {
                // rotate the coordinates.
                // reduces correlation between octaves.
                double xr = ((x * rot_11) + (y * rot_12));
                double yr = ((x * rot_21) + (y * rot_22));

                double noise = Noise(xr * frequency, yr * frequency, seed);

                noise *= amplitude;

                // if this is the last 'partial' octave,
                // reduce its contribution accordingly.
                if (partialOctaveFactor < 1)
                {
                    noise *= partialOctaveFactor;
                }

                total += noise;

                // scale amplitude for next octave.
                amplitude = amplitude * roughness;

                // if the contribution is going to be negligable,
                // don't bother with higher octaves.
                if (amplitude < 0.001)
                {
                    break;
                }                

                // setup for next octave
                frequency += frequency;
                partialOctaveFactor -= 1.0;

                // offset the coordinates by prime numbers, with prime difference.
                // reduces correlation between octaves.
                x = xr + 499; 
                y = yr + 506;
            }

            return total;
        }

        private static double Fade(double t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static double Grad(int hash, double x, double y)
        {
            int h = hash & 15;
            double u = (h < 8) ? x : y;
            double v = (h < 4) ? y : ((h == 12 || h == 14) ? x : 0);

            return (((h & 1) == 0) ? u : -u) + (((h & 2) == 0) ? v : -v);
        }

        private static double Lerp(double a, double b, double t) 
        { 
            return a + t * (b - a); 
        }

        private static double Noise(double x, double y, byte seed)
        {
            double xf = Math.Floor(x);
            double yf = Math.Floor(y);

            int ix = (int)xf & 255; 
            int iy = (int)yf & 255;

            x -= xf; 
            y -= yf; 

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

            double edge1 = Lerp(gradAA, gradBA, u);

            double gradAB = Grad(permuteLookup[ab], x, y - 1);
            double gradBB = Grad(permuteLookup[bb], x - 1, y - 1);

            double edge2 = Lerp(gradAB, gradBB, u);

            double lerped = Lerp(edge1, edge2, v);

            return lerped;
        }
    }
}
