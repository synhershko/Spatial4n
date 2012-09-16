using System;
using System.Collections.Generic;
using System.Linq;
using Spatial4n.Core.Context;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using Xunit;
using Xunit.Extensions;

namespace Spatial4n.Tests.shape
{
	public abstract class AbstractTestShapes
	{
		protected SpatialContext ctx;
		protected Random random;

		private const double EPS = 10e-9;

		protected AbstractTestShapes()
		{
			random = new Random(RandomSeed.Seed());
		}

		protected AbstractTestShapes(SpatialContext ctx)
		{
			random = new Random(RandomSeed.Seed());
			this.ctx = ctx;
		}

        protected void assertEquals<T>(string msg, T obj1, T obj2)
        {
            Assert.Equal(obj1, obj2);
        }

        protected void assertEquals<T>(T obj1, T obj2)
        {
            Assert.Equal(obj1, obj2);
        }

        //These few norm methods normalize the arguments for creating a shape to
        // account for the dateline. Some tests loop past the dateline or have offsets
        // that go past it and it's easier to have them coded that way and correct for
        // it here.  These norm methods should be used when needed, not frivolously.

        protected double normX(double x)
        {
            return ctx.IsGeo() ? DistanceUtils.NormLonDEG(x) : x;
        }
        protected double normY(double y)
        {
            return ctx.IsGeo() ? DistanceUtils.NormLatDEG(y) : y;
        }

        protected Rectangle makeNormRect(double minX, double maxX, double minY, double maxY)
        {
            if (ctx.IsGeo())
            {
                if (Math.Abs(maxX - minX) >= 360)
                {
                    minX = -180;
                    maxX = 180;
                }
                else
                {
                    minX = DistanceUtils.NormLonDEG(minX);
                    maxX = DistanceUtils.NormLonDEG(maxX);
                }
            }
            return ctx.MakeRectangle(minX, maxX, minY, maxY);
        }

		protected void assertRelation(String msg, SpatialRelation expected, Shape a, Shape b)
		{
			msg = a + " intersect " + b; //use different msg
			AssertIntersect(msg, expected, a, b);
			//check flipped a & b w/ transpose(), while we're at it
			AssertIntersect("(transposed) " + msg, expected.Transpose(), b, a);
		}

		private void AssertIntersect(String msg, SpatialRelation expected, Shape a, Shape b)
		{
			SpatialRelation sect = a.Relate(b);
			if (sect == expected)
				return;
			if (expected == SpatialRelation.WITHIN || expected == SpatialRelation.CONTAINS)
			{
				if (a.GetType() == b.GetType()) // they are the same shape type
					Assert.Equal(/*msg,*/ a, b);
				else
				{
					//they are effectively points or lines that are the same location
					Assert.True(!a.HasArea(), msg);
					Assert.True(!b.HasArea(), msg);

					Rectangle aBBox = a.GetBoundingBox();
					Rectangle bBBox = b.GetBoundingBox();
					if (aBBox.GetHeight() == 0 && bBBox.GetHeight() == 0
						&& (aBBox.GetMaxY() == 90 && bBBox.GetMaxY() == 90
							|| aBBox.GetMinY() == -90 && bBBox.GetMinY() == -90))
					{
						; //== a point at the pole
					}
					else
					{
						Assert.Equal( /*msg,*/ aBBox, bBBox);
					}
				}
			}
			else
			{
				Assert.Equal(/*msg,*/ expected, sect);
			}
		}

		private void AssertEqualsRatio(String msg, double expected, double actual)
		{
			double delta = Math.Abs(actual - expected);
			double baseValue = Math.Min(actual, expected);
			double deltaRatio = (baseValue == 0) ? delta : Math.Min(delta, delta / baseValue);
			CustomAssert.EqualWithDelta(/*msg,*/ 0, deltaRatio, EPS);
		}

		protected void TestRectangle(double minX, double width, double minY, double height)
		{
            double maxX = minX + width;
            double maxY = minY + height;
            minX = normX(minX);
            maxX = normX(maxX);

            Rectangle r = ctx.MakeRectangle(minX, maxX, minY, maxY);

			//test equals & hashcode of duplicate
            Rectangle r2 = ctx.MakeRectangle(minX, maxX, minY, maxY);
			Assert.Equal(r, r2);
			Assert.Equal(r.GetHashCode(), r2.GetHashCode());

			String msg = r.ToString();

			Assert.Equal( /*msg,*/ width != 0 && height != 0, r.HasArea());
			Assert.Equal( /*msg,*/ width != 0 && height != 0, r.GetArea(ctx) > 0);
			if (ctx.IsGeo() && r.GetWidth() == 360 && r.GetHeight() == 180)
			{
				//whole globe
                double earthRadius = DistanceUtils.ToDegrees(1);
				CustomAssert.EqualWithDelta(4 * Math.PI * earthRadius * earthRadius, r.GetArea(ctx), 1.0);//1km err
			}

			AssertEqualsRatio(msg, height, r.GetHeight());
			AssertEqualsRatio(msg, width, r.GetWidth());
			Point center = r.GetCenter();
			msg += " ctr:" + center;
			//System.out.println(msg);
			assertRelation(msg, SpatialRelation.CONTAINS, r, center);

			DistanceCalculator dc = ctx.GetDistCalc();
			double dUR = dc.Distance(center, r.GetMaxX(), r.GetMaxY());
			double dLR = dc.Distance(center, r.GetMaxX(), r.GetMinY());
			double dUL = dc.Distance(center, r.GetMinX(), r.GetMaxY());
			double dLL = dc.Distance(center, r.GetMinX(), r.GetMinY());

			Assert.Equal( /*msg,*/ width != 0 || height != 0, dUR != 0);
			if (dUR != 0)
				Assert.True(dUR > 0 && dLL > 0);
			AssertEqualsRatio(msg, dUR, dUL);
			AssertEqualsRatio(msg, dLR, dLL);
			if (!ctx.IsGeo() || center.GetY() == 0)
				AssertEqualsRatio(msg, dUR, dLL);
		}

