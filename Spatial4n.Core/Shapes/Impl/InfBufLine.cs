/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Diagnostics;

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

        internal InfBufLine(double slope, IPoint point, double buf)
        {
            Debug.Assert(!double.IsNaN(slope));
            this.slope = slope;
            if (double.IsInfinity(slope))
            {
                intercept = point.X;
                distDenomInv = double.NaN;
            }
            else
            {
                intercept = point.Y - slope * point.X;
                distDenomInv = 1 / Math.Sqrt(slope * slope + 1);
            }
            this.buf = buf;
        }

        internal virtual SpatialRelation Relate(IRectangle r, IPoint prC, IPoint scratch)
        {
            Debug.Assert(r.Center.Equals(prC));

            int cQuad = Quadrant(prC);

            IPoint? nearestP = scratch;
            CornerByQuadrant(r, oppositeQuad[cQuad], nearestP);
            bool nearestContains = Contains(nearestP);

            if (nearestContains)
            {
                IPoint farthestP = scratch;
                nearestP = null;//just to be safe (same scratch object)
                CornerByQuadrant(r, cQuad, farthestP);
                bool farthestContains = Contains(farthestP);
                if (farthestContains)
                    return SpatialRelation.Contains;
                return SpatialRelation.Intersects;
            }
            else
            {// not nearestContains
                if (Quadrant(nearestP) == cQuad)
                    return SpatialRelation.Disjoint;//out of buffer on same side as center
                return SpatialRelation.Intersects;//nearest & farthest points straddle the line
            }
        }

        internal virtual bool Contains(IPoint p)
        {
            return (DistanceUnbuffered(p) <= buf);
        }

        /// <summary>
        /// INTERNAL AKA lineToPointDistance
        /// </summary>
        public virtual double DistanceUnbuffered(IPoint c)
        {
            if (double.IsInfinity(slope))
                return Math.Abs(c.X - intercept);
            // http://math.ucsd.edu/~wgarner/math4c/derivations/distance/distptline.htm
            double num = Math.Abs(c.Y - slope * c.X - intercept);
            return num * distDenomInv;
        }

        //  /** Amount to add or subtract to intercept to indicate where the
        //   * buffered line edges cross the y axis.
        //   * @return
        //   */
        //  double interceptBuffOffset() {
        //    if (double.isInfinite(slope))
        //      return slope;
        //    if (buf == 0)
        //      return 0;
        //    double slopeDivBuf = slope / buf;
        //    return Math.sqrt(buf*buf + slopeDivBuf*slopeDivBuf);
        //  }

        /// <summary>
        /// INTERNAL: AKA lineToPointQuadrant
        /// </summary>
        public virtual int Quadrant(IPoint c)
        {
            //check vertical line case 1st
            if (double.IsInfinity(slope))
            {
                //when slope is infinite, intercept is x intercept instead of y
                return c.X > intercept ? 1 : 2; //4 : 3 would work too
            }
            //(below will work for slope==0 horizontal line too)
            //is c above or below the line
            double yAtCinLine = slope * c.X + intercept;
            bool above = c.Y >= yAtCinLine;
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

        public static void CornerByQuadrant(IRectangle r, int cornerQuad, IPoint output)
        {
            double x = (cornerQuad == 1 || cornerQuad == 4) ? r.MaxX : r.MinX;
            double y = (cornerQuad == 1 || cornerQuad == 2) ? r.MaxY : r.MinY;
            output.Reset(x, y);
        }

        public virtual double Slope => slope;

        public virtual double Intercept => intercept;

        public virtual double Buf => buf;

        /// <summary>
        /// <c>1 / Math.Sqrt(slope * slope + 1)</c>
        /// </summary>
        public virtual double DistDenomInv => distDenomInv;


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
