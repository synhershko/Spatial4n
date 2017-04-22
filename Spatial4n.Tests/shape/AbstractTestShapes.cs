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

using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using System;
using Xunit;
using Xunit.Extensions;

namespace Spatial4n.Core.Shape
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

            Assert.Equal( /*msg,*/ width != 0 && height != 0, r.HasArea);
            Assert.Equal( /*msg,*/ width != 0 && height != 0, r.GetArea(ctx) > 0);
            if (ctx.IsGeo && r.Width == 360 && r.Height == 180)
            {
                //whole globe
                double earthRadius = DistanceUtils.ToDegrees(1);
                CustomAssert.EqualWithDelta(4 * Math.PI * earthRadius * earthRadius, r.GetArea(ctx), 1.0);//1km err
            }

            AssertEqualsRatio(msg, height, r.Height);
            AssertEqualsRatio(msg, width, r.Width);
            IPoint center = r.Center;
            msg += " ctr:" + center;
            //System.out.println(msg);
            AssertRelation(msg, SpatialRelation.CONTAINS, r, center);

            IDistanceCalculator dc = ctx.DistCalc;
            double dUR = dc.Distance(center, r.MaxX, r.MaxY);
            double dLR = dc.Distance(center, r.MaxX, r.MinY);
            double dUL = dc.Distance(center, r.MinX, r.MaxY);
            double dLL = dc.Distance(center, r.MinX, r.MinY);

            Assert.Equal( /*msg,*/ width != 0 || height != 0, dUR != 0);
            if (dUR != 0)
                Assert.True(dUR > 0 && dLL > 0);
            AssertEqualsRatio(msg, dUR, dUL);
            AssertEqualsRatio(msg, dLR, dLL);
            if (!ctx.IsGeo || center.Y == 0)
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
                            AssertRelation(null, SpatialRelation.CONTAINS, r, r2.Center);
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

            Assert.Equal( /*msg,*/ dist > 0, c.HasArea);
            double area = c.GetArea(ctx);
            Assert.True(/*msg,*/ c.HasArea == (area > 0.0));
            IRectangle bbox = c.BoundingBox;
            Assert.Equal( /*msg,*/ dist > 0, bbox.GetArea(ctx) > 0);
            Assert.True(area <= bbox.GetArea(ctx));
            if (!ctx.IsGeo)
            {
                //if not geo then units of dist == units of x,y
                AssertEqualsRatio(msg, bbox.Height, dist * 2);
                AssertEqualsRatio(msg, bbox.Width, dist * 2);
            }
            AssertRelation(msg, SpatialRelation.CONTAINS, c, c.Center);
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
                return shape.Center;
            }

            protected override void OnAssertFail(/*AssertionError*/Exception e, /*Circle*/IShape shape, IRectangle r, SpatialRelation ic)
            {
                ICircle s = shape as ICircle;
                //Check if the circle's edge appears to coincide with the shape.
                double radius = s.Radius;
                if (radius == 180)
                    throw e;//if this happens, then probably a bug
                if (radius == 0)
                {
                    IPoint p = s.Center;
                    //if touches a side then don't throw
                    if (p.X == r.MinX || p.X == r.MaxX
                      || p.Y == r.MinY || p.Y == r.MaxY)
                        return;
                    throw e;
                }
                double eps = 0.0000001;
                s.Reset(s.Center.X, s.Center.Y, radius - eps);
                SpatialRelation rel1 = s.Relate(r);
                s.Reset(s.Center.X, s.Center.Y, radius + eps);
                SpatialRelation rel2 = s.Relate(r);
                if (rel1 == rel2)
                    throw e;
                s.Reset(s.Center.X, s.Center.Y, radius);//reset
                Console.WriteLine("Seed " + /*getContext().GetRunnerSeedAsString() +*/ ": Hid assertion due to ambiguous edge touch: " + s + " " + r);
            }
        }

        protected virtual void TestCircleIntersect()
        {
            new RectIntersectionAnonymousHelper(ctx).TestRelateWithRectangle();
        }

        //[Theory]
        //[PropertyData("Contexts")]
        public virtual void TestMakeRect(SpatialContext ctx)
        {
            this.ctx = ctx;

            //test rectangle constructor
            Assert.Equal(new Rectangle(1, 3, 2, 4, ctx),
                new Rectangle(new Point(1, 2, ctx), new Point(3, 4, ctx), ctx));

            //test ctx.makeRect
            Assert.Equal(ctx.MakeRectangle(1, 3, 2, 4),
                ctx.MakeRectangle(ctx.MakePoint(1, 2), ctx.MakePoint(3, 4)));
        }

        protected virtual void TestEmptiness(IShape emptyShape)
        {
            Assert.True(emptyShape.IsEmpty);
            IPoint emptyPt = emptyShape.Center;
            Assert.True(emptyPt.IsEmpty);
            IRectangle emptyRect = emptyShape.BoundingBox;
            Assert.True(emptyRect.IsEmpty);
            AssertEquals(emptyRect, emptyShape.BoundingBox);
            AssertEquals(emptyPt, emptyShape.Center);
            AssertRelation("EMPTY", SpatialRelation.DISJOINT, emptyShape, emptyPt);
            AssertRelation("EMPTY", SpatialRelation.DISJOINT, emptyShape, RandomPoint());
            AssertRelation("EMPTY", SpatialRelation.DISJOINT, emptyShape, emptyRect);
            AssertRelation("EMPTY", SpatialRelation.DISJOINT, emptyShape, RandomRectangle(10));
            Assert.True(emptyShape.GetBuffered(random.Next(4 + 1), ctx).IsEmpty);
        }
    }
}
