using NetTopologySuite.Geometries;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Spatial4n.Tests.shape
{
    public class BufferedLineStringTest
    {
        private readonly SpatialContext ctx = new SpatialContextFactory()
            { geo = false, worldBounds = new RectangleImpl(-100, 100, -50, 50, null) }.NewSpatialContext();

        private class RectIntersectionAnonymousHelper : RectIntersectionTestHelper
        {
            public RectIntersectionAnonymousHelper(SpatialContext ctx)
                : base(ctx)
            {
            }

            protected override IShape GenerateRandomShape(Core.Shapes.IPoint nearP)
            {
                IRectangle nearR = RandomRectangle(nearP);
                int numPoints = 2 + random.Next(3 + 1);//2-5 points

                List<Core.Shapes.IPoint> points = new List<Core.Shapes.IPoint>(numPoints);
                while (points.Count < numPoints)
                {
                    points.Add(RandomPointIn(nearR));
                }
                double maxBuf = Math.Max(nearR.GetWidth(), nearR.GetHeight());
                double buf = Math.Abs(RandomGaussian()) * maxBuf / 4;
                buf = random.Next((int)Divisible(buf));
                return new BufferedLineString(points, buf, ctx);
            }

            protected override Core.Shapes.IPoint RandomPointInEmptyShape(IShape shape)
            {
                IList<Core.Shapes.IPoint> points = ((BufferedLineString)shape).GetPoints();
                return points.Count == 0 ? null : points[random.Next(points.Count/* - 1*/)];
            }
        }


        [Fact]
        public virtual void TestRectIntersect()
        {
            new RectIntersectionAnonymousHelper(ctx).TestRelateWithRectangle();
        }
    }
}
