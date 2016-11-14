using Spatial4n.Core.Context;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Io.Nts;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Spatial4n.Tests.io
{
    public class NtsWKTReaderShapeParserTest
    {
        internal readonly SpatialContext ctx;

        public NtsWKTReaderShapeParserTest()
        {
            NtsSpatialContextFactory factory = new NtsSpatialContextFactory();
            factory.datelineRule = NtsWktShapeParser.DatelineRule.ccwRect;
            factory.wktShapeParserClass = typeof(NtsWKTReaderShapeParser);
            ctx = factory.NewSpatialContext();
        }

        [Fact]
        public virtual void WktGeoPt()
        {
            IShape s = ctx.ReadShape("Point(-160 30)");
            Assert.Equal(ctx.MakePoint(-160, 30), s);
        }

        [Fact]
        public virtual void WktGeoRect()
        {
            //REMEMBER: Polygon WKT's outer ring is counter-clockwise order. If you accidentally give the other direction,
            // NtsSpatialContext will give the wrong result for a rectangle crossing the dateline.

            // In these two tests, we give the same set of points, one that does not cross the dateline, and the 2nd does. The
            // order is counter-clockwise in both cases as it should be.

            IShape sNoDL = ctx.ReadShape("Polygon((-170 30, -170 15,  160 15,  160 30, -170 30))");
            IRectangle expectedNoDL = ctx.MakeRectangle(-170, 160, 15, 30);
            Assert.True(!expectedNoDL.GetCrossesDateLine());
            Assert.Equal(expectedNoDL, sNoDL);

            IShape sYesDL = ctx.ReadShape("Polygon(( 160 30,  160 15, -170 15, -170 30,  160 30))");
            IRectangle expectedYesDL = ctx.MakeRectangle(160, -170, 15, 30);
            Assert.True(expectedYesDL.GetCrossesDateLine());
            Assert.Equal(expectedYesDL, sYesDL);

        }


        [Fact]
        public virtual void TestWrapTopologyException()
        {
            try
            {
                ctx.ReadShape("POLYGON((0 0, 10 0, 10 20))");//doesn't connect around
                Assert.True(false);
            }
            catch (InvalidShapeException e)
            {
                //expected
            }

            try
            {
                ctx.ReadShape("POLYGON((0 0, 10 0, 10 20, 5 -5, 0 20, 0 0))");//Topology self-intersect
                Assert.True(false);
            }
            catch (InvalidShapeException e)
            {
                //expected
            }
        }
    }
}
