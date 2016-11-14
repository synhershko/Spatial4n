using System;
using System.Collections.Generic;
using System.Globalization;
using Spatial4n.Core.Context;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;
using Xunit;
using Xunit.Extensions;

namespace Spatial4n.Tests.shape
{
    public class TestShapesGeo : AbstractTestShapes
    {
		public static IEnumerable<object[]> Contexts
		{
			get
			{
                //TODO ENABLE LawOfCosines WHEN WORKING
                //DistanceCalculator distCalcL = new GeodesicSphereDistCalc.Haversine(units.earthRadius());//default
                DistanceCalculator distCalcH = new GeodesicSphereDistCalc.Haversine();
				DistanceCalculator distCalcV = new GeodesicSphereDistCalc.Vincenty();

			    yield return new object[] { new SpatialContextFactory() { geo = true, distCalc = new RoundingDistCalc(distCalcH) }.NewSpatialContext() };
			    yield return new object[] { new SpatialContextFactory() { geo = true, distCalc = new RoundingDistCalc(distCalcV) }.NewSpatialContext() };
			    yield return new object[] { new NtsSpatialContextFactory() { geo = true, distCalc = new RoundingDistCalc(distCalcH) }.NewSpatialContext() };
			}
		}

        //public TestShapesGeo()
        //{
        //}

        //public TestShapesGeo(SpatialContext ctx) : base(ctx)
        //{
        //}

        private static double DegToKm(double deg)
        {
            return DistanceUtils.Degrees2Dist(deg, DistanceUtils.EARTH_MEAN_RADIUS_KM);
        }

        private static double KmToDeg(double km)
        {
            return DistanceUtils.Dist2Degrees(km, DistanceUtils.EARTH_MEAN_RADIUS_KM);
        }

