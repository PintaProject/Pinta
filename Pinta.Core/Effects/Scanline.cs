/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core
{
    public struct Scanline
    {
        private int x;
        private int y;
        private int length;

        public int X
        {
            get
            {
                return x;
            }
        }

        public int Y
        {
            get
            {
                return y;
            }
        }

        public int Length
        {
            get
            {
                return length;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return length.GetHashCode() + x.GetHashCode() + y.GetHashCode();
            }
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Scanline)
            {
                Scanline rhs = (Scanline)obj;
                return x == rhs.x && y == rhs.y && length == rhs.length;
            }
            else
            {
                return false;
            }
        }

        public static bool operator== (Scanline lhs, Scanline rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.length == rhs.length;
        }

        public static bool operator!= (Scanline lhs, Scanline rhs)
        {
            return !(lhs == rhs);
        }

        public override string ToString()
        {
            return "(" + x + "," + y + "):[" + length.ToString() + "]";
        }

        public Scanline(int x, int y, int length)
        {
            this.x = x;
            this.y = y;
            this.length = length;
        }
    }
}
