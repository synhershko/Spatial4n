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
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using System;
using Xunit;

namespace Spatial4n.Core.Distance
{
    public class TestDistances
    {
        private readonly Random random = new Random(RandomSeed.Seed());

        //NOTE!  These are sometimes modified by tests.
        private SpatialContext ctx;
        private double EPS; //delta when doing double assertions. Geo eps is not that small.;

        // Setup for every test
        public TestDistances()
        {
            ctx = SpatialContext.GEO;
            EPS = 10e-4;
        }

        private IDistanceCalculator Dc()
        {
            return ctx.DistCalc;
        }

        [Fact]
        public virtual void TestSomeDistances()
        {
            //See to verify: from http://www.movable-type.co.uk/scripts/latlong.html
            IPoint ctr = PLL(0, 100);
            CustomAssert.EqualWithDelta(11100, Dc().Distance(ctr, PLL(10, 0)) * DistanceUtils.DegreesToKilometers, 3);
            double deg = Dc().Distance(ctr, PLL(10, -160));
            CustomAssert.EqualWithDelta(11100, deg * DistanceUtils.DegreesToKilometers, 3);

            CustomAssert.EqualWithDelta(314.40338, Dc().Distance(PLL(1, 2), PLL(3, 4)) * DistanceUtils.DegreesToKilometers, EPS);
        }

        [Fact]
        public virtual void TestCalcBoxByDistFromPt()
        {
            //first test regression
            {
                double d = 6894.1 * DistanceUtils.KilometersToDegrees;
                IPoint pCtr = PLL(-20, 84);
                IPoint pTgt = PLL(-42, 15);
                Assert.True(Dc().Distance(pCtr, pTgt) < d);
                //since the pairwise distance is less than d, a bounding box from ctr with d should contain pTgt.
                IRectangle r = Dc().CalcBoxByDistFromPt(pCtr, d, ctx, null);
                Assert.Equal(SpatialRelation.Contains, r.Relate(pTgt));
                CheckBBox(pCtr, d);
            }

            CustomAssert.EqualWithDelta(/*"0 dist, horiz line",*/
                -45, Dc().CalcBoxByDistFromPt_yHorizAxisDEG(ctx.MakePoint(-180, -45), 0, ctx), 0);

            double MAXDIST = (double)180 * DistanceUtils.DegreesToKilometers;
            CheckBBox(ctx.MakePoint(0, 0), MAXDIST);
            CheckBBox(ctx.MakePoint(0, 0), MAXDIST * 0.999999);
            CheckBBox(ctx.MakePoint(0, 0), 0);
            CheckBBox(ctx.MakePoint(0, 0), 0.000001);
            CheckBBox(ctx.MakePoint(0, 90), 0.000001);
            CheckBBox(ctx.MakePoint(-32.7, -5.42), 9829);
            CheckBBox(ctx.MakePoint(0, 90 - 20), (double)20 * DistanceUtils.DegreesToKilometers);
            {
                double d = 0.010;//10m
                CheckBBox(ctx.MakePoint(0, 90 - (d + 0.001) * DistanceUtils.KilometersToDegrees), d);
            }

            for (int T = 0; T < 100; T++)
            {
                double lat = -90 + random.NextDouble() * 180;
                double lon = -180 + random.NextDouble() * 360;
                IPoint ctr = ctx.MakePoint(lon, lat);
                double dist = MAXDIST * random.NextDouble();
                CheckBBox(ctr, dist);
            }
        }


