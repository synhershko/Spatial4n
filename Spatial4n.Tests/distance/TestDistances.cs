using System;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using Xunit;

namespace Spatial4n.Tests.distance
{
	public class TestDistances
	{
		private readonly Random random = new Random(RandomSeed.Seed());

		//NOTE!  These are sometimes modified by tests.
		private SpatialContext ctx = SpatialContext.GEO;
		private const double EPS = 10e-4; //delta when doing double assertions. Geo eps is not that small.;

		private DistanceCalculator dc() { return ctx.GetDistCalc(); }
		private Point pLL(double lat, double lon) { return ctx.MakePoint(lon, lat); }

		[Fact]
		public void testSomeDistances()
		{
			//See to verify: from http://www.movable-type.co.uk/scripts/latlong.html
			Point ctr = pLL(0, 100);
			CustomAssert.EqualWithDelta(11100, degToKm(dc().Distance(ctr, pLL(10, 0))), 3);
			CustomAssert.EqualWithDelta(11100, degToKm(dc().Distance(ctr, pLL(10, -160))), 3);

			CustomAssert.EqualWithDelta(314.40338, degToKm(dc().Distance(pLL(1, 2), pLL(3, 4))), EPS);

			CustomAssert.EqualWithDelta(11100, dc().Distance(pLL(0, 100), pLL(10, 0)), 3.0);     // we get 11102.445304151641
			CustomAssert.EqualWithDelta(11100, dc().Distance(pLL(0, 100), pLL(10, -160)), 3.0);  // we get 11102.445304151641

			Assert.Equal(314.4, dc().Distance(pLL(1, 2), pLL(3, 4)), precision: 1);   //314.4
			Assert.Equal(7506, dc().Distance(pLL(5, 70), pLL(10, 2)), precision: 0);  //7506
			Assert.Equal(6666, dc().Distance(pLL(5, 70), pLL(2, 10)), precision: 0);  //6666
			Assert.Equal(6675, dc().Distance(pLL(70, 5), pLL(10, 2)), precision: 0);  //6675

			Assert.Equal(841.4, dc().Distance(pLL(0.5, 1.2), pLL(7.8, 3.2)), precision: 1);
			Assert.Equal(841.4, dc().Distance(pLL(7.8, 3.2), pLL(0.5, 1.2)), precision: 1);

			Assert.Equal(67.10, dc().Distance(pLL(6.4, 7.9), pLL(5.8, 7.965)), precision: 2);
			Assert.Equal(67.10, dc().Distance(pLL(6.4, 7.9), pLL(7.0, 7.965)), precision: 2);

			CustomAssert.EqualWithDelta(314.40338, dc().Distance(pLL(1, 2), pLL(3, 4)), EPS);  //calculated value 314.40338388409333
		}

		[Fact]
		public void testCalcBoxByDistFromPt()
		{
			//first test regression
			{
				double d = degToKm(6894.1);
				Point pCtr = pLL(-20, 84);
				Point pTgt = pLL(-42, 15);
				Assert.True(dc().Distance(pCtr, pTgt) < d);
				//since the pairwise distance is less than d, a bounding box from ctr with d should contain pTgt.
				Rectangle r = dc().CalcBoxByDistFromPt(pCtr, d, ctx);
				Assert.Equal(SpatialRelation.CONTAINS, r.Relate(pTgt, ctx));
				CheckBBox(pCtr, d);
			}

			Assert.Equal(-45, dc().CalcBoxByDistFromPt_yHorizAxisDEG(ctx.MakePoint(-180, -45), 0, ctx), 0);

			double MAXDIST = degToKm(180);
			CheckBBox(ctx.MakePoint(0, 0), MAXDIST);
			CheckBBox(ctx.MakePoint(0, 0), MAXDIST * 0.999999);
			CheckBBox(ctx.MakePoint(0, 0), 0);
			CheckBBox(ctx.MakePoint(0, 0), 0.000001);
			CheckBBox(ctx.MakePoint(0, 90), 0.000001);
			CheckBBox(ctx.MakePoint(-32.7, -5.42), 9829);
			CheckBBox(ctx.MakePoint(0, 90 - 20), degToKm(20));
			{
				double d = 0.010;//10m
				CheckBBox(ctx.MakePoint(0, 90 - degToKm(d + 0.001)), d);
			}

			for (int T = 0; T < 100; T++)
			{
				double lat = -90 + random.NextDouble() * 180;
				double lon = -180 + random.NextDouble() * 360;
				Point ctr = ctx.MakePoint(lon, lat);
				double dist = MAXDIST * random.NextDouble();
				CheckBBox(ctr, dist);
			}
		}

