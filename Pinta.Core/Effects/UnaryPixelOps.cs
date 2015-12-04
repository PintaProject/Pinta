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
    /// Provides a set of standard UnaryPixelOps.
    /// </summary>
    public sealed class UnaryPixelOps
    {
        private UnaryPixelOps()
        {
        }

        /// <summary>
        /// Passes through the given color value.
        /// result(color) = color
        /// </summary>
        [Serializable]
        public class Identity
            : UnaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra color)
            {
                return color;
            }

            public unsafe override void Apply(ColorBgra *dst, ColorBgra *src, int length)
            {
		    for (int i = 0; i < length; i++) {
			*dst = *src;
			dst++;
			src++;
		    }
            }

            public unsafe override void Apply(ColorBgra* ptr, int length)
            {
                return;
            }
        }

        /// <summary>
        /// Always returns a constant color.
        /// </summary>
        [Serializable]
        public class Constant
            : UnaryPixelOp
        {
            private ColorBgra setColor;

            public override ColorBgra Apply(ColorBgra color)
            {
                return setColor;
            }

            public unsafe override void Apply(ColorBgra* dst, ColorBgra* src, int length)
            {
                while (length > 0)
                {
                    *dst = setColor;
                    ++dst;
                    --length;
                }
            }

            public unsafe override void Apply(ColorBgra* ptr, int length)
            {
                while (length > 0)
                {
                    *ptr = setColor;
                    ++ptr;
                    --length;
                }
            }

            public Constant(ColorBgra setColor)
            {
                this.setColor = setColor;
            }
        }

        /// <summary>
        /// Blends pixels with the specified constant color.
        /// </summary>
        [Serializable]
        public class BlendConstant
            : UnaryPixelOp
        {
            private ColorBgra blendColor;

            public override ColorBgra Apply(ColorBgra color)
            {
                int a = blendColor.A;
                int invA = 255 - a;

                int r = ((color.R * invA) + (blendColor.R * a)) / 256;
                int g = ((color.G * invA) + (blendColor.G * a)) / 256;
                int b = ((color.B * invA) + (blendColor.B * a)) / 256;
                byte a2 = ComputeAlpha(color.A, blendColor.A);

                return ColorBgra.FromBgra((byte)b, (byte)g, (byte)r, a2);
            }

            public BlendConstant(ColorBgra blendColor)
            {
                this.blendColor = blendColor;
            }
        }

        /// <summary>
        /// Used to set a given channel of a pixel to a given, predefined color.
        /// Useful if you want to set only the alpha value of a given region.
        /// </summary>
        [Serializable]
        public class SetChannel
            : UnaryPixelOp
        {
            private int channel;
            private byte setValue;

            public override ColorBgra Apply(ColorBgra color)
            {
                color[channel] = setValue;
                return color;
            }

            public override unsafe void Apply(ColorBgra* dst, ColorBgra* src, int length)
            {
                while (length > 0)
                {
                    *dst = *src;
                    (*dst)[channel] = setValue;
                    ++dst;
                    ++src;
                    --length;
                }
            }

            public override unsafe void Apply(ColorBgra* ptr, int length)
            {
                while (length > 0)
                {
                    (*ptr)[channel] = setValue;
                    ++ptr;
                    --length;
                }
            }


            public SetChannel(int channel, byte setValue)
            {
                this.channel = channel;
                this.setValue = setValue;
            }
        }

        /// <summary>
        /// Specialization of SetChannel that sets the alpha channel.
        /// </summary>
        /// <remarks>This class depends on the system being litte-endian with the alpha channel 
        /// occupying the 8 most-significant-bits of a ColorBgra instance.
        /// By the way, we use addition instead of bitwise-OR because an addition can be
        /// perform very fast (0.5 cycles) on a Pentium 4.</remarks>
        [Serializable]
        public class SetAlphaChannel
            : UnaryPixelOp
        {
            private UInt32 addValue;

            public override ColorBgra Apply(ColorBgra color)
            {
                return ColorBgra.FromUInt32((color.Bgra & 0x00ffffff) + addValue);
            }

            public override unsafe void Apply(ColorBgra* dst, ColorBgra* src, int length)
            {
                while (length > 0)
                {
                    dst->Bgra = (src->Bgra & 0x00ffffff) + addValue;
                    ++dst;
                    ++src;
                    --length;
                }
            }

            public override unsafe void Apply(ColorBgra* ptr, int length)
            {
                while (length > 0)
                {
                    ptr->Bgra = (ptr->Bgra & 0x00ffffff) + addValue;
                    ++ptr;
                    --length;
                }
            }

            public SetAlphaChannel(byte alphaValue)
            {
                addValue = (uint)alphaValue << 24;
            }
        }

        /// <summary>
        /// Specialization of SetAlphaChannel that always sets alpha to 255.
        /// </summary>
        [Serializable]
        public class SetAlphaChannelTo255
            : UnaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra color)
            {
                return ColorBgra.FromUInt32(color.Bgra | 0xff000000);
            }

            public override unsafe void Apply(ColorBgra* dst, ColorBgra* src, int length)
            {
                while (length > 0)
                {
                    dst->Bgra = src->Bgra | 0xff000000;
                    ++dst;
                    ++src;
                    --length;
                }
            }

            public override unsafe void Apply(ColorBgra* ptr, int length)
            {
                while (length > 0)
                {
                    ptr->Bgra |= 0xff000000;
                    ++ptr;
                    --length;
                }
            }
        }

        /// <summary>
        /// Inverts a pixel's color, and passes through the alpha component.
        /// </summary>
        [Serializable]
        public class Invert
            : UnaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra color)
            {
                //Note: Cairo images use premultiplied alpha values
                //The formula for changing B would be: (255 - B * 255 / A) * A / 255
                //This can be simplified to: A - B
                return ColorBgra.FromBgra((byte)(color.A - color.B), (byte)(color.A - color.G), (byte)(color.A - color.R), color.A);
            }
        }

        /// <summary>
        /// If the color is within the red tolerance, remove it
        /// </summary>
        [Serializable]
        public class RedEyeRemove
            : UnaryPixelOp
        {
            private int tolerence;
            private double setSaturation;

            public RedEyeRemove(int tol, int sat)
            {
                tolerence = tol;
                setSaturation = (double)sat / 100;
            }

            public override ColorBgra Apply(ColorBgra color)
            {
                // The higher the saturation, the more red it is
                int saturation = GetSaturation(color);

                // The higher the difference between the other colors, the more red it is
                int difference = color.R - Math.Max(color.B,color.G);

                // If it is within tolerence, and the saturation is high
                if ((difference > tolerence) && (saturation > 100)) 
                {
                    double i = 255.0 * color.GetIntensity();
                    byte ib = (byte)(i * setSaturation); // adjust the red color for user inputted saturation
                    return ColorBgra.FromBgra((byte)color.B,(byte)color.G, ib, color.A);
                }
                else
                {
                    return color;
                }
            }

            //Saturation formula from RgbColor.cs, public HsvColor ToHsv()
            private int GetSaturation(ColorBgra color)
            {
                double min;
                double max;
                double delta;

                double r = (double) color.R / 255;
                double g = (double) color.G / 255;
                double b = (double) color.B / 255;

                double s;

                min = Math.Min(Math.Min(r, g), b);
                max = Math.Max(Math.Max(r, g), b);
                delta = max - min;

                if (max == 0 || delta == 0) 
                {
                    // R, G, and B must be 0, or all the same.
                    // In this case, S is 0, and H is undefined.
                    // Using H = 0 is as good as any...
                    s = 0;
                } 
                else
                {
                    s = delta / max;
                }

                return (int)(s * 255);
            }               
        }

        /// <summary>
        /// Inverts a pixel's color and its alpha component.
        /// </summary>
        [Serializable]
        public class InvertWithAlpha
            : UnaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra color)
            {
                return ColorBgra.FromBgra((byte)(255 - color.B), (byte)(255 - color.G), (byte)(255 - color.R), (byte)(255 - color.A));
            }
        }

        /// <summary>
        /// Averages the input color's red, green, and blue channels. The alpha component
        /// is unaffected.
        /// </summary>
        [Serializable]
        public class AverageChannels
            : UnaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra color)
            {
                byte average = (byte)(((int)color.R + (int)color.G + (int)color.B) / 3);
                return ColorBgra.FromBgra(average, average, average, color.A);
            }
        }

        [Serializable]
        public class Desaturate
            : UnaryPixelOp
        {
            public override ColorBgra Apply(ColorBgra color)
            {
                byte i = color.GetIntensityByte();
                return ColorBgra.FromBgra(i, i, i, color.A);
            }

            public unsafe override void Apply(ColorBgra* ptr, int length)
            {
                while (length > 0)
                {
                    byte i = ptr->GetIntensityByte();

                    ptr->R = i;
                    ptr->G = i;
                    ptr->B = i;

                    ++ptr;
                    --length;
                }
            }

            public unsafe override void Apply(ColorBgra* dst, ColorBgra* src, int length)
            {
                while (length > 0)
                {
                    byte i = src->GetIntensityByte();

                    dst->B = i;
                    dst->G = i;
                    dst->R = i;
                    dst->A = src->A;

                    ++dst;
                    ++src;
                    --length;
                }
            }
        }

        [Serializable]
        public class LuminosityCurve
            : UnaryPixelOp
        {
            public byte[] Curve = new byte[256];

            public LuminosityCurve()
            {    
                for (int i = 0; i < 256; ++i)
                {
                    Curve[i] = (byte)i;
                }
            }

            public override ColorBgra Apply(ColorBgra color)
            {
                byte lumi = color.GetIntensityByte();
                int diff = Curve[lumi] - lumi;

                return ColorBgra.FromBgraClamped(
                    color.B + diff,
                    color.G + diff,
                    color.R + diff,
                    color.A);
            }
        }

        [Serializable]
        public class ChannelCurve
            : UnaryPixelOp
        {
            public byte[] CurveB = new byte[256];
            public byte[] CurveG = new byte[256];
            public byte[] CurveR = new byte[256];

            public ChannelCurve()
            {
                for (int i = 0; i < 256; ++i)
                {
                    CurveB[i] = (byte)i;
                    CurveG[i] = (byte)i;
                    CurveR[i] = (byte)i;
                }
            }

            public override unsafe void Apply(ColorBgra* dst, ColorBgra* src, int length)
            {
                while (--length >= 0)
                {
                    dst->B = CurveB[src->B];
                    dst->G = CurveG[src->G];
                    dst->R = CurveR[src->R];
                    dst->A = src->A;

                    ++dst;
                    ++src;
                }
            }

            public override unsafe void Apply(ColorBgra* ptr, int length)
            {
                while (--length >= 0)
                {
                    ptr->B = CurveB[ptr->B];
                    ptr->G = CurveG[ptr->G];
                    ptr->R = CurveR[ptr->R];

                    ++ptr;
                }
            }

            public override ColorBgra Apply(ColorBgra color)
            {
                return ColorBgra.FromBgra(CurveB[color.B], CurveG[color.G], CurveR[color.R], color.A);
            }

//            public override void Apply(Surface dst, Point dstOffset, Surface src, Point srcOffset, int scanLength)
//            {
//                base.Apply (dst, dstOffset, src, srcOffset, scanLength);
//            }
        }

        [Serializable]
        public class Level
            : ChannelCurve,
              ICloneable
        {
            private ColorBgra colorInLow;
            public ColorBgra ColorInLow 
            {
                get 
                {
                    return colorInLow; 
                }

                set 
                {
                    if (value.R == 255) 
                    {
                        value.R = 254;
                    }

                    if (value.G == 255)
                    {
                        value.G = 254;
                    }

                    if (value.B == 255)
                    {
                        value.B = 254;
                    }

                    if (colorInHigh.R < value.R + 1) 
                    {
                        colorInHigh.R = (byte)(value.R + 1);
                    }

                    if (colorInHigh.G < value.G + 1) 
                    {
                        colorInHigh.G = (byte)(value.R + 1);
                    }

                    if (colorInHigh.B < value.B + 1) 
                    {
                        colorInHigh.B = (byte)(value.R + 1);
                    }

                    colorInLow = value;
                    UpdateLookupTable();
                }
            }

            private ColorBgra colorInHigh;
            public ColorBgra ColorInHigh 
            {
                get 
                {
                    return colorInHigh;
                }

                set 
                {
                    if (value.R == 0) 
                    {
                        value.R = 1;
                    }

                    if (value.G == 0)
                    { 
                        value.G = 1;
                    }

                    if (value.B == 0)
                    {
                        value.B = 1;
                    }

                    if (colorInLow.R > value.R - 1) 
                    {
                        colorInLow.R = (byte)(value.R - 1);
                    }

                    if (colorInLow.G > value.G - 1) 
                    {
                        colorInLow.G = (byte)(value.R - 1);
                    }

                    if (colorInLow.B > value.B - 1) 
                    {
                        colorInLow.B = (byte)(value.R - 1);
                    }

                    colorInHigh = value;
                    UpdateLookupTable();
                }
            }

            private ColorBgra colorOutLow;
            public ColorBgra ColorOutLow 
            {
                get 
                {
                    return colorOutLow;
                }

                set 
                {
                    if (value.R == 255) 
                    {
                        value.R = 254;
                    }

                    if (value.G == 255)
                    {
                        value.G = 254;
                    }

                    if (value.B == 255)
                    {
                        value.B = 254;
                    }

                    if (colorOutHigh.R < value.R + 1) 
                    {
                        colorOutHigh.R = (byte)(value.R + 1);
                    }

                    if (colorOutHigh.G < value.G + 1) 
                    {
                        colorOutHigh.G = (byte)(value.G + 1);
                    }

                    if (colorOutHigh.B < value.B + 1) 
                    {
                        colorOutHigh.B = (byte)(value.B + 1);
                    }

                    colorOutLow = value;
                    UpdateLookupTable();
                }
            }

            private ColorBgra colorOutHigh;
            public ColorBgra ColorOutHigh 
            {
                get 
                {
                    return colorOutHigh;
                }

                set 
                {
                    if (value.R == 0) 
                    {
                        value.R = 1;
                    }

                    if (value.G == 0)
                    { 
                        value.G = 1;
                    }

                    if (value.B == 0)
                    {
                        value.B = 1;
                    }

                    if (colorOutLow.R > value.R - 1) 
                    {
                        colorOutLow.R = (byte)(value.R - 1);
                    }

                    if (colorOutLow.G > value.G - 1) 
                    {
                        colorOutLow.G = (byte)(value.G - 1);
                    }

                    if (colorOutLow.B > value.B - 1) 
                    {
                        colorOutLow.B = (byte)(value.B - 1);
                    }

                    colorOutHigh = value;
                    UpdateLookupTable();
                }       
            }               
                        
            private float[] gamma = new float[3];
            public float GetGamma(int index) 
            {               
                if (index < 0 || index >= 3) 
                {
                    throw new ArgumentOutOfRangeException("index", index, "Index must be between 0 and 2");
                }

                return gamma[index];
            }

            public void SetGamma(int index, float val) 
            {
                if (index < 0 || index >= 3) 
                {
                    throw new ArgumentOutOfRangeException("index", index, "Index must be between 0 and 2");
                }

                gamma[index] = Utility.Clamp(val, 0.1f, 10.0f);
                UpdateLookupTable();
            }

            public bool isValid = true;

            public static Level AutoFromLoMdHi(ColorBgra lo, ColorBgra md, ColorBgra hi) 
            {
                float[] gamma = new float[3];

                for (int i = 0; i < 3; i++)
                {
                    if (lo[i] < md[i] && md[i] < hi[i])
                    {
                        gamma[i] = (float)Utility.Clamp(Math.Log(0.5, (float)(md[i] - lo[i]) / (float)(hi[i] - lo[i])), 0.1, 10.0);
                    }
                    else
                    {
                        gamma[i] = 1.0f;
                    }
                }

                return new Level(lo, hi, gamma, ColorBgra.Black, ColorBgra.White);
            }

            private void UpdateLookupTable() 
            {
                for (int i = 0; i < 3; i++) 
                {
                    if (colorOutHigh[i] < colorOutLow[i] ||
                        colorInHigh[i] <= colorInLow[i] ||
                        gamma[i] < 0)
                    {
                        isValid = false;
                        return;
                    }

                    for (int j = 0; j < 256; j++) 
                    {
                        ColorBgra col = Apply(j, j, j);
                        CurveB[j] = col.B;
                        CurveG[j] = col.G;
                        CurveR[j] = col.R;
                    }
                }
            }

            public Level() 
                : this(ColorBgra.Black,
                       ColorBgra.White,
                       new float[] { 1, 1, 1 },
                       ColorBgra.Black,
                       ColorBgra.White)
            {
            }

            public Level(ColorBgra in_lo, ColorBgra in_hi, float[] gamma, ColorBgra out_lo, ColorBgra out_hi)
            {
                colorInLow = in_lo;
                colorInHigh = in_hi;
                colorOutLow = out_lo;
                colorOutHigh = out_hi;

                if (gamma.Length != 3) 
                {
                    throw new ArgumentException("gamma", "gamma must be a float[3]");
                }

                this.gamma = gamma;
                UpdateLookupTable();
            }

            public ColorBgra Apply(float r, float g, float b) 
            {
                ColorBgra ret = new ColorBgra();
                float[] input = new float[] { b, g, r };

                for (int i = 0; i < 3; i++) 
                {
                    float v = (input[i] - colorInLow[i]);

                    if (v < 0)
                    {
                        ret[i] = colorOutLow[i];
                    }
                    else if (v + colorInLow[i] >= colorInHigh[i])
                    {
                        ret[i] = colorOutHigh[i];
                    }
                    else
                    {
                        ret[i] = (byte)Utility.Clamp(
                            colorOutLow[i] + (colorOutHigh[i] - colorOutLow[i]) * Math.Pow(v / (colorInHigh[i] - colorInLow[i]), gamma[i]),
                            0.0f,
                            255.0f);
                    }
                }

                return ret;
            }

            public void UnApply(ColorBgra after, float[] beforeOut, float[] slopesOut) 
            {
                if (beforeOut.Length != 3) 
                {
                    throw new ArgumentException("before must be a float[3]", "before");
                }

                if (slopesOut.Length != 3) 
                {
                    throw new ArgumentException("slopes must be a float[3]", "slopes");
                }

                for (int i = 0; i < 3; i++) 
                {
                    beforeOut[i] = colorInLow[i] + (colorInHigh[i] - colorInLow[i]) *
                        (float)Math.Pow((float)(after[i] - colorOutLow[i]) / (colorOutHigh[i] - colorOutLow[i]), 1 / gamma[i]);

                    slopesOut[i] = (float)(colorInHigh[i] - colorInLow[i]) / ((colorOutHigh[i] - colorOutLow[i]) * gamma[i]) *
                        (float)Math.Pow((float)(after[i] - colorOutLow[i]) / (colorOutHigh[i] - colorOutLow[i]), 1 / gamma[i] - 1);

                    if (float.IsInfinity(slopesOut[i]) || float.IsNaN(slopesOut[i])) 
                    {
                        slopesOut[i] = 0;
                    }
                }
            }

            public object Clone()
            {
                Level copy = new Level(colorInLow, colorInHigh, (float[])gamma.Clone(), colorOutLow, colorOutHigh);

                copy.CurveB = (byte[])this.CurveB.Clone();
                copy.CurveG = (byte[])this.CurveG.Clone();
                copy.CurveR = (byte[])this.CurveR.Clone();

                return copy;
            }
        }

        [Serializable]
        public class HueSaturationLightness
            : UnaryPixelOp
        {
            private int hueDelta;
            private int satFactor;
            private UnaryPixelOp blendOp;

            public HueSaturationLightness(int hueDelta, int satDelta, int lightness)
            {
                this.hueDelta = hueDelta;
                this.satFactor = (satDelta * 1024) / 100;

                if (lightness == 0)
                {
                    blendOp = new UnaryPixelOps.Identity();
                }
                else if (lightness > 0)
                {
                    blendOp = new UnaryPixelOps.BlendConstant(ColorBgra.FromBgra(255, 255, 255, (byte)((lightness * 255) / 100)));
                }
                else // if (lightness < 0)
                {
                    blendOp = new UnaryPixelOps.BlendConstant(ColorBgra.FromBgra(0, 0, 0, (byte)((-lightness * 255) / 100)));
                }
            }
            
			public override ColorBgra Apply (ColorBgra color)
            {
            	//adjust saturation
	            byte intensity = color.GetIntensityByte();
	            color.R = Utility.ClampToByte((intensity * 1024 + (color.R - intensity) * satFactor) >> 10);
	            color.G = Utility.ClampToByte((intensity * 1024 + (color.G - intensity) * satFactor) >> 10);
	            color.B = Utility.ClampToByte((intensity * 1024 + (color.B - intensity) * satFactor) >> 10);
	
	            HsvColor  hsvColor = (new RgbColor(color.R, color.G, color.B)).ToHsv();
				int hue = hsvColor.Hue;
	
	            hue += hueDelta;
	
	            while (hue < 0)
                {
                	hue += 360;
                }
                       
                while (hue > 360)
                {
                	hue -= 360;
                }
	
	            hsvColor.Hue = hue;
	
				RgbColor rgbColor=hsvColor.ToRgb();
				ColorBgra newColor = ColorBgra.FromBgr((byte)rgbColor.Blue, (byte)rgbColor.Green, (byte)rgbColor.Red);
	            newColor = blendOp.Apply(newColor);
	            newColor.A = color.A;
	              
	            return newColor;
            }
            
        }
		
		[Serializable]
		public class PosterizePixel
            : UnaryPixelOp
        {
            private byte[] redLevels;
            private byte[] greenLevels;
            private byte[] blueLevels;

            public PosterizePixel(int red, int green, int blue)
            {
                this.redLevels = CalcLevels(red);
                this.greenLevels = CalcLevels(green);
                this.blueLevels = CalcLevels(blue);
            }

            private static byte[] CalcLevels(int levelCount)
            {
                byte[] t1 = new byte[levelCount];

                for (int i = 1; i < levelCount; i++)
                {
                    t1[i] = (byte)((255 * i) / (levelCount - 1));
                }

                byte[] levels = new byte[256];

                int j = 0;
                int k = 0;

                for (int i = 0; i < 256; i++)
                {
                    levels[i] = t1[j];

                    k += levelCount;

                    if (k > 255)
                    {
                        k -= 255;
                        j++;
                    }
                }

                return levels;
            }

            public override ColorBgra Apply(ColorBgra color)
            {
                return ColorBgra.FromBgra(blueLevels[color.B], greenLevels[color.G], redLevels[color.R], color.A);
            }

            public unsafe override void Apply(ColorBgra* ptr, int length)
            {
                while (length > 0)
                {
                    ptr->B = this.blueLevels[ptr->B];
                    ptr->G = this.greenLevels[ptr->G];
                    ptr->R = this.redLevels[ptr->R];

                    ++ptr;
                    --length;
                }
            }

            public unsafe override void Apply(ColorBgra* dst, ColorBgra* src, int length)
            {
                while (length > 0)
                {
                    dst->B = this.blueLevels[src->B];
                    dst->G = this.greenLevels[src->G];
                    dst->R = this.redLevels[src->R];
                    dst->A = src->A;

                    ++dst;
                    ++src;
                    --length;
                }
            }
        }

    }
}