        protected void testRectIntersect()
        {
            //This test loops past the dateline for some variables but the makeNormRect()
            // method ensures the rect is valid.
            const double INCR = 45;
            const double Y = 20;
            for (double left = -180; left < 180; left += INCR)
            {
                for (double right = left; right - left <= 360; right += INCR)
                {
                    Rectangle r = makeNormRect(left, right, -Y, Y);

                    //test contains (which also tests within)
                    for (double left2 = left; left2 <= right; left2 += INCR)
                    {
                        for (double right2 = left2; right2 <= right; right2 += INCR)
                        {
                            Rectangle r2 = makeNormRect(left2, right2, -Y, Y);
                            assertRelation(null, SpatialRelation.CONTAINS, r, r2);

                            //test point contains
                            assertRelation(null, SpatialRelation.CONTAINS, r, r2.GetCenter());
                        }
                    }

                    //test disjoint
                    for (double left2 = right + INCR; left2 - left < 360; left2 += INCR)
                    {
                        //test point disjoint
                        assertRelation(null, SpatialRelation.DISJOINT, r, ctx.MakePoint(
                            normX(left2), random.Next(-90, 90)));

                        for (double right2 = left2; right2 - left < 360; right2 += INCR)
                        {
                            Rectangle r2 = makeNormRect(left2, right2, -Y, Y);
                            assertRelation(null, SpatialRelation.DISJOINT, r, r2);
                        }
                    }
                    //test intersect
                    for (double left2 = left + INCR; left2 <= right; left2 += INCR)
                    {
                        for (double right2 = right + INCR; right2 - left < 360; right2 += INCR)
                        {
                            Rectangle r2 = makeNormRect(left2, right2, -Y, Y);
                            assertRelation(null, SpatialRelation.INTERSECTS, r, r2);
                        }
                    }

                }
            }
        }

	    protected void TestCircle(double x, double y, double dist)
		{
			Circle c = ctx.MakeCircle(x, y, dist);
			String msg = c.ToString();
			Circle c2 = ctx.MakeCircle(ctx.MakePoint(x, y), dist);
			Assert.Equal(c, c2);
			Assert.Equal(c.GetHashCode(), c2.GetHashCode());

			Assert.Equal( /*msg,*/ dist > 0, c.HasArea());
			double area = c.GetArea(ctx);
			Assert.True(/*msg,*/ c.HasArea() == (area > 0.0));
			Rectangle bbox = c.GetBoundingBox();
			Assert.Equal( /*msg,*/ dist > 0, bbox.GetArea(ctx) > 0);
			Assert.True(area <= bbox.GetArea(ctx));
			if (!ctx.IsGeo())
			{
				//if not geo then units of dist == units of x,y
				AssertEqualsRatio(msg, bbox.GetHeight(), dist * 2);
				AssertEqualsRatio(msg, bbox.GetWidth(), dist * 2);
			}
			assertRelation(msg, SpatialRelation.CONTAINS, c, c.GetCenter());
			assertRelation(msg, SpatialRelation.CONTAINS, bbox, c);
		}

