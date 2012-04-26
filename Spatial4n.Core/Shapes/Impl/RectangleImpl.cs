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
using Spatial4n.Core.Distance;

namespace Spatial4n.Core.Shapes.Impl
{
	/// <summary>
	/// A simple Rectangle implementation that also supports a longitudinal wrap-around. When minX > maxX, this will assume
	/// it is world coordinates that cross the date line using degrees.
	/// Immutable & threadsafe.
	/// </summary>
	public class RectangleImpl : Rectangle
	{
		private readonly double minX;
		private readonly double maxX;
		private readonly double minY;
		private readonly double maxY;

		//TODO change to West South East North to be more consistent with OGC?
		public RectangleImpl(double minX, double maxX, double minY, double maxY)
		{
			//We assume any normalization / validation of params already occurred.
			this.minX = minX;
			this.maxX = maxX;
			this.minY = minY;
			this.maxY = maxY;
			//assert minY <= maxY;
		}

		public RectangleImpl(Rectangle r)
		{
			minX = r.GetMinX();
			maxX = r.GetMaxX();
			minY = r.GetMinY();
			maxY = r.GetMaxY();
		}

		public SpatialRelation Relate(Shape other, SpatialContext ctx)
		{
			if (other is Point)
			{
				return Relate((Point)other, ctx);
			}
			if (other is Rectangle)
			{
				return Relate((Rectangle)other, ctx);
			}
			return other.Relate(this, ctx).Transpose();
		}

		public SpatialRelation Relate(Point point, SpatialContext ctx)
		{
			if (point.GetY() > GetMaxY() || point.GetY() < GetMinY() ||
				(GetCrossesDateLine() ?
					(point.GetX() < minX && point.GetX() > maxX)
					: (point.GetX() < minX || point.GetX() > maxX)))
				return SpatialRelation.DISJOINT;
			return SpatialRelation.CONTAINS;
		}

		public SpatialRelation relate(Rectangle rect, SpatialContext ctx)
		{
			SpatialRelation yIntersect = Relate_yRange(rect.GetMinY(), rect.GetMaxY(), ctx);
			if (yIntersect == SpatialRelation.DISJOINT)
				return SpatialRelation.DISJOINT;

			SpatialRelation xIntersect = Relate_xRange(rect.GetMinX(), rect.GetMaxX(), ctx);
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
				x = DistanceUtils.NormLonDEG(x);
			return new PointImpl(x, y);
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

		public double GetArea()
		{
			return GetWidth() * GetHeight();
		}

		public bool GetCrossesDateLine()
		{
			return (minX > maxX);
		}

		public SpatialRelation Relate_yRange(double ext_minY, double ext_maxY, SpatialContext ctx)
		{
			if (ext_minY > maxY || ext_maxY < minY)
			{
				return SpatialRelation.DISJOINT;
			}

			if (ext_minY >= minY && ext_maxY <= maxY)
			{
				return SpatialRelation.CONTAINS;
			}

			if (ext_minY <= minY && ext_maxY >= maxY)
			{
				return SpatialRelation.WITHIN;
			}
			return SpatialRelation.INTERSECTS;
		}

		public SpatialRelation Relate_xRange(double ext_minX, double ext_maxX, SpatialContext ctx)
		{
			//For ext & this we have local minX and maxX variable pairs. We rotate them so that minX <= maxX
			double minX = this.minX;
			double maxX = this.maxX;
			if (ctx.IsGeo())
			{
				//the 360 check is an edge-case for complete world-wrap
				double ext_width = ext_maxX - ext_minX;
				if (ext_width < 0)//this logic unfortunately duplicates getWidth()
					ext_width += 360;

				if (ext_width < 360)
				{
					ext_maxX = ext_minX + ext_width;
				}
				else
				{
					ext_maxX = 180 + 360;
				}

				if (GetWidth() < 360)
				{
					maxX = minX + GetWidth();
				}
				else
				{
					maxX = 180 + 360;
				}

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

			if (ext_minX > maxX || ext_maxX < minX)
			{
				return SpatialRelation.DISJOINT;
			}

			if (ext_minX >= minX && ext_maxX <= maxX)
			{
				return SpatialRelation.CONTAINS;
			}

			if (ext_minX <= minX && ext_maxX >= maxX)
			{
				return SpatialRelation.WITHIN;
			}
			return SpatialRelation.INTERSECTS;
		}

		public override string ToString()
		{
			return "Rect(minX=" + minX + ",maxX=" + maxX + ",minY=" + minY + ",maxY=" + maxY + ")";
		}

		public override bool Equals(object o)
		{
			if (this == o) return true;

			var rectangle = o as RectangleImpl;
			if (rectangle == null) return false;

			return maxX.Equals(rectangle.maxX) && minX.Equals(rectangle.minX) &&
			       maxY.Equals(rectangle.maxY) && minY.Equals(rectangle.minY);
		}

		public override int GetHashCode()
		{
			long temp = minX != +0.0d ? BitConverter.DoubleToInt64Bits(minX) : 0L;
			int result = (int)(temp ^ ((uint)temp >> 32));
			temp = maxX != +0.0d ? BitConverter.DoubleToInt64Bits(maxX) : 0L;
			result = 31 * result + (int)(temp ^ ((uint)temp >> 32));
			temp = minY != +0.0d ? BitConverter.DoubleToInt64Bits(minY) : 0L;
			result = 31 * result + (int)(temp ^ ((uint)temp >> 32));
			temp = maxY != +0.0d ? BitConverter.DoubleToInt64Bits(maxY) : 0L;
			result = 31*result + (int) (temp ^ ((uint)temp >> 32));
			return result;
		}
	}
}
