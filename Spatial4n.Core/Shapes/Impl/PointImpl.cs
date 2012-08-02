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
	/// A basic 2D implementation of a Point.
	/// </summary>
	public class PointImpl : Point
	{
		private readonly double x;
		private readonly double y;

		public PointImpl(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		public SpatialRelation Relate(Shape other, SpatialContext ctx)
		{
			if (other is Point)
				return this.Equals(other) ? SpatialRelation.INTERSECTS : SpatialRelation.DISJOINT;
			return other.Relate(this, ctx).Transpose();
		}

		public Rectangle GetBoundingBox()
		{
			return new RectangleImpl(x, x, y, y);
		}

		public bool HasArea()
		{
			return false;
		}

		public Point GetCenter()
		{
			return this;
		}

		public double GetX()
		{
			return x;
		}

		public double GetY()
		{
			return y;
		}

		public double GetArea(SpatialContext ctx)
		{
			return 0;
		}

		public override string ToString()
		{
			return "Pt(x=" + x + ",y=" + y + ")";
		}

		public override bool Equals(object o)
		{
			return Equals(this, o);
		}

		/// <summary>
		/// All {@link Point} implementations should use this definition of {@link Object#equals(Object)}.
		/// </summary>
		/// <param name="thiz"></param>
		/// <param name="o"></param>
		/// <returns></returns>
		public static bool Equals(Point thiz, Object o)
		{
			if (thiz == null)
				throw new ArgumentNullException("thiz");

			if (thiz == o) return true;
			
			var point = o as PointImpl;
			if (point == null) return false;

			return thiz.GetX().Equals(point.x) && thiz.GetY().Equals(point.y);
		}

		public override int GetHashCode()
		{
			return GetHashCode(this);
		}

		/// <summary>
		/// All {@link Point} implementations should use this definition of {@link Object#hashCode()}.
		/// </summary>
		/// <param name="thiz"></param>
		/// <returns></returns>
		public static int GetHashCode(Point thiz)
		{
			if (thiz == null)
				throw new ArgumentNullException("thiz");

			long temp = thiz.GetX() != +0.0d ? BitConverter.DoubleToInt64Bits(thiz.GetX()) : 0L;
			int result = (int)(temp ^ ((uint)temp >> 32));
			temp = thiz.GetY() != +0.0d ? BitConverter.DoubleToInt64Bits(thiz.GetY()) : 0L;
			result = 31 * result + (int)(temp ^ ((uint)temp >> 32));
			return result;
		}
	}
}
