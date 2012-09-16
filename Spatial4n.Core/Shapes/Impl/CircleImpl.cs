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
			this.radiusDEG = radiusDEG;
            this.enclosingBox = ctx.GetDistCalc().CalcBoxByDistFromPt(point, this.radiusDEG, ctx, null);
		}

        public virtual void Reset(double x, double y, double radiusDEG)
        {
            point.Reset(x, y);
            this.radiusDEG = radiusDEG;
            this.enclosingBox = ctx.GetDistCalc().CalcBoxByDistFromPt(point, this.radiusDEG, ctx, enclosingBox);
        }

		public SpatialRelation Relate(Shape other)
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

		public SpatialRelation Relate(Point point)
		{
			return Contains(point.GetX(), point.GetY()) ? SpatialRelation.CONTAINS : SpatialRelation.DISJOINT;
		}

		public SpatialRelation Relate(Rectangle r)
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
			//see http://stackoverflow.com/questions/401847/circle-rectangle-collision-detection-intersection/1879223#1879223
			double closestX;
			double ctr_x = GetXAxis();
			if (ctr_x < r.GetMinX())
				closestX = r.GetMinX();
			else if (ctr_x > r.GetMaxX())
				closestX = r.GetMaxX();
			else
				closestX = ctr_x;

			double closestY;
			double ctr_y = GetYAxis();
			if (ctr_y < r.GetMinY())
				closestY = r.GetMinY();
			else if (ctr_y > r.GetMaxY())
				closestY = r.GetMaxY();
			else
				closestY = ctr_y;

			//Check if there is an intersection from this circle to closestXY
			bool didContainOnClosestXY = false;
			if (ctr_x == closestX)
			{
				double deltaY = Math.Abs(ctr_y - closestY);
				double distYCirc = (ctr_y < closestY ? enclosingBox.GetMaxY() - ctr_y : ctr_y - enclosingBox.GetMinY());
				if (deltaY > distYCirc)
					return SpatialRelation.DISJOINT;
			}
			else if (ctr_y == closestY)
			{
				double deltaX = Math.Abs(ctr_x - closestX);
				double distXCirc = (ctr_x < closestX ? enclosingBox.GetMaxX() - ctr_x : ctr_x - enclosingBox.GetMinX());
				if (deltaX > distXCirc)
					return SpatialRelation.DISJOINT;
			}
			else
			{
				//fallback on more expensive calculation
				didContainOnClosestXY = true;
				if (!Contains(closestX, closestY))
					return SpatialRelation.DISJOINT;
			}

			//At this point we know that it's *NOT* DISJOINT, so there is some level of intersection. It's *NOT* WITHIN either.
			// The only question left is whether circle CONTAINS r or simply intersects it.

			//If circle contains r, then its bbox MUST also CONTAIN r.
			if (bboxSect != SpatialRelation.CONTAINS)
				return SpatialRelation.INTERSECTS;

			//Find the farthest point of r away from the center of the circle. If that point is contained, then all of r is
			// contained.
			double farthestX = r.GetMaxX() - ctr_x > ctr_x - r.GetMinX() ? r.GetMaxX() : r.GetMinX();
			double farthestY = r.GetMaxY() - ctr_y > ctr_y - r.GetMinY() ? r.GetMaxY() : r.GetMinY();
			if (Contains(farthestX, farthestY))
				return SpatialRelation.CONTAINS;
			return SpatialRelation.INTERSECTS;
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

		public SpatialRelation Relate(Circle circle)
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


		public Rectangle GetBoundingBox()
		{
			return enclosingBox;
		}

		public bool HasArea()
		{
			return radiusDEG > 0;
		}

		public Point GetCenter()
		{
			return point;
		}

		public double GetRadius()
		{
			return radiusDEG;
		}

		public double GetArea(SpatialContext ctx)
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

		public bool Contains(double x, double y)
		{
			return ctx.GetDistCalc().Distance(point, x, y) <= radiusDEG;
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
