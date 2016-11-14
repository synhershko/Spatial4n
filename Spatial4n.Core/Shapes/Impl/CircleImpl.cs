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
using Spatial4n.Core.Context;
using System.Diagnostics;

namespace Spatial4n.Core.Shapes.Impl
{
    /// <summary>
    /// A circle, also known as a point-radius, based on a
    /// {@link com.spatial4j.core.distance.DistanceCalculator} which does all the work. This implementation
    /// implementation should work for both cartesian 2D and geodetic sphere surfaces.
    /// </summary>
    public class Circle : ICircle
    {
        protected readonly SpatialContext ctx;

        protected IPoint point;
        protected double radiusDEG;

        // calculated & cached
        protected IRectangle enclosingBox;

        //we don't have a line shape so we use a rectangle for these axis

        public Circle(IPoint p, double radiusDEG, SpatialContext ctx)
        {
            //We assume any validation of params already occurred (including bounding dist)
            this.ctx = ctx;
            this.point = p;
            this.radiusDEG = point.IsEmpty ? double.NaN : radiusDEG;
            this.enclosingBox = point.IsEmpty ? ctx.MakeRectangle(double.NaN, double.NaN, double.NaN, double.NaN) :
                ctx.DistCalc.CalcBoxByDistFromPt(point, this.radiusDEG, ctx, null);
        }

        public virtual void Reset(double x, double y, double radiusDEG)
        {
            Debug.Assert(!IsEmpty);
            point.Reset(x, y);
            this.radiusDEG = radiusDEG;
            this.enclosingBox = ctx.DistCalc.CalcBoxByDistFromPt(point, this.radiusDEG, ctx, enclosingBox);
        }

        public virtual bool IsEmpty
        {
            get { return point.IsEmpty; }
        }

        public virtual IPoint Center
        {
            get { return point; }
        }

        public virtual double Radius
        {
            get { return radiusDEG; }
        }

        public virtual double GetArea(SpatialContext ctx)
        {
            if (ctx == null)
            {
                return Math.PI * radiusDEG * radiusDEG;
            }
            else
            {
                return ctx.DistCalc.Area(this);
            }
        }

        public virtual IShape GetBuffered(double distance, SpatialContext ctx)
        {
            return ctx.MakeCircle(point, distance + radiusDEG);
        }

        public virtual bool Contains(double x, double y)
        {
            return ctx.DistCalc.Distance(point, x, y) <= radiusDEG;
        }

        public virtual bool HasArea
        {
            get { return radiusDEG > 0; }
        }

        public virtual IRectangle BoundingBox
        {
            get { return enclosingBox; }
        }


        public virtual SpatialRelation Relate(IShape other)
        {
            //This shortcut was problematic in testing due to distinctions of CONTAINS/WITHIN for no-area shapes (lines, points).
            //    if (distance == 0) {
            //      return point.relate(other,ctx).intersects() ? SpatialRelation.WITHIN : SpatialRelation.DISJOINT;
            //    }

            var other1 = other as IPoint;
            if (other1 != null)
            {
                return Relate(other1);
            }
            var rectangle = other as IRectangle;
            if (rectangle != null)
            {
                return Relate(rectangle);
            }
            var circle = other as ICircle;
            if (circle != null)
            {
                return Relate(circle);
            }
            return other.Relate(this).Transpose();

        }

        public virtual SpatialRelation Relate(IPoint point)
        {
            return Contains(point.X, point.Y) ? SpatialRelation.CONTAINS : SpatialRelation.DISJOINT;
        }

        public virtual SpatialRelation Relate(IRectangle r)
        {
            //Note: Surprisingly complicated!

            //--We start by leveraging the fact we have a calculated bbox that is "cheaper" than use of DistanceCalculator.
            SpatialRelation bboxSect = enclosingBox.Relate(r);
            if (bboxSect == SpatialRelation.DISJOINT || bboxSect == SpatialRelation.WITHIN)
                return bboxSect;
            if (bboxSect == SpatialRelation.CONTAINS && enclosingBox.Equals(r)) //nasty identity edge-case
                return SpatialRelation.WITHIN;
            //bboxSect is INTERSECTS or CONTAINS
            //The result can be DISJOINT, CONTAINS, or INTERSECTS (not WITHIN)

            return RelateRectanglePhase2(r, bboxSect);
        }

