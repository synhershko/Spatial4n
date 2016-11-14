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
    public abstract class AbstractTestShapes : RandomizedShapeTest
    {
        protected AbstractTestShapes()
        {
        }

        protected AbstractTestShapes(SpatialContext ctx)
            : base(ctx)
        {
        }

        //public readonly TestLog testLog = TestLog.instance;

        protected virtual void AssertEquals<T>(string msg, T obj1, T obj2)
        {
            Assert.Equal(obj1, obj2);
        }

        protected virtual void AssertEquals<T>(T obj1, T obj2)
        {
            Assert.Equal(obj1, obj2);
        }

        protected virtual void TestRectangle(double minX, double width, double minY, double height)
        {
            double maxX = minX + width;
            double maxY = minY + height;
            minX = NormX(minX);
            maxX = NormX(maxX);

            IRectangle r = ctx.MakeRectangle(minX, maxX, minY, maxY);

            //test equals & hashcode of duplicate
            IRectangle r2 = ctx.MakeRectangle(minX, maxX, minY, maxY);
            Assert.Equal(r, r2);
            Assert.Equal(r.GetHashCode(), r2.GetHashCode());

            string msg = r.ToString();

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
            IPoint center = r.GetCenter();
            msg += " ctr:" + center;
            //System.out.println(msg);
            AssertRelation(msg, SpatialRelation.CONTAINS, r, center);

            IDistanceCalculator dc = ctx.GetDistCalc();
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

        protected virtual void TestRectIntersect()
        {
            //This test loops past the dateline for some variables but the makeNormRect()
            // method ensures the rect is valid.
            const double INCR = 45;
            const double Y = 20;
            for (double left = -180; left < 180; left += INCR)
            {
                for (double right = left; right - left <= 360; right += INCR)
                {
                    IRectangle r = MakeNormRect(left, right, -Y, Y);

                    //test contains (which also tests within)
                    for (double left2 = left; left2 <= right; left2 += INCR)
                    {
                        for (double right2 = left2; right2 <= right; right2 += INCR)
                        {
                            IRectangle r2 = MakeNormRect(left2, right2, -Y, Y);
                            AssertRelation(null, SpatialRelation.CONTAINS, r, r2);

                            //test point contains
                            AssertRelation(null, SpatialRelation.CONTAINS, r, r2.GetCenter());
                        }
                    }

                    //test disjoint
                    for (double left2 = right + INCR; left2 - left < 360; left2 += INCR)
                    {
                        //test point disjoint
                        AssertRelation(null, SpatialRelation.DISJOINT, r, ctx.MakePoint(
                            NormX(left2), random.Next(-90, 90)));

                        for (double right2 = left2; right2 - left < 360; right2 += INCR)
                        {
                            IRectangle r2 = MakeNormRect(left2, right2, -Y, Y);
                            AssertRelation(null, SpatialRelation.DISJOINT, r, r2);
                        }
                    }
                    //test intersect
                    for (double left2 = left + INCR; left2 <= right; left2 += INCR)
                    {
                        for (double right2 = right + INCR; right2 - left < 360; right2 += INCR)
                        {
                            IRectangle r2 = MakeNormRect(left2, right2, -Y, Y);
                            AssertRelation(null, SpatialRelation.INTERSECTS, r, r2);
                        }
                    }

                }
            }
        }

        protected virtual void TestCircle(double x, double y, double dist)
        {
            ICircle c = ctx.MakeCircle(x, y, dist);
            String msg = c.ToString();
            ICircle c2 = ctx.MakeCircle(ctx.MakePoint(x, y), dist);
            Assert.Equal(c, c2);
            Assert.Equal(c.GetHashCode(), c2.GetHashCode());

            Assert.Equal( /*msg,*/ dist > 0, c.HasArea());
            double area = c.GetArea(ctx);
            Assert.True(/*msg,*/ c.HasArea() == (area > 0.0));
            IRectangle bbox = c.GetBoundingBox();
            Assert.Equal( /*msg,*/ dist > 0, bbox.GetArea(ctx) > 0);
            Assert.True(area <= bbox.GetArea(ctx));
            if (!ctx.IsGeo())
            {
                //if not geo then units of dist == units of x,y
                AssertEqualsRatio(msg, bbox.GetHeight(), dist * 2);
                AssertEqualsRatio(msg, bbox.GetWidth(), dist * 2);
            }
            AssertRelation(msg, SpatialRelation.CONTAINS, c, c.GetCenter());
            AssertRelation(msg, SpatialRelation.CONTAINS, bbox, c);
        }

        private class RectIntersectionAnonymousHelper : RectIntersectionTestHelper
        {
            public RectIntersectionAnonymousHelper(SpatialContext ctx)
                : base(ctx)
            { }

            protected override IShape GenerateRandomShape(IPoint nearP)
            {
                double cX = RandomIntBetweenDivisible(-180, 179);
                double cY = RandomIntBetweenDivisible(-90, 90);
                double cR_dist = RandomIntBetweenDivisible(0, 180);
                return ctx.MakeCircle(cX, cY, cR_dist);
            }

            protected override IPoint RandomPointInEmptyShape(IShape shape)
            {
                return shape.GetCenter();
            }

            protected override void OnAssertFail(/*AssertionError*/Exception e, /*Circle*/IShape shape, IRectangle r, SpatialRelation ic)
            {
                ICircle s = shape as ICircle;
                //Check if the circle's edge appears to coincide with the shape.
                double radius = s.GetRadius();
                if (radius == 180)
                    throw e;//if this happens, then probably a bug
                if (radius == 0)
                {
                    IPoint p = s.GetCenter();
                    //if touches a side then don't throw
                    if (p.GetX() == r.GetMinX() || p.GetX() == r.GetMaxX()
                      || p.GetY() == r.GetMinY() || p.GetY() == r.GetMaxY())
                        return;
                    throw e;
                }
                double eps = 0.0000001;
                s.Reset(s.GetCenter().GetX(), s.GetCenter().GetY(), radius - eps);
                SpatialRelation rel1 = s.Relate(r);
                s.Reset(s.GetCenter().GetX(), s.GetCenter().GetY(), radius + eps);
                SpatialRelation rel2 = s.Relate(r);
                if (rel1 == rel2)
                    throw e;
                s.Reset(s.GetCenter().GetX(), s.GetCenter().GetY(), radius);//reset
                Console.WriteLine("Seed " + /*getContext().GetRunnerSeedAsString() +*/ ": Hid assertion due to ambiguous edge touch: " + s + " " + r);
            }
        }

        protected virtual void TestCircleIntersect()
        {
            new RectIntersectionAnonymousHelper(ctx).TestRelateWithRectangle();
        }

        [Theory]
        [PropertyData("Contexts")]
        public virtual void TestMakeRect(SpatialContext ctx)
        {
            this.ctx = ctx;

            //test rectangle constructor
            Assert.Equal(new RectangleImpl(1, 3, 2, 4, ctx),
                new RectangleImpl(new PointImpl(1, 2, ctx), new PointImpl(3, 4, ctx), ctx));

            //test ctx.makeRect
            Assert.Equal(ctx.MakeRectangle(1, 3, 2, 4),
                ctx.MakeRectangle(ctx.MakePoint(1, 2), ctx.MakePoint(3, 4)));
        }

        protected virtual void TestEmptiness(IShape emptyShape)
        {
            Assert.True(emptyShape.IsEmpty);
            IPoint emptyPt = emptyShape.GetCenter();
            Assert.True(emptyPt.IsEmpty);
            IRectangle emptyRect = emptyShape.GetBoundingBox();
            Assert.True(emptyRect.IsEmpty);
            AssertEquals(emptyRect, emptyShape.GetBoundingBox());
            AssertEquals(emptyPt, emptyShape.GetCenter());
            AssertRelation("EMPTY", SpatialRelation.DISJOINT, emptyShape, emptyPt);
            AssertRelation("EMPTY", SpatialRelation.DISJOINT, emptyShape, RandomPoint());
            AssertRelation("EMPTY", SpatialRelation.DISJOINT, emptyShape, emptyRect);
            AssertRelation("EMPTY", SpatialRelation.DISJOINT, emptyShape, RandomRectangle(10));
            Assert.True(emptyShape.GetBuffered(random.Next(4 + 1), ctx).IsEmpty);
        }
    }
}
