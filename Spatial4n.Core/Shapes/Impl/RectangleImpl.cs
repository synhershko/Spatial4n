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
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;

namespace Spatial4n.Core.Shapes.Impl
{
	/// <summary>
	/// A simple Rectangle implementation that also supports a longitudinal
	/// wrap-around. When minX > maxX, this will assume it is world coordinates that
	/// cross the date line using degrees. Immutable & threadsafe.
	/// </summary>
	public class RectangleImpl : Rectangle
	{
        private readonly SpatialContext ctx;
		private double minX;
		private double maxX;
		private double minY;
		private double maxY;

        public RectangleImpl(double minX, double maxX, double minY, double maxY, SpatialContext ctx)
		{
            //TODO change to West South East North to be more consistent with OGC?
            this.ctx = ctx;
            Reset(minX, maxX, minY, maxY);
		}

        public RectangleImpl(Point lowerLeft, Point upperRight, SpatialContext ctx)
            : this(lowerLeft.GetX(), upperRight.GetX(), lowerLeft.GetY(), upperRight.GetY(), ctx)
		{
		}

        public RectangleImpl(Rectangle r, SpatialContext ctx)
            : this(r.GetMinX(), r.GetMaxX(), r.GetMinY(), r.GetMaxY(), ctx)
		{
		}

        public void Reset(double minX, double maxX, double minY, double maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            Debug.Assert(minY <= maxY);
        }

	    public SpatialRelation Relate(Shape other)
		{
			var point = other as Point;
			if (point != null)
			{
				return Relate(point);
			}
			var rectangle = other as Rectangle;
			if (rectangle != null)
			{
				return Relate(rectangle);
			}
			return other.Relate(this).Transpose();
		}

		public SpatialRelation Relate(Point point)
		{
			if (point.GetY() > GetMaxY() || point.GetY() < GetMinY())
				return SpatialRelation.DISJOINT;
			//  all the below logic is rather unfortunate but some dateline cases demand it
			double minX = this.minX;
			double maxX = this.maxX;
			double pX = point.GetX();
			if (ctx.IsGeo())
			{
				//unwrap dateline and normalize +180 to become -180
				double rawWidth = maxX - minX;
				if (rawWidth < 0)
				{
					maxX = minX + (rawWidth + 360);
				}
				//shift to potentially overlap
				if (pX < minX)
				{
					pX += 360;
				}
				else if (pX > maxX)
				{
					pX -= 360;
				} else {
					return SpatialRelation.CONTAINS; //short-circuit
				}
			}
			if (pX < minX || pX > maxX)
				return SpatialRelation.DISJOINT;
			return SpatialRelation.CONTAINS;
		}

		public SpatialRelation Relate(Rectangle rect)
		{
			SpatialRelation yIntersect = RelateYRange(rect.GetMinY(), rect.GetMaxY());
			if (yIntersect == SpatialRelation.DISJOINT)
				return SpatialRelation.DISJOINT;

			SpatialRelation xIntersect = RelateXRange(rect.GetMinX(), rect.GetMaxX());
			if (xIntersect == SpatialRelation.DISJOINT)
				return SpatialRelation.DISJOINT;

			if (xIntersect == yIntersect)//in agreement
				return xIntersect;

			//if one side is equal, return the other
			if (GetMinX() == rect.GetMinX() && GetMaxX() == rect.GetMaxX())
				return yIntersect;
			if (GetMinY() == rect.GetMinY() && GetMaxY() == rect.GetMaxY())
				return xIntersect;

			return SpatialRelation.INTERSECTS;
		}

		public Rectangle GetBoundingBox()
		{
			return this;
		}

		public bool HasArea()
		{
			return maxX != minX && maxY != minY;
		}

		public Point GetCenter()
		{
			double y = GetHeight() / 2 + minY;
			double x = GetWidth() / 2 + minX;
			if (minX > maxX)//WGS84
				x = DistanceUtils.NormLonDEG(x); //in case falls outside the standard range
			return new PointImpl(x, y, ctx);
		}

		public double GetWidth()
		{
			double w = maxX - minX;
			if (w < 0)
			{
				//only true when minX > maxX (WGS84 assumed)
				w += 360;
				//assert w >= 0;
			}
			return w;
		}

		public double GetHeight()
		{
			return maxY - minY;
		}

		public double GetMinX()
		{
			return minX;
		}

