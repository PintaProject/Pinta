/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace Pinta.Core
{
	public struct TextPosition : IComparable<TextPosition>
	{
		private int line;
		private int offset;

		public TextPosition (int line, int offset)
		{
			this.line = line;
			this.offset = offset;
		}

		public int Line {
			get { return line; }
			set { line = Math.Max (value, 0); }
		}

		public int Offset {
			get { return offset; }
			set { offset = Math.Max (value, 0); }
        }

        #region Operators
        public override bool Equals (object obj)
        {
            return obj is TextPosition && this == (TextPosition)obj;
        }

        public override int GetHashCode ()
        {
            return new { line, offset }.GetHashCode ();
        }

        public override string ToString ()
        {
            return string.Format("({0}, {1})", line, offset);
        }

        public static bool operator==(TextPosition x, TextPosition y)
        {
            return x.CompareTo (y) == 0;
        }

        public static bool operator!=(TextPosition x, TextPosition y)
        {
            return x.CompareTo (y) != 0;
        }

        public int CompareTo (TextPosition other)
        {
            if (line.CompareTo(other.line) != 0)
                return line.CompareTo (other.line);
            else
                return offset.CompareTo (other.offset);
        }

        public static TextPosition Max(TextPosition p1, TextPosition p2)
        {
            return (p1.CompareTo (p2) > 0) ? p1 : p2;
        }

        public static TextPosition Min(TextPosition p1, TextPosition p2)
        {
            return (p1.CompareTo (p2) < 0) ? p1 : p2;
        }
        #endregion
    }
}
