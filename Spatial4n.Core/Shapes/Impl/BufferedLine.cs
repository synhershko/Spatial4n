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

using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using System;
using System.Diagnostics;

namespace Spatial4n.Core.Shapes.Impl
{
    /// <summary>
    /// INTERNAL: A line between two points with a buffer distance extending in every direction. By
    /// contrast, an un-buffered line covers no area and as such is extremely unlikely to intersect with
    /// a point. BufferedLine isn't yet aware of geodesics (e.g. the dateline); it operates in Euclidean
    /// space.
    /// </summary>
    public class BufferedLine : IShape
    {
        private readonly IPoint pA, pB;
        private readonly double buf;
        private readonly IRectangle bbox;
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
        public BufferedLine(IPoint pA, IPoint pB, double buf, SpatialContext ctx)
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

            double deltaY = pB.Y - pA.Y;
            double deltaX = pB.X - pA.X;

            Point center = new Point(pA.X + deltaX / 2,
                pA.Y + deltaY / 2, null);

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
                if (pA.Y <= pB.Y)
                {
                    minY = pA.Y;
                    maxY = pB.Y;
                }
                else
                {
                    minY = pB.Y;
                    maxY = pA.Y;
                }
                minX = pA.X - buf;
                maxX = pA.X + buf;
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
                double bboxBuf = buf * (1 + Math.Abs(linePrimary.Slope))
                    * linePrimary.DistDenomInv;
                Debug.Assert(bboxBuf >= buf && bboxBuf <= buf * 1.5);

                if (pA.X <= pB.X)
                {
                    minX = pA.X - bboxBuf;
                    maxX = pB.X + bboxBuf;
                }
                else
                {
                    minX = pB.X - bboxBuf;
                    maxX = pA.X + bboxBuf;
                }
                if (pA.Y <= pB.Y)
                {
                    minY = pA.Y - bboxBuf;
                    maxY = pB.Y + bboxBuf;
                }
                else
                {
                    minY = pB.Y - bboxBuf;
                    maxY = pA.Y + bboxBuf;
                }

            }
            IRectangle bounds = ctx.WorldBounds;

            bbox = ctx.MakeRectangle(
                Math.Max(bounds.MinX, minX),
                Math.Min(bounds.MaxX, maxX),
                Math.Max(bounds.MinY, minY),
                Math.Min(bounds.MaxY, maxY));
        }

        public virtual bool IsEmpty
        {
            get { return pA.IsEmpty; }
        }


        public virtual IShape GetBuffered(double distance, SpatialContext ctx)
        {
            return new BufferedLine(pA, pB, buf + distance, ctx);
        }

        /**
         * Calls {@link DistanceUtils#calcLonDegreesAtLat(double, double)} given pA or pB's latitude;
         * whichever is farthest. It's useful to expand a buffer of a line segment when used in
         * a geospatial context to cover the desired area.
         */
        public static double ExpandBufForLongitudeSkew(IPoint pA, IPoint pB,
                                                       double buf)
        {
            double absA = Math.Abs(pA.Y);
            double absB = Math.Abs(pB.Y);
            double maxLat = Math.Max(absA, absB);
            double newBuf = DistanceUtils.CalcLonDegreesAtLat(maxLat, buf);
            //    if (newBuf + maxLat >= 90) {
            //      //TODO substitute spherical cap ?
            //    }
            Debug.Assert(newBuf >= buf);
            return newBuf;
        }


        public virtual SpatialRelation Relate(IShape other)
        {
            if (other is IPoint)
                return Contains((IPoint)other) ? SpatialRelation.CONTAINS : SpatialRelation.DISJOINT;
            if (other is IRectangle)
                return Relate((IRectangle)other);
            throw new NotSupportedException();
        }

        public virtual SpatialRelation Relate(IRectangle r)
        {
            //Check BBox for disjoint & within.
            SpatialRelation bboxR = bbox.Relate(r);
            if (bboxR == SpatialRelation.DISJOINT || bboxR == SpatialRelation.WITHIN)
                return bboxR;
            //Either CONTAINS, INTERSECTS, or DISJOINT

            IPoint scratch = new Point(0, 0, null);
            IPoint prC = r.Center;
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

        public virtual bool Contains(IPoint p)
        {
            //TODO check bbox 1st?
            return linePrimary.Contains(p) && linePerp.Contains(p);
        }

        public virtual IRectangle BoundingBox
        {
            get { return bbox; }
        }


        public virtual bool HasArea
        {
            get { return buf > 0; }
        }


        public virtual double GetArea(SpatialContext ctx)
        {
            return linePrimary.Buf * linePerp.Buf * 4;
        }


        public virtual IPoint Center
        {
            get { return BoundingBox.Center; }
        }

        public virtual IPoint A
        {
            get { return pA; }
        }

        public virtual IPoint B
        {
            get { return pB; }
        }

        public virtual double Buf
        {
            get { return buf; }
        }

        /**
         * INTERNAL
         */
        public virtual InfBufLine LinePrimary
        {
            get { return linePrimary; }
        }

        /**
         * INTERNAL
         */
        public virtual InfBufLine LinePerp
        {
            get { return linePerp; }
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
