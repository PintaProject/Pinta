/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core
{
    /// <summary>
    /// Adapted from: 
    /// "A Primer on Building a Color Picker User Control with GDI+ in Visual Basic .NET or C#"
    /// http://www.msdnaa.net/Resources/display.aspx?ResID=2460
    /// 
    /// This class is only used by the ColorsForm and ColorWheel. Nothing else in this program
    /// should be using it!
    /// </summary>
    [Serializable]
    public struct RgbColor
    {
        // All values are between 0 and 255.
        public int Red;
        public int Green;
        public int Blue;

        public RgbColor(int R, int G, int B) 
        {
#if DEBUG
            if (R < 0 || R > 255) 
            {
                throw new ArgumentOutOfRangeException("R", R, "R must corrospond to a byte value");
            }
            if (G < 0 || G > 255) 
            {
                throw new ArgumentOutOfRangeException("G", G, "G must corrospond to a byte value");
            }
            if (B < 0 || B > 255) 
            {
                throw new ArgumentOutOfRangeException("B", B, "B must corrospond to a byte value");
            }
#endif
            Red = R;
            Green = G;
            Blue = B;
        }

        public static RgbColor FromHsv(HsvColor hsv)
        {
            return hsv.ToRgb();
        }

//        public Color ToColor()
//        {
//            return Color.FromArgb(Red, Green, Blue);
//        }

        public HsvColor ToHsv()
        {
            // In this function, R, G, and B values must be scaled 
            // to be between 0 and 1.
            // HsvColor.Hue will be a value between 0 and 360, and 
            // HsvColor.Saturation and value are between 0 and 1.

            double min;
            double max;
            double delta;

            double r = (double) Red / 255;
            double g = (double) Green / 255;
            double b = (double) Blue / 255;

            double h;
            double s;
            double v;

            min = Math.Min(Math.Min(r, g), b);
            max = Math.Max(Math.Max(r, g), b);
            v = max;
            delta = max - min;

            if (max == 0 || delta == 0) 
            {
                // R, G, and B must be 0, or all the same.
                // In this case, S is 0, and H is undefined.
                // Using H = 0 is as good as any...
                s = 0;
                h = 0;
            } 
            else 
            {
                s = delta / max;
                if (r == max) 
                {
                    // Between Yellow and Magenta
                    h = (g - b) / delta;
                } 
                else if (g == max) 
                {
                    // Between Cyan and Yellow
                    h = 2 + (b - r) / delta;
                } 
                else 
                {
                    // Between Magenta and Cyan
                    h = 4 + (r - g) / delta;
                }

            }
            // Scale h to be between 0 and 360. 
            // This may require adding 360, if the value
            // is negative.
            h *= 60;

            if (h < 0)
            {
                h += 360;
            }

            // Scale to the requirements of this 
            // application. All values are between 0 and 255.
            return new HsvColor((int)h, (int)(s * 100), (int)(v * 100));
        }

        public override string  ToString() 
        {
            return String.Format("({0}, {1}, {2})", Red, Green, Blue);
        }
    } 

}
