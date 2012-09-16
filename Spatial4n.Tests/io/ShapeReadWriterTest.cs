using System;
using System.Collections.Generic;
using Spatial4n.Core.Context;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Shapes;
using Xunit;
using Xunit.Extensions;

namespace Spatial4n.Tests.io
{
	public class ShapeReadWriterTest
	{
		public static IEnumerable<object[]> Contexts
		{
			get
			{
				yield return new object[] { SpatialContext.GEO };
				yield return new object[] { NtsSpatialContext.GEO };
			}
		}

		private T WriteThenRead<T>(T s, SpatialContext ctx) where T : Shape
		{
			String buff = ctx.ToString(s);
			return (T)ctx.ReadShape(buff);
		}

		[Theory]
		[PropertyData("Contexts")]
		public void testPoint(SpatialContext ctx)
		{
			Shape s = ctx.ReadShape("10 20");
			Assert.Equal(ctx.MakePoint(10, 20), s);
			Assert.Equal(s, WriteThenRead(s, ctx));
			Assert.Equal(s, ctx.ReadShape("20,10"));//check comma for y,x format
			Assert.Equal(s, ctx.ReadShape("20, 10"));//test space
			Assert.False(s.HasArea());
		}

		[Theory]
		[PropertyData("Contexts")]
		public void testRectangle(SpatialContext ctx)
		{
			Shape s = ctx.ReadShape("-10 -20 10 20");
			Assert.Equal(ctx.MakeRectangle(-10, 10, -20, 20), s);
			Assert.Equal(s, WriteThenRead(s, ctx));
			Assert.True(s.HasArea());
		}

		[Theory]
		[PropertyData("Contexts")]
		public void testCircle(SpatialContext ctx)
		{
			Shape s = ctx.ReadShape("Circle(1.23 4.56 distance=7.89)");
			Assert.Equal(ctx.MakeCircle(1.23, 4.56, 7.89), s);
			Assert.Equal(s, WriteThenRead(s, ctx));
			Assert.Equal(s, ctx.ReadShape("CIRCLE( 4.56,1.23 d=7.89 )")); // use lat,lon and use 'd' abbreviation
			Assert.True(s.HasArea());
		}


		//  Looking for more tests?  Shapes are tested in TestShapes2D.

	}
}
