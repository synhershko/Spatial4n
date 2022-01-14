﻿/*
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

using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using System;

namespace Spatial4n.Core.Distance
{
    /// <summary>
    /// A base class for a Distance Calculator that assumes a spherical earth model. 
    /// </summary>
    public abstract class GeodesicSphereDistCalc : AbstractDistanceCalculator
    {
        private readonly double radiusDEG = DistanceUtils.ToDegrees(1);//in degrees

        public override IPoint PointOnBearing(IPoint from, double distDEG, double bearingDEG, SpatialContext ctx, IPoint? reuse)
        {
            if (distDEG == 0)
            {
                if (reuse is null)
                    return from;
                reuse.Reset(from.X, from.Y);
                return reuse;
            }
            IPoint result = DistanceUtils.PointOnBearingRAD(
                DistanceUtils.ToRadians(from.Y), DistanceUtils.ToRadians(from.X),
                DistanceUtils.ToRadians(distDEG),
                DistanceUtils.ToRadians(bearingDEG), ctx, reuse);//output result is in radians
            result.Reset(DistanceUtils.ToDegrees(result.X), DistanceUtils.ToDegrees(result.Y));
            return result;
        }

        public override IRectangle CalcBoxByDistFromPt(IPoint from, double distDEG, SpatialContext ctx, IRectangle? reuse)
        {
            return DistanceUtils.CalcBoxByDistFromPtDEG(from.Y, from.X, distDEG, ctx, reuse);
        }

        public override double CalcBoxByDistFromPt_yHorizAxisDEG(IPoint from, double distDEG, SpatialContext ctx)
        {
            return DistanceUtils.CalcBoxByDistFromPt_latHorizAxisDEG(from.Y, from.X, distDEG);
        }

        public override double Area(IRectangle rect)
        {
            //From http://mathforum.org/library/drmath/view/63767.html
            double lat1 = DistanceUtils.ToRadians(rect.MinY);
            double lat2 = DistanceUtils.ToRadians(rect.MaxY);
            return Math.PI / 180 * radiusDEG * radiusDEG *
                    Math.Abs(Math.Sin(lat1) - Math.Sin(lat2)) *
                    rect.Width;
        }

        public override double Area(ICircle circle)
        {
            //formula is a simplified case of area(rect).
            double lat = DistanceUtils.ToRadians(90 - circle.Radius);
            return 2 * Math.PI * radiusDEG * radiusDEG * (1 - Math.Sin(lat));
        }

        public override bool Equals(object o)
        {
            if (o == null) return false;
            return GetType() == o.GetType();
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        public override double Distance(IPoint @from, double toX, double toY)
        {
            return DistanceUtils.ToDegrees(DistanceLatLonRAD(DistanceUtils.ToRadians(from.Y),
                DistanceUtils.ToRadians(from.X), DistanceUtils.ToRadians(toY), DistanceUtils.ToRadians(toX)));
        }

        protected abstract double DistanceLatLonRAD(double lat1, double lon1, double lat2, double lon2);

        public class Haversine : GeodesicSphereDistCalc
        {

            protected override double DistanceLatLonRAD(double lat1, double lon1, double lat2, double lon2)
            {
                return DistanceUtils.DistHaversineRAD(lat1, lon1, lat2, lon2);
            }
        }

        public class LawOfCosines : GeodesicSphereDistCalc
        {
            protected override double DistanceLatLonRAD(double lat1, double lon1, double lat2, double lon2)
            {
                return DistanceUtils.DistLawOfCosinesRAD(lat1, lon1, lat2, lon2);
            }

        }

        public class Vincenty : GeodesicSphereDistCalc
        {
            protected override double DistanceLatLonRAD(double lat1, double lon1, double lat2, double lon2)
            {
                return DistanceUtils.DistVincentyRAD(lat1, lon1, lat2, lon2);
            }
        }
    }
}