        private void CheckBBox(IPoint ctr, double distKm)
        {
            string msg = "ctr: " + ctr + " distKm: " + distKm;
            double dist = distKm * DistanceUtils.KilometersToDegrees;

            IRectangle r = Dc().CalcBoxByDistFromPt(ctr, dist, ctx, null);
            double horizAxisLat = Dc().CalcBoxByDistFromPt_yHorizAxisDEG(ctr, dist, ctx);
            if (!double.IsNaN(horizAxisLat))
                Assert.True(r.RelateYRange(horizAxisLat, horizAxisLat).Intersects());

            //horizontal
            if (r.Width >= 180)
            {
                double deg = Dc().Distance(ctr, r.MinX, r.MaxY == 90 ? 90 : -90);
                double calcDistKm = deg * DistanceUtils.DegreesToKilometers;
                Assert.True(/*msg,*/ calcDistKm <= distKm + EPS);
                //horizAxisLat is meaningless in this context
            }
            else
            {
                IPoint tPt = FindClosestPointOnVertToPoint(r.MinX, r.MinY, r.MaxY, ctr);
                double calcDistKm = Dc().Distance(ctr, tPt) * DistanceUtils.DegreesToKilometers;
                CustomAssert.EqualWithDelta(/*msg,*/ distKm, calcDistKm, EPS);
                CustomAssert.EqualWithDelta(/*msg,*/ tPt.Y, horizAxisLat, EPS);
            }

            //vertical
            double topDistKm = Dc().Distance(ctr, ctr.X, r.MaxY) * DistanceUtils.DegreesToKilometers;
            if (r.MaxY == 90)
                Assert.True(/*msg,*/ topDistKm <= distKm + EPS);
            else
                CustomAssert.EqualWithDelta(msg, distKm, topDistKm, EPS);
            double botDistKm = Dc().Distance(ctr, ctr.X, r.MinY) * DistanceUtils.DegreesToKilometers;
            if (r.MinY == -90)
                Assert.True(/*msg,*/ botDistKm <= distKm + EPS);
            else
                CustomAssert.EqualWithDelta(/*msg,*/ distKm, botDistKm, EPS);
        }

        private IPoint FindClosestPointOnVertToPoint(double lon, double lowLat, double highLat, IPoint ctr)
        {
            //A binary search algorithm to find the point along the vertical lon between lowLat & highLat that is closest
            // to ctr, and returns the distance.
            double midLat = (highLat - lowLat) / 2 + lowLat;
            double midLatDist = ctx.DistCalc.Distance(ctr, lon, midLat);
            for (int L = 0; L < 100 && (highLat - lowLat > 0.001 || L < 20); L++)
            {
                bool bottom = (midLat - lowLat > highLat - midLat);
                double newMid = bottom ? (midLat - lowLat) / 2 + lowLat : (highLat - midLat) / 2 + midLat;
                double newMidDist = ctx.DistCalc.Distance(ctr, lon, newMid);
                if (newMidDist < midLatDist)
                {
                    if (bottom)
                        highLat = midLat;
                    else
                        lowLat = midLat;
                    midLat = newMid;
                    midLatDist = newMidDist;
                }
                else
                {
                    if (bottom)
                        lowLat = newMid;
                    else
                        highLat = newMid;
                }
            }
            return ctx.MakePoint(lon, midLat);
        }

        [Fact]
        public virtual void TestDistCalcPointOnBearing_Cartesian()
        {
#pragma warning disable 612, 618
            ctx = new SpatialContext(false);
#pragma warning restore 612, 618
            EPS = 10e-6; //tighter epsilon (aka delta)
            for (int i = 0; i < 1000; i++)
            {
                TestDistCalcPointOnBearing(random.Next(100));
            }
        }

        [Fact]
        public virtual void TestDistCalcPointOnBearing_Geo()
        {
            //The haversine formula has a higher error if the points are near antipodal. We adjust EPS tolerance for this case.
            //TODO Eventually we should add the Vincenty formula for improved accuracy, or try some other cleverness.

            //test known high delta
            //{
            //    Point c = ctx.makePoint(-103, -79);
            //    double angRAD = Math.toRadians(236);
            //    double dist = 20025;
            //    Point p2 = dc().pointOnBearingRAD(c, dist, angRAD, ctx);
            //    //Pt(x=76.61200011750923,y=79.04946929870962)
            //    double calcDist = dc().distance(c, p2);
            //    assertEqualsRatio(dist, calcDist);
            //}

            double maxDistKm = (double)180 * DistanceUtils.DegreesToKilometers;
            for (int i = 0; i < 1000; i++)
            {
                int dist = random.Next((int)maxDistKm);
                EPS = (dist < maxDistKm * 0.75 ? 10e-6 : 10e-3);
                TestDistCalcPointOnBearing(dist);
            }
        }

