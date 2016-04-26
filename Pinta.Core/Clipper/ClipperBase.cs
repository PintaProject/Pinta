using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClipperLibrary
{
    using Polygon = List<IntPoint>;
    using Polygons = List<List<IntPoint>>;

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
            return (pt1.X == pt2.X && pt1.Y == pt2.Y);
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
                        Int128.Int128Mul(pp2.prev.pt.X - pp2.pt.X, pt.Y - pp2.pt.Y) /
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
              (Int64)(e1.xtop - e1.xbot) * (e2.ytop - e2.ybot) == 0;
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
            while (m_MinimaList != null)
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
                else if (j > 0 && SlopesEqual(p[j - 1], p[j], pg[i], m_UseFullRange))
                {
                    if (PointsEqual(p[j - 1], pg[i])) j--;
                }
                else j++;
                if (j < p.Count)
                    p[j] = pg[i];
                else
                    p.Add(new IntPoint(pg[i].X, pg[i].Y));
            }
            if (j < 2) return false;

            len = j + 1;
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
            InitEdge(edges[len - 1], edges[0], edges[len - 2], p[len - 1], polyType);
            for (int i = len - 2; i > 0; --i)
                InitEdge(edges[i], edges[i + 1], edges[i - 1], p[i], polyType);
            InitEdge(edges[0], edges[1], edges[len - 1], p[0], polyType);

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
            while (e != edges[0]);

            //make sure eHighest is positioned so the following loop works safely ...
            if (eHighest.windDelta > 0) eHighest = eHighest.next;
            if (eHighest.dx == horizontal) eHighest = eHighest.next;

            //finally insert each local minima ...
            e = eHighest;
            do
            {
                e = AddBoundsToLML(e);
            }
            while (e != eHighest);
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
            }
            else
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
            else e.dx = (double)(e.xtop - e.xbot) / (e.ytop - e.ybot);
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
                if (e.dx == horizontal)
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

            if (e.dx == horizontal) //horizontal edges never start a left bound
            {
                if (e.xbot != e.prev.xbot) SwapX(e);
                newLm.leftBound = e.prev;
                newLm.rightBound = e;
            }
            else if (e.dx < e.prev.dx)
            {
                newLm.leftBound = e.prev;
                newLm.rightBound = e;
            }
            else
            {
                newLm.leftBound = e;
                newLm.rightBound = e.prev;
            }
            newLm.leftBound.side = EdgeSide.esLeft;
            newLm.rightBound.side = EdgeSide.esRight;
            InsertLocalMinima(newLm);

            for (;;)
            {
                if (e.next.ytop == e.ytop && e.next.dx != horizontal) break;
                e.nextInLML = e.next;
                e = e.next;
                if (e.dx == horizontal && e.xbot != e.prev.xtop) SwapX(e);
            }
            return e.next;
        }
        //------------------------------------------------------------------------------

        private void InsertLocalMinima(LocalMinima newLm)
        {
            if (m_MinimaList == null)
            {
                m_MinimaList = newLm;
            }
            else if (newLm.Y >= m_MinimaList.Y)
            {
                newLm.next = m_MinimaList;
                m_MinimaList = newLm;
            }
            else
            {
                LocalMinima tmpLm = m_MinimaList;
                while (tmpLm.next != null && (newLm.Y < tmpLm.next.Y))
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
                for (;;)
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
}