		public double GetMinY()
		{
			return minY;
		}

		public double GetMaxX()
		{
			return maxX;
		}

		public double GetMaxY()
		{
			return maxY;
		}

		public double GetArea(SpatialContext ctx)
		{
			if (ctx == null)
			{
				return GetWidth()*GetHeight();
			}
			else
			{
				return ctx.GetDistCalc().Area(this);
			}
		}

		public bool GetCrossesDateLine()
		{
			return (minX > maxX);
		}

		private static SpatialRelation Relate_Range(double int_min, double int_max, double ext_min, double ext_max)
		{
			if (ext_min > int_max || ext_max < int_min)
			{
				return SpatialRelation.DISJOINT;
			}

			if (ext_min >= int_min && ext_max <= int_max)
			{
				return SpatialRelation.CONTAINS;
			}

			if (ext_min <= int_min && ext_max >= int_max)
			{
				return SpatialRelation.WITHIN;
			}

			return SpatialRelation.INTERSECTS;
		}

		public SpatialRelation RelateYRange(double ext_minY, double ext_maxY)
		{
			return Relate_Range(minY, maxY, ext_minY, ext_maxY);
		}

		public SpatialRelation RelateXRange(double ext_minX, double ext_maxX)
		{
			//For ext & this we have local minX and maxX variable pairs. We rotate them so that minX <= maxX
			double minX = this.minX;
			double maxX = this.maxX;
			if (ctx.IsGeo())
			{
				//unwrap dateline, plus do world-wrap short circuit
				double rawWidth = maxX - minX;
				if (rawWidth == 360)
					return SpatialRelation.CONTAINS;
				if (rawWidth < 0)
				{
					maxX = minX + (rawWidth + 360);
				}

				double ext_rawWidth = ext_maxX - ext_minX;
				if (ext_rawWidth == 360)
					return SpatialRelation.WITHIN;
				if (ext_rawWidth < 0)
				{
					ext_maxX = ext_minX + (ext_rawWidth + 360);
				}

				//shift to potentially overlap
				if (maxX < ext_minX)
				{
					minX += 360;
					maxX += 360;
				}
				else if (ext_maxX < minX)
				{
					ext_minX += 360;
					ext_maxX += 360;
				}
			}

			return Relate_Range(minX, maxX, ext_minX, ext_maxX);
		}

		public override string ToString()
		{
			return "Rect(minX=" + minX + ",maxX=" + maxX + ",minY=" + minY + ",maxY=" + maxY + ")";
		}

		public override bool Equals(object obj)
		{
			return Equals(this, obj);
		}

		/// <summary>
		/// All {@link Rectangle} implementations should use this definition of {@link Object#equals(Object)}.
		/// </summary>
		/// <param name="thiz"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		public static bool Equals(Rectangle thiz, Object o)
		{
			if (thiz == null)
				throw new ArgumentNullException("thiz");

			if (thiz == o) return true;

			var rectangle = o as Rectangle;
			if (rectangle == null) return false;

			return thiz.GetMaxX().Equals(rectangle.GetMaxX()) && thiz.GetMinX().Equals(rectangle.GetMinX()) &&
			       thiz.GetMaxY().Equals(rectangle.GetMaxY()) && thiz.GetMinY().Equals(rectangle.GetMinY());
		}

		public override int GetHashCode()
		{
			return GetHashCode(this);
		}

		public static int GetHashCode(Rectangle thiz)
		{
			if (thiz == null)
				throw new ArgumentNullException("thiz");

			long temp = thiz.GetMinX() != +0.0d ? BitConverter.DoubleToInt64Bits(thiz.GetMinX()) : 0L;
			int result = (int)(temp ^ ((uint)temp >> 32));
			temp = thiz.GetMaxX() != +0.0d ? BitConverter.DoubleToInt64Bits(thiz.GetMaxX()) : 0L;
			result = 31 * result + (int)(temp ^ ((uint)temp >> 32));
			temp = thiz.GetMinY() != +0.0d ? BitConverter.DoubleToInt64Bits(thiz.GetMinY()) : 0L;
			result = 31 * result + (int)(temp ^ ((uint)temp >> 32));
			temp = thiz.GetMaxY() != +0.0d ? BitConverter.DoubleToInt64Bits(thiz.GetMaxY()) : 0L;
			result = 31*result + (int) (temp ^ ((uint)temp >> 32));
			return result;
		}
	}
}
