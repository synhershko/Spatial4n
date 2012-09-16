using System;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using Xunit;

namespace Spatial4n.Tests.distance
{
    public class TestDistances
    {
        private readonly Random random = new Random(RandomSeed.Seed());

        //NOTE!  These are sometimes modified by tests.
        private SpatialContext ctx = SpatialContext.GEO;
        private const double EPS = 10e-4; //delta when doing double assertions. Geo eps is not that small.;

        private DistanceCalculator dc()
        {
            return ctx.GetDistCalc();
        }

        private Point pLL(double lat, double lon)
        {
            return ctx.MakePoint(lon, lat);
        }

        [Fact]
        public void testSomeDistances()
        {
            //See to verify: from http://www.movable-type.co.uk/scripts/latlong.html
            Point ctr = pLL(0, 100);
            CustomAssert.EqualWithDelta(11100, dc().Distance(ctr, pLL(10, 0))*DistanceUtils.DEG_TO_KM, 3);
            double deg = dc().Distance(ctr, pLL(10, -160));
            CustomAssert.EqualWithDelta(11100, deg*DistanceUtils.DEG_TO_KM, 3);

            CustomAssert.EqualWithDelta(314.40338, dc().Distance(pLL(1, 2), pLL(3, 4))*DistanceUtils.DEG_TO_KM, EPS);
        }

        [Fact]
        public void testCalcBoxByDistFromPt()
        {
            //first test regression
            {
                double d = 6894.1 * DistanceUtils.KM_TO_DEG;
                Point pCtr = pLL(-20, 84);
                Point pTgt = pLL(-42, 15);
                Assert.True(dc().Distance(pCtr, pTgt) < d);
                //since the pairwise distance is less than d, a bounding box from ctr with d should contain pTgt.
                Rectangle r = dc().CalcBoxByDistFromPt(pCtr, d, ctx, null);
                Assert.Equal(SpatialRelation.CONTAINS, r.Relate(pTgt));
                checkBBox(pCtr, d);
            }

            Assert.Equal(/*"0 dist, horiz line",*/
                -45, dc().CalcBoxByDistFromPt_yHorizAxisDEG(ctx.MakePoint(-180, -45), 0, ctx), 0);

            double MAXDIST = (double)180 * DistanceUtils.DEG_TO_KM;
            checkBBox(ctx.MakePoint(0, 0), MAXDIST);
            checkBBox(ctx.MakePoint(0, 0), MAXDIST * 0.999999);
            checkBBox(ctx.MakePoint(0, 0), 0);
            checkBBox(ctx.MakePoint(0, 0), 0.000001);
            checkBBox(ctx.MakePoint(0, 90), 0.000001);
            checkBBox(ctx.MakePoint(-32.7, -5.42), 9829);
            checkBBox(ctx.MakePoint(0, 90 - 20), (double)20 * DistanceUtils.DEG_TO_KM);
            {
                double d = 0.010;//10m
                checkBBox(ctx.MakePoint(0, 90 - (d + 0.001) * DistanceUtils.KM_TO_DEG), d);
            }

            for (int T = 0; T < 100; T++)
            {
                double lat = -90 + random.NextDouble() * 180;
                double lon = -180 + random.NextDouble() * 360;
                Point ctr = ctx.MakePoint(lon, lat);
                double dist = MAXDIST * random.NextDouble();
                checkBBox(ctr, dist);
            }
        }


