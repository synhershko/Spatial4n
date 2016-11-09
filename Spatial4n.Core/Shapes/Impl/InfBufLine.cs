using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Shapes.Impl
{
    /// <summary>
    /// INERNAL: A buffered line of infinite length.
    /// Public for test access.
    /// </summary>
    public class InfBufLine
    {
        //TODO consider removing support for vertical line -- let caller
        // do something else.  BufferedLine could have a factory method
        // that returns a rectangle, for example.

        // line: y = slope * x + intercept

        private readonly double slope;//can be infinite for vertical line
                                      //if slope is infinite, this is x intercept, otherwise y intercept
        private readonly double intercept;

        private readonly double buf;

        private readonly double distDenomInv;//cached: 1 / Math.sqrt(slope * slope + 1)

        internal InfBufLine(double slope, Point point, double buf)
        {
            Debug.Assert(!double.IsNaN(slope));
            this.slope = slope;
            if (double.IsInfinity(slope))
            {
                intercept = point.GetX();
                distDenomInv = double.NaN;
            }
            else
            {
                intercept = point.GetY() - slope * point.GetX();
                distDenomInv = 1 / Math.Sqrt(slope * slope + 1);
            }
            this.buf = buf;
        }

        internal virtual SpatialRelation Relate(Rectangle r, Point prC, Point scratch)
        {
            Debug.Assert(r.GetCenter().Equals(prC));

            int cQuad = Quadrant(prC);

            Point nearestP = scratch;
            CornerByQuadrant(r, oppositeQuad[cQuad], nearestP);
            bool nearestContains = Contains(nearestP);

            if (nearestContains)
            {
                Point farthestP = scratch;
                nearestP = null;//just to be safe (same scratch object)
                CornerByQuadrant(r, cQuad, farthestP);
                bool farthestContains = Contains(farthestP);
                if (farthestContains)
                    return SpatialRelation.CONTAINS;
                return SpatialRelation.INTERSECTS;
            }
            else
            {// not nearestContains
                if (Quadrant(nearestP) == cQuad)
                    return SpatialRelation.DISJOINT;//out of buffer on same side as center
                return SpatialRelation.INTERSECTS;//nearest & farthest points straddle the line
            }
        }

        internal virtual bool Contains(Point p)
        {
            return (DistanceUnbuffered(p) <= buf);
        }

        /** INTERNAL AKA lineToPointDistance */
        public virtual double DistanceUnbuffered(Point c)
        {
            if (double.IsInfinity(slope))
                return Math.Abs(c.GetX() - intercept);
            // http://math.ucsd.edu/~wgarner/math4c/derivations/distance/distptline.htm
            double num = Math.Abs(c.GetY() - slope * c.GetX() - intercept);
            return num * distDenomInv;
        }

        //  /** Amount to add or subtract to intercept to indicate where the
        //   * buffered line edges cross the y axis.
        //   * @return
        //   */
        //  double interceptBuffOffset() {
        //    if (Double.isInfinite(slope))
        //      return slope;
        //    if (buf == 0)
        //      return 0;
        //    double slopeDivBuf = slope / buf;
        //    return Math.sqrt(buf*buf + slopeDivBuf*slopeDivBuf);
        //  }

        /** INTERNAL: AKA lineToPointQuadrant */
        public virtual int Quadrant(Point c)
        {
            //check vertical line case 1st
            if (double.IsInfinity(slope))
            {
                //when slope is infinite, intercept is x intercept instead of y
                return c.GetX() > intercept ? 1 : 2; //4 : 3 would work too
            }
            //(below will work for slope==0 horizontal line too)
            //is c above or below the line
            double yAtCinLine = slope * c.GetX() + intercept;
            bool above = c.GetY() >= yAtCinLine;
            if (slope > 0)
            {
                //if slope is a forward slash, then result is 2 | 4
                return above ? 2 : 4;
            }
            else
            {
                //if slope is a backward slash, then result is 1 | 3
                return above ? 1 : 3;
            }
        }

        //TODO ? Use an Enum for quadrant?

        /* quadrants 1-4: NE, NW, SW, SE. */
        private static readonly int[] oppositeQuad = { -1, 3, 4, 1, 2 };

        public static void CornerByQuadrant(Rectangle r, int cornerQuad, Point output)
        {
            double x = (cornerQuad == 1 || cornerQuad == 4) ? r.GetMaxX() : r.GetMinX();
            double y = (cornerQuad == 1 || cornerQuad == 2) ? r.GetMaxY() : r.GetMinY();
            output.Reset(x, y);
        }

        public virtual double GetSlope()
        {
            return slope;
        }

        public virtual double GetIntercept()
        {
            return intercept;
        }

        public virtual double GetBuf()
        {
            return buf;
        }

        /** 1 / Math.sqrt(slope * slope + 1) */
        public virtual double GetDistDenomInv()
        {
            return distDenomInv;
        }


        public override string ToString()
        {
            return "InfBufLine{" +
                "buf=" + buf +
                ", intercept=" + intercept +
                ", slope=" + slope +
                '}';
        }
    }
}
