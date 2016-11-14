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

        /**
   * @param squared Set to true to have {@link #distance(com.spatial4j.core.shape.Point, com.spatial4j.core.shape.Point)}
   *                return the square of the correct answer. This is a
   *                performance optimization used when sorting in which the
   *                actual distance doesn't matter so long as the sort order is
   *                consistent.
   */
        public CartesianDistCalc(bool squared)
        {
            this.squared = squared;
        }

        public override double Distance(IPoint from, double toX, double toY)
        {
            double deltaX = from.GetX() - toX;
            double deltaY = from.GetY() - toY;
            double xSquaredPlusYSquared = deltaX * deltaX + deltaY * deltaY;

            if (squared)
                return xSquaredPlusYSquared;

            return Math.Sqrt(xSquaredPlusYSquared);
        }

        public override bool Within(IPoint from, double toX, double toY, double distance)
        {
            double deltaX = from.GetX() - toX;
            double deltaY = from.GetY() - toY;
            return deltaX * deltaX + deltaY * deltaY <= distance * distance;
        }

        public override IPoint PointOnBearing(IPoint from, double distDEG, double bearingDEG, SpatialContext ctx, IPoint reuse)
        {
            if (distDEG == 0)
            {
                if (reuse == null)
                    return from;
                reuse.Reset(from.GetX(), from.GetY());
                return reuse;
            }
            double bearingRAD = DistanceUtils.ToRadians(bearingDEG);
            double x = from.GetX() + Math.Sin(bearingRAD) * distDEG;
            double y = from.GetY() + Math.Cos(bearingRAD) * distDEG;
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

        public override IRectangle CalcBoxByDistFromPt(IPoint from, double distDEG, SpatialContext ctx, IRectangle reuse)
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

        public override double CalcBoxByDistFromPt_yHorizAxisDEG(IPoint from, double distDEG, SpatialContext ctx)
        {
            return from.GetY();
        }

        public override double Area(IRectangle rect)
        {
            return rect.GetArea(null);
        }

        public override double Area(ICircle circle)
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