        private void checkBBox(Point ctr, double distKm)
        {
            String msg = "ctr: " + ctr + " distKm: " + distKm;
            double dist = distKm * DistanceUtils.KM_TO_DEG;

            Rectangle r = dc().CalcBoxByDistFromPt(ctr, dist, ctx, null);
            double horizAxisLat = dc().CalcBoxByDistFromPt_yHorizAxisDEG(ctr, dist, ctx);
            if (!Double.IsNaN(horizAxisLat))
                Assert.True(r.RelateYRange(horizAxisLat, horizAxisLat).Intersects());

            //horizontal
            if (r.GetWidth() >= 180)
            {
                double deg = dc().Distance(ctr, r.GetMinX(), r.GetMaxY() == 90 ? 90 : -90);
                double calcDistKm = deg * DistanceUtils.DEG_TO_KM;
                Assert.True(/*msg,*/ calcDistKm <= distKm + EPS);
                //horizAxisLat is meaningless in this context
            }
            else
            {
                Point tPt = FindClosestPointOnVertToPoint(r.GetMinX(), r.GetMinY(), r.GetMaxY(), ctr);
                double calcDistKm = dc().Distance(ctr, tPt) * DistanceUtils.DEG_TO_KM;
                CustomAssert.EqualWithDelta(/*msg,*/ distKm, calcDistKm, EPS);
                CustomAssert.EqualWithDelta(/*msg,*/ tPt.GetY(), horizAxisLat, EPS);
            }

            //vertical
            double topDistKm = dc().Distance(ctr, ctr.GetX(), r.GetMaxY()) * DistanceUtils.DEG_TO_KM;
            if (r.GetMaxY() == 90)
                Assert.True(/*msg,*/ topDistKm <= distKm + EPS);
            else
                CustomAssert.EqualWithDelta(msg, distKm, topDistKm, EPS);
            double botDistKm = dc().Distance(ctr, ctr.GetX(), r.GetMinY()) * DistanceUtils.DEG_TO_KM;
            if (r.GetMinY() == -90)
                Assert.True(/*msg,*/ botDistKm <= distKm + EPS);
            else
                CustomAssert.EqualWithDelta(/*msg,*/ distKm, botDistKm, EPS);
        }