        private void TestDistCalcPointOnBearing(double distKm)
        {
            for (int angDEG = 0; angDEG < 360; angDEG += random.Next(1, 20 + 1))
            {
                IPoint c = ctx.MakePoint(
                    DistanceUtils.NormLonDEG(random.Next(359 + 1)),
                    random.Next(-90, 90 + 1));

                //0 distance means same point
                IPoint p2 = Dc().PointOnBearing(c, 0, angDEG, ctx, null);
                Assert.Equal(c, p2);

                p2 = Dc().PointOnBearing(c, distKm * DistanceUtils.KilometersToDegrees, angDEG, ctx, null);
                double calcDistKm = Dc().Distance(c, p2) * DistanceUtils.DegreesToKilometers;
                AssertEqualsRatio(distKm, calcDistKm);
            }
        }

        private void AssertEqualsRatio(double expected, double actual)
        {
            double delta = Math.Abs(actual - expected);
            double baseValue = Math.Min(actual, expected);
            double deltaRatio = baseValue == 0 ? delta : Math.Min(delta, delta / baseValue);
            CustomAssert.EqualWithDelta(0, deltaRatio, EPS);
        }

        [Fact]
        public virtual void TestNormLat()
        {
            var lats = new double[][]
            {
                new double[] {1.23, 1.23},
                //1.23 might become 1.2299999 after some math and we want to ensure that doesn't happen
                new double[] {-90, -90},
                new double[] {90, 90},
                new double[] {0, 0},
                new double[] {-100, -80},
                new double[] {-90 - 180, 90},
                new double[] {-90 - 360, -90},
                new double[] {90 + 180, -90},
                new double[] {90 + 360, 90},
                new double[] {-12 + 180, 12}
            };
            foreach (var pair in lats)
            {
                CustomAssert.EqualWithDelta( /* "input "+pair[0],*/
                    pair[1], DistanceUtils.NormLatDEG(pair[0]), double.Epsilon);
            }
            var random = new Random(RandomSeed.Seed());
            for (int i = -1000; i < 1000; i += random.Next(9) * 10)
            {
                double d = DistanceUtils.NormLatDEG(i);
                Assert.True( /*i + " " + d,*/ d >= -90 && d <= 90);
            }
        }

        [Fact]
        public virtual void TestNormLon()
        {
            var lons = new double[][]
            {
                new double[] {1.23, 1.23},
                //1.23 might become 1.2299999 after some math and we want to ensure that doesn't happen
                new double[] {-180, -180},
                new double[] {180, +180},
                new double[] {0, 0},
                new double[] {-190, 170},
                new double[] {181, -179},
                new double[] {-180 - 360, -180},
                new double[] {-180 - 720, -180},
                new double[] {180 + 360, +180},
                new double[] {180 + 720, +180}
            };
            foreach (var pair in lons)
            {
                CustomAssert.EqualWithDelta( /*"input "+pair[0],*/
                    pair[1], DistanceUtils.NormLonDEG(pair[0]), double.Epsilon);
            }

            var random = new Random(RandomSeed.Seed());
            for (int i = -1000; i < 1000; i += random.Next(9) * 10)
            {
                double d = DistanceUtils.NormLonDEG(i);
                Assert.True(d >= -180 && d <= 180, i + " " + d);
            }
        }

