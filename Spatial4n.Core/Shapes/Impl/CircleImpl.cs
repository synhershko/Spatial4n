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
	public class CircleImpl : Circle
	{
		protected readonly SpatialContext ctx;

        protected Point point;
        protected double radiusDEG;

        // calculated & cached
        protected Rectangle enclosingBox;

		//we don't have a line shape so we use a rectangle for these axis

		public CircleImpl(Point p, double radiusDEG, SpatialContext ctx)
		{
            //We assume any validation of params already occurred (including bounding dist)
            this.ctx = ctx;
			this.point = p;
            this.radiusDEG = point.IsEmpty ? double.NaN : radiusDEG;
            this.enclosingBox = point.IsEmpty ? ctx.MakeRectangle(double.NaN, double.NaN, double.NaN, double.NaN) :
              ctx.GetDistCalc().CalcBoxByDistFromPt(point, this.radiusDEG, ctx, null);
            //this.radiusDEG = radiusDEG;
            //         this.enclosingBox = ctx.GetDistCalc().CalcBoxByDistFromPt(point, this.radiusDEG, ctx, null);
        }

        public virtual void Reset(double x, double y, double radiusDEG)
        {
            Debug.Assert(!IsEmpty);
            point.Reset(x, y);
            this.radiusDEG = radiusDEG;
            this.enclosingBox = ctx.GetDistCalc().CalcBoxByDistFromPt(point, this.radiusDEG, ctx, enclosingBox);
        }

        public virtual bool IsEmpty
        {
            get { return point.IsEmpty; }
        }

        public virtual Point GetCenter()
        {
            return point;
        }

        public virtual double GetRadius()
        {
            return radiusDEG;
        }

        public virtual double GetArea(SpatialContext ctx)
        {
            if (ctx == null)
            {
                return Math.PI * radiusDEG * radiusDEG;
            }
            else
            {
                return ctx.GetDistCalc().Area(this);
            }
        }

        public virtual Circle GetBuffered(double distance, SpatialContext ctx)
        {
            return ctx.MakeCircle(point, distance + radiusDEG);
        }

        public virtual bool Contains(double x, double y)
        {
            return ctx.GetDistCalc().Distance(point, x, y) <= radiusDEG;
        }

        public virtual bool HasArea()
        {
            return radiusDEG > 0;
        }

        public virtual Rectangle GetBoundingBox()
        {
            return enclosingBox;
        }


        public virtual SpatialRelation Relate(Shape other)
		{
			//This shortcut was problematic in testing due to distinctions of CONTAINS/WITHIN for no-area shapes (lines, points).
			//    if (distance == 0) {
			//      return point.relate(other,ctx).intersects() ? SpatialRelation.WITHIN : SpatialRelation.DISJOINT;
			//    }

			var other1 = other as Point;
			if (other1 != null)
			{
				return Relate(other1);
			}
			var rectangle = other as Rectangle;
			if (rectangle != null)
			{
				return Relate(rectangle);
			}
			var circle = other as Circle;
			if (circle != null)
			{
				return Relate(circle);
			}
			return other.Relate(this).Transpose();

		}

		public virtual SpatialRelation Relate(Point point)
		{
			return Contains(point.GetX(), point.GetY()) ? SpatialRelation.CONTAINS : SpatialRelation.DISJOINT;
		}

		public virtual SpatialRelation Relate(Rectangle r)
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

		protected virtual SpatialRelation RelateRectanglePhase2(Rectangle r, SpatialRelation bboxSect)
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
            double xAxis = GetXAxis();
            if (xAxis < r.GetMinX())
            {
                closestX = r.GetMinX();
                farthestX = r.GetMaxX();
            }
            else if (xAxis > r.GetMaxX())
            {
                closestX = r.GetMaxX();
                farthestX = r.GetMinX();
            }
            else
            {
                closestX = xAxis; //we don't really use this value but to check this condition
                farthestX = r.GetMaxX() - xAxis > xAxis - r.GetMinX() ? r.GetMaxX() : r.GetMinX();
            }

            double closestY, farthestY;
            double yAxis = GetYAxis();
            if (yAxis < r.GetMinY())
            {
                closestY = r.GetMinY();
                farthestY = r.GetMaxY();
            }
            else if (yAxis > r.GetMaxY())
            {
                closestY = r.GetMaxY();
                farthestY = r.GetMinY();
            }
            else
            {
                closestY = yAxis; //we don't really use this value but to check this condition
                farthestY = r.GetMaxY() - yAxis > yAxis - r.GetMinY() ? r.GetMaxY() : r.GetMinY();
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
            if (point.GetY() != GetYAxis())
            {//geodetic
                if (yAxis == closestY)
                {//r crosses north to south over x axis (confusing)
                    double otherY = (farthestY == r.GetMaxY() ? r.GetMinY() : r.GetMaxY());
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
		protected virtual double GetYAxis()
		{
			return point.GetY();
		}

		/// <summary>
		/// The <code>X</code> coordinate of where the circle axis intersect.
		/// </summary>
		/// <returns></returns>
		protected virtual double GetXAxis()
		{
			return point.GetX();
		}

		public virtual SpatialRelation Relate(Circle circle)
		{
			double crossDist = ctx.GetDistCalc().Distance(point, circle.GetCenter());
			double aDist = radiusDEG, bDist = circle.GetRadius();
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

		public static bool Equals(Circle thiz, Object o)
		{
			if (thiz == null)
				throw new ArgumentNullException("thiz");

			if (thiz == o) return true;

			var circle = o as Circle;
			if (circle == null) return false;

			if (!thiz.GetCenter().Equals(circle.GetCenter())) return false;
			if (!circle.GetRadius().Equals(thiz.GetRadius())) return false;

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
		public static int GetHashCode(Circle thiz)
		{
			if (thiz == null)
				throw new ArgumentNullException("thiz");

			int result = thiz.GetCenter().GetHashCode();
			long temp = Math.Abs(thiz.GetRadius() - +0.0d) > Double.Epsilon
			            	? BitConverter.DoubleToInt64Bits(thiz.GetRadius())
			            	: 0L;
			result = 31*result + (int) (temp ^ ((uint) temp >> 32));
			return result;
		}
	}
}
