using System;
using System.Diagnostics;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using Xunit;

namespace Spatial4n.Tests.shape
{
    public abstract class AbstractTestShapes
    {
        protected Random random;

        protected SpatialContext ctx;
        private static double EPS = 10e-9;

        //This is the bit each concrete class needs to implement
        protected abstract SpatialContext GetContext();

        public void BeforeClass()
        {
            random = new Random(RandomSeed.Seed());
            ctx = GetContext();
        }

        protected void AssertRelation(String msg, SpatialRelation expected, Shape a, Shape b)
        {
            msg = a + " intersect " + b; //use different msg
            AssertIntersect(msg, expected, a, b);
            //check flipped a & b w/ transpose(), while we're at it
            AssertIntersect("(transposed) " + msg, expected.Transpose(), b, a);
        }

        private void AssertIntersect(String msg, SpatialRelation expected, Shape a, Shape b)
        {
            SpatialRelation sect = a.Relate(b, ctx);
            if (sect == expected)
                return;
            if (expected == SpatialRelation.WITHIN || expected == SpatialRelation.CONTAINS)
            {
                if (a.GetType().Equals(b.GetType())) // they are the same shape type
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
            Rectangle r = ctx.MakeRect(minX, minX + width, minY, minY + height);
            //test equals & hashcode of duplicate
            Rectangle r2 = ctx.MakeRect(minX, minX + width, minY, minY + height);
            Assert.Equal(r, r2);
            Assert.Equal(r.GetHashCode(), r2.GetHashCode());

            String msg = r.ToString();

            Assert.Equal( /*msg,*/ width != 0 && height != 0, r.HasArea());
            Assert.Equal( /*msg,*/ width != 0 && height != 0, r.GetArea() > 0);

            AssertEqualsRatio(msg, height, r.GetHeight());
            AssertEqualsRatio(msg, width, r.GetWidth());
            Point center = r.GetCenter();
            msg += " ctr:" + center;
            //System.out.println(msg);
            AssertRelation(msg, SpatialRelation.CONTAINS, r, center);

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

        protected void TestRectIntersect()
        {
            double INCR = 45;
            double Y = 10;
            for (double left = -180; left <= 180; left += INCR)
            {
                for (double right = left; right - left <= 360; right += INCR)
                {
                    Rectangle r = ctx.MakeRect(left, right, -Y, Y);

                    //test contains (which also tests within)
                    for (double left2 = left; left2 <= right; left2 += INCR)
                    {
                        for (double right2 = left2; right2 <= right; right2 += INCR)
                        {
                            Rectangle r2 = ctx.MakeRect(left2, right2, -Y, Y);
                            AssertRelation(null, SpatialRelation.CONTAINS, r, r2);
                        }
                    }
                    //test point contains
                    AssertRelation(null, SpatialRelation.CONTAINS, r, ctx.MakePoint(left, Y));

                    //test disjoint
                    for (double left2 = right + INCR; left2 - left < 360; left2 += INCR)
                    {
                        for (double right2 = left2; right2 - left < 360; right2 += INCR)
                        {
                            Rectangle r2 = ctx.MakeRect(left2, right2, -Y, Y);
                            AssertRelation(null, SpatialRelation.DISJOINT, r, r2);

                            //test point disjoint
                            AssertRelation(null, SpatialRelation.DISJOINT, r, ctx.MakePoint(left2, Y));
                        }
                    }
                    //test intersect
                    for (double left2 = left + INCR; left2 <= right; left2 += INCR)
                    {
                        for (double right2 = right + INCR; right2 - left < 360; right2 += INCR)
                        {
                            Rectangle r2 = ctx.MakeRect(left2, right2, -Y, Y);
                            AssertRelation(null, SpatialRelation.INTERSECTS, r, r2);
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
            Rectangle bbox = c.GetBoundingBox();
            Assert.Equal( /*msg,*/ dist > 0, bbox.GetArea() > 0);
            if (!ctx.IsGeo())
            {
                //if not geo then units of dist == units of x,y
                AssertEqualsRatio(msg, bbox.GetHeight(), dist*2);
                AssertEqualsRatio(msg, bbox.GetWidth(), dist*2);
            }
            AssertRelation(msg, SpatialRelation.CONTAINS, c, c.GetCenter());
            AssertRelation(msg, SpatialRelation.CONTAINS, bbox, c);
        }

        protected void TestCircleIntersect()
        {
            //Now do some randomized tests:
            int i_C = 0, i_I = 0, i_W = 0, i_O = 0; //counters for the different intersection cases
            int laps = 0;
            int MINLAPSPERCASE = 20;
            while (i_C < MINLAPSPERCASE || i_I < MINLAPSPERCASE || i_W < MINLAPSPERCASE || i_O < MINLAPSPERCASE)
            {
                laps++;
                double cX = RandRange(-180, 179);
                double cY = RandRange(-90, 90);
                double cR = RandRange(0, 180);
                double cR_dist = ctx.GetDistCalc().Distance(ctx.MakePoint(0, 0), 0, cR);
                Circle c = ctx.MakeCircle(cX, cY, cR_dist);

                double rX = RandRange(-180, 179);
                double rW = RandRange(0, 360);
                double rY1 = RandRange(-90, 90);
                double rY2 = RandRange(-90, 90);
                double rYmin = Math.Min(rY1, rY2);
                double rYmax = Math.Max(rY1, rY2);
                Rectangle r = ctx.MakeRect(rX, rX + rW, rYmin, rYmax);

                SpatialRelation ic = c.Relate(r, ctx);

                Point p;
                switch (ic)
                {
                    case SpatialRelation.CONTAINS:
                        i_C++;
                        p = RandomPointWithin(random, r, ctx);
                        Assert.Equal(SpatialRelation.CONTAINS, c.Relate(p, ctx));
                        break;
                    case SpatialRelation.INTERSECTS:
                        i_I++;
                        //hard to test anything here; instead we'll test it separately
                        break;
                    case SpatialRelation.WITHIN:
                        i_W++;
                        p = RandomPointWithin(random, c, ctx);
                        Assert.Equal(SpatialRelation.CONTAINS, r.Relate(p, ctx));
                        break;
                    case SpatialRelation.DISJOINT:
                        i_O++;
                        p = RandomPointWithin(random, r, ctx);
                        Assert.Equal(SpatialRelation.DISJOINT, c.Relate(p, ctx));
                        break;
                    default:
                        Debug.Fail("" + ic);
                        break;
                }
            }
            //System.out.println("Laps: "+laps);

            //TODO deliberately test INTERSECTS based on known intersection point
        }

        /** Returns a random integer between [start, end] with a limited number of possibilities instead of end-start+1. */
        private int RandRange(int start, int end)
        {
            //I tested this.
            double r = random.NextDouble();
            int BUCKETS = 91;
            int ir = (int) Math.Round(r*(BUCKETS - 1)); //put into buckets
            //TODO work out if the bracketing here is okat????
            int result = (int)((double)((end - start)*ir) / (double)(BUCKETS - 1) + start);
            Debug.Assert(result >= start && result <= end);
            return result;
        }

        private Point RandomPointWithin(Random random, Circle c, SpatialContext ctx)
        {
            double d = c.GetDistance()*random.NextDouble();
            double angleDEG = 360*random.NextDouble();
            Point p = ctx.GetDistCalc().PointOnBearing(c.GetCenter(), d, angleDEG, ctx);
            Assert.Equal(SpatialRelation.CONTAINS, c.Relate(p, ctx));
            return p;
        }

        private Point RandomPointWithin(Random random, Rectangle r, SpatialContext ctx)
        {
            double x = r.GetMinX() + random.NextDouble()*r.GetWidth();
            double y = r.GetMinY() + random.NextDouble()*r.GetHeight();
            Point p = ctx.MakePoint(x, y);
            Assert.Equal(SpatialRelation.CONTAINS, r.Relate(p, ctx));
            return p;
        }
    }
}
