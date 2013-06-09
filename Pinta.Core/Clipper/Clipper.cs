//
//
// Author    :  Angus Johnson                                                   *
// Version   :  4.8.4                                                           *
// Date      :  1 June 2012                                                     *
// Website   :  http://www.angusj.com                                           *
// Copyright :  Angus Johnson 2010-2012                                         *
//                                                                              *
// License:                                                                     *
// Use, modification & distribution is subject to Boost Software License Ver 1. *
// http://www.boost.org/LICENSE_1_0.txt                                         *
//                                                                              *
// Attributions:                                                                *
// The code in this library is an extension of Bala Vatti's clipping algorithm: *
// "A generic solution to polygon clipping"                                     *
// Communications of the ACM, Vol 35, Issue 7 (July 1992) pp 56-63.             *
// http://portal.acm.org/citation.cfm?id=129906                                 *
//                                                                              *
// Computer graphics and geometric modeling: implementation and algorithms      *
// By Max K. Agoston                                                            *
// Springer; 1 edition (January 4, 2005)                                        *
// http://books.google.com/books?q=vatti+clipping+agoston                       *
//                                                                              *
// See also:                                                                    *
// "Polygon Offsetting by Computing Winding Numbers"                            *
// Paper no. DETC2005-85513 pp. 565-575                                         *
// ASME 2005 International Design Engineering Technical Conferences             *
// and Computers and Information in Engineering Conference (IDETC/CIE2005)      *
// September 24–28, 2005 , Long Beach, California, USA                          *
// http://www.me.berkeley.edu/~mcmains/pubs/DAC05OffsetPolygon.pdf              *
//                                                                              *
//

//
// This is a translation of the Delphi Clipper library and the naming style
// used has retained a Delphi flavour.
//

//
// This Clipper Library code was imported into Pinta on June 6th, 2012 by
// Andrew Davis of GSoC 2012 for the MagicWandTool selection modes
// implementation. It calculates "boolean clipping operations" (union,
// difference (exclude), xor, and intersect) on any type, number, and/or
// combination of polygons. It is independent C# code that strictly serves
// this purpose. More information can be found here:
//
// http://www.angusj.com/delphi/clipper.php
// http://sourceforge.net/projects/polyclipping/
//
// - Andrew Davis
//

using System;
using System.Collections.Generic;

namespace ClipperLibrary
{

    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;
    using ExPolygons = List<ExPolygon>;
        
    //------------------------------------------------------------------------------
    // Int128 struct (enables safe math on signed 64bit integers)
    // eg Int128 val1((Int64)9223372036854775807); //ie 2^63 -1
    //    Int128 val2((Int64)9223372036854775807);
    //    Int128 val3 = val1 * val2;
    //    val3.ToString => "85070591730234615847396907784232501249" (8.5e+37)
    //------------------------------------------------------------------------------

    internal struct Int128
    {
        private Int64 hi;
        private Int64 lo;

        public Int128(Int64 lo)
        {
            this.lo = lo;
            if (lo < 0)
                this.hi = -1;
            else
                this.hi = 0;
        }

		public Int128(Int64 lo, Int64 hi)
		{
			this.lo = lo;
			this.hi = hi;
		}
 
        public Int128(Int128 val)
        {
            hi = val.hi;
            lo = val.lo;
        }

        public bool IsNegative()
        {
            return hi < 0;
        }

        public static bool operator ==(Int128 val1, Int128 val2)
        {
            if ((object)val1 == (object)val2) return true;
            else if ((object)val1 == null || (object)val2 == null) return false;
            return (val1.hi == val2.hi && val1.lo == val2.lo);
        }

        public static bool operator!= (Int128 val1, Int128 val2) 
        { 
            return !(val1 == val2); 
        }

        public override bool Equals(System.Object obj)
        {
            if (obj == null || !(obj is Int128))
	            return false;
            Int128 i128 = (Int128)obj;
            return (i128.hi == hi && i128.lo == lo);
        }

        public override int GetHashCode()
        {
            return hi.GetHashCode() ^ lo.GetHashCode();
        }

        public static bool operator> (Int128 val1, Int128 val2) 
        {
            if (val1.hi != val2.hi)
                return val1.hi > val2.hi;
            else
                return val1.lo > val2.lo;
        }

        public static bool operator< (Int128 val1, Int128 val2) 
        {
            if (val1.hi != val2.hi)
                return val1.hi < val2.hi;
            else
                return val1.lo < val2.lo;
        }

        public static Int128 operator+ (Int128 lhs, Int128 rhs) 
        {
            lhs.hi += rhs.hi;
            lhs.lo += rhs.lo;
            if ( (UInt64)lhs.lo < (UInt64)rhs.lo) lhs.hi++;
            return lhs;
        }

        public static Int128 operator- (Int128 lhs, Int128 rhs) 
        {
            return lhs + -rhs;
        }

		public static Int128 operator -(Int128 val)
		{
			if (val.lo == 0) {
                if (val.hi == 0) return val;
                return new Int128(0, -val.hi);
			} 
            else return new Int128(-val.lo, ~val.hi);
		}

        //nb: Constructing two new Int128 objects every time we want to multiply longs  
        //is slow. So, although calling the Int128Mul method doesn't look as clean, the 
        //code runs significantly faster than if we'd used the * operator.
        
        public static Int128 Int128Mul(Int64 lhs, Int64 rhs)
        {
            bool negate = (lhs < 0) != (rhs < 0);
            if (lhs < 0) lhs = -lhs;
            if (rhs < 0) rhs = -rhs;
            UInt64 int1Hi = (UInt64)lhs >> 32;
            UInt64 int1Lo = (UInt64)lhs & 0xFFFFFFFF;
            UInt64 int2Hi = (UInt64)rhs >> 32;
            UInt64 int2Lo = (UInt64)rhs & 0xFFFFFFFF;

            //nb: see comments in clipper.pas
            UInt64 a = int1Hi * int2Hi;
            UInt64 b = int1Lo * int2Lo;
            UInt64 c = int1Hi * int2Lo + int1Lo * int2Hi; 

            Int64 lo, hi;
            hi = (Int64)(a + (c >> 32));

            lo = (Int64)(c << 32);
            lo += (Int64)b;
            if ((UInt64)lo < b) hi++;
            var result = new Int128(lo, hi);
            return negate ? -result : result;            
        }

        public static Int128 operator /(Int128 lhs, Int128 rhs)
        {
            if (rhs.lo == 0 && rhs.hi == 0)
                throw new ClipperException("Int128: divide by zero");
            bool negate = (rhs.hi < 0) != (lhs.hi < 0);
            Int128 result = new Int128(lhs), denom = new Int128(rhs);
            if (result.hi < 0) result = -result;
            if (denom.hi < 0) denom = -denom;
            if (denom > result) return new Int128(0); //result is only a fraction of 1
            denom = -denom;

            Int128 p = new Int128(0);
            for (int i = 0; i < 128; ++i)
            {
                p.hi = p.hi << 1;
                if (p.lo < 0) p.hi++;
                p.lo = (Int64)p.lo << 1;
                if (result.hi < 0) p.lo++;
                result.hi = result.hi << 1;
                if (result.lo < 0) result.hi++;
                result.lo = (Int64)result.lo << 1;
                if (p.hi >= 0)
                {
                    p += denom;
                    result.lo++;
                }
            }
            return negate ? -result : result;
        }

        public double ToDouble()
        {
            const double shift64 = 18446744073709551616.0; //2^64
            const double bit64 = 9223372036854775808.0; 
            if (hi < 0)
            {
                Int128 tmp = new Int128(this);
                tmp = -tmp;
                if (tmp.lo < 0)
                    return (double)tmp.lo - bit64 - tmp.hi * shift64;
                else
                    return -(double)tmp.lo - tmp.hi * shift64;
            }
            else if (lo < 0)
                return -(double)lo + bit64 + hi * shift64;
            else
                return (double)lo + (double)hi * shift64;
        }

        ////for bug testing ...
        //public override string ToString()
        //{
        //    int r = 0;
        //    Int128 tmp = new Int128(0), val = new Int128(this);
        //    if (hi < 0) Negate(val);
        //    StringBuilder builder = new StringBuilder(50);
        //    while (val.hi != 0 || val.lo != 0)
        //    {
        //        Div10(val, ref tmp, ref r);
        //        builder.Insert(0, (char)('0' + r));
        //        val = tmp;
        //    }
        //    if (hi < 0) return '-' + builder.ToString();
        //    if (builder.Length == 0) return "0";
        //    return builder.ToString();
        //}

        ////debugging only ...
        //private void Div10(Int128 val, ref Int128 result, ref int remainder)
        //{
        //    remainder = 0;
        //    result = new Int128(0);
        //    for (int i = 63; i >= 0; --i)
        //    {
        //        if ((val.hi & ((Int64)1 << i)) != 0)
        //            remainder = (remainder * 2) + 1;
        //        else
        //            remainder *= 2;
        //        if (remainder >= 10)
        //        {
        //            result.hi += ((Int64)1 << i);
        //            remainder -= 10;
        //        }
        //    }
        //    for (int i = 63; i >= 0; --i)
        //    {
        //        if ((val.lo & ((Int64)1 << i)) != 0)
        //            remainder = (remainder * 2) + 1;
        //        else
        //            remainder *= 2;
        //        if (remainder >= 10)
        //        {
        //            result.lo += ((Int64)1 << i);
        //            remainder -= 10;
        //        }
        //    }
        //}
    };

    //------------------------------------------------------------------------------
    //------------------------------------------------------------------------------
   
    public struct IntPoint
    {
        public Int64 X;
        public Int64 Y;
        
        public IntPoint(Int64 X, Int64 Y)
        {
            this.X = X; this.Y = Y;
        }
        
        public IntPoint(IntPoint pt)
        {
            this.X = pt.X; this.Y = pt.Y;
        }
    }

    public struct IntRect
    {
        public Int64 left;
        public Int64 top;
        public Int64 right;
        public Int64 bottom;

        public IntRect(Int64 l, Int64 t, Int64 r, Int64 b)
        {
            this.left = l; this.top = t;
            this.right = r; this.bottom = b;
        }
    }

    public struct ExPolygon
    {
        public Polygon outer;
        public Polygons holes;
    }

    public enum ClipType { ctIntersection, ctUnion, ctDifference, ctXor };
    public enum PolyType { ptSubject, ptClip };
    //By far the most widely used winding rules for polygon filling are
    //EvenOdd & NonZero (GDI, GDI+, XLib, OpenGL, Cairo, AGG, Quartz, SVG, Gr32)
    //Others rules include Positive, Negative and ABS_GTR_EQ_TWO (only in OpenGL)
    //see http://glprogramming.com/red/chapter11.html
    public enum PolyFillType { pftEvenOdd, pftNonZero, pftPositive, pftNegative };
    public enum JoinType { jtSquare, jtRound, jtMiter };


    [Flags]
    internal enum EdgeSide { esNeither = 0, esLeft = 1, esRight = 2, esBoth = 3 };
    [Flags]
    internal enum Protects { ipNone = 0, ipLeft = 1, ipRight = 2, ipBoth = 3 };
    internal enum Direction { dRightToLeft, dLeftToRight };
    
    internal class TEdge {
        public Int64 xbot;
        public Int64 ybot;
        public Int64 xcurr;
        public Int64 ycurr;
        public Int64 xtop;
        public Int64 ytop;
        public double dx;
        public Int64 tmpX;
        public PolyType polyType;
        public EdgeSide side;
        public int windDelta; //1 or -1 depending on winding direction
        public int windCnt;
        public int windCnt2; //winding count of the opposite polytype
        public int outIdx;
        public TEdge next;
        public TEdge prev;
        public TEdge nextInLML;
        public TEdge nextInAEL;
        public TEdge prevInAEL;
        public TEdge nextInSEL;
        public TEdge prevInSEL;
    };

    internal class IntersectNode
    {
        public TEdge edge1;
        public TEdge edge2;
        public IntPoint pt;
        public IntersectNode next;
    };

    internal class LocalMinima
    {
        public Int64 Y;
        public TEdge leftBound;
        public TEdge rightBound;
        public LocalMinima next;
    };

    internal class Scanbeam
    {
        public Int64 Y;
        public Scanbeam next;
    };

    internal class OutRec
    {
        public int idx;
        public bool isHole;
        public OutRec FirstLeft;
        public OutRec AppendLink;
        public OutPt pts;
        public OutPt bottomPt;
        public OutPt bottomFlag;
        public EdgeSide sides;
    };

    internal class OutPt
    {
        public int idx;
        public IntPoint pt;
        public OutPt next;
        public OutPt prev;
    };

    internal class JoinRec
    {
        public IntPoint pt1a;
        public IntPoint pt1b;
        public int poly1Idx;
        public IntPoint pt2a;
        public IntPoint pt2b;
        public int poly2Idx;
    };

    internal class HorzJoinRec
    {
        public TEdge edge;
        public int savedIdx;
    };

    public class ClipperBase
    {
        protected const double horizontal = -3.4E+38;
        internal const Int64 loRange = 1518500249;           //sqrt(2^63 -1)/2
        internal const Int64 hiRange = 6521908912666391106L; //sqrt(2^127 -1)/2

        internal LocalMinima m_MinimaList;
        internal LocalMinima m_CurrentLM;
        internal List<List<TEdge>> m_edges = new List<List<TEdge>>();
        internal bool m_UseFullRange;

        //------------------------------------------------------------------------------

        protected static bool PointsEqual(IntPoint pt1, IntPoint pt2)
        {
          return ( pt1.X == pt2.X && pt1.Y == pt2.Y );
        }
        //------------------------------------------------------------------------------

        internal bool PointIsVertex(IntPoint pt, OutPt pp)
        {
          OutPt pp2 = pp;
          do
          {
            if (PointsEqual(pp2.pt, pt)) return true;
            pp2 = pp2.next;
          }
          while (pp2 != pp);
          return false;
        }
        //------------------------------------------------------------------------------

        internal bool PointInPolygon(IntPoint pt, OutPt pp, bool UseFulllongRange)
        {
          OutPt pp2 = pp;
          bool result = false;
          if (UseFulllongRange)
          {
              do
              {
                  if ((((pp2.pt.Y <= pt.Y) && (pt.Y < pp2.prev.pt.Y)) ||
                      ((pp2.prev.pt.Y <= pt.Y) && (pt.Y < pp2.pt.Y))) &&
                      new Int128(pt.X - pp2.pt.X) < 
                      Int128.Int128Mul(pp2.prev.pt.X - pp2.pt.X,  pt.Y - pp2.pt.Y) / 
                      new Int128(pp2.prev.pt.Y - pp2.pt.Y))
                        result = !result;
                  pp2 = pp2.next;
              }
              while (pp2 != pp);
          }
          else
          {
              do
              {
                  if ((((pp2.pt.Y <= pt.Y) && (pt.Y < pp2.prev.pt.Y)) ||
                    ((pp2.prev.pt.Y <= pt.Y) && (pt.Y < pp2.pt.Y))) &&
                    (pt.X - pp2.pt.X < (pp2.prev.pt.X - pp2.pt.X) * (pt.Y - pp2.pt.Y) /
                    (pp2.prev.pt.Y - pp2.pt.Y))) result = !result;
                  pp2 = pp2.next;
              }
              while (pp2 != pp);
          }
          return result;
        }
        //------------------------------------------------------------------------------

        internal bool SlopesEqual(TEdge e1, TEdge e2, bool UseFullRange)
        {
            if (UseFullRange)
              return Int128.Int128Mul(e1.ytop - e1.ybot, e2.xtop - e2.xbot) ==
                  Int128.Int128Mul(e1.xtop - e1.xbot, e2.ytop - e2.ybot);
            else return (Int64)(e1.ytop - e1.ybot) * (e2.xtop - e2.xbot) -
              (Int64)(e1.xtop - e1.xbot)*(e2.ytop - e2.ybot) == 0;
        }
        //------------------------------------------------------------------------------

