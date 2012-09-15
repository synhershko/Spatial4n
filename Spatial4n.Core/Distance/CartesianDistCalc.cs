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
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Util;

namespace Spatial4n.Core.Distance
{
	/// <summary>
	/// Calculates based on Euclidean / Cartesian 2d plane.
	/// </summary>
	public class CartesianDistCalc : AbstractDistanceCalculator
	{
		private readonly bool squared;

		public CartesianDistCalc()
		{
			squared = false;
		}

		public CartesianDistCalc(bool squared)
		{
			this.squared = squared;
		}

		public override double Distance(Point from, double toX, double toY)
		{
			double result = 0;

			double v = from.GetX() - toX;
			result += (v * v);

			v = from.GetY() - toY;
			result += (v * v);

			if (squared)
				return result;

			return Math.Sqrt(result);
		}

        public override Point PointOnBearing(Point from, double distDEG, double bearingDEG, SpatialContext ctx, Point reuse)
        {
            if (distDEG == 0)
            {
                if (reuse == null)
                    return from;
                reuse.Reset(from.GetX(), from.GetY());
                return reuse;
            }
            double bearingRAD = DistanceUtils.ToRadians(bearingDEG);
            double x = from.GetX() + Math.Sin(bearingRAD)*distDEG;
            double y = from.GetY() + Math.Cos(bearingRAD)*distDEG;
            if (reuse == null)
            {
                return ctx.MakePoint(x, y);
            }
            else
            {
                reuse.Reset(x, y);
                return reuse;
            }
        }

        public override Rectangle CalcBoxByDistFromPt(Point from, double distDEG, SpatialContext ctx, Rectangle reuse)
        {
            double minX = from.GetX() - distDEG;
            double maxX = from.GetX() + distDEG;
            double minY = from.GetY() - distDEG;
            double maxY = from.GetY() + distDEG;
            if (reuse == null)
            {
                return ctx.MakeRectangle(minX, maxX, minY, maxY);
            }
            else
            {
                reuse.Reset(minX, maxX, minY, maxY);
                return reuse;
            }
        }

        public override double CalcBoxByDistFromPt_yHorizAxisDEG(Point from, double distDEG, SpatialContext ctx)
		{
			return from.GetY();
		}

		public override double Area(Rectangle rect)
		{
			return rect.GetArea(null);
		}

		public override double Area(Circle circle)
		{
			return circle.GetArea(null);
		}

		public override bool Equals(object o)
		{
			if (this == o) return true;

			var that = o as CartesianDistCalc;
			if (that == null) return false;
			return squared == that.squared;
		}

		public override int GetHashCode()
		{
			return (squared ? 1 : 0);
		}
	}
}