        [Theory]
		[PropertyData("Contexts")]
		public virtual void TestGeoRectangle(SpatialContext ctx)
		{
			base.ctx = ctx;

            double v = 200 * (random.NextDouble() > 0.5 ? -1 : 1);
		    Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(v,0,0,0));
            Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(0,v,0,0));
            Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(0,0,v,0));
            Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(0,0,0,v));
            Assert.Throws<InvalidShapeException>(() => ctx.MakeRectangle(0, 0, 10, -10));

			//test some relateXRange
			//    opposite +/- 180
			Assert.Equal(SpatialRelation.INTERSECTS, ctx.MakeRectangle(170, 180, 0, 0).RelateXRange(-180, -170));
			Assert.Equal(SpatialRelation.INTERSECTS, ctx.MakeRectangle(-90, -45, 0, 0).RelateXRange(-45, -135));
			Assert.Equal(SpatialRelation.CONTAINS, ctx.GetWorldBounds().RelateXRange(-90, -135));
			//point on edge at dateline using opposite +/- 180
			Assert.Equal(SpatialRelation.CONTAINS, ctx.MakeRectangle(170, 180, 0, 0).Relate(ctx.MakePoint(-180, 0)));

			//test 180 becomes -180 for non-zero width rectangle
			Assert.Equal(ctx.MakeRectangle(-180, -170, 0, 0), ctx.MakeRectangle(180, -170, 0, 0));
			Assert.Equal(ctx.MakeRectangle(170, 180, 0, 0), ctx.MakeRectangle(170, -180, 0, 0));

            double[] lons = new double[] { 0, 45, 160, 180, -45, -175, -180 }; //minX
            foreach (double lon in lons)
            {
                double[] lonWs = new double[] { 0, 20, 180, 200, 355, 360 }; //width
                foreach (double lonW in lonWs)
                {
                    if (lonW == 360 && lon != -180)
                        continue;
                    TestRectangle(lon, lonW, 0, 0);
                    TestRectangle(lon, lonW, -10, 10);
                    TestRectangle(lon, lonW, 80, 10); //polar cap
                    TestRectangle(lon, lonW, -90, 180); //full lat range
                }
            }

            TestShapes2D.TestCircleReset(ctx);

            //Test geo rectangle intersections
            TestRectIntersect();

            //Test buffer
            AssertEquals(ctx.MakeRectangle(-10, 10, -10, 10), ctx.MakeRectangle(0, 0, 0, 0).GetBuffered(10, ctx));
            for (int i = 0; i < AtLeast(100); i++)
            {
                Rectangle r = RandomRectangle(1);
                int buf = random.Next(0, 90 +1);
                Rectangle br = (Rectangle)r.GetBuffered(buf, ctx);
                AssertRelation(null, SpatialRelation.CONTAINS, br, r);
                if (r.GetWidth() + 2 * buf >= 360)
                    CustomAssert.EqualWithDelta(360, br.GetWidth(), 0.0);
                else
                    Assert.True(br.GetWidth() - r.GetWidth() >= 2 * buf);
                //TODO test more thoroughly; we don't check that we over-buf
            }
            Assert.True(ctx.MakeRectangle(0, 10, 0, 89).GetBuffered(0.5, ctx).GetBoundingBox().GetWidth()
                > 11);
        }

		[Theory]
		[PropertyData("Contexts")]
        public virtual void TestGeoCircle(SpatialContext ctx)
		{
			base.ctx = ctx;

            Assert.Equal(string.Format("Circle(Pt(x={0:0.0},y={1:0.0}), d={2:0.0}° {3:0.00}km)", 10, 20, 30, 3335.85), ctx.MakeCircle(10, 20, 30).ToString());

            double v = 200 * (random.NextDouble() > 0.5 ? -1 : 1);
		    Assert.Throws<InvalidShapeException>(() => ctx.MakeCircle(v,0,5));
            Assert.Throws<InvalidShapeException>(() => ctx.MakeCircle(0, v, 5));
            //Assert.Throws<InvalidShapeException>(() => ctx.MakeCircle(random.Next(-180, 180), random.Next(-90, 90), v));

            //--Start with some static tests that once failed:

            //Bug: numeric edge at pole, fails to init
            ctx.MakeCircle(110, -12, 90 + 12);

            //Bug: horizXAxis not in enclosing rectangle, assertion
            ctx.MakeCircle(-44, 16, 106);
            ctx.MakeCircle(-36, -76, 14);
            ctx.MakeCircle(107, 82, 172);

            // TODO need to update this test to be valid
            //{
            //    //Bug in which distance was being confused as being in the same coordinate system as x,y.
            //    double distDeltaToPole = 0.001;//1m
            //    double distDeltaToPoleDEG = ctx.getDistCalc().distanceToDegrees(distDeltaToPole);
            //    double dist = 1;//1km
            //    double distDEG = ctx.getDistCalc().distanceToDegrees(dist);
            //    Circle c = ctx.makeCircle(0, 90 - distDeltaToPoleDEG - distDEG, dist);
            //    Rectangle cBBox = c.getBoundingBox();
            //    Rectangle r = ctx.makeRect(cBBox.getMaxX() * 0.99, cBBox.getMaxX() + 1, c.getCenter().getY(), c.getCenter().getY());
            //    assertEquals(INTERSECTS, c.getBoundingBox().relate(r, ctx));
            //    assertEquals("dist != xy space", INTERSECTS, c.relate(r, ctx));//once failed here
            //}

            AssertEquals("bad proportion logic", SpatialRelation.INTERSECTS, ctx.MakeCircle(64, -70, 18).Relate(ctx.MakeRectangle(46, 116, -86, -62)));

            AssertEquals("Both touch pole", SpatialRelation.INTERSECTS, ctx.MakeCircle(-90, 30, 60).Relate(ctx.MakeRectangle(-24, -16, 14, 90)));

            AssertEquals("Spherical cap should contain enclosed band", SpatialRelation.CONTAINS,
                ctx.MakeCircle(0, -90, 30).Relate(ctx.MakeRectangle(-180, 180, -90, -80)));

            AssertEquals("touches pole", SpatialRelation.INTERSECTS, ctx.MakeCircle(0, -88, 2).Relate(ctx.MakeRectangle(40, 60, -90, -86)));

            AssertEquals("wrong farthest opp corner", SpatialRelation.INTERSECTS, ctx.MakeCircle(92, 36, 46).Relate(ctx.MakeRectangle(134, 136, 32, 80)));

            AssertEquals("edge rounding issue 2", SpatialRelation.INTERSECTS, ctx.MakeCircle(84, -40, 136).Relate(ctx.MakeRectangle(-150, -80, 34, 84)));

            AssertEquals("edge rounding issue", SpatialRelation.CONTAINS, ctx.MakeCircle(0, 66, 156).Relate(ctx.MakePoint(0, -90)));

            AssertEquals("nudge back circle", SpatialRelation.CONTAINS, ctx.MakeCircle(-150, -90, 122).Relate(ctx.MakeRectangle(0, -132, 32, 32)));

            AssertEquals("wrong estimate", SpatialRelation.DISJOINT, ctx.MakeCircle(-166, 59, KmToDeg(5226.2)).Relate(ctx.MakeRectangle(36, 66, 23, 23)));

            AssertEquals("bad CONTAINS (dateline)", SpatialRelation.INTERSECTS, ctx.MakeCircle(56, -50, KmToDeg(12231.5)).Relate(ctx.MakeRectangle(108, 26, 39, 48)));

            AssertEquals("bad CONTAINS (backwrap2)", SpatialRelation.INTERSECTS,
                ctx.MakeCircle(112, -3, 91).Relate(ctx.MakeRectangle(-163, 29, -38, 10)));

            AssertEquals("bad CONTAINS (r x-wrap)", SpatialRelation.INTERSECTS,
                ctx.MakeCircle(-139, 47, 80).Relate(ctx.MakeRectangle(-180, 180, -3, 12)));

            AssertEquals("bad CONTAINS (pwrap)", SpatialRelation.INTERSECTS,
                ctx.MakeCircle(-139, 47, 80).Relate(ctx.MakeRectangle(-180, 179, -3, 12)));

            AssertEquals("no-dist 1", SpatialRelation.WITHIN,
                ctx.MakeCircle(135, 21, 0).Relate(ctx.MakeRectangle(-103, -154, -47, 52)));

            AssertEquals("bbox <= >= -90 bug", SpatialRelation.CONTAINS,
                ctx.MakeCircle(-64, -84, 124).Relate(ctx.MakeRectangle(-96, 96, -10, -10)));

            //The horizontal axis line of a geo circle doesn't necessarily pass through c's ctr.
            AssertEquals("c's horiz axis doesn't pass through ctr", SpatialRelation.INTERSECTS,
                ctx.MakeCircle(71, -44, 40).Relate(ctx.MakeRectangle(15, 27, -62, -34)));

            AssertEquals("pole boundary", SpatialRelation.INTERSECTS,
                ctx.MakeCircle(-100, -12, 102).Relate(ctx.MakeRectangle(143, 175, 4, 32)));

            AssertEquals("full circle assert", SpatialRelation.CONTAINS,
                ctx.MakeCircle(-64, 32, 180).Relate(ctx.MakeRectangle(47, 47, -14, 90)));

            //--Now proceed with systematic testing:
            AssertEquals(ctx.GetWorldBounds(), ctx.MakeCircle(0, 0, 180).GetBoundingBox());
            //assertEquals(ctx.makeCircle(0,0,distToOpposeSide/2 - 500).getBoundingBox());

            double[] theXs = new double[] { -180, -45, 90 };
            foreach (double x in theXs)
            {
                double[] theYs = new double[] { -90, -45, 0, 45, 90 };
                foreach (double y in theYs)
                {
                    TestCircle(x, y, 0);
                    TestCircle(x, y, KmToDeg(500));
                    TestCircle(x, y, 90);
                    TestCircle(x, y, 180);
                }
            }

            TestCircleIntersect();
        }
    }
}