        protected bool SlopesEqual(IntPoint pt1, IntPoint pt2,
            IntPoint pt3, bool UseFullRange)
        {
            if (UseFullRange)
                return Int128.Int128Mul(pt1.Y - pt2.Y, pt2.X - pt3.X) ==
                  Int128.Int128Mul(pt1.X - pt2.X, pt2.Y - pt3.Y);
            else return
              (Int64)(pt1.Y - pt2.Y) * (pt2.X - pt3.X) - (Int64)(pt1.X - pt2.X) * (pt2.Y - pt3.Y) == 0;
        }
        //------------------------------------------------------------------------------

        protected bool SlopesEqual(IntPoint pt1, IntPoint pt2,
            IntPoint pt3, IntPoint pt4, bool UseFullRange)
        {
            if (UseFullRange)
                return Int128.Int128Mul(pt1.Y - pt2.Y, pt3.X - pt4.X) ==
                  Int128.Int128Mul(pt1.X - pt2.X, pt3.Y - pt4.Y);
            else return
              (Int64)(pt1.Y - pt2.Y) * (pt3.X - pt4.X) - (Int64)(pt1.X - pt2.X) * (pt3.Y - pt4.Y) == 0;
        }
        //------------------------------------------------------------------------------

        internal ClipperBase() //constructor (nb: no external instantiation)
        {
            m_MinimaList = null;
            m_CurrentLM = null;
            m_UseFullRange = false;
        }
        //------------------------------------------------------------------------------

        //destructor - commented out since I gather this impedes the GC 
        //~ClipperBase() 
        //{
        //    Clear();
        //}
        //------------------------------------------------------------------------------

        public virtual void Clear()
        {
            DisposeLocalMinimaList();
            for (int i = 0; i < m_edges.Count; ++i)
            {
                for (int j = 0; j < m_edges[i].Count; ++j) m_edges[i][j] = null;
                m_edges[i].Clear();
            }
            m_edges.Clear();
            m_UseFullRange = false;
        }
        //------------------------------------------------------------------------------

        private void DisposeLocalMinimaList()
        {
            while( m_MinimaList != null )
            {
                LocalMinima tmpLm = m_MinimaList.next;
                m_MinimaList = null;
                m_MinimaList = tmpLm;
            }
            m_CurrentLM = null;
        }
        //------------------------------------------------------------------------------

        public bool AddPolygons(Polygons ppg, PolyType polyType)
        {
            bool result = false;
            for (int i = 0; i < ppg.Count; ++i)
                if (AddPolygon(ppg[i], polyType)) result = true;
            return result;
        }
        //------------------------------------------------------------------------------

        public bool AddPolygon(Polygon pg, PolyType polyType)
        {
            int len = pg.Count;
            if (len < 3) return false;
            Polygon p = new Polygon(len);
            p.Add(new IntPoint(pg[0].X, pg[0].Y));
            int j = 0;
            for (int i = 1; i < len; ++i)
            {

                Int64 maxVal;
                if (m_UseFullRange) maxVal = hiRange; else maxVal = loRange;
                if (Math.Abs(pg[i].X) > maxVal || Math.Abs(pg[i].Y) > maxVal)
                {
                    if (Math.Abs(pg[i].X) > hiRange || Math.Abs(pg[i].Y) > hiRange)
                    throw new ClipperException("Coordinate exceeds range bounds");
                  maxVal = hiRange;
                  m_UseFullRange = true;
                }

                if (PointsEqual(p[j], pg[i])) continue;
                else if (j > 0 && SlopesEqual(p[j-1], p[j], pg[i], m_UseFullRange))
                {
                    if (PointsEqual(p[j-1], pg[i])) j--;
                } else j++;
                if (j < p.Count)
                    p[j] = pg[i]; else
                    p.Add(new IntPoint(pg[i].X, pg[i].Y));
            }
            if (j < 2) return false;

            len = j+1;
            while (len > 2)
            {
                //nb: test for point equality before testing slopes ...
                if (PointsEqual(p[j], p[0])) j--;
                else if (PointsEqual(p[0], p[1]) || SlopesEqual(p[j], p[0], p[1], m_UseFullRange))
                    p[0] = p[j--];
                else if (SlopesEqual(p[j - 1], p[j], p[0], m_UseFullRange)) j--;
                else if (SlopesEqual(p[0], p[1], p[2], m_UseFullRange))
                {
                    for (int i = 2; i <= j; ++i) p[i - 1] = p[i];
                    j--;
                }
                else break;
                len--;
            }
            if (len < 3) return false;

            //create a new edge array ...
            List<TEdge> edges = new List<TEdge>(len);
            for (int i = 0; i < len; i++) edges.Add(new TEdge());
            m_edges.Add(edges);

            //convert vertices to a double-linked-list of edges and initialize ...
            edges[0].xcurr = p[0].X;
            edges[0].ycurr = p[0].Y;
            InitEdge(edges[len-1], edges[0], edges[len-2], p[len-1], polyType);
            for (int i = len-2; i > 0; --i)
            InitEdge(edges[i], edges[i+1], edges[i-1], p[i], polyType);
            InitEdge(edges[0], edges[1], edges[len-1], p[0], polyType);

            //reset xcurr & ycurr and find 'eHighest' (given the Y axis coordinates
            //increase downward so the 'highest' edge will have the smallest ytop) ...
            TEdge e = edges[0];
            TEdge eHighest = e;
            do
            {
            e.xcurr = e.xbot;
            e.ycurr = e.ybot;
            if (e.ytop < eHighest.ytop) eHighest = e;
            e = e.next;
            }
            while ( e != edges[0]);

            //make sure eHighest is positioned so the following loop works safely ...
            if (eHighest.windDelta > 0) eHighest = eHighest.next;
            if (eHighest.dx == horizontal) eHighest = eHighest.next;

            //finally insert each local minima ...
            e = eHighest;
            do {
            e = AddBoundsToLML(e);
            }
            while( e != eHighest );
            return true;
        }
        //------------------------------------------------------------------------------

        private void InitEdge(TEdge e, TEdge eNext,
          TEdge ePrev, IntPoint pt, PolyType polyType)
        {
          e.next = eNext;
          e.prev = ePrev;
          e.xcurr = pt.X;
          e.ycurr = pt.Y;
          if (e.ycurr >= e.next.ycurr)
          {
            e.xbot = e.xcurr;
            e.ybot = e.ycurr;
            e.xtop = e.next.xcurr;
            e.ytop = e.next.ycurr;
            e.windDelta = 1;
          } else
          {
            e.xtop = e.xcurr;
            e.ytop = e.ycurr;
            e.xbot = e.next.xcurr;
            e.ybot = e.next.ycurr;
            e.windDelta = -1;
          }
          SetDx(e);
          e.polyType = polyType;
          e.outIdx = -1;
        }
        //------------------------------------------------------------------------------

        private void SetDx(TEdge e)
        {
          if (e.ybot == e.ytop) e.dx = horizontal;
          else e.dx = (double)(e.xtop - e.xbot)/(e.ytop - e.ybot);
        }
        //---------------------------------------------------------------------------

        TEdge AddBoundsToLML(TEdge e)
        {
          //Starting at the top of one bound we progress to the bottom where there's
          //a local minima. We then go to the top of the next bound. These two bounds
          //form the left and right (or right and left) bounds of the local minima.
          e.nextInLML = null;
          e = e.next;
          for (;;)
          {
            if ( e.dx == horizontal )
            {
              //nb: proceed through horizontals when approaching from their right,
              //    but break on horizontal minima if approaching from their left.
              //    This ensures 'local minima' are always on the left of horizontals.
              if (e.next.ytop < e.ytop && e.next.xbot > e.prev.xbot) break;
              if (e.xtop != e.prev.xbot) SwapX(e);
              e.nextInLML = e.prev;
            }
            else if (e.ycurr == e.prev.ycurr) break;
            else e.nextInLML = e.prev;
            e = e.next;
          }

          //e and e.prev are now at a local minima ...
          LocalMinima newLm = new LocalMinima();
          newLm.next = null;
          newLm.Y = e.prev.ybot;

          if ( e.dx == horizontal ) //horizontal edges never start a left bound
          {
            if (e.xbot != e.prev.xbot) SwapX(e);
            newLm.leftBound = e.prev;
            newLm.rightBound = e;
          } else if (e.dx < e.prev.dx)
          {
            newLm.leftBound = e.prev;
            newLm.rightBound = e;
          } else
          {
            newLm.leftBound = e;
            newLm.rightBound = e.prev;
          }
          newLm.leftBound.side = EdgeSide.esLeft;
          newLm.rightBound.side = EdgeSide.esRight;
          InsertLocalMinima( newLm );

          for (;;)
          {
            if ( e.next.ytop == e.ytop && e.next.dx != horizontal ) break;
            e.nextInLML = e.next;
            e = e.next;
            if ( e.dx == horizontal && e.xbot != e.prev.xtop) SwapX(e);
          }
          return e.next;
        }
        //------------------------------------------------------------------------------

        private void InsertLocalMinima(LocalMinima newLm)
        {
          if( m_MinimaList == null )
          {
            m_MinimaList = newLm;
          }
          else if( newLm.Y >= m_MinimaList.Y )
          {
            newLm.next = m_MinimaList;
            m_MinimaList = newLm;
          } else
          {
            LocalMinima tmpLm = m_MinimaList;
            while( tmpLm.next != null  && ( newLm.Y < tmpLm.next.Y ) )
              tmpLm = tmpLm.next;
            newLm.next = tmpLm.next;
            tmpLm.next = newLm;
          }
        }
        //------------------------------------------------------------------------------

        protected void PopLocalMinima()
        {
            if (m_CurrentLM == null) return;
            m_CurrentLM = m_CurrentLM.next;
        }
        //------------------------------------------------------------------------------

        private void SwapX(TEdge e)
        {
          //swap horizontal edges' top and bottom x's so they follow the natural
          //progression of the bounds - ie so their xbots will align with the
          //adjoining lower edge. [Helpful in the ProcessHorizontal() method.]
          e.xcurr = e.xtop;
          e.xtop = e.xbot;
          e.xbot = e.xcurr;
        }
        //------------------------------------------------------------------------------

        protected virtual void Reset()
        {
            m_CurrentLM = m_MinimaList;

            //reset all edges ...
            LocalMinima lm = m_MinimaList;
            while (lm != null)
            {
                TEdge e = lm.leftBound;
                while (e != null)
                {
                    e.xcurr = e.xbot;
                    e.ycurr = e.ybot;
                    e.side = EdgeSide.esLeft;
                    e.outIdx = -1;
                    e = e.nextInLML;
                }
                e = lm.rightBound;
                while (e != null)
                {
                    e.xcurr = e.xbot;
                    e.ycurr = e.ybot;
                    e.side = EdgeSide.esRight;
                    e.outIdx = -1;
                    e = e.nextInLML;
                }
                lm = lm.next;
            }
            return;
        }
        //------------------------------------------------------------------------------

        public IntRect GetBounds()
        {
            IntRect result = new IntRect();
            LocalMinima lm = m_MinimaList;
            if (lm == null) return result;
            result.left = lm.leftBound.xbot;
            result.top = lm.leftBound.ybot;
            result.right = lm.leftBound.xbot;
            result.bottom = lm.leftBound.ybot;
            while (lm != null)
            {
                if (lm.leftBound.ybot > result.bottom)
                    result.bottom = lm.leftBound.ybot;
                TEdge e = lm.leftBound;
                for (; ; )
                {
                    TEdge bottomE = e;
                    while (e.nextInLML != null)
                    {
                        if (e.xbot < result.left) result.left = e.xbot;
                        if (e.xbot > result.right) result.right = e.xbot;
                        e = e.nextInLML;
                    }
                    if (e.xbot < result.left) result.left = e.xbot;
                    if (e.xbot > result.right) result.right = e.xbot;
                    if (e.xtop < result.left) result.left = e.xtop;
                    if (e.xtop > result.right) result.right = e.xtop;
                    if (e.ytop < result.top) result.top = e.ytop;

                    if (bottomE == lm.leftBound) e = lm.rightBound;
                    else break;
                }
                lm = lm.next;
            }
            return result;
        }

    } //ClipperBase

    public class Clipper : ClipperBase
    {
        private List<OutRec> m_PolyOuts;
        private ClipType m_ClipType;
        private Scanbeam m_Scanbeam;
        private TEdge m_ActiveEdges;
        private TEdge m_SortedEdges;
        private IntersectNode m_IntersectNodes;
        private bool m_ExecuteLocked;
        private PolyFillType m_ClipFillType;
        private PolyFillType m_SubjFillType;
        private List<JoinRec> m_Joins;
        private List<HorzJoinRec> m_HorizJoins;
        private bool m_ReverseOutput;

        public Clipper()
        {
            m_Scanbeam = null;
            m_ActiveEdges = null;
            m_SortedEdges = null;
            m_IntersectNodes = null;
            m_ExecuteLocked = false;
            m_PolyOuts = new List<OutRec>();
            m_Joins = new List<JoinRec>();
            m_HorizJoins = new List<HorzJoinRec>();
            m_ReverseOutput = false;
        }
        //------------------------------------------------------------------------------

        //destructor - commented out since I gather this impedes the GC 
        //~Clipper() //destructor
        //{
        //    Clear();
        //    DisposeScanbeamList();
        //}
        //------------------------------------------------------------------------------

        public override void Clear()
        {
            if (m_edges.Count == 0) return; //avoids problems with ClipperBase destructor
            DisposeAllPolyPts();
            base.Clear();
        }
        //------------------------------------------------------------------------------

        void DisposeScanbeamList()
        {
          while ( m_Scanbeam != null ) {
          Scanbeam sb2 = m_Scanbeam.next;
          m_Scanbeam = null;
          m_Scanbeam = sb2;
          }
        }
        //------------------------------------------------------------------------------

        protected override void Reset() 
        {
          base.Reset();
          m_Scanbeam = null;
          m_ActiveEdges = null;
          m_SortedEdges = null;
          DisposeAllPolyPts();
          LocalMinima lm = m_MinimaList;
          while (lm != null)
          {
            InsertScanbeam(lm.Y);
            InsertScanbeam(lm.leftBound.ytop);
            lm = lm.next;
          }
        }
        //------------------------------------------------------------------------------

        public bool ReverseSolution
        {
            get { return m_ReverseOutput; }
            set { m_ReverseOutput = value; }
        }
        //------------------------------------------------------------------------------
        
        private void InsertScanbeam(Int64 Y)
        {
          if( m_Scanbeam == null )
          {
            m_Scanbeam = new Scanbeam();
            m_Scanbeam.next = null;
            m_Scanbeam.Y = Y;
          }
          else if(  Y > m_Scanbeam.Y )
          {
            Scanbeam newSb = new Scanbeam();
            newSb.Y = Y;
            newSb.next = m_Scanbeam;
            m_Scanbeam = newSb;
          } else
          {
            Scanbeam sb2 = m_Scanbeam;
            while( sb2.next != null  && ( Y <= sb2.next.Y ) ) sb2 = sb2.next;
            if(  Y == sb2.Y ) return; //ie ignores duplicates
            Scanbeam newSb = new Scanbeam();
            newSb.Y = Y;
            newSb.next = sb2.next;
            sb2.next = newSb;
          }
        }
        //------------------------------------------------------------------------------

        public bool Execute(ClipType clipType, Polygons solution,
            PolyFillType subjFillType, PolyFillType clipFillType)
        {
            if (m_ExecuteLocked) return false;
            m_ExecuteLocked = true;
            solution.Clear();
            m_SubjFillType = subjFillType;
            m_ClipFillType = clipFillType;
            m_ClipType = clipType;
            bool succeeded = ExecuteInternal(false);
            //build the return polygons ...
            if (succeeded) BuildResult(solution);
            m_ExecuteLocked = false;
            return succeeded;
        }
        //------------------------------------------------------------------------------