		private void CheckBBox(Point ctr, double distKm)
		{
			String msg = "ctr: "+ctr+" distKm: "+distKm;
			double dist = kmToDeg(distKm);

			Rectangle r = dc().CalcBoxByDistFromPt(ctr, dist, ctx);
			double horizAxisLat = dc().CalcBoxByDistFromPt_yHorizAxisDEG(ctr, dist, ctx);
			if (!Double.IsNaN(horizAxisLat))
				Assert.True(r.RelateYRange(horizAxisLat, horizAxisLat, ctx).Intersects());

			//horizontal
			if (r.GetWidth() >= 180)
			{
				double calcDistKm = degToKm(dc().Distance(ctr, r.GetMinX(), r.GetMaxY() == 90 ? 90 : -90));
				Assert.True(calcDistKm <= distKm + EPS, msg);
				//horizAxisLat is meaningless in this context
			}
			else
			{
				Point tPt = FindClosestPointOnVertToPoint(r.GetMinX(), r.GetMinY(), r.GetMaxY(), ctr);
				double calcDistKm = degToKm(dc().Distance(ctr, tPt));
				CustomAssert.EqualWithDelta(/*msg,*/ distKm, calcDistKm, EPS);
				CustomAssert.EqualWithDelta(/*msg,*/ tPt.GetY(), horizAxisLat, EPS);
			}

			//vertical
			double topDistKm = degToKm(dc().Distance(ctr, ctr.GetX(), r.GetMaxY()));
			if (r.GetMaxY() == 90)
				Assert.True(topDistKm <= distKm + EPS, msg);
			else
				CustomAssert.EqualWithDelta(dist, topDistKm, EPS);

			double botDistKm = degToKm(dc().Distance(ctr, ctr.GetX(), r.GetMinY()));
			if (r.GetMinY() == -90)
				Assert.True(botDistKm <= distKm + EPS, msg);
			else
				CustomAssert.EqualWithDelta(/*msg,*/ distKm, botDistKm, EPS);
		}

