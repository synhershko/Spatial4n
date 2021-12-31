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
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Xunit.Extensions;

namespace Xunit.Extensions
{

}

namespace Spatial4n.Core.Shape
{
    public class BufferedLineTest
    {
        protected readonly Random random = new Random(RandomSeed.Seed());

        private readonly SpatialContext ctx = new SpatialContextFactory()
        { geo = false, worldBounds = new Rectangle(-100, 100, -50, 50, null) }.CreateSpatialContext();

        //      @Rule
        //public TestLog testLog = TestLog.instance;
        //SpatialContext.GEO ;//
#pragma warning disable xUnit1013
        public static void LogShapes(BufferedLine line, IRectangle rect)
#pragma warning restore xUnit1013
        {
            string lineWKT =
                "LINESTRING(" + line.A.X + " " + line.A.Y + "," +
                    line.B.X + " " + line.B.Y + ")";
            Console.WriteLine(
                "GEOMETRYCOLLECTION(" + lineWKT + "," + RectToWkt(line.BoundingBox
                    ) + ")");

            string rectWKT = RectToWkt(rect);
            Console.WriteLine(rectWKT);
        }

        static private string RectToWkt(IRectangle rect)
        {
            return "POLYGON((" + rect.MinX + " " + rect.MinY + "," +
                rect.MaxX + " " + rect.MinY + "," +
                rect.MaxX + " " + rect.MinY + "," +
                rect.MinX + " " + rect.MinY + "," +
                rect.MinX + " " + rect.MinY + "))";
        }

        [Fact]
        public virtual void Distance()
        {
            //negative slope
            TestDistToPoint(ctx.MakePoint(7, -4), ctx.MakePoint(3, 2),
                ctx.MakePoint(5, 6), 3.88290);
            //positive slope
            TestDistToPoint(ctx.MakePoint(3, 2), ctx.MakePoint(7, 5),
                ctx.MakePoint(5, 6), 2.0);
            //vertical line
            TestDistToPoint(ctx.MakePoint(3, 2), ctx.MakePoint(3, 8),
                ctx.MakePoint(4, 3), 1.0);
            //horiz line
            TestDistToPoint(ctx.MakePoint(3, 2), ctx.MakePoint(6, 2),
                ctx.MakePoint(4, 3), 1.0);
        }

        private void TestDistToPoint(IPoint pA, IPoint pB, IPoint pC, double dist)
        {
            if (dist > 0)
            {
                Assert.False(new BufferedLine(pA, pB, dist * 0.999, ctx).Contains(pC));
            }
            else
            {
                Debug.Assert(dist == 0);
                Assert.True(new BufferedLine(pA, pB, 0, ctx).Contains(pC));
            }
            Assert.True(new BufferedLine(pA, pB, dist * 1.001, ctx).Contains(pC));
        }

        [Fact]
        public virtual void Misc()
        {
            //pa == pb
            IPoint pt = ctx.MakePoint(10, 1);
            BufferedLine line = new BufferedLine(pt, pt, 3, ctx);
            Assert.True(line.Contains(ctx.MakePoint(10, 1 + 3 - 0.1)));
            Assert.False(line.Contains(ctx.MakePoint(10, 1 + 3 + 0.1)));
        }

#if FEATURE_XUNIT_1X
        [RepeatFact(15)]
        public virtual void Quadrants()
#else
        [Repeat(15)]
        [Theory]
        public virtual void Quadrants(int iterationNumber)
#endif
        {
            //random line
            BufferedLine line = NewRandomLine();
            //    if (line.getA().equals(line.getB()))
            //      return;//this test doesn't work
            IRectangle rect = NewRandomLine().BoundingBox;
            //logShapes(line, rect);
            //compute closest corner brute force
            IList<IPoint> corners = QuadrantCorners(rect);
            // a collection instead of 1 value due to ties
            IList<int?> farthestDistanceQuads = new List<int?>();
            double farthestDistance = -1;
            int quad = 1;
            foreach (IPoint corner in corners)
            {
                double d = line.LinePrimary.DistanceUnbuffered(corner);
                if (Math.Abs(d - farthestDistance) < 0.000001)
                {//about equal
                    farthestDistanceQuads.Add(quad);
                }
                else if (d > farthestDistance)
                {
                    farthestDistanceQuads.Clear();
                    farthestDistanceQuads.Add(quad);
                    farthestDistance = d;
                }
                quad++;
            }
            //compare results
            int calcClosestQuad = line.LinePrimary.Quadrant(rect.Center);
            Assert.Contains(calcClosestQuad, farthestDistanceQuads);
        }

        private BufferedLine NewRandomLine()
        {
            IPoint pA = new Point(random.Next(9 + 1), random.Next(9 + 1), ctx);
            IPoint pB = new Point(random.Next(9 + 1), random.Next(9 + 1), ctx);
            int buf = random.Next(5 + 1);
            return new BufferedLine(pA, pB, buf, ctx);
        }

        private IList<IPoint> QuadrantCorners(IRectangle rect)
        {
            IList<IPoint> corners = new List<IPoint>(4);
            corners.Add(ctx.MakePoint(rect.MaxX, rect.MaxY));
            corners.Add(ctx.MakePoint(rect.MinX, rect.MaxY));
            corners.Add(ctx.MakePoint(rect.MinX, rect.MinY));
            corners.Add(ctx.MakePoint(rect.MaxX, rect.MinY));
            return corners;
        }

        private class RectIntersectionAnonymousHelper : RectIntersectionTestHelper
        {
            private readonly BufferedLineTest outerInstance;

            public RectIntersectionAnonymousHelper(BufferedLineTest outerInstance, SpatialContext ctx)
                : base(ctx)
            {
                this.outerInstance = outerInstance;
            }

            protected override IShape GenerateRandomShape(IPoint nearP)
            {
                IRectangle nearR = RandomRectangle(nearP);
                IList<IPoint> corners = outerInstance.QuadrantCorners(nearR);
                int r4 = outerInstance.random.Next(3 + 1);//0..3
                IPoint pA = corners[r4];
                IPoint pB = corners[(r4 + 2) % 4];
                double maxBuf = Math.Max(nearR.Width, nearR.Height);
                double buf = Math.Abs(RandomGaussian()) * maxBuf / 4;
                buf = outerInstance.random.Next((int)Divisible(buf) + 1);
                return new BufferedLine(pA, pB, buf, ctx);
            }

            protected override IPoint RandomPointInEmptyShape(IShape shape)
            {
                int r = outerInstance.random.Next(1 + 1);
                if (r == 0) return ((BufferedLine)shape).A;
                //if (r == 1)
                return ((BufferedLine)shape).B;
                //        Point c = shape.getCenter();
                //        if (shape.contains(c));
            }
        }

        [Fact]
        public virtual void TestRectIntersect()
        {
            new RectIntersectionAnonymousHelper(this, ctx).TestRelateWithRectangle();
        }

        private BufferedLine NewBufLine(int x1, int y1, int x2, int y2, int buf)
        {
            IPoint pA = ctx.MakePoint(x1, y1);
            IPoint pB = ctx.MakePoint(x2, y2);
            if (random.Next(100) % 2 == 0)
            {
                return new BufferedLine(pB, pA, buf, ctx);
            }
            else
            {
                return new BufferedLine(pA, pB, buf, ctx);
            }
        }
    }
}
