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
    public class RectangleImpl : IRectangle
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

        public RectangleImpl(IPoint lowerLeft, IPoint upperRight, SpatialContext ctx)
            : this(lowerLeft.GetX(), upperRight.GetX(), lowerLeft.GetY(), upperRight.GetY(), ctx)
        {
        }

        public RectangleImpl(IRectangle r, SpatialContext ctx)
            : this(r.GetMinX(), r.GetMaxX(), r.GetMinY(), r.GetMaxY(), ctx)
        {
        }

        public void Reset(double minX, double maxX, double minY, double maxY)
        {
            Debug.Assert(!IsEmpty);
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            Debug.Assert(minY <= maxY || double.IsNaN(minY), "minY, maxY: " + minY + ", " + maxY);
        }

        public virtual bool IsEmpty
        {
            get { return double.IsNaN(minX); }
        }

        public virtual /*Rectangle*/ IShape GetBuffered(double distance, SpatialContext ctx)
        {
            if (ctx.IsGeo())
            {
                //first check pole touching, triggering a world-wrap rect
                if (maxY + distance >= 90)
                {
                    return ctx.MakeRectangle(-180, 180, Math.Max(-90, minY - distance), 90);
                }
                else if (minY - distance <= -90)
                {
                    return ctx.MakeRectangle(-180, 180, -90, Math.Min(90, maxY + distance));
                }
                else
                {
                    //doesn't touch pole
                    double latDistance = distance;
                    double closestToPoleY = (maxY - minY > 0) ? maxY : minY;
                    double lonDistance = DistanceUtils.CalcBoxByDistFromPt_deltaLonDEG(
                        closestToPoleY, minX, distance);//lat,lon order
                                                        //could still wrap the world though...
                    if (lonDistance * 2 + GetWidth() >= 360)
                        return ctx.MakeRectangle(-180, 180, minY - latDistance, maxY + latDistance);
                    return ctx.MakeRectangle(
                        DistanceUtils.NormLonDEG(minX - lonDistance),
                        DistanceUtils.NormLonDEG(maxX + lonDistance),
                        minY - latDistance, maxY + latDistance);
                }
            }
            else
            {
                IRectangle worldBounds = ctx.GetWorldBounds();
                double newMinX = Math.Max(worldBounds.GetMinX(), minX - distance);
                double newMaxX = Math.Min(worldBounds.GetMaxX(), maxX + distance);
                double newMinY = Math.Max(worldBounds.GetMinY(), minY - distance);
                double newMaxY = Math.Min(worldBounds.GetMaxY(), maxY + distance);
                return ctx.MakeRectangle(newMinX, newMaxX, newMinY, newMaxY);
            }
        }

        public virtual bool HasArea()
        {
            return maxX != minX && maxY != minY;
        }

        public virtual double GetArea(SpatialContext ctx)
        {
            if (ctx == null)
            {
                return GetWidth() * GetHeight();
            }
            else
            {
                return ctx.GetDistCalc().Area(this);
            }
        }

        public virtual bool GetCrossesDateLine()
        {
            return (minX > maxX);
        }

        public virtual double GetHeight()
        {
            return maxY - minY;
        }

        public virtual double GetWidth()
        {
            double w = maxX - minX;
            if (w < 0)
            {
                //only true when minX > maxX (WGS84 assumed)
                w += 360;
                Debug.Assert(w >= 0);
            }
            return w;
        }

        public virtual double GetMaxX()
        {
            return maxX;
        }

        public virtual double GetMaxY()
        {
            return maxY;
        }

        public virtual double GetMinX()
        {
            return minX;
        }

        public virtual double GetMinY()
        {
            return minY;
        }

        public virtual IRectangle GetBoundingBox()
        {
            return this;
        }

        public virtual SpatialRelation Relate(IShape other)
        {
            if (IsEmpty || other.IsEmpty)
                return SpatialRelation.DISJOINT;
            var point = other as IPoint;
            if (point != null)
            {
                return Relate(point);
            }
            var rectangle = other as IRectangle;
            if (rectangle != null)
            {
                return Relate(rectangle);
            }
            return other.Relate(this).Transpose();
        }

        public virtual SpatialRelation Relate(IPoint point)
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
                }
                else
                {
                    return SpatialRelation.CONTAINS; //short-circuit
                }
            }
            if (pX < minX || pX > maxX)
                return SpatialRelation.DISJOINT;
            return SpatialRelation.CONTAINS;
        }

        public virtual SpatialRelation Relate(IRectangle rect)
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

        public virtual IPoint GetCenter()
        {
            if (double.IsNaN(minX))
                return ctx.MakePoint(double.NaN, double.NaN);
            double y = GetHeight() / 2 + minY;
            double x = GetWidth() / 2 + minX;
            if (minX > maxX)//WGS84
                x = DistanceUtils.NormLonDEG(x); //in case falls outside the standard range
            return new PointImpl(x, y, ctx);
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
        public static bool Equals(IRectangle thiz, object o)
        {
            if (thiz == null)
                throw new ArgumentNullException("thiz");

            if (thiz == o) return true;

            var rectangle = o as IRectangle;
            if (rectangle == null) return false;

            return thiz.GetMaxX().Equals(rectangle.GetMaxX()) && thiz.GetMinX().Equals(rectangle.GetMinX()) &&
                   thiz.GetMaxY().Equals(rectangle.GetMaxY()) && thiz.GetMinY().Equals(rectangle.GetMinY());
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        public static int GetHashCode(IRectangle thiz)
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
            result = 31 * result + (int)(temp ^ ((uint)temp >> 32));
            return result;
        }
    }
}