        public bool Execute(ClipType clipType, ExPolygons solution,
            PolyFillType subjFillType, PolyFillType clipFillType)
        {
            if (m_ExecuteLocked) return false;
            m_ExecuteLocked = true;
            solution.Clear();
            m_SubjFillType = subjFillType;
            m_ClipFillType = clipFillType;
            m_ClipType = clipType;
            bool succeeded = ExecuteInternal(true);
            //build the return polygons ...
            if (succeeded) BuildResultEx(solution);
            m_ExecuteLocked = false;
            return succeeded;
        }
        //------------------------------------------------------------------------------

        public bool Execute(ClipType clipType, Polygons solution)
        {
            return Execute(clipType, solution,
                PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
        }
        //------------------------------------------------------------------------------

        public bool Execute(ClipType clipType, ExPolygons solution)
        {
            return Execute(clipType, solution,
                PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
        }
        //------------------------------------------------------------------------------

        internal int PolySort(OutRec or1, OutRec or2)
        {
          if (or1 == or2) return 0;
          else if (or1.pts == null || or2.pts == null)
          {
            if ((or1.pts == null) != (or2.pts == null))
            {
                return or1.pts == null ? 1 : -1;
            }
            else return 0;          
          }
          int i1, i2;
          if (or1.isHole)
            i1 = or1.FirstLeft.idx; else
            i1 = or1.idx;
          if (or2.isHole)
            i2 = or2.FirstLeft.idx; else
            i2 = or2.idx;
          int result = i1 - i2;
          if (result == 0 && (or1.isHole != or2.isHole))
          {
              return or1.isHole ? 1 : -1;
          }
          return result;
        }
        //------------------------------------------------------------------------------

        internal OutRec FindAppendLinkEnd(OutRec outRec)
        {
          while (outRec.AppendLink != null) outRec = outRec.AppendLink;
          return outRec;
        }
        //------------------------------------------------------------------------------

        internal void FixHoleLinkage(OutRec outRec)
        {
            OutRec tmp;
            if (outRec.bottomPt != null) 
                tmp = m_PolyOuts[outRec.bottomPt.idx].FirstLeft; 
            else
                tmp = outRec.FirstLeft;
            if (outRec == tmp) throw new ClipperException("HoleLinkage error");

            if (tmp != null) 
            {
                if (tmp.AppendLink != null) tmp = FindAppendLinkEnd(tmp);

                if (tmp == outRec) tmp = null;
                else if (tmp.isHole)
                {
                    FixHoleLinkage(tmp);
                    tmp = tmp.FirstLeft;
                }
            }
            outRec.FirstLeft = tmp;
            if (tmp == null) outRec.isHole = false;
            outRec.AppendLink = null;
        }
        //------------------------------------------------------------------------------

        private bool ExecuteInternal(bool fixHoleLinkages)
        {
            bool succeeded;
            try
            {
                Reset();
                if (m_CurrentLM == null) return true;
                Int64 botY = PopScanbeam();
                do
                {
                    InsertLocalMinimaIntoAEL(botY);
                    m_HorizJoins.Clear();
                    ProcessHorizontals();
                    Int64 topY = PopScanbeam();
                    succeeded = ProcessIntersections(botY, topY);
                    if (!succeeded) break;
                    ProcessEdgesAtTopOfScanbeam(topY);
                    botY = topY;
                } while (m_Scanbeam != null);
            }
            catch { succeeded = false; }

            if (succeeded)
            { 
                //tidy up output polygons and fix orientations where necessary ...
                foreach (OutRec outRec in m_PolyOuts)
                {
                  if (outRec.pts == null) continue;
                  FixupOutPolygon(outRec);
                  if (outRec.pts == null) continue;
                  if (outRec.isHole && fixHoleLinkages) FixHoleLinkage(outRec);

                  if (outRec.bottomPt == outRec.bottomFlag &&
                    (Orientation(outRec, m_UseFullRange) != (Area(outRec, m_UseFullRange) > 0)))
                  {
                      DisposeBottomPt(outRec);
                      FixupOutPolygon(outRec);
                  }

                  if (outRec.isHole == (m_ReverseOutput ^ Orientation(outRec, m_UseFullRange)))
                    ReversePolyPtLinks(outRec.pts);
                }

                JoinCommonEdges(fixHoleLinkages);
                if (fixHoleLinkages) m_PolyOuts.Sort(new Comparison<OutRec>(PolySort));
            }
            m_Joins.Clear();
            m_HorizJoins.Clear();
            return succeeded;
        }
        //------------------------------------------------------------------------------

        private Int64 PopScanbeam()
        {
          Int64 Y = m_Scanbeam.Y;
          m_Scanbeam = m_Scanbeam.next;
          return Y;
        }
        //------------------------------------------------------------------------------

        private void DisposeAllPolyPts(){
          for (int i = 0; i < m_PolyOuts.Count; ++i) DisposeOutRec(i);
          m_PolyOuts.Clear();
        }
        //------------------------------------------------------------------------------

        void DisposeBottomPt(OutRec outRec)
        {
          OutPt next = outRec.bottomPt.next;
          OutPt prev = outRec.bottomPt.prev;
          if (outRec.pts == outRec.bottomPt) outRec.pts = next;
          outRec.bottomPt = null;
          next.prev = prev;
          prev.next = next;
          outRec.bottomPt = next;
        }
        //------------------------------------------------------------------------------

        void DisposeOutRec(int index)
        {
          OutRec outRec = m_PolyOuts[index];
          if (outRec.pts != null) DisposeOutPts(outRec.pts);
          outRec = null;
          m_PolyOuts[index] = null;
        }
        //------------------------------------------------------------------------------

        private void DisposeOutPts(OutPt pp)
        {
            if (pp == null) return;
            pp.prev.next = null;
            while (pp != null)
            {
                pp = pp.next;
            }
        }
        //------------------------------------------------------------------------------

        private void AddJoin(TEdge e1, TEdge e2, int e1OutIdx, int e2OutIdx)
        {
            JoinRec jr = new JoinRec();
            if (e1OutIdx >= 0)
                jr.poly1Idx = e1OutIdx; else
            jr.poly1Idx = e1.outIdx;
            jr.pt1a = new IntPoint(e1.xcurr, e1.ycurr);
            jr.pt1b = new IntPoint(e1.xtop, e1.ytop);
            if (e2OutIdx >= 0)
                jr.poly2Idx = e2OutIdx; else
                jr.poly2Idx = e2.outIdx;
            jr.pt2a = new IntPoint(e2.xcurr, e2.ycurr);
            jr.pt2b = new IntPoint(e2.xtop, e2.ytop);
            m_Joins.Add(jr);
        }
        //------------------------------------------------------------------------------

        private void AddHorzJoin(TEdge e, int idx)
        {
            HorzJoinRec hj = new HorzJoinRec();
            hj.edge = e;
            hj.savedIdx = idx;
            m_HorizJoins.Add(hj);
        }
        //------------------------------------------------------------------------------

        private void InsertLocalMinimaIntoAEL(Int64 botY)
        {
          while(  m_CurrentLM != null  && ( m_CurrentLM.Y == botY ) )
          {
            TEdge lb = m_CurrentLM.leftBound;
            TEdge rb = m_CurrentLM.rightBound;

            InsertEdgeIntoAEL( lb );
            InsertScanbeam( lb.ytop );
            InsertEdgeIntoAEL( rb );

            if (IsEvenOddFillType(lb))
            {
                lb.windDelta = 1;
                rb.windDelta = 1;
            }
            else
            {
                rb.windDelta = -lb.windDelta;
            }
            SetWindingCount(lb);
            rb.windCnt = lb.windCnt;
            rb.windCnt2 = lb.windCnt2;

            if(  rb.dx == horizontal )
            {
              //nb: only rightbounds can have a horizontal bottom edge
              AddEdgeToSEL( rb );
              InsertScanbeam( rb.nextInLML.ytop );
            }
            else
              InsertScanbeam( rb.ytop );

            if( IsContributing(lb) )
                AddLocalMinPoly(lb, rb, new IntPoint(lb.xcurr, m_CurrentLM.Y));

            //if any output polygons share an edge, they'll need joining later ...
            if (rb.outIdx >= 0)
            {
                if (rb.dx == horizontal)
                {
                    for (int i = 0; i < m_HorizJoins.Count; i++)
                    {
                        IntPoint pt = new IntPoint(), pt2 = new IntPoint(); //used as dummy params.
                        HorzJoinRec hj = m_HorizJoins[i];
                        //if horizontals rb and hj.edge overlap, flag for joining later ...
                        if (GetOverlapSegment(new IntPoint(hj.edge.xbot, hj.edge.ybot),
                            new IntPoint(hj.edge.xtop, hj.edge.ytop),
                            new IntPoint(rb.xbot, rb.ybot),
                            new IntPoint(rb.xtop, rb.ytop), 
                            ref pt, ref pt2))
                            AddJoin(hj.edge, rb, hj.savedIdx, -1);
                    }
                }
            }


            if( lb.nextInAEL != rb )
            {
                if (rb.outIdx >= 0 && rb.prevInAEL.outIdx >= 0 && 
                    SlopesEqual(rb.prevInAEL, rb, m_UseFullRange))
                    AddJoin(rb, rb.prevInAEL, -1, -1);

              TEdge e = lb.nextInAEL;
              IntPoint pt = new IntPoint(lb.xcurr, lb.ycurr);
              while( e != rb )
              {
                if(e == null) 
                    throw new ClipperException("InsertLocalMinimaIntoAEL: missing rightbound!");
                //nb: For calculating winding counts etc, IntersectEdges() assumes
                //that param1 will be to the right of param2 ABOVE the intersection ...
                IntersectEdges( rb , e , pt , Protects.ipNone); //order important here
                e = e.nextInAEL;
              }
            }
            PopLocalMinima();
          }
        }
        //------------------------------------------------------------------------------

        private void InsertEdgeIntoAEL(TEdge edge)
        {
          edge.prevInAEL = null;
          edge.nextInAEL = null;
          if (m_ActiveEdges == null)
          {
            m_ActiveEdges = edge;
          }
          else if( E2InsertsBeforeE1(m_ActiveEdges, edge) )
          {
            edge.nextInAEL = m_ActiveEdges;
            m_ActiveEdges.prevInAEL = edge;
            m_ActiveEdges = edge;
          } else
          {
            TEdge e = m_ActiveEdges;
            while (e.nextInAEL != null && !E2InsertsBeforeE1(e.nextInAEL, edge))
              e = e.nextInAEL;
            edge.nextInAEL = e.nextInAEL;
            if (e.nextInAEL != null) e.nextInAEL.prevInAEL = edge;
            edge.prevInAEL = e;
            e.nextInAEL = edge;
          }
        }
        //----------------------------------------------------------------------

        private bool E2InsertsBeforeE1(TEdge e1, TEdge e2)
        {
          return e2.xcurr == e1.xcurr? e2.dx > e1.dx : e2.xcurr < e1.xcurr;
        }
        //------------------------------------------------------------------------------

        private bool IsEvenOddFillType(TEdge edge) 
        {
          if (edge.polyType == PolyType.ptSubject)
              return m_SubjFillType == PolyFillType.pftEvenOdd; 
          else
              return m_ClipFillType == PolyFillType.pftEvenOdd;
        }
        //------------------------------------------------------------------------------

        private bool IsEvenOddAltFillType(TEdge edge) 
        {
          if (edge.polyType == PolyType.ptSubject)
              return m_ClipFillType == PolyFillType.pftEvenOdd; 
          else
              return m_SubjFillType == PolyFillType.pftEvenOdd;
        }
        //------------------------------------------------------------------------------

        private bool IsContributing(TEdge edge)
        {
            PolyFillType pft, pft2;
            if (edge.polyType == PolyType.ptSubject)
            {
                pft = m_SubjFillType;
                pft2 = m_ClipFillType;
            }
            else
            {
                pft = m_ClipFillType;
                pft2 = m_SubjFillType;
            }

            switch (pft)
            {
                case PolyFillType.pftEvenOdd:
                case PolyFillType.pftNonZero:
                    if (Math.Abs(edge.windCnt) != 1) return false;
                    break;
                case PolyFillType.pftPositive:
                    if (edge.windCnt != 1) return false;
                    break;
                default: //PolyFillType.pftNegative
                    if (edge.windCnt != -1) return false; 
                    break;
            }

            switch (m_ClipType)
            {
                case ClipType.ctIntersection:
                    switch (pft2)
                    {
                        case PolyFillType.pftEvenOdd:
                        case PolyFillType.pftNonZero:
                            return (edge.windCnt2 != 0);
                        case PolyFillType.pftPositive:
                            return (edge.windCnt2 > 0);
                        default:
                            return (edge.windCnt2 < 0);
                    }
                case ClipType.ctUnion:
                    switch (pft2)
                    {
                        case PolyFillType.pftEvenOdd:
                        case PolyFillType.pftNonZero:
                            return (edge.windCnt2 == 0);
                        case PolyFillType.pftPositive:
                            return (edge.windCnt2 <= 0);
                        default:
                            return (edge.windCnt2 >= 0);
                    }
                case ClipType.ctDifference:
                    if (edge.polyType == PolyType.ptSubject)
                        switch (pft2)
                        {
                            case PolyFillType.pftEvenOdd:
                            case PolyFillType.pftNonZero:
                                return (edge.windCnt2 == 0);
                            case PolyFillType.pftPositive:
                                return (edge.windCnt2 <= 0);
                            default:
                                return (edge.windCnt2 >= 0);
                        }
                    else
                        switch (pft2)
                        {
                            case PolyFillType.pftEvenOdd:
                            case PolyFillType.pftNonZero:
                                return (edge.windCnt2 != 0);
                            case PolyFillType.pftPositive:
                                return (edge.windCnt2 > 0);
                            default:
                                return (edge.windCnt2 < 0);
                        }
            }
            return true;
        }
        //------------------------------------------------------------------------------

        private void SetWindingCount(TEdge edge)
        {
            TEdge e = edge.prevInAEL;
            //find the edge of the same polytype that immediately preceeds 'edge' in AEL
            while (e != null && e.polyType != edge.polyType)
                e = e.prevInAEL;
            if (e == null)
            {
                edge.windCnt = edge.windDelta;
                edge.windCnt2 = 0;
                e = m_ActiveEdges; //ie get ready to calc windCnt2
            }
            else if (IsEvenOddFillType(edge))
            {
                //even-odd filling ...
                edge.windCnt = 1;
                edge.windCnt2 = e.windCnt2;
                e = e.nextInAEL; //ie get ready to calc windCnt2
            }
            else
            {
                //nonZero filling ...
                if (e.windCnt * e.windDelta < 0)
                {
                    if (Math.Abs(e.windCnt) > 1)
                    {
                        if (e.windDelta * edge.windDelta < 0)
                            edge.windCnt = e.windCnt;
                        else
                            edge.windCnt = e.windCnt + edge.windDelta;
                    }
                    else
                        edge.windCnt = e.windCnt + e.windDelta + edge.windDelta;
                }
                else
                {
                    if (Math.Abs(e.windCnt) > 1 && e.windDelta * edge.windDelta < 0)
                        edge.windCnt = e.windCnt;
                    else if (e.windCnt + edge.windDelta == 0)
                        edge.windCnt = e.windCnt;
                    else
                        edge.windCnt = e.windCnt + edge.windDelta;
                }
                edge.windCnt2 = e.windCnt2;
                e = e.nextInAEL; //ie get ready to calc windCnt2
            }

            //update windCnt2 ...
            if (IsEvenOddAltFillType(edge))
            {
                //even-odd filling ...
                while (e != edge)
                {
                    edge.windCnt2 = (edge.windCnt2 == 0) ? 1 : 0;
                    e = e.nextInAEL;
                }
            }
            else
            {
                //nonZero filling ...
                while (e != edge)
                {
                    edge.windCnt2 += e.windDelta;
                    e = e.nextInAEL;
                }
            }
        }
        //------------------------------------------------------------------------------

        private void AddEdgeToSEL(TEdge edge)
        {
            //SEL pointers in PEdge are reused to build a list of horizontal edges.
            //However, we don't need to worry about order with horizontal edge processing.
            if (m_SortedEdges == null)
            {
                m_SortedEdges = edge;
                edge.prevInSEL = null;
                edge.nextInSEL = null;
            }
            else
            {
                edge.nextInSEL = m_SortedEdges;
                edge.prevInSEL = null;
                m_SortedEdges.prevInSEL = edge;
                m_SortedEdges = edge;
            }
        }
        //------------------------------------------------------------------------------

        private void CopyAELToSEL()
        {
            TEdge e = m_ActiveEdges;
            m_SortedEdges = e;
            if (m_ActiveEdges == null)
                return;
            m_SortedEdges.prevInSEL = null;
            e = e.nextInAEL;
            while (e != null)
            {
                e.prevInSEL = e.prevInAEL;
                e.prevInSEL.nextInSEL = e;
                e.nextInSEL = null;
                e = e.nextInAEL;
            }
        }
        //------------------------------------------------------------------------------

        private void SwapPositionsInAEL(TEdge edge1, TEdge edge2)
        {
            if (edge1.nextInAEL == null && edge1.prevInAEL == null)
                return;
            if (edge2.nextInAEL == null && edge2.prevInAEL == null)
                return;

            if (edge1.nextInAEL == edge2)
            {
                TEdge next = edge2.nextInAEL;
                if (next != null)
                    next.prevInAEL = edge1;
                TEdge prev = edge1.prevInAEL;
                if (prev != null)
                    prev.nextInAEL = edge2;
                edge2.prevInAEL = prev;
                edge2.nextInAEL = edge1;
                edge1.prevInAEL = edge2;
                edge1.nextInAEL = next;
            }
            else if (edge2.nextInAEL == edge1)
            {
                TEdge next = edge1.nextInAEL;
                if (next != null)
                    next.prevInAEL = edge2;
                TEdge prev = edge2.prevInAEL;
                if (prev != null)
                    prev.nextInAEL = edge1;
                edge1.prevInAEL = prev;
                edge1.nextInAEL = edge2;
                edge2.prevInAEL = edge1;
                edge2.nextInAEL = next;
            }
            else
            {
                TEdge next = edge1.nextInAEL;
                TEdge prev = edge1.prevInAEL;
                edge1.nextInAEL = edge2.nextInAEL;
                if (edge1.nextInAEL != null)
                    edge1.nextInAEL.prevInAEL = edge1;
                edge1.prevInAEL = edge2.prevInAEL;
                if (edge1.prevInAEL != null)
                    edge1.prevInAEL.nextInAEL = edge1;
                edge2.nextInAEL = next;
                if (edge2.nextInAEL != null)
                    edge2.nextInAEL.prevInAEL = edge2;
                edge2.prevInAEL = prev;
                if (edge2.prevInAEL != null)
                    edge2.prevInAEL.nextInAEL = edge2;
            }

            if (edge1.prevInAEL == null)
                m_ActiveEdges = edge1;
            else if (edge2.prevInAEL == null)
                m_ActiveEdges = edge2;
        }
        //------------------------------------------------------------------------------

        private void SwapPositionsInSEL(TEdge edge1, TEdge edge2)
        {
            if (edge1.nextInSEL == null && edge1.prevInSEL == null)
                return;
            if (edge2.nextInSEL == null && edge2.prevInSEL == null)
                return;

            if (edge1.nextInSEL == edge2)
            {
                TEdge next = edge2.nextInSEL;
                if (next != null)
                    next.prevInSEL = edge1;
                TEdge prev = edge1.prevInSEL;
                if (prev != null)
                    prev.nextInSEL = edge2;
                edge2.prevInSEL = prev;
                edge2.nextInSEL = edge1;
                edge1.prevInSEL = edge2;
                edge1.nextInSEL = next;
            }
            else if (edge2.nextInSEL == edge1)
            {
                TEdge next = edge1.nextInSEL;
                if (next != null)
                    next.prevInSEL = edge2;
                TEdge prev = edge2.prevInSEL;
                if (prev != null)
                    prev.nextInSEL = edge1;
                edge1.prevInSEL = prev;
                edge1.nextInSEL = edge2;
                edge2.prevInSEL = edge1;
                edge2.nextInSEL = next;
            }
            else
            {
                TEdge next = edge1.nextInSEL;
                TEdge prev = edge1.prevInSEL;
                edge1.nextInSEL = edge2.nextInSEL;
                if (edge1.nextInSEL != null)
                    edge1.nextInSEL.prevInSEL = edge1;
                edge1.prevInSEL = edge2.prevInSEL;
                if (edge1.prevInSEL != null)
                    edge1.prevInSEL.nextInSEL = edge1;
                edge2.nextInSEL = next;
                if (edge2.nextInSEL != null)
                    edge2.nextInSEL.prevInSEL = edge2;
                edge2.prevInSEL = prev;
                if (edge2.prevInSEL != null)
                    edge2.prevInSEL.nextInSEL = edge2;
            }

            if (edge1.prevInSEL == null)
                m_SortedEdges = edge1;
            else if (edge2.prevInSEL == null)
                m_SortedEdges = edge2;
        }
        //------------------------------------------------------------------------------


        private void AddLocalMaxPoly(TEdge e1, TEdge e2, IntPoint pt)
        {
            AddOutPt(e1, pt);
            if (e1.outIdx == e2.outIdx)
            {
                e1.outIdx = -1;
                e2.outIdx = -1;
            }
            else if (e1.outIdx < e2.outIdx) 
                AppendPolygon(e1, e2);
            else 
                AppendPolygon(e2, e1);
        }
        //------------------------------------------------------------------------------

        private void AddLocalMinPoly(TEdge e1, TEdge e2, IntPoint pt)
        {
            TEdge e, prevE;
            if (e2.dx == horizontal || (e1.dx > e2.dx))
            {
                AddOutPt(e1, pt);
                e2.outIdx = e1.outIdx;
                e1.side = EdgeSide.esLeft;
                e2.side = EdgeSide.esRight;
                e = e1;
                if (e.prevInAEL == e2)
                  prevE = e2.prevInAEL; 
                else
                  prevE = e.prevInAEL;
            }
            else
            {
                AddOutPt(e2, pt);
                e1.outIdx = e2.outIdx;
                e1.side = EdgeSide.esRight;
                e2.side = EdgeSide.esLeft;
                e = e2;
                if (e.prevInAEL == e1)
                    prevE = e1.prevInAEL;
                else
                    prevE = e.prevInAEL;
            }

            if (prevE != null && prevE.outIdx >= 0 &&
                (TopX(prevE, pt.Y) == TopX(e, pt.Y)) &&
                 SlopesEqual(e, prevE, m_UseFullRange))
                   AddJoin(e, prevE, -1, -1);

        }
        //------------------------------------------------------------------------------

        private OutRec CreateOutRec()
        {
          OutRec result = new OutRec();
          result.idx = -1;
          result.isHole = false;
          result.FirstLeft = null;
          result.AppendLink = null;
          result.pts = null;
          result.bottomPt = null;
          result.bottomFlag = null;
          result.sides = EdgeSide.esNeither;
          return result;
        }
        //------------------------------------------------------------------------------

        private void AddOutPt(TEdge e, IntPoint pt)
        {
          bool ToFront = (e.side == EdgeSide.esLeft);
          if(  e.outIdx < 0 )
          {
              OutRec outRec = CreateOutRec();
              m_PolyOuts.Add(outRec);
              outRec.idx = m_PolyOuts.Count -1;
              e.outIdx = outRec.idx;
              OutPt op = new OutPt();
              outRec.pts = op;
              outRec.bottomPt = op;
              op.pt = pt;
              op.idx = outRec.idx;
              op.next = op;
              op.prev = op;
              SetHoleState(e, outRec);
          } else
          {
              OutRec outRec = m_PolyOuts[e.outIdx];
              OutPt op = outRec.pts, op2, opBot;
              if (ToFront && PointsEqual(pt, op.pt) || 
                  (!ToFront && PointsEqual(pt, op.prev.pt))) return;

              if ((e.side | outRec.sides) != outRec.sides)
              {
                  //check for 'rounding' artefacts ...
                  if (outRec.sides == EdgeSide.esNeither && pt.Y == op.pt.Y)
                      if (ToFront)
                      {
                          if (pt.X == op.pt.X + 1) return;    //ie wrong side of bottomPt
                      }
                      else if (pt.X == op.pt.X - 1) return; //ie wrong side of bottomPt

                  outRec.sides = (EdgeSide)(outRec.sides | e.side);
                  if (outRec.sides == EdgeSide.esBoth)
                  {
                    //A vertex from each side has now been added.
                    //Vertices of one side of an output polygon are quite commonly close to
                    //or even 'touching' edges of the other side of the output polygon.
                    //Very occasionally vertices from one side can 'cross' an edge on the
                    //the other side. The distance 'crossed' is always less that a unit
                    //and is purely an artefact of coordinate rounding. Nevertheless, this
                    //results in very tiny self-intersections. Because of the way
                    //orientation is calculated, even tiny self-intersections can cause
                    //the Orientation function to return the wrong result. Therefore, it's
                    //important to ensure that any self-intersections close to BottomPt are
                    //detected and removed before orientation is assigned.

                    if (ToFront)
                    {
                      opBot = outRec.pts;
                      op2 = opBot.next; //op2 == right side
                      if (opBot.pt.Y != op2.pt.Y && opBot.pt.Y != pt.Y &&
                        ((opBot.pt.X - pt.X) / (opBot.pt.Y - pt.Y) <
                        (opBot.pt.X - op2.pt.X) / (opBot.pt.Y - op2.pt.Y)))
                          outRec.bottomFlag = opBot;
                    }
                    else
                    {
                      opBot = outRec.pts.prev;
                      op2 = opBot.next; //op2 == left side
                      if (opBot.pt.Y != op2.pt.Y && opBot.pt.Y != pt.Y &&
                        ((opBot.pt.X - pt.X) / (opBot.pt.Y - pt.Y) >
                        (opBot.pt.X - op2.pt.X) / (opBot.pt.Y - op2.pt.Y)))
                          outRec.bottomFlag = opBot;
                    }
                  }
              }

              op2 = new OutPt();
              op2.pt = pt;
              op2.idx = outRec.idx;
              if (op2.pt.Y == outRec.bottomPt.pt.Y &&
                op2.pt.X < outRec.bottomPt.pt.X)
                  outRec.bottomPt = op2;
              op2.next = op;
              op2.prev = op.prev;
              op2.prev.next = op2;
              op.prev = op2;
              if (ToFront) outRec.pts = op2;
          }
        }
        //------------------------------------------------------------------------------

        internal void SwapPoints(ref IntPoint pt1, ref IntPoint pt2)
        {
            IntPoint tmp = pt1;
            pt1 = pt2;
            pt2 = tmp;
        }
        //------------------------------------------------------------------------------

        private bool GetOverlapSegment(IntPoint pt1a, IntPoint pt1b, IntPoint pt2a,
            IntPoint pt2b, ref IntPoint pt1, ref IntPoint pt2)
        {
            //precondition: segments are colinear.
            if ( pt1a.Y == pt1b.Y || Math.Abs((pt1a.X - pt1b.X)/(pt1a.Y - pt1b.Y)) > 1 )
            {
            if (pt1a.X > pt1b.X) SwapPoints(ref pt1a, ref pt1b);
            if (pt2a.X > pt2b.X) SwapPoints(ref pt2a, ref pt2b);
            if (pt1a.X > pt2a.X) pt1 = pt1a; else pt1 = pt2a;
            if (pt1b.X < pt2b.X) pt2 = pt1b; else pt2 = pt2b;
            return pt1.X < pt2.X;
            } else
            {
            if (pt1a.Y < pt1b.Y) SwapPoints(ref pt1a, ref pt1b);
            if (pt2a.Y < pt2b.Y) SwapPoints(ref pt2a, ref pt2b);
            if (pt1a.Y < pt2a.Y) pt1 = pt1a; else pt1 = pt2a;
            if (pt1b.Y > pt2b.Y) pt2 = pt1b; else pt2 = pt2b;
            return pt1.Y > pt2.Y;
            }
        }
        //------------------------------------------------------------------------------

        private bool FindSegment(ref OutPt pp, ref IntPoint pt1, ref IntPoint pt2)
        {
            if (pp == null) return false;
            OutPt pp2 = pp;
            IntPoint pt1a = new IntPoint(pt1);
            IntPoint pt2a = new IntPoint(pt2);
            do
            {
                if (SlopesEqual(pt1a, pt2a, pp.pt, pp.prev.pt, true) &&
                    SlopesEqual(pt1a, pt2a, pp.pt, true) &&
                    GetOverlapSegment(pt1a, pt2a, pp.pt, pp.prev.pt, ref pt1, ref pt2))
                        return true;
            pp = pp.next;
            }
            while (pp != pp2);
            return false;
        }
        //------------------------------------------------------------------------------

        internal bool Pt3IsBetweenPt1AndPt2(IntPoint pt1, IntPoint pt2, IntPoint pt3)
        {
            if (PointsEqual(pt1, pt3) || PointsEqual(pt2, pt3)) return true;
            else if (pt1.X != pt2.X) return (pt1.X < pt3.X) == (pt3.X < pt2.X);
            else return (pt1.Y < pt3.Y) == (pt3.Y < pt2.Y);
        }
        //------------------------------------------------------------------------------

        private OutPt InsertPolyPtBetween(OutPt p1, OutPt p2, IntPoint pt)
        {
            OutPt result = new OutPt();
            result.pt = pt;
            if (p2 == p1.next)
            {
                p1.next = result;
                p2.prev = result;
                result.next = p2;
                result.prev = p1;
            } else
            {
                p2.next = result;
                p1.prev = result;
                result.next = p1;
                result.prev = p2;
            }
            return result;
        }
        //------------------------------------------------------------------------------

        private void SetHoleState(TEdge e, OutRec outRec)
        {
            bool isHole = false;
            TEdge e2 = e.prevInAEL;
            while (e2 != null)
            {
                if (e2.outIdx >= 0)
                {
                    isHole = !isHole;
                    if (outRec.FirstLeft == null)
                        outRec.FirstLeft = m_PolyOuts[e2.outIdx];
                }
                e2 = e2.prevInAEL;
            }
            if (isHole) outRec.isHole = true;
        }
        //------------------------------------------------------------------------------

        private double GetDx(IntPoint pt1, IntPoint pt2)
        {
            if (pt1.Y == pt2.Y) return horizontal;
            else return (double)(pt2.X - pt1.X) / (double)(pt2.Y - pt1.Y);
        }
        //---------------------------------------------------------------------------

        private bool FirstIsBottomPt(OutPt btmPt1, OutPt btmPt2)
        {
          OutPt p = btmPt1.prev;
          while (PointsEqual(p.pt, btmPt1.pt) && (p != btmPt1)) p = p.prev;
          double dx1p = Math.Abs(GetDx(btmPt1.pt, p.pt));
          p = btmPt1.next;
          while (PointsEqual(p.pt, btmPt1.pt) && (p != btmPt1)) p = p.next;
          double dx1n = Math.Abs(GetDx(btmPt1.pt, p.pt));

          p = btmPt2.prev;
          while (PointsEqual(p.pt, btmPt2.pt) && (p != btmPt2)) p = p.prev;
          double dx2p = Math.Abs(GetDx(btmPt2.pt, p.pt));
          p = btmPt2.next;
          while (PointsEqual(p.pt, btmPt2.pt) && (p != btmPt2)) p = p.next;
          double dx2n = Math.Abs(GetDx(btmPt2.pt, p.pt));
          return (dx1p >= dx2p && dx1p >= dx2n) || (dx1n >= dx2p && dx1n >= dx2n);
        }
        //------------------------------------------------------------------------------

        private OutPt GetBottomPt(OutPt pp)
        {
          OutPt dups = null;
          OutPt p = pp.next;
          while (p != pp)
          {
            if (p.pt.Y > pp.pt.Y)
            {
              pp = p;
              dups = null;
            }
            else if (p.pt.Y == pp.pt.Y && p.pt.X <= pp.pt.X)
            {
              if (p.pt.X < pp.pt.X)
              {
                  dups = null;
                  pp = p;
              } else
              {
                if (p.next != pp && p.prev != pp) dups = p;
              }
            }
            p = p.next;
          }
          if (dups != null)
          {
            //there appears to be at least 2 vertices at bottomPt so ...
            while (dups != p)
            {
              if (!FirstIsBottomPt(p, dups)) pp = dups;
              dups = dups.next;
              while (!PointsEqual(dups.pt, pp.pt)) dups = dups.next;
            }
          }
          return pp;
        }
        //------------------------------------------------------------------------------

        private OutRec GetLowermostRec(OutRec outRec1, OutRec outRec2)
        {
            //work out which polygon fragment has the correct hole state ...
            OutPt bPt1 = outRec1.bottomPt;
            OutPt bPt2 = outRec2.bottomPt;
            if (bPt1.pt.Y > bPt2.pt.Y) return outRec1;
            else if (bPt1.pt.Y < bPt2.pt.Y) return outRec2;
            else if (bPt1.pt.X < bPt2.pt.X) return outRec1;
            else if (bPt1.pt.X > bPt2.pt.X) return outRec2;
            else if (bPt1.next == bPt1) return outRec2;
            else if (bPt2.next == bPt2) return outRec1;
            else if (FirstIsBottomPt(bPt1, bPt2)) return outRec1;
            else return outRec2;
        }
        //------------------------------------------------------------------------------

        private void AppendPolygon(TEdge e1, TEdge e2)
        {
          //get the start and ends of both output polygons ...
          OutRec outRec1 = m_PolyOuts[e1.outIdx];
          OutRec outRec2 = m_PolyOuts[e2.outIdx];

          OutRec holeStateRec;
          if (outRec1.FirstLeft == outRec2) holeStateRec = outRec2;
          else if (outRec2.FirstLeft == outRec1) holeStateRec = outRec1;
          else holeStateRec = GetLowermostRec(outRec1, outRec2);

          OutPt p1_lft = outRec1.pts;
          OutPt p1_rt = p1_lft.prev;
          OutPt p2_lft = outRec2.pts;
          OutPt p2_rt = p2_lft.prev;

          EdgeSide side;
          //join e2 poly onto e1 poly and delete pointers to e2 ...
          if(  e1.side == EdgeSide.esLeft )
          {
            if (e2.side == EdgeSide.esLeft)
            {
              //z y x a b c
              ReversePolyPtLinks(p2_lft);
              p2_lft.next = p1_lft;
              p1_lft.prev = p2_lft;
              p1_rt.next = p2_rt;
              p2_rt.prev = p1_rt;
              outRec1.pts = p2_rt;
            } else
            {
              //x y z a b c
              p2_rt.next = p1_lft;
              p1_lft.prev = p2_rt;
              p2_lft.prev = p1_rt;
              p1_rt.next = p2_lft;
              outRec1.pts = p2_lft;
            }
            side = EdgeSide.esLeft;
          } else
          {
            if (e2.side == EdgeSide.esRight)
            {
              //a b c z y x
              ReversePolyPtLinks( p2_lft );
              p1_rt.next = p2_rt;
              p2_rt.prev = p1_rt;
              p2_lft.next = p1_lft;
              p1_lft.prev = p2_lft;
            } else
            {
              //a b c x y z
              p1_rt.next = p2_lft;
              p2_lft.prev = p1_rt;
              p1_lft.prev = p2_rt;
              p2_rt.next = p1_lft;
            }
            side = EdgeSide.esRight;
          }

          if (holeStateRec == outRec2)
          {
              outRec1.bottomPt = outRec2.bottomPt;
              outRec1.bottomPt.idx = outRec1.idx;
              if (outRec2.FirstLeft != outRec1)
                  outRec1.FirstLeft = outRec2.FirstLeft;
              outRec1.isHole = outRec2.isHole;
          }
          outRec2.pts = null;
          outRec2.bottomPt = null;
          outRec2.AppendLink = outRec1;
          int OKIdx = e1.outIdx;
          int ObsoleteIdx = e2.outIdx;

          e1.outIdx = -1; //nb: safe because we only get here via AddLocalMaxPoly
          e2.outIdx = -1;

          TEdge e = m_ActiveEdges;
          while( e != null )
          {
            if( e.outIdx == ObsoleteIdx )
            {
              e.outIdx = OKIdx;
              e.side = side;
              break;
            }
            e = e.nextInAEL;
          }


          for (int i = 0; i < m_Joins.Count; ++i)
          {
              if (m_Joins[i].poly1Idx == ObsoleteIdx) m_Joins[i].poly1Idx = OKIdx;
              if (m_Joins[i].poly2Idx == ObsoleteIdx) m_Joins[i].poly2Idx = OKIdx;
          }

          for (int i = 0; i < m_HorizJoins.Count; ++i)
          {
              if (m_HorizJoins[i].savedIdx == ObsoleteIdx)
                m_HorizJoins[i].savedIdx = OKIdx;
          }

        }
        //------------------------------------------------------------------------------

        private void ReversePolyPtLinks(OutPt pp)
        {
            OutPt pp1;
            OutPt pp2;
            pp1 = pp;
            do
            {
                pp2 = pp1.next;
                pp1.next = pp1.prev;
                pp1.prev = pp2;
                pp1 = pp2;
            } while (pp1 != pp);
        }
        //------------------------------------------------------------------------------

        private static void SwapSides(TEdge edge1, TEdge edge2)
        {
            EdgeSide side = edge1.side;
            edge1.side = edge2.side;
            edge2.side = side;
        }
        //------------------------------------------------------------------------------

        private static void SwapPolyIndexes(TEdge edge1, TEdge edge2)
        {
            int outIdx = edge1.outIdx;
            edge1.outIdx = edge2.outIdx;
            edge2.outIdx = outIdx;
        }
        //------------------------------------------------------------------------------

        private void DoEdge1(TEdge edge1, TEdge edge2, IntPoint pt)
        {
            AddOutPt(edge1, pt);
            SwapSides(edge1, edge2);
            SwapPolyIndexes(edge1, edge2);
        }
        //------------------------------------------------------------------------------

        private void DoEdge2(TEdge edge1, TEdge edge2, IntPoint pt)
        {
            AddOutPt(edge2, pt);
            SwapSides(edge1, edge2);
            SwapPolyIndexes(edge1, edge2);
        }
        //------------------------------------------------------------------------------

        private void DoBothEdges(TEdge edge1, TEdge edge2, IntPoint pt)
        {
            AddOutPt(edge1, pt);
            AddOutPt(edge2, pt);
            SwapSides(edge1, edge2);
            SwapPolyIndexes(edge1, edge2);
        }
        //------------------------------------------------------------------------------

        private void IntersectEdges(TEdge e1, TEdge e2, IntPoint pt, Protects protects)
        {
            //e1 will be to the left of e2 BELOW the intersection. Therefore e1 is before
            //e2 in AEL except when e1 is being inserted at the intersection point ...

            bool e1stops = (Protects.ipLeft & protects) == 0 && e1.nextInLML == null &&
              e1.xtop == pt.X && e1.ytop == pt.Y;
            bool e2stops = (Protects.ipRight & protects) == 0 && e2.nextInLML == null &&
              e2.xtop == pt.X && e2.ytop == pt.Y;
            bool e1Contributing = (e1.outIdx >= 0);
            bool e2contributing = (e2.outIdx >= 0);

            //update winding counts...
            //assumes that e1 will be to the right of e2 ABOVE the intersection
            if (e1.polyType == e2.polyType)
            {
                if (IsEvenOddFillType(e1))
                {
                    int oldE1WindCnt = e1.windCnt;
                    e1.windCnt = e2.windCnt;
                    e2.windCnt = oldE1WindCnt;
                }
                else
                {
                    if (e1.windCnt + e2.windDelta == 0) e1.windCnt = -e1.windCnt;
                    else e1.windCnt += e2.windDelta;
                    if (e2.windCnt - e1.windDelta == 0) e2.windCnt = -e2.windCnt;
                    else e2.windCnt -= e1.windDelta;
                }
            }
            else
            {
                if (!IsEvenOddFillType(e2)) e1.windCnt2 += e2.windDelta;
                else e1.windCnt2 = (e1.windCnt2 == 0) ? 1 : 0;
                if (!IsEvenOddFillType(e1)) e2.windCnt2 -= e1.windDelta;
                else e2.windCnt2 = (e2.windCnt2 == 0) ? 1 : 0;
            }

            PolyFillType e1FillType, e2FillType, e1FillType2, e2FillType2;
            if (e1.polyType == PolyType.ptSubject)
            {
                e1FillType = m_SubjFillType;
                e1FillType2 = m_ClipFillType;
            }
            else
            {
                e1FillType = m_ClipFillType;
                e1FillType2 = m_SubjFillType;
            }
            if (e2.polyType == PolyType.ptSubject)
            {
                e2FillType = m_SubjFillType;
                e2FillType2 = m_ClipFillType;
            }
            else
            {
                e2FillType = m_ClipFillType;
                e2FillType2 = m_SubjFillType;
            }

            int e1Wc, e2Wc;
            switch (e1FillType)
            {
                case PolyFillType.pftPositive: e1Wc = e1.windCnt; break;
                case PolyFillType.pftNegative: e1Wc = -e1.windCnt; break;
                default: e1Wc = Math.Abs(e1.windCnt); break;
            }
            switch (e2FillType)
            {
                case PolyFillType.pftPositive: e2Wc = e2.windCnt; break;
                case PolyFillType.pftNegative: e2Wc = -e2.windCnt; break;
                default: e2Wc = Math.Abs(e2.windCnt); break;
            }


            if (e1Contributing && e2contributing)
            {
                if ( e1stops || e2stops || 
                  (e1Wc != 0 && e1Wc != 1) || (e2Wc != 0 && e2Wc != 1) ||
                  (e1.polyType != e2.polyType && m_ClipType != ClipType.ctXor))
                    AddLocalMaxPoly(e1, e2, pt);
                else
                    DoBothEdges(e1, e2, pt);
            }
            else if (e1Contributing)
            {
                if ((e2Wc == 0 || e2Wc == 1) && 
                  (m_ClipType != ClipType.ctIntersection || 
                    e2.polyType == PolyType.ptSubject || (e2.windCnt2 != 0))) 
                        DoEdge1(e1, e2, pt);
            }
            else if (e2contributing)
            {
                if ((e1Wc == 0 || e1Wc == 1) &&
                  (m_ClipType != ClipType.ctIntersection ||
                                e1.polyType == PolyType.ptSubject || (e1.windCnt2 != 0))) 
                        DoEdge2(e1, e2, pt);
            }
            else if ( (e1Wc == 0 || e1Wc == 1) && 
                (e2Wc == 0 || e2Wc == 1) && !e1stops && !e2stops )
            {
                //neither edge is currently contributing ...
                Int64 e1Wc2, e2Wc2;
                switch (e1FillType2)
                {
                    case PolyFillType.pftPositive: e1Wc2 = e1.windCnt2; break;
                    case PolyFillType.pftNegative: e1Wc2 = -e1.windCnt2; break;
                    default: e1Wc2 = Math.Abs(e1.windCnt2); break;
                }
                switch (e2FillType2)
                {
                    case PolyFillType.pftPositive: e2Wc2 = e2.windCnt2; break;
                    case PolyFillType.pftNegative: e2Wc2 = -e2.windCnt2; break;
                    default: e2Wc2 = Math.Abs(e2.windCnt2); break;
                }

                if (e1.polyType != e2.polyType)
                    AddLocalMinPoly(e1, e2, pt);
                else if (e1Wc == 1 && e2Wc == 1)
                    switch (m_ClipType)
                    {
                        case ClipType.ctIntersection:
                            {
                                if (e1Wc2 > 0 && e2Wc2 > 0)
                                    AddLocalMinPoly(e1, e2, pt);
                                break;
                            }
                        case ClipType.ctUnion:
                            {
                                if (e1Wc2 <= 0 && e2Wc2 <= 0)
                                    AddLocalMinPoly(e1, e2, pt);
                                break;
                            }
                        case ClipType.ctDifference:
                            {
                                if (((e1.polyType == PolyType.ptClip) && (e1Wc2 > 0) && (e2Wc2 > 0)) ||
                                   ((e1.polyType == PolyType.ptSubject) && (e1Wc2 <= 0) && (e2Wc2 <= 0)))
                                        AddLocalMinPoly(e1, e2, pt);
                                break;
                            }
                        case ClipType.ctXor:
                            {
                                AddLocalMinPoly(e1, e2, pt);
                                break;
                            }
                    }
                else 
                    SwapSides(e1, e2);
            }

            if ((e1stops != e2stops) &&
              ((e1stops && (e1.outIdx >= 0)) || (e2stops && (e2.outIdx >= 0))))
            {
                SwapSides(e1, e2);
                SwapPolyIndexes(e1, e2);
            }

            //finally, delete any non-contributing maxima edges  ...
            if (e1stops) DeleteFromAEL(e1);
            if (e2stops) DeleteFromAEL(e2);
        }
        //------------------------------------------------------------------------------

        private void DeleteFromAEL(TEdge e)
        {
            TEdge AelPrev = e.prevInAEL;
            TEdge AelNext = e.nextInAEL;
            if (AelPrev == null && AelNext == null && (e != m_ActiveEdges))
                return; //already deleted
            if (AelPrev != null)
                AelPrev.nextInAEL = AelNext;
            else m_ActiveEdges = AelNext;
            if (AelNext != null)
                AelNext.prevInAEL = AelPrev;
            e.nextInAEL = null;
            e.prevInAEL = null;
        }
        //------------------------------------------------------------------------------

        private void DeleteFromSEL(TEdge e)
        {
            TEdge SelPrev = e.prevInSEL;
            TEdge SelNext = e.nextInSEL;
            if (SelPrev == null && SelNext == null && (e != m_SortedEdges))
                return; //already deleted
            if (SelPrev != null)
                SelPrev.nextInSEL = SelNext;
            else m_SortedEdges = SelNext;
            if (SelNext != null)
                SelNext.prevInSEL = SelPrev;
            e.nextInSEL = null;
            e.prevInSEL = null;
        }
        //------------------------------------------------------------------------------

        private void UpdateEdgeIntoAEL(ref TEdge e)
        {
            if (e.nextInLML == null)
                throw new ClipperException("UpdateEdgeIntoAEL: invalid call");
            TEdge AelPrev = e.prevInAEL;
            TEdge AelNext = e.nextInAEL;
            e.nextInLML.outIdx = e.outIdx;
            if (AelPrev != null)
                AelPrev.nextInAEL = e.nextInLML;
            else m_ActiveEdges = e.nextInLML;
            if (AelNext != null)
                AelNext.prevInAEL = e.nextInLML;
            e.nextInLML.side = e.side;
            e.nextInLML.windDelta = e.windDelta;
            e.nextInLML.windCnt = e.windCnt;
            e.nextInLML.windCnt2 = e.windCnt2;
            e = e.nextInLML;
            e.prevInAEL = AelPrev;
            e.nextInAEL = AelNext;
            if (e.dx != horizontal) InsertScanbeam(e.ytop);
        }
        //------------------------------------------------------------------------------

        private void ProcessHorizontals()
        {
            TEdge horzEdge = m_SortedEdges;
            while (horzEdge != null)
            {
                DeleteFromSEL(horzEdge);
                ProcessHorizontal(horzEdge);
                horzEdge = m_SortedEdges;
            }
        }
        //------------------------------------------------------------------------------

        private void ProcessHorizontal(TEdge horzEdge)
        {
            Direction Direction;
            Int64 horzLeft, horzRight;

            if (horzEdge.xcurr < horzEdge.xtop)
            {
                horzLeft = horzEdge.xcurr;
                horzRight = horzEdge.xtop;
                Direction = Direction.dLeftToRight;
            }
            else
            {
                horzLeft = horzEdge.xtop;
                horzRight = horzEdge.xcurr;
                Direction = Direction.dRightToLeft;
            }

            TEdge eMaxPair;
            if (horzEdge.nextInLML != null)
                eMaxPair = null;
            else
                eMaxPair = GetMaximaPair(horzEdge);

            TEdge e = GetNextInAEL(horzEdge, Direction);
            while (e != null)
            {
                TEdge eNext = GetNextInAEL(e, Direction);
                if (eMaxPair != null ||
                  ((Direction == Direction.dLeftToRight) && (e.xcurr <= horzRight)) ||
                  ((Direction == Direction.dRightToLeft) && (e.xcurr >= horzLeft)))
                {
                    //ok, so far it looks like we're still in range of the horizontal edge
                    if (e.xcurr == horzEdge.xtop && eMaxPair == null)
                    {
                        if (SlopesEqual(e, horzEdge.nextInLML, m_UseFullRange))
                        {
                            //if output polygons share an edge, they'll need joining later ...
                            if (horzEdge.outIdx >= 0 && e.outIdx >= 0)
                                AddJoin(horzEdge.nextInLML, e, horzEdge.outIdx, -1);
                            break; //we've reached the end of the horizontal line
                        }
                        else if (e.dx < horzEdge.nextInLML.dx)
                            //we really have got to the end of the intermediate horz edge so quit.
                            //nb: More -ve slopes follow more +ve slopes ABOVE the horizontal.
                            break;
                    }

                    if (e == eMaxPair)
                    {
                        //horzEdge is evidently a maxima horizontal and we've arrived at its end.
                        if (Direction == Direction.dLeftToRight)
                            IntersectEdges(horzEdge, e, new IntPoint(e.xcurr, horzEdge.ycurr), 0);
                        else
                            IntersectEdges(e, horzEdge, new IntPoint(e.xcurr, horzEdge.ycurr), 0);
                        if (eMaxPair.outIdx >= 0) throw new ClipperException("ProcessHorizontal error");
                        return;
                    }
                    else if (e.dx == horizontal && !IsMinima(e) && !(e.xcurr > e.xtop))
                    {
                        if (Direction == Direction.dLeftToRight)
                            IntersectEdges(horzEdge, e, new IntPoint(e.xcurr, horzEdge.ycurr),
                              (IsTopHorz(horzEdge, e.xcurr)) ? Protects.ipLeft : Protects.ipBoth);
                        else
                            IntersectEdges(e, horzEdge, new IntPoint(e.xcurr, horzEdge.ycurr),
                              (IsTopHorz(horzEdge, e.xcurr)) ? Protects.ipRight : Protects.ipBoth);
                    }
                    else if (Direction == Direction.dLeftToRight)
                    {
                        IntersectEdges(horzEdge, e, new IntPoint(e.xcurr, horzEdge.ycurr),
                          (IsTopHorz(horzEdge, e.xcurr)) ? Protects.ipLeft : Protects.ipBoth);
                    }
                    else
                    {
                        IntersectEdges(e, horzEdge, new IntPoint(e.xcurr, horzEdge.ycurr),
                          (IsTopHorz(horzEdge, e.xcurr)) ? Protects.ipRight : Protects.ipBoth);
                    }
                    SwapPositionsInAEL(horzEdge, e);
                }
                else if ( (Direction == Direction.dLeftToRight && 
                    e.xcurr > horzRight && horzEdge.nextInSEL == null) || 
                    (Direction == Direction.dRightToLeft && 
                    e.xcurr < horzLeft && horzEdge.nextInSEL == null) ) break;
                e = eNext;
            } //end while ( e )

            if (horzEdge.nextInLML != null)
            {
                if (horzEdge.outIdx >= 0)
                    AddOutPt(horzEdge, new IntPoint(horzEdge.xtop, horzEdge.ytop));
                UpdateEdgeIntoAEL(ref horzEdge);
            }
            else
            {
                if (horzEdge.outIdx >= 0)
                    IntersectEdges(horzEdge, eMaxPair, 
                        new IntPoint(horzEdge.xtop, horzEdge.ycurr), Protects.ipBoth);
                DeleteFromAEL(eMaxPair);
                DeleteFromAEL(horzEdge);
            }
        }
        //------------------------------------------------------------------------------

        private bool IsTopHorz(TEdge horzEdge, double XPos)
        {
            TEdge e = m_SortedEdges;
            while (e != null)
            {
                if ((XPos >= Math.Min(e.xcurr, e.xtop)) && (XPos <= Math.Max(e.xcurr, e.xtop)))
                    return false;
                e = e.nextInSEL;
            }
            return true;
        }
        //------------------------------------------------------------------------------

        private TEdge GetNextInAEL(TEdge e, Direction Direction)
        {
            return Direction == Direction.dLeftToRight ? e.nextInAEL: e.prevInAEL;
        }
        //------------------------------------------------------------------------------

        private bool IsMinima(TEdge e)
        {
            return e != null && (e.prev.nextInLML != e) && (e.next.nextInLML != e);
        }
        //------------------------------------------------------------------------------

        private bool IsMaxima(TEdge e, double Y)
        {
            return (e != null && e.ytop == Y && e.nextInLML == null);
        }
        //------------------------------------------------------------------------------

        private bool IsIntermediate(TEdge e, double Y)
        {
            return (e.ytop == Y && e.nextInLML != null);
        }
        //------------------------------------------------------------------------------

        private TEdge GetMaximaPair(TEdge e)
        {
            if (!IsMaxima(e.next, e.ytop) || (e.next.xtop != e.xtop))
                return e.prev; else
                return e.next;
        }
        //------------------------------------------------------------------------------

        private bool ProcessIntersections(Int64 botY, Int64 topY)
        {
          if( m_ActiveEdges == null ) return true;
          try {
            BuildIntersectList(botY, topY);
            if ( m_IntersectNodes == null) return true;
            if ( FixupIntersections() ) ProcessIntersectList();
            else return false;
          }
          catch {
            m_SortedEdges = null;
            DisposeIntersectNodes();
            throw new ClipperException("ProcessIntersections error");
          }
          return true;
        }
        //------------------------------------------------------------------------------

        private void BuildIntersectList(Int64 botY, Int64 topY)
        {
          if ( m_ActiveEdges == null ) return;

          //prepare for sorting ...
          TEdge e = m_ActiveEdges;
          e.tmpX = TopX( e, topY );
          m_SortedEdges = e;
          m_SortedEdges.prevInSEL = null;
          e = e.nextInAEL;
          while( e != null )
          {
            e.prevInSEL = e.prevInAEL;
            e.prevInSEL.nextInSEL = e;
            e.nextInSEL = null;
            e.tmpX = TopX( e, topY );
            e = e.nextInAEL;
          }

          //bubblesort ...
          bool isModified = true;
          while( isModified && m_SortedEdges != null )
          {
            isModified = false;
            e = m_SortedEdges;
            while( e.nextInSEL != null )
            {
              TEdge eNext = e.nextInSEL;
              IntPoint pt = new IntPoint();
              if(e.tmpX > eNext.tmpX && IntersectPoint(e, eNext, ref pt))
              {
                  if (pt.Y > botY)
                  {
                      pt.Y = botY;
                      pt.X = TopX(e, pt.Y);
                  }
                  AddIntersectNode(e, eNext, pt);
                  SwapPositionsInSEL(e, eNext);
                  isModified = true;
              }
              else
                e = eNext;
            }
            if( e.prevInSEL != null ) e.prevInSEL.nextInSEL = null;
            else break;
          }
          m_SortedEdges = null;
        }
        //------------------------------------------------------------------------------

        private bool FixupIntersections()
        {
          if ( m_IntersectNodes.next == null ) return true;

          CopyAELToSEL();
          IntersectNode int1 = m_IntersectNodes;
          IntersectNode int2 = m_IntersectNodes.next;
          while (int2 != null)
          {
            TEdge e1 = int1.edge1;
            TEdge e2;
            if (e1.prevInSEL == int1.edge2) e2 = e1.prevInSEL;
            else if (e1.nextInSEL == int1.edge2) e2 = e1.nextInSEL;
            else
            {
              //The current intersection is out of order, so try and swap it with
              //a subsequent intersection ...
              while (int2 != null)
              {
                if (int2.edge1.nextInSEL == int2.edge2 ||
                  int2.edge1.prevInSEL == int2.edge2) break;
                else int2 = int2.next;
              }
              if (int2 == null) return false; //oops!!!

              //found an intersect node that can be swapped ...
              SwapIntersectNodes(int1, int2);
              e1 = int1.edge1;
              e2 = int1.edge2;
            }
            SwapPositionsInSEL(e1, e2);
            int1 = int1.next;
            int2 = int1.next;
          }

          m_SortedEdges = null;

          //finally, check the last intersection too ...
          return (int1.edge1.prevInSEL == int1.edge2 || int1.edge1.nextInSEL == int1.edge2);
        }
        //------------------------------------------------------------------------------

        private void ProcessIntersectList()
        {
          while( m_IntersectNodes != null )
          {
            IntersectNode iNode = m_IntersectNodes.next;
            {
              IntersectEdges( m_IntersectNodes.edge1 ,
                m_IntersectNodes.edge2 , m_IntersectNodes.pt, Protects.ipBoth );
              SwapPositionsInAEL( m_IntersectNodes.edge1 , m_IntersectNodes.edge2 );
            }
            m_IntersectNodes = null;
            m_IntersectNodes = iNode;
          }
        }
        //------------------------------------------------------------------------------

        private static Int64 Round(double value)
        {
            return value < 0 ? (Int64)(value - 0.5) : (Int64)(value + 0.5);
        }
        //------------------------------------------------------------------------------

        private static Int64 TopX(TEdge edge, Int64 currentY)
        {
            if (currentY == edge.ytop)
                return edge.xtop;
            return edge.xbot + Round(edge.dx *(currentY - edge.ybot));
        }
        //------------------------------------------------------------------------------

        private Int64 TopX(IntPoint pt1, IntPoint pt2, Int64 currentY)
        {
          //preconditions: pt1.Y <> pt2.Y and pt1.Y > pt2.Y
          if (currentY >= pt1.Y) return pt1.X;
          else if (currentY == pt2.Y) return pt2.X;
          else if (pt1.X == pt2.X) return pt1.X;
          else
          {
            double q = (pt1.X-pt2.X)/(pt1.Y-pt2.Y);
            return (Int64)Round(pt1.X + (currentY - pt1.Y) * q);
          }
        }
        //------------------------------------------------------------------------------

        private void AddIntersectNode(TEdge e1, TEdge e2, IntPoint pt)
        {
          IntersectNode newNode = new IntersectNode();
          newNode.edge1 = e1;
          newNode.edge2 = e2;
          newNode.pt = pt;
          newNode.next = null;
          if (m_IntersectNodes == null) m_IntersectNodes = newNode;
          else if( Process1Before2(newNode, m_IntersectNodes) )
          {
            newNode.next = m_IntersectNodes;
            m_IntersectNodes = newNode;
          }
          else
          {
            IntersectNode iNode = m_IntersectNodes;
            while( iNode.next != null  && Process1Before2(iNode.next, newNode) )
                iNode = iNode.next;
            newNode.next = iNode.next;
            iNode.next = newNode;
          }
        }
        //------------------------------------------------------------------------------

        private bool Process1Before2(IntersectNode node1, IntersectNode node2)
        {
          bool result;
          if (node1.pt.Y == node2.pt.Y)
          {
            if (node1.edge1 == node2.edge1 || node1.edge2 == node2.edge1)
            {
              result = node2.pt.X > node1.pt.X;
                return node2.edge1.dx > 0 ? !result : result;
            }
            else if (node1.edge1 == node2.edge2 || node1.edge2 == node2.edge2)
            {
              result = node2.pt.X > node1.pt.X;
                return node2.edge2.dx > 0 ? !result : result;
            }
            else return node2.pt.X > node1.pt.X;
          }
          else return node1.pt.Y > node2.pt.Y;
        }
        //------------------------------------------------------------------------------

        private void SwapIntersectNodes(IntersectNode int1, IntersectNode int2)
        {
          TEdge e1 = int1.edge1;
          TEdge e2 = int1.edge2;
          IntPoint p = int1.pt;
          int1.edge1 = int2.edge1;
          int1.edge2 = int2.edge2;
          int1.pt = int2.pt;
          int2.edge1 = e1;
          int2.edge2 = e2;
          int2.pt = p;
        }
        //------------------------------------------------------------------------------

        private bool IntersectPoint(TEdge edge1, TEdge edge2, ref IntPoint ip)
        {
          double b1, b2;
          if (SlopesEqual(edge1, edge2, m_UseFullRange)) return false;
          else if (edge1.dx == 0)
          {
            ip.X = edge1.xbot;
            if (edge2.dx == horizontal)
            {
              ip.Y = edge2.ybot;
            } else
            {
              b2 = edge2.ybot - (edge2.xbot/edge2.dx);
              ip.Y = Round(ip.X/edge2.dx + b2);
            }
          }
          else if (edge2.dx == 0)
          {
            ip.X = edge2.xbot;
            if (edge1.dx == horizontal)
            {
              ip.Y = edge1.ybot;
            } else
            {
              b1 = edge1.ybot - (edge1.xbot/edge1.dx);
              ip.Y = Round(ip.X/edge1.dx + b1);
            }
          } else
          {
            b1 = edge1.xbot - edge1.ybot * edge1.dx;
            b2 = edge2.xbot - edge2.ybot * edge2.dx;
            b2 = (b2-b1)/(edge1.dx - edge2.dx);
            ip.Y = Round(b2);
            ip.X = Round(edge1.dx * b2 + b1);
          }

          return
            //can be *so close* to the top of one edge that the rounded Y equals one ytop ...
            (ip.Y == edge1.ytop && ip.Y >= edge2.ytop && edge1.tmpX > edge2.tmpX) ||
            (ip.Y == edge2.ytop && ip.Y >= edge1.ytop && edge1.tmpX > edge2.tmpX) ||
            (ip.Y > edge1.ytop && ip.Y > edge2.ytop);
        }
        //------------------------------------------------------------------------------

        private void DisposeIntersectNodes()
        {
          while ( m_IntersectNodes != null )
          {
            IntersectNode iNode = m_IntersectNodes.next;
            m_IntersectNodes = null;
            m_IntersectNodes = iNode;
          }
        }
        //------------------------------------------------------------------------------

        private void ProcessEdgesAtTopOfScanbeam(Int64 topY)
        {
          TEdge e = m_ActiveEdges;
          while( e != null )
          {
            //1. process maxima, treating them as if they're 'bent' horizontal edges,
            //   but exclude maxima with horizontal edges. nb: e can't be a horizontal.
            if( IsMaxima(e, topY) && GetMaximaPair(e).dx != horizontal )
            {
              //'e' might be removed from AEL, as may any following edges so ...
              TEdge ePrior = e.prevInAEL;
              DoMaxima(e, topY);
              if( ePrior == null ) e = m_ActiveEdges;
              else e = ePrior.nextInAEL;
            }
            else
            {
              //2. promote horizontal edges, otherwise update xcurr and ycurr ...
              if(  IsIntermediate(e, topY) && e.nextInLML.dx == horizontal )
              {
                if (e.outIdx >= 0)
                {
                    AddOutPt(e, new IntPoint(e.xtop, e.ytop));

                    for (int i = 0; i < m_HorizJoins.Count; ++i)
                    {
                        IntPoint pt = new IntPoint(), pt2 = new IntPoint();
                        HorzJoinRec hj = m_HorizJoins[i];
                        if (GetOverlapSegment(new IntPoint(hj.edge.xbot, hj.edge.ybot),
                            new IntPoint(hj.edge.xtop, hj.edge.ytop),
                            new IntPoint(e.nextInLML.xbot, e.nextInLML.ybot),
                            new IntPoint(e.nextInLML.xtop, e.nextInLML.ytop), ref pt, ref pt2))
                                AddJoin(hj.edge, e.nextInLML, hj.savedIdx, e.outIdx);
                    }

                    AddHorzJoin(e.nextInLML, e.outIdx);
                }
                UpdateEdgeIntoAEL(ref e);
                AddEdgeToSEL(e);
              } 
              else
              {
                //this just simplifies horizontal processing ...
                e.xcurr = TopX( e, topY );
                e.ycurr = topY;
              }
              e = e.nextInAEL;
            }
          }

          //3. Process horizontals at the top of the scanbeam ...
          ProcessHorizontals();

          //4. Promote intermediate vertices ...
          e = m_ActiveEdges;
          while( e != null )
          {
            if( IsIntermediate( e, topY ) )
            {
                if (e.outIdx >= 0) AddOutPt(e, new IntPoint(e.xtop, e.ytop));
              UpdateEdgeIntoAEL(ref e);

              //if output polygons share an edge, they'll need joining later ...
              if (e.outIdx >= 0 && e.prevInAEL != null && e.prevInAEL.outIdx >= 0 &&
                e.prevInAEL.xcurr == e.xbot && e.prevInAEL.ycurr == e.ybot &&
                SlopesEqual(new IntPoint(e.xbot, e.ybot), new IntPoint(e.xtop, e.ytop),
                  new IntPoint(e.xbot, e.ybot),
                  new IntPoint(e.prevInAEL.xtop, e.prevInAEL.ytop), m_UseFullRange))
              {
                  AddOutPt(e.prevInAEL, new IntPoint(e.xbot, e.ybot));
                  AddJoin(e, e.prevInAEL, -1, -1);
              }
              else if (e.outIdx >= 0 && e.nextInAEL != null && e.nextInAEL.outIdx >= 0 &&
                e.nextInAEL.ycurr > e.nextInAEL.ytop &&
                e.nextInAEL.ycurr <= e.nextInAEL.ybot && 
                e.nextInAEL.xcurr == e.xbot && e.nextInAEL.ycurr == e.ybot &&
                SlopesEqual(new IntPoint(e.xbot, e.ybot), new IntPoint(e.xtop, e.ytop),
                  new IntPoint(e.xbot, e.ybot),
                  new IntPoint(e.nextInAEL.xtop, e.nextInAEL.ytop), m_UseFullRange))
              {
                  AddOutPt(e.nextInAEL, new IntPoint(e.xbot, e.ybot));
                  AddJoin(e, e.nextInAEL, -1, -1);
              }

            }
            e = e.nextInAEL;
          }
        }
        //------------------------------------------------------------------------------

        private void DoMaxima(TEdge e, Int64 topY)
        {
          TEdge eMaxPair = GetMaximaPair(e);
          Int64 X = e.xtop;
          TEdge eNext = e.nextInAEL;
          while( eNext != eMaxPair )
          {
            if (eNext == null) throw new ClipperException("DoMaxima error");
            IntersectEdges( e, eNext, new IntPoint(X, topY), Protects.ipBoth );
            eNext = eNext.nextInAEL;
          }
          if( e.outIdx < 0 && eMaxPair.outIdx < 0 )
          {
            DeleteFromAEL( e );
            DeleteFromAEL( eMaxPair );
          }
          else if( e.outIdx >= 0 && eMaxPair.outIdx >= 0 )
          {
              IntersectEdges(e, eMaxPair, new IntPoint(X, topY), Protects.ipNone);
          }
          else throw new ClipperException("DoMaxima error");
        }
        //------------------------------------------------------------------------------

        public static void ReversePolygons(Polygons polys)
        { 
            foreach (var poly in polys) poly.Reverse();
        }
        //------------------------------------------------------------------------------

        public static bool Orientation(Polygon poly)
        {
            int highI = poly.Count -1;
            if (highI < 2) return false;
            int j = 0, jplus, jminus;
            for (int i = 0; i <= highI; ++i) 
            {
                if (poly[i].Y < poly[j].Y) continue;
                if ((poly[i].Y > poly[j].Y || poly[i].X < poly[j].X)) j = i;
            };
            if (j == highI) jplus = 0;
            else jplus = j +1;
            if (j == 0) jminus = highI;
            else jminus = j -1;

            //get cross product of vectors of the edges adjacent to highest point ...
            IntPoint vec1 = new IntPoint(poly[j].X - poly[jminus].X, poly[j].Y - poly[jminus].Y);
            IntPoint vec2 = new IntPoint(poly[jplus].X - poly[j].X, poly[jplus].Y - poly[j].Y);
            if (Math.Abs(vec1.X) > loRange || Math.Abs(vec1.Y) > loRange ||
                Math.Abs(vec2.X) > loRange || Math.Abs(vec2.Y) > loRange)
            {
                if (Math.Abs(vec1.X) > hiRange || Math.Abs(vec1.Y) > hiRange ||
                    Math.Abs(vec2.X) > hiRange || Math.Abs(vec2.Y) > hiRange)
                    throw new ClipperException("Coordinate exceeds range bounds.");
                Int128 cross = Int128.Int128Mul(vec1.X, vec2.Y) - Int128.Int128Mul(vec2.X, vec1.Y);
                return !cross.IsNegative();
            }
            else
                return (vec1.X * vec2.Y - vec2.X * vec1.Y) > 0;
        }
        //------------------------------------------------------------------------------

        private static bool FullRangeNeeded(Polygon pts)
        {
            bool result = false;
            for (int i = 0; i <  pts.Count; i++) 
            {
                if (Math.Abs(pts[i].X) > hiRange || Math.Abs(pts[i].Y) > hiRange)
                    throw new ClipperException("Coordinate exceeds range bounds.");
                else if (Math.Abs(pts[i].X) > loRange || Math.Abs(pts[i].Y) > loRange)
                  result = true;
            }
            return result;
        }
        //------------------------------------------------------------------------------

        private bool Orientation(OutRec outRec, bool UseFull64BitRange)
        {
            //first make sure bottomPt is correctly assigned ...
            OutPt opBottom = outRec.pts, op = outRec.pts.next;
            while (op != outRec.pts) 
            {
	            if (op.pt.Y >= opBottom.pt.Y) 
	            {
		            if (op.pt.Y > opBottom.pt.Y || op.pt.X < opBottom.pt.X) 
		            opBottom = op;
	            }
	            op = op.next;
            }
            outRec.bottomPt = opBottom;
            opBottom.idx = outRec.idx;
            
            op = opBottom;
            //find vertices either side of bottomPt (skipping duplicate points) ....
            OutPt opPrev = op.prev;
            OutPt opNext = op.next;
            while (op != opPrev && PointsEqual(op.pt, opPrev.pt)) 
              opPrev = opPrev.prev;
            while (op != opNext && PointsEqual(op.pt, opNext.pt))
              opNext = opNext.next;

            IntPoint vec1 = new IntPoint(op.pt.X - opPrev.pt.X, op.pt.Y - opPrev.pt.Y);
            IntPoint vec2 = new IntPoint(opNext.pt.X - op.pt.X, opNext.pt.Y - op.pt.Y);

            if (UseFull64BitRange)
            {
                Int128 cross = Int128.Int128Mul(vec1.X, vec2.Y) - Int128.Int128Mul(vec2.X, vec1.Y);
                return !cross.IsNegative();
            }
            else
                return (vec1.X * vec2.Y - vec2.X * vec1.Y) > 0;

        }
        //------------------------------------------------------------------------------

        private int PointCount(OutPt pts)
        {
            if (pts == null) return 0;
            int result = 0;
            OutPt p = pts;
            do
            {
                result++;
                p = p.next;
            }
            while (p != pts);
            return result;
        }
        //------------------------------------------------------------------------------

        private void BuildResult(Polygons polyg)
        {
            polyg.Clear();
            polyg.Capacity = m_PolyOuts.Count;
            foreach (OutRec outRec in m_PolyOuts)
            {
                if (outRec.pts == null) continue;
                OutPt p = outRec.pts;
                int cnt = PointCount(p);
                if (cnt < 3) continue;
                Polygon pg = new Polygon(cnt);
                for (int j = 0; j < cnt; j++)
                {
                    pg.Add(p.pt);
                    p = p.next;
                }
                polyg.Add(pg);
            }
        }
        //------------------------------------------------------------------------------

        private void BuildResultEx(ExPolygons polyg)
        {         
            polyg.Clear();
            polyg.Capacity = m_PolyOuts.Count;
            int i = 0;
            while (i < m_PolyOuts.Count)
            {
                OutRec outRec = m_PolyOuts[i++];
                if (outRec.pts == null) break; //nb: already sorted here
                OutPt p = outRec.pts;
                int cnt = PointCount(p);
                if (cnt < 3) continue;
                ExPolygon epg = new ExPolygon();
                epg.outer = new Polygon(cnt);
                epg.holes = new Polygons();
                for (int j = 0; j < cnt; j++)
                {
                    epg.outer.Add(p.pt);
                    p = p.next;
                }
                while (i < m_PolyOuts.Count)
                {
                    outRec = m_PolyOuts[i];
                    if (outRec.pts == null || !outRec.isHole) break;
                    Polygon pg = new Polygon();
                    p = outRec.pts;
                    do
                    {
                        pg.Add(p.pt);
                        p = p.next;
                    } while (p != outRec.pts);
                    epg.holes.Add(pg);
                    i++;
                }
                polyg.Add(epg);
            }
        }
        //------------------------------------------------------------------------------

        private void FixupOutPolygon(OutRec outRec)
        {
            //FixupOutPolygon() - removes duplicate points and simplifies consecutive
            //parallel edges by removing the middle vertex.
            OutPt lastOK = null;
            outRec.pts = outRec.bottomPt;
            OutPt pp = outRec.bottomPt;
            for (;;)
            {
                if (pp.prev == pp || pp.prev == pp.next)
                {
                    DisposeOutPts(pp);
                    outRec.pts = null;
                    outRec.bottomPt = null;
                    return;
                }
                //test for duplicate points and for same slope (cross-product) ...
                if (PointsEqual(pp.pt, pp.next.pt) ||
                  SlopesEqual(pp.prev.pt, pp.pt, pp.next.pt, m_UseFullRange))
                {
                    lastOK = null;
                    if (pp == outRec.bottomPt)
                         outRec.bottomPt = null; //flags need for updating
                    pp.prev.next = pp.next;
                    pp.next.prev = pp.prev;
                    pp = pp.prev;
                }
                else if (pp == lastOK) break;
                else
                {
                    if (lastOK == null) lastOK = pp;
                    pp = pp.next;
                }
            }
          if (outRec.bottomPt == null) 
          {
            outRec.bottomPt = GetBottomPt(pp);
            outRec.bottomPt.idx = outRec.idx;
            outRec.pts = outRec.bottomPt;
          }
        }
        //------------------------------------------------------------------------------

        private void CheckHoleLinkages1(OutRec outRec1, OutRec outRec2)
        {
          //when a polygon is split into 2 polygons, make sure any holes the original
          //polygon contained link to the correct polygon ...
          for (int i = 0; i < m_PolyOuts.Count; ++i)
          {
            if (m_PolyOuts[i].isHole && m_PolyOuts[i].bottomPt != null &&
                m_PolyOuts[i].FirstLeft == outRec1 &&
                !PointInPolygon(m_PolyOuts[i].bottomPt.pt, 
                outRec1.pts, m_UseFullRange))
                    m_PolyOuts[i].FirstLeft = outRec2;
          }
        }
        //----------------------------------------------------------------------

        private void CheckHoleLinkages2(OutRec outRec1, OutRec outRec2)
        {
          //if a hole is owned by outRec2 then make it owned by outRec1 ...
          for (int i = 0; i < m_PolyOuts.Count; ++i)
            if (m_PolyOuts[i].isHole && m_PolyOuts[i].bottomPt != null &&
              m_PolyOuts[i].FirstLeft == outRec2)
                m_PolyOuts[i].FirstLeft = outRec1;
        }
        //----------------------------------------------------------------------

        private void JoinCommonEdges(bool fixHoleLinkages)
        {
          for (int i = 0; i < m_Joins.Count; i++)
          {
            JoinRec j = m_Joins[i];
            OutRec outRec1 = m_PolyOuts[j.poly1Idx];
            OutPt pp1a = outRec1.pts;
            OutRec outRec2 = m_PolyOuts[j.poly2Idx];
            OutPt pp2a = outRec2.pts;
            IntPoint pt1 = new IntPoint(j.pt2a);
            IntPoint pt2 = new IntPoint(j.pt2b);
            IntPoint pt3 = new IntPoint(j.pt1a);
            IntPoint pt4 = new IntPoint(j.pt1b);
            if (!FindSegment(ref pp1a, ref pt1, ref pt2)) continue;
            if (j.poly1Idx == j.poly2Idx)
            {
                //we're searching the same polygon for overlapping segments so
                //segment 2 mustn't be the same as segment 1 ...
                pp2a = pp1a.next;
                if (!FindSegment(ref pp2a, ref pt3, ref pt4) || (pp2a == pp1a)) continue;
            }
            else if (!FindSegment(ref pp2a, ref pt3, ref pt4)) continue;

            if (!GetOverlapSegment(pt1, pt2, pt3, pt4, ref pt1, ref pt2)) continue;

            OutPt p1, p2, p3, p4;
            OutPt prev = pp1a.prev;
            //get p1 & p2 polypts - the overlap start & endpoints on poly1
            
            if (PointsEqual(pp1a.pt, pt1)) p1 = pp1a;
            else if (PointsEqual(prev.pt, pt1)) p1 = prev;
            else p1 = InsertPolyPtBetween(pp1a, prev, pt1);

            if (PointsEqual(pp1a.pt, pt2)) p2 = pp1a;
            else if (PointsEqual(prev.pt, pt2)) p2 = prev;
            else if ((p1 == pp1a) || (p1 == prev))
                p2 = InsertPolyPtBetween(pp1a, prev, pt2);
            else if (Pt3IsBetweenPt1AndPt2(pp1a.pt, p1.pt, pt2))
                p2 = InsertPolyPtBetween(pp1a, p1, pt2); 
            else
                p2 = InsertPolyPtBetween(p1, prev, pt2);

            //get p3 & p4 polypts - the overlap start & endpoints on poly2
            prev = pp2a.prev;
            if (PointsEqual(pp2a.pt, pt1)) p3 = pp2a;
            else if (PointsEqual(prev.pt, pt1)) p3 = prev;
            else p3 = InsertPolyPtBetween(pp2a, prev, pt1);

            if (PointsEqual(pp2a.pt, pt2)) p4 = pp2a;
            else if (PointsEqual(prev.pt, pt2)) p4 = prev;
            else if ((p3 == pp2a) || (p3 == prev))
                p4 = InsertPolyPtBetween(pp2a, prev, pt2);
            else if (Pt3IsBetweenPt1AndPt2(pp2a.pt, p3.pt, pt2))
                p4 = InsertPolyPtBetween(pp2a, p3, pt2);
            else
                p4 = InsertPolyPtBetween(p3, prev, pt2);

            //p1.pt should equal p3.pt and p2.pt should equal p4.pt here, so ...
            //join p1 to p3 and p2 to p4 ...
            if (p1.next == p2 && p3.prev == p4)
            {
                p1.next = p3;
                p3.prev = p1;
                p2.prev = p4;
                p4.next = p2;
            }
            else if (p1.prev == p2 && p3.next == p4)
            {
                p1.prev = p3;
                p3.next = p1;
                p2.next = p4;
                p4.prev = p2;
            }
            else
                continue; //an orientation is probably wrong

            if (j.poly2Idx == j.poly1Idx)
            {
                //instead of joining two polygons, we've just created a new one by
                //splitting one polygon into two.
                outRec1.pts = GetBottomPt(p1);
                outRec1.bottomPt = outRec1.pts;
                outRec1.bottomPt.idx = outRec1.idx;
                outRec2 = CreateOutRec();
                m_PolyOuts.Add(outRec2);
                outRec2.idx = m_PolyOuts.Count - 1;
                j.poly2Idx = outRec2.idx;
                outRec2.pts = GetBottomPt(p2);
                outRec2.bottomPt = outRec2.pts;
                outRec2.bottomPt.idx = outRec2.idx;

                if (PointInPolygon(outRec2.pts.pt, outRec1.pts, m_UseFullRange))
                {
                    //outRec1 is contained by outRec2 ...
                    outRec2.isHole = !outRec1.isHole;
                    outRec2.FirstLeft = outRec1;
                    if (outRec2.isHole == Orientation(outRec2, m_UseFullRange)) 
                      ReversePolyPtLinks(outRec2.pts);
                }
                else if (PointInPolygon(outRec1.pts.pt, outRec2.pts, m_UseFullRange))
                {
                    //outRec2 is contained by outRec1 ...
                    outRec2.isHole = outRec1.isHole;
                    outRec1.isHole = !outRec2.isHole;
                    outRec2.FirstLeft = outRec1.FirstLeft;
                    outRec1.FirstLeft = outRec2;
                    if (outRec1.isHole == Orientation(outRec1, m_UseFullRange))
                      ReversePolyPtLinks(outRec1.pts);
                    //make sure any contained holes now link to the correct polygon ...
                    if (fixHoleLinkages) CheckHoleLinkages1(outRec1, outRec2);
                }
                else
                {
                    outRec2.isHole = outRec1.isHole;
                    outRec2.FirstLeft = outRec1.FirstLeft;
                    //make sure any contained holes now link to the correct polygon ...
                    if (fixHoleLinkages) CheckHoleLinkages1(outRec1, outRec2);
                }

                //now fixup any subsequent m_Joins that match this polygon
                for (int k = i + 1; k < m_Joins.Count; k++)
                {
                    JoinRec j2 = m_Joins[k];
                    if (j2.poly1Idx == j.poly1Idx && PointIsVertex(j2.pt1a, p2))
                        j2.poly1Idx = j.poly2Idx;
                    if (j2.poly2Idx == j.poly1Idx && PointIsVertex(j2.pt2a, p2))
                        j2.poly2Idx = j.poly2Idx;
                }
                
                //now cleanup redundant edges too ...
                FixupOutPolygon(outRec1);
                FixupOutPolygon(outRec2);
            }
            else
            {
                //joined 2 polygons together ...

                //make sure any holes contained by outRec2 now link to outRec1 ...
                if (fixHoleLinkages) CheckHoleLinkages2(outRec1, outRec2);

                //now cleanup redundant edges too ...
                FixupOutPolygon(outRec1);

                if (outRec1.pts != null)
                {
                    outRec1.isHole = !Orientation(outRec1, m_UseFullRange);
                    if (outRec1.isHole &&  outRec1.FirstLeft == null) 
                      outRec1.FirstLeft = outRec2.FirstLeft;
                }

                //delete the obsolete pointer ...
                int OKIdx = outRec1.idx;
                int ObsoleteIdx = outRec2.idx;
                outRec2.pts = null;
                outRec2.bottomPt = null;
                outRec2.AppendLink = outRec1;

                //now fixup any subsequent joins that match this polygon
                for (int k = i + 1; k < m_Joins.Count; k++)
                {
                    JoinRec j2 = m_Joins[k];
                    if (j2.poly1Idx == ObsoleteIdx) j2.poly1Idx = OKIdx;
                    if (j2.poly2Idx == ObsoleteIdx) j2.poly2Idx = OKIdx;
                }
            }
          }
        }
        //------------------------------------------------------------------------------

        public static double Area(Polygon poly)
        {
            int highI = poly.Count - 1;
            if (highI < 2) return 0;
            if (FullRangeNeeded(poly))
            {
                Int128 a = new Int128();
                a = Int128.Int128Mul(poly[highI].X, poly[0].Y) -
                    Int128.Int128Mul(poly[0].X, poly[highI].Y);
                for (int i = 0; i < highI; ++i)
                    a += Int128.Int128Mul(poly[i].X, poly[i + 1].Y) -
                    Int128.Int128Mul(poly[i + 1].X, poly[i].Y);
                return a.ToDouble() / 2;
            }
            else
            {
                double area = (double)poly[highI].X * (double)poly[0].Y -
                    (double)poly[0].X * (double)poly[highI].Y;
                for (int i = 0; i < highI; ++i)
                    area += (double)poly[i].X * (double)poly[i + 1].Y -
                        (double)poly[i + 1].X * (double)poly[i].Y;
                return area / 2;
            }
        }
        //------------------------------------------------------------------------------

        double Area(OutRec outRec, bool UseFull64BitRange)
        {
          OutPt op = outRec.pts;
          if (UseFull64BitRange) 
          {
            Int128 a = new Int128(0);
            do
            {
                a += Int128.Int128Mul(op.prev.pt.X, op.pt.Y) -
                    Int128.Int128Mul(op.pt.X, op.prev.pt.Y);
                op = op.next;
            } while (op != outRec.pts);
            return a.ToDouble() / 2;          
          }
          else
          {
            double a = 0;
            do {
              a += (op.prev.pt.X * op.pt.Y) - (op.pt.X * op.prev.pt.Y);
              op = op.next;
            } while (op != outRec.pts);
            return a/2;
          }
        }

        //------------------------------------------------------------------------------
        // OffsetPolygon functions ...
        //------------------------------------------------------------------------------

        internal static Polygon BuildArc(IntPoint pt, double a1, double a2, double r)
        {
            int steps = Math.Max(6, (int)(Math.Sqrt(Math.Abs(r)) * Math.Abs(a2 - a1)));
            Polygon result = new Polygon(steps);
            int n = steps - 1;
            double da = (a2 - a1) / n;
            double a = a1;
            for (int i = 0; i < steps; ++i)
            {
                result.Add(new IntPoint(pt.X + Round(Math.Cos(a) * r), pt.Y + Round(Math.Sin(a) * r)));
                a += da;
            }
            return result;
        }
        //------------------------------------------------------------------------------

        internal static DoublePoint GetUnitNormal(IntPoint pt1, IntPoint pt2)
        {
            double dx = (pt2.X - pt1.X);
            double dy = (pt2.Y - pt1.Y);
            if ((dx == 0) && (dy == 0)) return new DoublePoint();

            double f = 1 * 1.0 / Math.Sqrt(dx * dx + dy * dy);
            dx *= f;
            dy *= f;

            return new DoublePoint(dy, -dx);
        }
        //------------------------------------------------------------------------------

        internal class DoublePoint
        {
            public double X { get; set; }
            public double Y { get; set; }
            public DoublePoint(double x = 0, double y = 0)
            {
                this.X = x; this.Y = y;
            }
        };
        //------------------------------------------------------------------------------

        private class PolyOffsetBuilder
        {
            private Polygons pts; 
            private Polygon currentPoly;
            private List<DoublePoint> normals;
            private double delta, m_R;
            private int m_i, m_j, m_k;
            private const int buffLength = 128;

            public PolyOffsetBuilder(Polygons pts, Polygons solution, double delta, JoinType jointype, double MiterLimit = 2)
            {
                //precondtion: solution != pts

                if (delta == 0)
                {
                    solution = pts;
                    return;
                }

                this.pts = pts;
                this.delta = delta;
                if (MiterLimit <= 1) MiterLimit = 1;
                double RMin = 2/(MiterLimit*MiterLimit);

                normals = new List<DoublePoint>();

                solution.Clear();
                solution.Capacity = pts.Count;
                for (m_i = 0; m_i < pts.Count; m_i++)
                {
                    int len = pts[m_i].Count;
                    if (len > 1 && pts[m_i][0].X == pts[m_i][len - 1].X &&
                        pts[m_i][0].Y == pts[m_i][len - 1].Y) len--;

                    if (len == 0 || (len < 3 && delta <= 0)) 
                        continue;
                    else if (len == 1)
                    {
                        Polygon arc;
                        arc = BuildArc(pts[m_i][len - 1], 0, 2 * Math.PI, delta);
                        solution.Add(arc);
                        continue;
                    }

                    //build normals ...
                    normals.Clear();
                    normals.Capacity = len;
                    for (int j = 0; j < len -1; ++j)
                        normals.Add(GetUnitNormal(pts[m_i][j], pts[m_i][j+1]));
                    normals.Add(GetUnitNormal(pts[m_i][len - 1], pts[m_i][0]));

                    currentPoly = new Polygon();
                    m_k = len - 1;
                    for (m_j = 0; m_j < len; ++m_j)
                    {
                        switch (jointype)
                        {
                            case JoinType.jtMiter:
                            {
                                m_R = 1 + (normals[m_j].X*normals[m_k].X + 
                                    normals[m_j].Y*normals[m_k].Y);
                                if (m_R >= RMin) DoMiter(); else DoSquare(MiterLimit);
                                break;
                            }
                            case JoinType.jtRound: 
                                DoRound();
                                break;
                            case JoinType.jtSquare:
                                DoSquare(1);
                                break;
                        }
                        m_k = m_j;
                    }
                    solution.Add(currentPoly);
                }

                //finally, clean up untidy corners ...
                Clipper clpr = new Clipper();
                clpr.AddPolygons(solution, PolyType.ptSubject);
                if (delta > 0)
                {
                    clpr.Execute(ClipType.ctUnion, solution, PolyFillType.pftPositive, PolyFillType.pftPositive);
                }
                else
                {
                    IntRect r = clpr.GetBounds();
                    Polygon outer = new Polygon(4);

                    outer.Add(new IntPoint(r.left - 10, r.bottom + 10));
                    outer.Add(new IntPoint(r.right + 10, r.bottom + 10));
                    outer.Add(new IntPoint(r.right + 10, r.top - 10));
                    outer.Add(new IntPoint(r.left - 10, r.top - 10));

                    clpr.AddPolygon(outer, PolyType.ptSubject);
                    clpr.Execute(ClipType.ctUnion, solution, PolyFillType.pftNegative, PolyFillType.pftNegative);
                    if (solution.Count > 0)
                    {
                        solution.RemoveAt(0);
                        for (int i = 0; i < solution.Count; i++)
                            solution[i].Reverse();
                    }
                }
            }
            //------------------------------------------------------------------------------
            
            internal void AddPoint(IntPoint pt)
            {
                int len = currentPoly.Count;
                if (len == currentPoly.Capacity)
                    currentPoly.Capacity = len + buffLength;
                currentPoly.Add(pt);
            }
            //------------------------------------------------------------------------------

            internal void DoSquare(double mul)
            {
                IntPoint pt1 = new IntPoint((Int64)Round(pts[m_i][m_j].X + normals[m_k].X * delta),
                    (Int64)Round(pts[m_i][m_j].Y + normals[m_k].Y * delta));
                IntPoint pt2 = new IntPoint((Int64)Round(pts[m_i][m_j].X + normals[m_j].X * delta),
                    (Int64)Round(pts[m_i][m_j].Y + normals[m_j].Y * delta));
                if ((normals[m_k].X * normals[m_j].Y - normals[m_j].X * normals[m_k].Y) * delta >= 0)
                {
                    double a1 = Math.Atan2(normals[m_k].Y, normals[m_k].X);
                    double a2 = Math.Atan2(-normals[m_j].Y, -normals[m_j].X);
                    a1 = Math.Abs(a2 - a1);
                    if (a1 > Math.PI) a1 = Math.PI * 2 - a1;
                    double dx = Math.Tan((Math.PI - a1) / 4) * Math.Abs(delta * mul);
                    pt1 = new IntPoint((Int64)(pt1.X - normals[m_k].Y * dx),
                        (Int64)(pt1.Y + normals[m_k].X * dx));
                    AddPoint(pt1);
                    pt2 = new IntPoint((Int64)(pt2.X + normals[m_j].Y * dx),
                        (Int64)(pt2.Y - normals[m_j].X * dx));
                    AddPoint(pt2);
                }
                else
                {
                    AddPoint(pt1);
                    AddPoint(pts[m_i][m_j]);
                    AddPoint(pt2);
                }
            }
            //------------------------------------------------------------------------------

            internal void DoMiter()
            {
                if ((normals[m_k].X * normals[m_j].Y - normals[m_j].X * normals[m_k].Y) * delta >= 0)
                {
                    double q = delta / m_R;
                    AddPoint(new IntPoint((Int64)Round(pts[m_i][m_j].X + 
                        (normals[m_k].X + normals[m_j].X) * q),
                        (Int64)Round(pts[m_i][m_j].Y + (normals[m_k].Y + normals[m_j].Y) * q)));
                }
                else
                {
                    IntPoint pt1 = new IntPoint((Int64)Round(pts[m_i][m_j].X + normals[m_k].X * delta),
                        (Int64)Round(pts[m_i][m_j].Y + normals[m_k].Y * delta));
                    IntPoint pt2 = new IntPoint((Int64)Round(pts[m_i][m_j].X + normals[m_j].X * delta),
                        (Int64)Round(pts[m_i][m_j].Y + normals[m_j].Y * delta));
                    AddPoint(pt1);
                    AddPoint(pts[m_i][m_j]);
                    AddPoint(pt2);
                }
            }
            //------------------------------------------------------------------------------

            internal void DoRound()
            {
                IntPoint pt1 = new IntPoint(Round(pts[m_i][m_j].X + normals[m_k].X * delta),
                    Round(pts[m_i][m_j].Y + normals[m_k].Y * delta));
                IntPoint pt2 = new IntPoint(Round(pts[m_i][m_j].X + normals[m_j].X * delta),
                    Round(pts[m_i][m_j].Y + normals[m_j].Y * delta));
                AddPoint(pt1);
                //round off reflex angles (ie > 180 deg) unless almost flat (ie < 10deg).
                //cross product normals < 0 . angle > 180 deg.
                //dot product normals == 1 . no angle
                if ((normals[m_k].X * normals[m_j].Y - normals[m_j].X * normals[m_k].Y) * delta >= 0)
                {
                    if ((normals[m_j].X * normals[m_k].X + normals[m_j].Y * normals[m_k].Y) < 0.985)
                    {
                        double a1 = Math.Atan2(normals[m_k].Y, normals[m_k].X);
                        double a2 = Math.Atan2(normals[m_j].Y, normals[m_j].X);
                        if (delta > 0 && a2 < a1) a2 += Math.PI * 2;
                        else if (delta < 0 && a2 > a1) a2 -= Math.PI * 2;
                        Polygon arc = BuildArc(pts[m_i][m_j], a1, a2, delta);
                        for (int m = 0; m < arc.Count; m++)
                            AddPoint(arc[m]);
                    }
                }
                else
                    AddPoint(pts[m_i][m_j]);
                AddPoint(pt2);
            }
            //------------------------------------------------------------------------------

        } //end PolyOffsetBuilder
        //------------------------------------------------------------------------------

        public static Polygons OffsetPolygons(Polygons poly, double delta,
            JoinType jointype, double MiterLimit)
        {
            Polygons result = new Polygons(poly.Count);
            new PolyOffsetBuilder(poly, result, delta, jointype, MiterLimit);
            return result;
        }
        //------------------------------------------------------------------------------

        public static Polygons OffsetPolygons(Polygons poly, double delta, JoinType jointype)
        {
            Polygons result = new Polygons(poly.Count);
            new PolyOffsetBuilder(poly, result, delta, jointype, 2.0);
            return result;
        }
        //------------------------------------------------------------------------------

        public static Polygons OffsetPolygons(Polygons poly, double delta)
        {
            Polygons result = new Polygons(poly.Count);
            new PolyOffsetBuilder(poly, result, delta, JoinType.jtSquare, 2.0);
            return result;
        }

        //------------------------------------------------------------------------------
        // SimplifyPolygon functions ...
        // Convert self-intersecting polygons into simple polygons
        //------------------------------------------------------------------------------

        public static Polygons SimplifyPolygon(Polygon poly)
        {
            Polygons result = new Polygons();
            Clipper c = new Clipper();
            c.AddPolygon(poly, PolyType.ptSubject);
            c.Execute(ClipType.ctUnion, result);
            return result;
        }
        //------------------------------------------------------------------------------

        public static Polygons SimplifyPolygons(Polygons polys)
        {
            Polygons result = new Polygons();
            Clipper c = new Clipper();
            c.AddPolygons(polys, PolyType.ptSubject);
            c.Execute(ClipType.ctUnion, result);
            return result;
        }
        //------------------------------------------------------------------------------

    } //end ClipperLib namespace
  
    class ClipperException : Exception
    {
        public ClipperException(string description) : base(description){}
    }
    //------------------------------------------------------------------------------
}
