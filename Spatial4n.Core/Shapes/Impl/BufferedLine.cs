using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Shapes.Impl
{
    /// <summary>
    /// INTERNAL: A line between two points with a buffer distance extending in every direction. By
    /// contrast, an un-buffered line covers no area and as such is extremely unlikely to intersect with
    /// a point. BufferedLine isn't yet aware of geodesics (e.g. the dateline); it operates in Euclidean
    /// space.
    /// </summary>
    public class BufferedLine : Shape
    {
        private readonly Point pA, pB;
        private readonly double buf;
        private readonly Rectangle bbox;
        /**
         * the primary line; passes through pA & pB
         */
        private readonly InfBufLine linePrimary;
        /**
         * perpendicular to the primary line, centered between pA & pB
         */
        private readonly InfBufLine linePerp;

        /**
         * Creates a buffered line from pA to pB. The buffer extends on both sides of
         * the line, making the width 2x the buffer. The buffer extends out from
         * pA & pB, making the line in effect 2x the buffer longer than pA to pB.
         *
         * @param pA  start point
         * @param pB  end point
         * @param buf the buffer distance in degrees
         * @param ctx
         */
        public BufferedLine(Point pA, Point pB, double buf, SpatialContext ctx)
        {
            Debug.Assert(buf >= 0);//TODO support buf=0 via another class ?

            /**
             * If true, buf should bump-out from the pA & pB, in effect
             *                  extending the line a little.
             */
            bool bufExtend = true;//TODO support false and make this a
                                  // parameter

            this.pA = pA;
            this.pB = pB;
            this.buf = buf;

            double deltaY = pB.GetY() - pA.GetY();
            double deltaX = pB.GetX() - pA.GetX();

            PointImpl center = new PointImpl(pA.GetX() + deltaX / 2,
                pA.GetY() + deltaY / 2, null);

            double perpExtent = bufExtend ? buf : 0;

            if (deltaX == 0 && deltaY == 0)
            {
                linePrimary = new InfBufLine(0, center, buf);
                linePerp = new InfBufLine(double.PositiveInfinity, center, buf);
            }
            else
            {
                linePrimary = new InfBufLine(deltaY / deltaX, center, buf);
                double length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                linePerp = new InfBufLine(-deltaX / deltaY, center,
                    length / 2 + perpExtent);
            }

            double minY, maxY;
            double minX, maxX;
            if (deltaX == 0)
            { // vertical
                if (pA.GetY() <= pB.GetY())
                {
                    minY = pA.GetY();
                    maxY = pB.GetY();
                }
                else
                {
                    minY = pB.GetY();
                    maxY = pA.GetY();
                }
                minX = pA.GetX() - buf;
                maxX = pA.GetX() + buf;
                minY = minY - perpExtent;
                maxY = maxY + perpExtent;

            }
            else
            {
                if (!bufExtend)
                {
                    throw new NotSupportedException("TODO");
                    //solve for B & A (C=buf), one is buf-x, other is buf-y.
                }

                //Given a right triangle of A, B, C sides, C (hypotenuse) ==
                // buf, and A + B == the bounding box offset from pA & pB in x & y.
                double bboxBuf = buf * (1 + Math.Abs(linePrimary.getSlope()))
                    * linePrimary.getDistDenomInv();
                Debug.Assert(bboxBuf >= buf && bboxBuf <= buf * 1.5);

                if (pA.GetX() <= pB.GetX())
                {
                    minX = pA.GetX() - bboxBuf;
                    maxX = pB.GetX() + bboxBuf;
                }
                else
                {
                    minX = pB.GetX() - bboxBuf;
                    maxX = pA.GetX() + bboxBuf;
                }
                if (pA.GetY() <= pB.GetY())
                {
                    minY = pA.GetY() - bboxBuf;
                    maxY = pB.GetY() + bboxBuf;
                }
                else
                {
                    minY = pB.GetY() - bboxBuf;
                    maxY = pA.GetY() + bboxBuf;
                }

            }
            Rectangle bounds = ctx.GetWorldBounds();

            bbox = ctx.MakeRectangle(
                Math.Max(bounds.GetMinX(), minX),
                Math.Min(bounds.GetMaxX(), maxX),
                Math.Max(bounds.GetMinY(), minY),
                Math.Min(bounds.GetMaxY(), maxY));
        }

        public override bool IsEmpty
        {
            get { return pA.IsEmpty; }
        }


        public override Shape GetBuffered(double distance, SpatialContext ctx)
        {
            return new BufferedLine(pA, pB, buf + distance, ctx);
        }

        /**
         * Calls {@link DistanceUtils#calcLonDegreesAtLat(double, double)} given pA or pB's latitude;
         * whichever is farthest. It's useful to expand a buffer of a line segment when used in
         * a geospatial context to cover the desired area.
         */
        public static double ExpandBufForLongitudeSkew(Point pA, Point pB,
                                                       double buf)
        {
            double absA = Math.Abs(pA.GetY());
            double absB = Math.Abs(pB.GetY());
            double maxLat = Math.Max(absA, absB);
            double newBuf = DistanceUtils.CalcLonDegreesAtLat(maxLat, buf);
            //    if (newBuf + maxLat >= 90) {
            //      //TODO substitute spherical cap ?
            //    }
            Debug.Assert(newBuf >= buf);
            return newBuf;
        }


        public override SpatialRelation Relate(Shape other)
        {
            if (other is Point)
                return Contains((Point)other) ? SpatialRelation.CONTAINS : SpatialRelation.DISJOINT;
            if (other is Rectangle)
                return Relate((Rectangle)other);
            throw new NotSupportedException();
        }

        public virtual SpatialRelation Relate(Rectangle r)
        {
            //Check BBox for disjoint & within.
            SpatialRelation bboxR = bbox.Relate(r);
            if (bboxR == SpatialRelation.DISJOINT || bboxR == SpatialRelation.WITHIN)
                return bboxR;
            //Either CONTAINS, INTERSECTS, or DISJOINT

            Point scratch = new PointImpl(0, 0, null);
            Point prC = r.GetCenter();
            SpatialRelation result = linePrimary.Relate(r, prC, scratch);
            if (result == SpatialRelation.DISJOINT)
                return SpatialRelation.DISJOINT;
            SpatialRelation resultOpp = linePerp.Relate(r, prC, scratch);
            if (resultOpp == SpatialRelation.DISJOINT)
                return SpatialRelation.DISJOINT;
            if (result == resultOpp)//either CONTAINS or INTERSECTS
                return result;
            return SpatialRelation.INTERSECTS;
        }

        public virtual bool Contains(Point p)
        {
            //TODO check bbox 1st?
            return linePrimary.Contains(p) && linePerp.Contains(p);
        }

        public virtual Rectangle BoundingBox
        {
            get { return bbox; }
        }


        public override bool HasArea
        {
            get { return buf > 0; }
        }


        public override double GetArea(SpatialContext ctx)
        {
            return linePrimary.GetBuf() * linePerp.GetBuf() * 4;
        }


        public override Point GetCenter()
        {
            return BoundingBox.GetCenter();
        }

        public virtual Point GetA()
        {
            return pA;
        }

        public virtual Point GetB()
        {
            return pB;
        }

        public virtual double GetBuf()
        {
            return buf;
        }

        /**
         * INTERNAL
         */
        public virtual InfBufLine GetLinePrimary()
        {
            return linePrimary;
        }

        /**
         * INTERNAL
         */
        public virtual InfBufLine GetLinePerp()
        {
            return linePerp;
        }


        public override string ToString()
        {
            return "BufferedLine(" + pA + ", " + pB + " b=" + buf + ")";
        }


        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            BufferedLine that = (BufferedLine)o;

            if (that.buf.CompareTo(buf) != 0) return false;
            if (!pA.Equals(that.pA)) return false;
            if (!pB.Equals(that.pB)) return false;

            return true;
        }


        public override int GetHashCode()
        {
            int result;
            long temp;
            result = pA.GetHashCode();
            result = 31 * result + pB.GetHashCode();
            temp = buf != +0.0d ? BitConverter.DoubleToInt64Bits(buf) : 0L;
            result = 31 * result + (int)(temp ^ (long)((ulong)temp >> 32));
            return result;
        }
    }
}
