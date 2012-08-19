using System;
using System.Collections.Generic;
using Spatial4n.Core.Context;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Distance;
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
				DistanceUnits units = DistanceUnits.KILOMETERS;

				DistanceCalculator distCalcH = new GeodesicSphereDistCalc.Haversine(units.EarthRadius());//default
				DistanceCalculator distCalcV = new GeodesicSphereDistCalc.Vincenty(units.EarthRadius());//default

				yield return new object[] { new SpatialContext(units, distCalcH, SpatialContext.GEO_WORLDBOUNDS) };
				yield return new object[] { new SpatialContext(units, distCalcV, SpatialContext.GEO_WORLDBOUNDS) };
				yield return new object[] { NtsSpatialContext.GEO_KM };
			}
		}

    	public TestShapesGeo()
    	{
    	}

		public TestShapesGeo(SpatialContext ctx) : base(ctx)
    	{
    	}

		[Theory]
		[PropertyData("Contexts")]
		public void TestGeoRectangle(SpatialContext ctx)
		{
			base.ctx = ctx;

			//First test some relateXRange
			//    opposite +/- 180
			Assert.Equal(SpatialRelation.INTERSECTS, ctx.MakeRect(170, 180, 0, 0).RelateXRange(-180, -170, ctx));
			Assert.Equal(SpatialRelation.INTERSECTS, ctx.MakeRect(-90, -45, 0, 0).RelateXRange(-45, -135, ctx));
			Assert.Equal(SpatialRelation.CONTAINS, ctx.GetWorldBounds().RelateXRange(-90, -135, ctx));
			//point on edge at dateline using opposite +/- 180
			Assert.Equal(SpatialRelation.CONTAINS, ctx.MakeRect(170, 180, 0, 0).Relate(ctx.MakePoint(-180, 0), ctx));

			//test 180 becomes -180 for non-zero width rectangle
			Assert.Equal(ctx.MakeRect(-180, -170, 0, 0), ctx.MakeRect(180, -170, 0, 0));
			Assert.Equal(ctx.MakeRect(170, 180, 0, 0), ctx.MakeRect(170, -180, 0, 0));

            double[] lons = new double[] { 0, 45, 160, 180, -45, -175, -180 }; //minX
            foreach (double lon in lons)
            {
                double[] lonWs = new double[] { 0, 20, 180, 200, 355, 360 }; //width
                foreach (double lonW in lonWs)
                {
                    TestRectangle(lon, lonW, 0, 0);
                    TestRectangle(lon, lonW, -10, 10);
                    TestRectangle(lon, lonW, 80, 10); //polar cap
                    TestRectangle(lon, lonW, -90, 180); //full lat range
                }
            }

            //Test geo rectangle intersections
            TestRectIntersect();
        }

		[Fact]
		public void foo()
		{
			ctx = NtsSpatialContext.GEO_KM;
			Assert.Equal(/*"nudge back circle", */ SpatialRelation.CONTAINS, ctx.MakeCircle(-150, -90, DegToDist(122)).Relate(ctx.MakeRect(0, -132, 32, 32), ctx));

			//var deg = DegToDist(180);
			//var circle = ctx.MakeCircle(-64, 32, deg);
			//var rect = ctx.MakeRect(47, 47, -14, 90);
			//var rel = circle.Relate(rect, ctx);
			//Assert.Equal(/*"full circle assert",*/ SpatialRelation.CONTAINS, rel);
		}

		[Theory]
		[PropertyData("Contexts")]
        public void TestGeoCircle(SpatialContext ctx)
		{
			base.ctx = ctx;

            //--Start with some static tests that once failed:

            //Bug: numeric edge at pole, fails to init
            ctx.MakeCircle(110, -12, ctx.GetDistCalc().DegreesToDistance(90 + 12));

            //Bug: horizXAxis not in enclosing rectangle, assertion
            ctx.MakeCircle(-44, 16, DegToDist(106));
            ctx.MakeCircle(-36, -76, DegToDist(14));
            ctx.MakeCircle(107, 82, DegToDist(172));

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

			Assert.Equal(/*"nudge back circle", */ SpatialRelation.CONTAINS, ctx.MakeCircle(-150, -90, DegToDist(122)).Relate(ctx.MakeRect(0, -132, 32, 32), ctx));

            Assert.Equal(/* "wrong estimate", */ SpatialRelation.DISJOINT, ctx.MakeCircle(-166, 59, 5226.2).Relate(ctx.MakeRect(36, 66, 23, 23), ctx));

            Assert.Equal(/*"bad CONTAINS (dateline)",*/ SpatialRelation.INTERSECTS, 
                ctx.MakeCircle(56, -50, 12231.5).Relate(ctx.MakeRect(108, 26, 39, 48), ctx));

            Assert.Equal(/*"bad CONTAINS (backwrap2)",*/ SpatialRelation.INTERSECTS,
                ctx.MakeCircle(112, -3, DegToDist(91)).Relate(ctx.MakeRect(-163, 29, -38, 10), ctx));

            Assert.Equal(/*"bad CONTAINS (r x-wrap)",*/ SpatialRelation.INTERSECTS,
                ctx.MakeCircle(-139, 47, DegToDist(80)).Relate(ctx.MakeRect(-180, 180, -3, 12), ctx));

            Assert.Equal(/*"bad CONTAINS (pwrap)",*/ SpatialRelation.INTERSECTS,
                ctx.MakeCircle(-139, 47, DegToDist(80)).Relate(ctx.MakeRect(-180, 179, -3, 12), ctx));

            Assert.Equal(/*"no-dist 1",*/ SpatialRelation.WITHIN,
                ctx.MakeCircle(135, 21, 0).Relate(ctx.MakeRect(-103, -154, -47, 52), ctx));

            Assert.Equal(/*"bbox <= >= -90 bug",*/ SpatialRelation.CONTAINS,
                ctx.MakeCircle(-64, -84, DegToDist(124)).Relate(ctx.MakeRect(-96, 96, -10, -10), ctx));

            //The horizontal axis line of a geo circle doesn't necessarily pass through c's ctr.
            Assert.Equal(/*"c's horiz axis doesn't pass through ctr",*/ SpatialRelation.INTERSECTS,
                ctx.MakeCircle(71, -44, DegToDist(40)).Relate(ctx.MakeRect(15, 27, -62, -34), ctx));

            Assert.Equal(/*"pole boundary",*/ SpatialRelation.INTERSECTS,
                ctx.MakeCircle(-100, -12, DegToDist(102)).Relate(ctx.MakeRect(143, 175, 4, 32), ctx));

            Assert.Equal(/*"full circle assert",*/ SpatialRelation.CONTAINS,
                ctx.MakeCircle(-64, 32, DegToDist(180)).Relate(ctx.MakeRect(47, 47, -14, 90), ctx));

            //--Now proceed with systematic testing:

            double distToOpposeSide = ctx.GetUnits().EarthRadius() * Math.PI;
            Assert.Equal(ctx.GetWorldBounds(), ctx.MakeCircle(0, 0, distToOpposeSide).GetBoundingBox());
            //assertEquals(ctx.makeCircle(0,0,distToOpposeSide/2 - 500).getBoundingBox());

            double[] theXs = new double[] { -180, -45, 90 };
            foreach (double x in theXs)
            {
                double[] theYs = new double[] { -90, -45, 0, 45, 90 };
                foreach (double y in theYs)
                {
                    TestCircle(x, y, 0);
                    TestCircle(x, y, 500);
                    TestCircle(x, y, DegToDist(90));
                    TestCircle(x, y, ctx.GetUnits().EarthRadius() * 6);
                }
            }

            TestCircleIntersect();
        }

        private double DegToDist(int deg)
        {
            return ctx.GetDistCalc().DegreesToDistance(deg);
        }
    }
}