        protected virtual SpatialRelation RelateRectanglePhase2(IRectangle r, SpatialRelation bboxSect)
        {
            /*
			 !! DOES NOT WORK WITH GEO CROSSING DATELINE OR WORLD-WRAP.
			 TODO upgrade to handle crossing dateline, but not world-wrap; use some x-shifting code from RectangleImpl.
			 */

            //At this point, the only thing we are certain of is that circle is *NOT* WITHIN r, since the bounding box of a
            // circle MUST be within r for the circle to be within r.

            //--Quickly determine if they are DISJOINT or not.
            // Find the closest & farthest point to the circle within the rectangle
            double closestX, farthestX;
            double xAxis = XAxis;
            if (xAxis < r.MinX)
            {
                closestX = r.MinX;
                farthestX = r.MaxX;
            }
            else if (xAxis > r.MaxX)
            {
                closestX = r.MaxX;
                farthestX = r.MinX;
            }
            else
            {
                closestX = xAxis; //we don't really use this value but to check this condition
                farthestX = r.MaxX - xAxis > xAxis - r.MinX ? r.MaxX : r.MinX;
            }

            double closestY, farthestY;
            double yAxis = YAxis;
            if (yAxis < r.MinY)
            {
                closestY = r.MinY;
                farthestY = r.MaxY;
            }
            else if (yAxis > r.MaxY)
            {
                closestY = r.MaxY;
                farthestY = r.MinY;
            }
            else
            {
                closestY = yAxis; //we don't really use this value but to check this condition
                farthestY = r.MaxY - yAxis > yAxis - r.MinY ? r.MaxY : r.MinY;
            }

            //If r doesn't overlap an axis, then could be disjoint. Test closestXY
            if (xAxis != closestX && yAxis != closestY)
            {
                if (!Contains(closestX, closestY))
                    return SpatialRelation.DISJOINT;
            } // else CAN'T be disjoint if spans axis because earlier bbox check ruled that out

            //Now, we know it's *NOT* DISJOINT and it's *NOT* WITHIN either.
            // Does circle CONTAINS r or simply intersect it?

            //If circle contains r, then its bbox MUST also CONTAIN r.
            if (bboxSect != SpatialRelation.CONTAINS)
                return SpatialRelation.INTERSECTS;

            //If the farthest point of r away from the center of the circle is contained, then all of r is
            // contained.
            if (!Contains(farthestX, farthestY))
                return SpatialRelation.INTERSECTS;

            //geodetic detection of farthest Y when rect crosses x axis can't be reliably determined, so
            // check other corner too, which might actually be farthest
            if (point.Y != YAxis)
            {//geodetic
                if (yAxis == closestY)
                {//r crosses north to south over x axis (confusing)
                    double otherY = (farthestY == r.MaxY ? r.MinY : r.MaxY);
                    if (!Contains(farthestX, otherY))
                        return SpatialRelation.INTERSECTS;
                }
            }

            return SpatialRelation.CONTAINS;
        }

        /// <summary>
        /// The <code>Y</code> coordinate of where the circle axis intersect.
        /// </summary>
        /// <returns></returns>
        protected virtual double YAxis
        {
            get { return point.Y; }
        }

        /// <summary>
        /// The <code>X</code> coordinate of where the circle axis intersect.
        /// </summary>
        /// <returns></returns>
        protected virtual double XAxis
        {
            get { return point.X; }
        }

        public virtual SpatialRelation Relate(ICircle circle)
        {
            double crossDist = ctx.DistCalc.Distance(point, circle.Center);
            double aDist = radiusDEG, bDist = circle.Radius;
            if (crossDist > aDist + bDist)
                return SpatialRelation.DISJOINT;
            if (crossDist < aDist && crossDist + bDist <= aDist)
                return SpatialRelation.CONTAINS;
            if (crossDist < bDist && crossDist + aDist <= bDist)
                return SpatialRelation.WITHIN;

            return SpatialRelation.INTERSECTS;
        }

        public override string ToString()
        {
            return string.Format("Circle({0}, d={1:0.0}\u00B0)", point, radiusDEG);
        }

        public override bool Equals(object obj)
        {
            return Equals(this, obj);
        }

        public static bool Equals(ICircle thiz, Object o)
        {
            if (thiz == null)
                throw new ArgumentNullException("thiz");

            if (thiz == o) return true;

            var circle = o as ICircle;
            if (circle == null) return false;

            if (!thiz.Center.Equals(circle.Center)) return false;
            if (!circle.Radius.Equals(thiz.Radius)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        /// <summary>
        /// All {@link Circle} implementations should use this definition of {@link Object#hashCode()}.
        /// </summary>
        /// <param name="thiz"></param>
        /// <returns></returns>
        public static int GetHashCode(ICircle thiz)
        {
            if (thiz == null)
                throw new ArgumentNullException("thiz");

            int result = thiz.Center.GetHashCode();
            long temp = Math.Abs(thiz.Radius - +0.0d) > double.Epsilon
                            ? BitConverter.DoubleToInt64Bits(thiz.Radius)
                            : 0L;
            result = 31 * result + (int)(temp ^ ((uint)temp >> 32));
            return result;
        }
    }
}