        private Point FindClosestPointOnVertToPoint(double lon, double lowLat, double highLat, Point ctr)
        {
            //A binary search algorithm to find the point along the vertical lon between lowLat & highLat that is closest
            // to ctr, and returns the distance.
            double midLat = (highLat - lowLat)/2 + lowLat;
            double midLatDist = ctx.GetDistCalc().Distance(ctr, lon, midLat);
            for (int L = 0; L < 100 && (highLat - lowLat > 0.001 || L < 20); L++)
            {
                bool bottom = (midLat - lowLat > highLat - midLat);
                double newMid = bottom ? (midLat - lowLat)/2 + lowLat : (highLat - midLat)/2 + midLat;
                double newMidDist = ctx.GetDistCalc().Distance(ctr, lon, newMid);
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
        public void TestDistCalcPointOnBearing_Cartesian()
        {
            ctx = new SpatialContext(false);
            var EPS = 10e-6; //tighter epsilon (aka delta)
            for (int i = 0; i < 1000; i++)
            {
                TestDistCalcPointOnBearing(random.Next(100), EPS);
            }
        }

        [Fact]
        public void TestDistCalcPointOnBearing_Geo()
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

            double maxDistKm = (double) 180*DistanceUtils.DEG_TO_KM;
            for (int i = 0; i < 1000; i++)
            {
                int dist = random.Next((int) maxDistKm);
                var EPS = (dist < maxDistKm*0.75 ? 10e-6 : 10e-3);
                TestDistCalcPointOnBearing(dist, EPS);
            }
        }

        private void TestDistCalcPointOnBearing(double distKm, double EPS)
        {
            for (int angDEG = 0; angDEG < 360; angDEG += random.Next(1, 20))
            {
                Point c = ctx.MakePoint(
                    DistanceUtils.NormLonDEG(random.Next(359)),
                    random.Next(-90, 90));

                //0 distance means same point
                Point p2 = dc().PointOnBearing(c, 0, angDEG, ctx, null);
                Assert.Equal(c, p2);

                p2 = dc().PointOnBearing(c, distKm*DistanceUtils.KM_TO_DEG, angDEG, ctx, null);
                double calcDistKm = dc().Distance(c, p2)*DistanceUtils.DEG_TO_KM;
                AssertEqualsRatio(distKm, calcDistKm, EPS);
            }
        }

        private static void AssertEqualsRatio(double expected, double actual, double EPS)
        {
            double delta = Math.Abs(actual - expected);
            double baseValue = Math.Min(actual, expected);
            double deltaRatio = baseValue == 0 ? delta : Math.Min(delta, delta/baseValue);
            CustomAssert.EqualWithDelta(0, deltaRatio, EPS);
        }

        [Fact]
        public void TestNormLat()
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
                    pair[1], DistanceUtils.NormLatDEG(pair[0]), Double.Epsilon);
            }
            var random = new Random(RandomSeed.Seed());
            for (int i = -1000; i < 1000; i += random.Next(9)*10)
            {
                double d = DistanceUtils.NormLatDEG(i);
                Assert.True( /*i + " " + d,*/ d >= -90 && d <= 90);
            }
        }

        [Fact]
        public void TestNormLon()
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
                    pair[1], DistanceUtils.NormLonDEG(pair[0]), Double.Epsilon);
            }

            var random = new Random(RandomSeed.Seed());
            for (int i = -1000; i < 1000; i += random.Next(9)*10)
            {
                double d = DistanceUtils.NormLonDEG(i);
                Assert.True(d >= -180 && d <= 180, i + " " + d);
            }
        }

        [Fact]
        public void assertDistanceConversion()
        {
            assertDistanceConversion(0);
            assertDistanceConversion(500);
            assertDistanceConversion(DistanceUtils.EARTH_MEAN_RADIUS_KM);
        }

        private void assertDistanceConversion(double dist)
        {
            double radius = DistanceUtils.EARTH_MEAN_RADIUS_KM;
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
                                                DistanceUtils.DEG_90_AS_RADS, ctx, new PointImpl(0, 0, ctx)).GetX(),
                distRAD, 10e-5);
        }

        [Fact]
        public void testArea()
        {
            double radius = DistanceUtils.EARTH_MEAN_RADIUS_KM*DistanceUtils.KM_TO_DEG;
            //surface of a sphere is 4 * pi * r^2
            double earthArea = 4*Math.PI*radius*radius;

            Circle c = ctx.MakeCircle(random.Next(-180, 180), random.Next(-90, 90),
                                      180); //180 means whole earth
            CustomAssert.EqualWithDelta(earthArea, c.GetArea(ctx), 1.0);

            CustomAssert.EqualWithDelta(earthArea, ctx.GetWorldBounds().GetArea(ctx), 1.0);

            //now check half earth
            Circle cHalf = ctx.MakeCircle(c.GetCenter(), 90);
            CustomAssert.EqualWithDelta(earthArea/2, cHalf.GetArea(ctx), 1.0);

            //circle with same radius at +20 lat with one at -20 lat should have same area as well as bbox with same area
            Circle c2 = ctx.MakeCircle(c.GetCenter(), 30);
            Circle c3 = ctx.MakeCircle(c.GetCenter().GetX(), 20, 30);
            CustomAssert.EqualWithDelta(c2.GetArea(ctx), c3.GetArea(ctx), 0.01);
            Circle c3Opposite = ctx.MakeCircle(c.GetCenter().GetX(), -20, 30);
            CustomAssert.EqualWithDelta(c3.GetArea(ctx), c3Opposite.GetArea(ctx), 0.01);
            CustomAssert.EqualWithDelta(c3.GetBoundingBox().GetArea(ctx), c3Opposite.GetBoundingBox().GetArea(ctx), 0.01);

            //small shapes near the equator should have similar areas to euclidean rectangle
            Rectangle smallRect = ctx.MakeRectangle(0, 1, 0, 1);
            CustomAssert.EqualWithDelta(1.0, smallRect.GetArea(null), 0.0);
            double smallDelta = smallRect.GetArea(null) - smallRect.GetArea(ctx);
            Assert.True(smallDelta > 0 && smallDelta < 0.0001);

            Circle smallCircle = ctx.MakeCircle(0, 0, 1);
            smallDelta = smallCircle.GetArea(null) - smallCircle.GetArea(ctx);
            Assert.True(smallDelta > 0 && smallDelta < 0.0001);

            //bigger, but still fairly similar
            //c2 = ctx.makeCircle(c.getCenter(), 30);
            double areaRatio = c2.GetArea(null)/c2.GetArea(ctx);
            Assert.True(areaRatio > 1 && areaRatio < 1.1);
        }
    }
}

