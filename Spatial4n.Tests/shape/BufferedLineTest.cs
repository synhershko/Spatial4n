using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace Spatial4n.Tests.shape
{
    public class BufferedLineTest
    {
        protected readonly Random random = new Random(RandomSeed.Seed());

        private readonly SpatialContext ctx = new SpatialContextFactory()
        { geo = false, worldBounds = new RectangleImpl(-100, 100, -50, 50, null) }.NewSpatialContext();

        // TODO: What is this for?
        //      @Rule
        //public TestLog testLog = TestLog.instance;
        //SpatialContext.GEO ;//

        public static void LogShapes(BufferedLine line, Rectangle rect)
        {
            string lineWKT =
                "LINESTRING(" + line.GetA().GetX() + " " + line.GetA().GetY() + "," +
                    line.GetB().GetX() + " " + line.GetB().GetY() + ")";
            Console.WriteLine(
                "GEOMETRYCOLLECTION(" + lineWKT + "," + RectToWkt(line.GetBoundingBox
                    ()) + ")");

            string rectWKT = RectToWkt(rect);
            Console.WriteLine(rectWKT);
        }

        static private string RectToWkt(Rectangle rect)
        {
            return "POLYGON((" + rect.GetMinX() + " " + rect.GetMinY() + "," +
                rect.GetMaxX() + " " + rect.GetMinY() + "," +
                rect.GetMaxX() + " " + rect.GetMinY() + "," +
                rect.GetMinX() + " " + rect.GetMinY() + "," +
                rect.GetMinX() + " " + rect.GetMinY() + "))";
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

        private void TestDistToPoint(Point pA, Point pB, Point pC, double dist)
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
            Point pt = ctx.MakePoint(10, 1);
            BufferedLine line = new BufferedLine(pt, pt, 3, ctx);
            Assert.True(line.Contains(ctx.MakePoint(10, 1 + 3 - 0.1)));
            Assert.False(line.Contains(ctx.MakePoint(10, 1 + 3 + 0.1)));
        }

        [Fact]
        [RepeatTest(15)]
        public virtual void Quadrants()
        {
            //random line
            BufferedLine line = NewRandomLine();
            //    if (line.getA().equals(line.getB()))
            //      return;//this test doesn't work
            Rectangle rect = NewRandomLine().GetBoundingBox();
            //logShapes(line, rect);
            //compute closest corner brute force
            List<Point> corners = QuadrantCorners(rect);
            // a collection instead of 1 value due to ties
            List<int?> farthestDistanceQuads = new List<int?>();
            double farthestDistance = -1;
            int quad = 1;
            foreach (Point corner in corners)
            {
                double d = line.GetLinePrimary().DistanceUnbuffered(corner);
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
            int calcClosestQuad = line.GetLinePrimary().Quadrant(rect.GetCenter());
            Assert.True(farthestDistanceQuads.Contains(calcClosestQuad));
        }

        private BufferedLine NewRandomLine()
        {
            Point pA = new PointImpl(random.Next(9 + 1), random.Next(9 + 1), ctx);
            Point pB = new PointImpl(random.Next(9 + 1), random.Next(9 + 1), ctx);
            int buf = random.Next(5 + 1);
            return new BufferedLine(pA, pB, buf, ctx);
        }

        private List<Point> QuadrantCorners(Rectangle rect)
        {
            List<Point> corners = new List<Point>(4);
            corners.Add(ctx.MakePoint(rect.GetMaxX(), rect.GetMaxY()));
            corners.Add(ctx.MakePoint(rect.GetMinX(), rect.GetMaxY()));
            corners.Add(ctx.MakePoint(rect.GetMinX(), rect.GetMinY()));
            corners.Add(ctx.MakePoint(rect.GetMaxX(), rect.GetMinY()));
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

            protected override Shape GenerateRandomShape(Point nearP)
            {
                Rectangle nearR = RandomRectangle(nearP);
                List<Point> corners = outerInstance.QuadrantCorners(nearR);
                int r4 = outerInstance.random.Next(3 + 1);//0..3
                Point pA = corners[r4];
                Point pB = corners[(r4 + 2) % 4];
                double maxBuf = Math.Max(nearR.GetWidth(), nearR.GetHeight());
                double buf = Math.Abs(RandomGaussian()) * maxBuf / 4;
                buf = outerInstance.random.Next((int)Divisible(buf) + 1);
                return new BufferedLine(pA, pB, buf, ctx);
            }

            protected override Point RandomPointInEmptyShape(Shape shape)
            {
                int r = outerInstance.random.Next(1 + 1);
                if (r == 0) return ((BufferedLine)shape).GetA();
                //if (r == 1)
                return ((BufferedLine)shape).GetB();
                //        Point c = shape.getCenter();
                //        if (shape.contains(c));
            }
        }

        [Fact]
        public virtual void TestRectIntersect()
        {
            new RectIntersectionAnonymousHelper(this, ctx).TestRelateWithRectangle();

            //        new RectIntersectionTestHelper/*<BufferedLine>*/(ctx) {

            //  @Override
            //  protected BufferedLine generateRandomShape(Point nearP)
            //    {
            //        Rectangle nearR = randomRectangle(nearP);
            //        ArrayList<Point> corners = quadrantCorners(nearR);
            //        int r4 = randomInt(3);//0..3
            //        Point pA = corners.get(r4);
            //        Point pB = corners.get((r4 + 2) % 4);
            //        double maxBuf = Math.max(nearR.getWidth(), nearR.getHeight());
            //        double buf = Math.abs(randomGaussian()) * maxBuf / 4;
            //        buf = randomInt((int)divisible(buf));
            //        return new BufferedLine(pA, pB, buf, ctx);
            //    }

            //    protected Point randomPointInEmptyShape(BufferedLine shape)
            //    {
            //        int r = randomInt(1);
            //        if (r == 0) return shape.getA();
            //        //if (r == 1)
            //        return shape.getB();
            //        //        Point c = shape.getCenter();
            //        //        if (shape.contains(c));
            //    }
            //}.testRelateWithRectangle();
        }

        private BufferedLine NewBufLine(int x1, int y1, int x2, int y2, int buf)
        {
            Point pA = ctx.MakePoint(x1, y1);
            Point pB = ctx.MakePoint(x2, y2);
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