		private Point FindClosestPointOnVertToPoint(double lon, double lowLat, double highLat, Point ctr)
		{
			//A binary search algorithm to find the point along the vertical lon between lowLat & highLat that is closest
			// to ctr, and returns the distance.
			double midLat = (highLat - lowLat) / 2 + lowLat;
			double midLatDist = ctx.GetDistCalc().Distance(ctr, lon, midLat);
			for (int L = 0; L < 100 && (highLat - lowLat > 0.001 || L < 20); L++)
			{
				bool bottom = (midLat - lowLat > highLat - midLat);
				double newMid = bottom ? (midLat - lowLat) / 2 + lowLat : (highLat - midLat) / 2 + midLat;
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

			double maxDistKm = degToKm(180);
			for (int i = 0; i < 1000; i++)
			{
				int dist = random.Next((int)maxDistKm);
				var EPS = (dist < maxDistKm * 0.75 ? 10e-6 : 10e-3);
				TestDistCalcPointOnBearing(dist, EPS);
			}
		}

		private void TestDistCalcPointOnBearing(double dist, double EPS)
		{
			for (int angDEG = 0; angDEG < 360; angDEG += random.Next(1,20))
			{
				Point c = ctx.MakePoint(random.Next(359),random.Next(-90,90));

				//0 distance means same point
				Point p2 = dc().PointOnBearing(c, 0, angDEG, ctx);
				Assert.Equal(c, p2);

				p2 = dc().PointOnBearing(c, dist, angDEG, ctx);
				double calcDist = dc().Distance(c, p2);
				AssertEqualsRatio(dist, calcDist, EPS);
			}
		}

		private static void AssertEqualsRatio(double expected, double actual, double EPS)
		{
			double delta = Math.Abs(actual - expected);
			double baseValue = Math.Min(actual, expected);
			double deltaRatio = baseValue == 0 ? delta : Math.Min(delta, delta / baseValue);
			CustomAssert.EqualWithDelta(0, deltaRatio, EPS);
		}

		[Fact]
		public void TestNormLat()
		{
			var lats = new double[][] 
			{
				new double[] {1.23,1.23},//1.23 might become 1.2299999 after some math and we want to ensure that doesn't happen
				new double[] {-90,-90}, 
				new double[] {90,90}, 
				new double[] {0,0}, 
				new double[] {-100,-80},
				new double[] {-90-180,90},
				new double[] {-90-360,-90},
				new double[] {90+180,-90},
				new double[] {90+360,90},
				new double[] {-12+180,12}
			};
			foreach (var pair in lats)
			{
				//Assert.Equal(/* "input "+pair[0],*/ pair[1], ctx.NormY(pair[0]), precision: 0);
				CustomAssert.EqualWithDelta(/* "input "+pair[0],*/ pair[1], ctx.NormY(pair[0]), Double.Epsilon);
			}
			var random = new Random(RandomSeed.Seed());
			for (int i = -1000; i < 1000; i += random.Next(9) * 10)
			{
				double d = ctx.NormY(i);
				Assert.True(/*i + " " + d,*/ d >= -90 && d <= 90);
			}
		}

		[Fact]
		public void TestNormLon()
		{
			var lons = new double[][]
			{
				new double[] {1.23, 1.23}, //1.23 might become 1.2299999 after some math and we want to ensure that doesn't happen
				new double[] {-180, -180},
				new double[] {180, +180}, 
				new double[] {0, 0}, 
				new double[] {-190, 170},
				new double[] {181,-179},
				new double[] {-180 - 360, -180}, 
				new double[] {-180 - 720, -180}, 
				new double[] {180 + 360, +180},
				new double[] {180 + 720, +180}
			};
			foreach (var pair in lons)
			{
				//Assert.Equal( /*"input "+pair[0],*/ pair[1], ctx.NormX(pair[0]), 0);
				CustomAssert.EqualWithDelta( /*"input "+pair[0],*/ pair[1], ctx.NormX(pair[0]), Double.Epsilon);
			}

			var random = new Random(RandomSeed.Seed());
			for (int i = -1000; i < 1000; i += random.Next(9) * 10)
			{
				double d = ctx.NormX(i);
				Assert.True(d >= -180 && d <= 180, i + " " + d);
			}
		}

		[Fact]
		public void TestDistToRadians()
		{
			AssertDistToRadians(0);
			AssertDistToRadians(500);
			AssertDistToRadians(ctx.GetUnits().EarthRadius());
		}

		private void AssertDistToRadians(double dist)
		{
			double radius = ctx.GetUnits().EarthRadius();
			CustomAssert.EqualWithDelta(
				DistanceUtils.PointOnBearingRAD(0, 0, DistanceUtils.Dist2Radians(dist, radius), DistanceUtils.DEG_90_AS_RADS, null)[1],
				DistanceUtils.Dist2Radians(dist, radius), 10e-5);
		}

		[Fact]
		public void testArea()
		{
			//surface of a sphere is 4 * pi * r^2
			double earthArea = 4 * Math.PI * ctx.GetUnits().EarthRadius() * ctx.GetUnits().EarthRadius();

			var random = new Random(RandomSeed.Seed());
			Circle c = ctx.MakeCircle(random.Next(-180, 180), random.Next(-90, 90),
					ctx.GetDistCalc().DegreesToDistance(180));//180 means whole earth
			CustomAssert.EqualWithDelta(earthArea, c.GetArea(ctx), 1.0);

			//now check half earth
			Circle cHalf = ctx.MakeCircle(c.GetCenter(), ctx.GetDistCalc().DegreesToDistance(90));
			CustomAssert.EqualWithDelta(earthArea / 2, cHalf.GetArea(ctx), 1.0);

			//picked out of the blue
			Circle c2 = ctx.MakeCircle(c.GetCenter(), ctx.GetDistCalc().DegreesToDistance(30));
			CustomAssert.EqualWithDelta(3.416E7, c2.GetArea(ctx), 3.416E7 * 0.01);

			//circle with same radius at +20 lat with one at -20 lat should have same area as well as bbox with same area
			Circle c3 = ctx.MakeCircle(c.GetCenter().GetX(), 20, ctx.GetDistCalc().DegreesToDistance(30));
			CustomAssert.EqualWithDelta(c2.GetArea(ctx), c3.GetArea(ctx), 0.01);
			Circle c3Opposite = ctx.MakeCircle(c.GetCenter().GetX(), -20, ctx.GetDistCalc().DegreesToDistance(30));
			CustomAssert.EqualWithDelta(c3.GetArea(ctx), c3Opposite.GetArea(ctx), 0.01);
			CustomAssert.EqualWithDelta(c3.GetBoundingBox().GetArea(ctx), c3Opposite.GetBoundingBox().GetArea(ctx), 0.01);

			CustomAssert.EqualWithDelta(earthArea, ctx.GetWorldBounds().GetArea(ctx), 1.0);
		}

		private static double degToKm(double deg)
		{
			return DistanceUtils.ToRadians(deg) * DistanceUtils.EARTH_MEAN_RADIUS_KM;
		}

		private static double kmToDeg(double km)
		{
			return DistanceUtils.ToDegrees(km / DistanceUtils.EARTH_MEAN_RADIUS_KM);
		}
	}
}