        [Fact]
        public void AssertDistanceConversion()
        {
            AssertDistanceConversionImpl(0);
            AssertDistanceConversionImpl(500);
            AssertDistanceConversionImpl(DistanceUtils.EarthMeanRadiusKilometers);
        }

        private void AssertDistanceConversionImpl(double dist)
        {
            double radius = DistanceUtils.EarthMeanRadiusKilometers;
            //test back & forth conversion for both
            double distRAD = DistanceUtils.Dist2Radians(dist, radius);
            CustomAssert.EqualWithDelta(dist, DistanceUtils.Radians2Dist(distRAD, radius), EPS);
            double distDEG = DistanceUtils.Dist2Degrees(dist, radius);
            CustomAssert.EqualWithDelta(dist, DistanceUtils.Degrees2Dist(distDEG, radius), EPS);
            //test across rad & deg
            CustomAssert.EqualWithDelta(distDEG, DistanceUtils.ToDegrees(distRAD), EPS);
            //test point on bearing
            CustomAssert.EqualWithDelta(
                DistanceUtils.PointOnBearingRAD(0, 0, DistanceUtils.Dist2Radians(dist, radius),
                    DistanceUtils.Degrees90AsRadians, ctx, new Point(0, 0, ctx)).X,
                distRAD, 10e-5);
        }

        private IPoint PLL(double lat, double lon)
        {
            return ctx.MakePoint(lon, lat);
        }

        [Fact]
        public virtual void TestArea()
        {
            double radius = DistanceUtils.EarthMeanRadiusKilometers * DistanceUtils.KilometersToDegrees;
            //surface of a sphere is 4 * pi * r^2
            double earthArea = 4 * Math.PI * radius * radius;

            ICircle c = ctx.MakeCircle(random.Next(-180, 180), random.Next(-90, 90),
                                      180); //180 means whole earth
            CustomAssert.EqualWithDelta(earthArea, c.GetArea(ctx), 1.0);

            CustomAssert.EqualWithDelta(earthArea, ctx.WorldBounds.GetArea(ctx), 1.0);

            //now check half earth
            ICircle cHalf = ctx.MakeCircle(c.Center, 90);
            CustomAssert.EqualWithDelta(earthArea / 2, cHalf.GetArea(ctx), 1.0);

            //circle with same radius at +20 lat with one at -20 lat should have same area as well as bbox with same area
            ICircle c2 = ctx.MakeCircle(c.Center, 30);
            ICircle c3 = ctx.MakeCircle(c.Center.X, 20, 30);
            CustomAssert.EqualWithDelta(c2.GetArea(ctx), c3.GetArea(ctx), 0.01);
            ICircle c3Opposite = ctx.MakeCircle(c.Center.X, -20, 30);
            CustomAssert.EqualWithDelta(c3.GetArea(ctx), c3Opposite.GetArea(ctx), 0.01);
            CustomAssert.EqualWithDelta(c3.BoundingBox.GetArea(ctx), c3Opposite.BoundingBox.GetArea(ctx), 0.01);

            //small shapes near the equator should have similar areas to euclidean rectangle
            IRectangle smallRect = ctx.MakeRectangle(0, 1, 0, 1);
            CustomAssert.EqualWithDelta(1.0, smallRect.GetArea(null), 0.0);
            double smallDelta = smallRect.GetArea(null) - smallRect.GetArea(ctx);
            Assert.True(smallDelta > 0 && smallDelta < 0.0001);

            ICircle smallCircle = ctx.MakeCircle(0, 0, 1);
            smallDelta = smallCircle.GetArea(null) - smallCircle.GetArea(ctx);
            Assert.True(smallDelta > 0 && smallDelta < 0.0001);

            //bigger, but still fairly similar
            //c2 = ctx.makeCircle(c.getCenter(), 30);
            double areaRatio = c2.GetArea(null) / c2.GetArea(ctx);
            Assert.True(areaRatio > 1 && areaRatio < 1.1);
        }
    }
}