		protected void TestCircleIntersect()
		{
			//Now do some randomized tests:
			int i_C = 0, i_I = 0, i_W = 0, i_O = 0; //counters for the different intersection cases
			int laps = 0;
			int MINLAPSPERCASE = 20;// *(int)multiplier();
			int TEST_DIVISIBLE = 2;//just use even numbers in this test
			while (i_C < MINLAPSPERCASE || i_I < MINLAPSPERCASE || i_W < MINLAPSPERCASE || i_O < MINLAPSPERCASE)
			{
				laps++;
				double cX = RandomIntBetweenDivisible(-180, 179, TEST_DIVISIBLE);
				double cY = RandomIntBetweenDivisible(-90, 90, TEST_DIVISIBLE);
				double cR = RandomIntBetweenDivisible(0, 180, TEST_DIVISIBLE);
				double cR_dist = ctx.GetDistCalc().Distance(ctx.MakePoint(0, 0), 0, cR);
				Circle c = ctx.MakeCircle(cX, cY, cR_dist);

				Rectangle r = RandomRectangle(TEST_DIVISIBLE);

				SpatialRelation ic = c.Relate(r);

				Point p;
				switch (ic)
				{
					case SpatialRelation.CONTAINS:
						i_C++;
						p = RandomPointWithin(r);
						Assert.Equal(SpatialRelation.CONTAINS, c.Relate(p));
						break;
					case SpatialRelation.INTERSECTS:
						i_I++;
						//hard to test anything here; instead we'll test it separately
						break;
					case SpatialRelation.WITHIN:
						i_W++;
						p = RandomPointWithin(c);
						Assert.Equal(SpatialRelation.CONTAINS, r.Relate(p));
						break;
					case SpatialRelation.DISJOINT:
						i_O++;
						p = RandomPointWithin(r);
						Assert.Equal(SpatialRelation.DISJOINT, c.Relate(p));
						break;
					default:
						Assert.True(false, "" + ic);
						break;
				}
			}
			//System.out.println("Laps: "+laps);

			//TODO deliberately test INTERSECTS based on known intersection point
		}

		protected Rectangle RandomRectangle(int divisible)
		{
			double rX = RandomIntBetweenDivisible(-180, 180, divisible);
			double rW = RandomIntBetweenDivisible(0, 360, divisible);
			double rY1 = RandomIntBetweenDivisible(-90, 90, divisible);
			double rY2 = RandomIntBetweenDivisible(-90, 90, divisible);
			double rYmin = Math.Min(rY1, rY2);
			double rYmax = Math.Max(rY1, rY2);
			if (rW > 0 && rX == 180)
				rX = -180;
			return makeNormRect(rX, rX + rW, rYmin, rYmax);
		}

		[Theory]
		[PropertyData("Contexts")]
		public void testMakeRect(SpatialContext ctx)
		{
			this.ctx = ctx;

			//test rectangle constructor
			Assert.Equal(new RectangleImpl(1, 3, 2, 4, ctx),
				new RectangleImpl(new PointImpl(1, 2, ctx), new PointImpl(3, 4, ctx), ctx));

			//test ctx.makeRect
			Assert.Equal(ctx.MakeRectangle(1, 3, 2, 4),
				ctx.MakeRectangle(ctx.MakePoint(1, 2), ctx.MakePoint(3, 4)));
		}

		[RepeatTheory(20)]
		[PropertyData("Contexts")]
		public void testMultiShape(SpatialContext ctx)
		{
			this.ctx = ctx;

			if(ctx.IsGeo()) return;//TODO not yet supported!

			//come up with some random shapes
			int NUM_SHAPES = random.Next(1, 5);
			var shapes = new List<Rectangle>(NUM_SHAPES);
			while (shapes.Count < NUM_SHAPES)
			{
				shapes.Add(RandomRectangle(20));
			}
			var multiShape = new MultiShape(shapes.Cast<Shape>(), ctx);

			//test multiShape.getBoundingBox();
			Rectangle msBbox = multiShape.GetBoundingBox();
			if (shapes.Count == 1)
			{
				Assert.Equal(shapes[0], msBbox.GetBoundingBox());
			}
			else
			{
				foreach (Rectangle shape in shapes)
				{
					assertRelation("bbox contains shape", SpatialRelation.CONTAINS, msBbox, shape);
				}
			}

			//TODO test multiShape.relate()
		}

		/** Returns a random integer between [start, end]. Integers between must be divisible by the 3rd argument. */
		private int RandomIntBetweenDivisible(int start, int end, int divisible)
		{
			// DWS: I tested this
			int divisStart = (int)Math.Ceiling((start + 1) / (double)divisible);
			int divisEnd = (int)Math.Floor((end - 1) / (double)divisible);
			int divisRange = Math.Max(0, divisEnd - divisStart + 1);
			int r = random.Next(1 + divisRange);//remember that '0' is counted
			if (r == 0)
				return start;
			if (r == 1)
				return end;
			return (r - 2 + divisStart) * divisible;
		}

		private Point RandomPointWithin(Circle c)
		{
			double d = c.GetRadius() * random.NextDouble();
			double angleDEG = 360 * random.NextDouble();
			Point p = ctx.GetDistCalc().PointOnBearing(c.GetCenter(), d, angleDEG, ctx, null);
			Assert.Equal(SpatialRelation.CONTAINS, c.Relate(p));
			return p;
		}

		private Point RandomPointWithin(Rectangle r)
		{
			double x = r.GetMinX() + random.NextDouble() * r.GetWidth();
			double y = r.GetMinY() + random.NextDouble() * r.GetHeight();
            x = normX(x);
            y = normY(y);
			Point p = ctx.MakePoint(x, y);
			Assert.Equal(SpatialRelation.CONTAINS, r.Relate(p));
			return p;
		}
	}
}
