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

		public override string ToString()
		{
			return "Pt(x=" + x + ",y=" + y + ")";
		}

		public override bool Equals(object o)
		{
			if (this == o) return true;
			
			var point = o as PointImpl;
			if (point == null) return false;

			return x.Equals(point.x) && y.Equals(point.y);
		}

		public override int GetHashCode()
		{
			long temp = x != +0.0d ? BitConverter.DoubleToInt64Bits(x) : 0L;
			int result = (int)(temp ^ ((uint)temp >> 32));
			temp = y != +0.0d ? BitConverter.DoubleToInt64Bits(y) : 0L;
			result = 31 * result + (int)(temp ^ ((uint)temp >> 32));
			return result;
		}
	}
}
