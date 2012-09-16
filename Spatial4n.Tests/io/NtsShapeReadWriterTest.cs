using Spatial4n.Core.Context;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Shapes;
using Xunit;

namespace Spatial4n.Tests.io
{
	public class NtsShapeReadWriterTest
	{
		readonly SpatialContext ctx = NtsSpatialContext.GEO;

		[Fact]
		public void wktGeoPt()
		{
			Shape s = ctx.ReadShape("Point(-160 30)");
			Assert.Equal(ctx.MakePoint(-160, 30), s);
		}

		[Fact]
		public void wktGeoRect()
		{
			//REMEMBER: Polygon WKT's outer ring is counter-clockwise order. If you accidentally give the other direction,
			// JtsSpatialContext will give the wrong result for a rectangle crossing the dateline.

			// In these two tests, we give the same set of points, one that does not cross the dateline, and the 2nd does. The
			// order is counter-clockwise in both cases as it should be.

			Shape sNoDL = ctx.ReadShape("Polygon((-170 30, -170 15,  160 15,  160 30, -170 30))");
			Rectangle expectedNoDL = ctx.MakeRectangle(-170, 160, 15, 30);
			Assert.True(!expectedNoDL.GetCrossesDateLine());
			Assert.Equal(expectedNoDL, sNoDL);

			Shape sYesDL = ctx.ReadShape("Polygon(( 160 30,  160 15, -170 15, -170 30,  160 30))");
			Rectangle expectedYesDL = ctx.MakeRectangle(160, -170, 15, 30);
			Assert.True(expectedYesDL.GetCrossesDateLine());
			Assert.Equal(expectedYesDL, sYesDL);
		}
	}
}
