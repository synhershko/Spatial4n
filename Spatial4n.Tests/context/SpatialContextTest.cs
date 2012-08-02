using System;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using Xunit;

namespace Spatial4n.Tests.context
{
    public class SpatialContextTest
    {
		//@ParametersFactory
		//public static IEnumerable<Object[]> parameters() {
		//    return new [] { SpatialContext.GEO_KM, JtsSpatialContext.GEO_KM};
		//}

        private readonly SpatialContext ctx;

		public SpatialContextTest(SpatialContext ctx)
		{
			this.ctx = ctx;
    	}

        public interface IWriteReader
        {
            Shape WriteThenRead(Shape s);
        }

        public class WriteReader : IWriteReader
        {
            private readonly SpatialContext _io;

            public WriteReader(SpatialContext io)
            {
                _io = io;
            }

            public Shape WriteThenRead(Shape s)
            {
                String buff = _io.ToString(s);
                return _io.ReadShape(buff);
            }
        }

        //Looking for more tests?  Shapes are tested in TestShapes2D.

		private T WriteThenRead<T>(T s) where T : Shape
		{
			String buff = ctx.ToString(s);
			return (T) ctx.ReadShape(buff);
		}

    	public void TestBasicShapeIO()
        {
            // Simple Point
            Shape s = ctx.ReadShape("10 20");
			Assert.Equal(s, WriteThenRead(s));
            Assert.Equal(s, ctx.ReadShape("20,10"));//check comma for y,x format
            Assert.Equal(s, ctx.ReadShape("20, 10"));//test space
            Point p = (Point)s;
            CustomAssert.EqualWithDelta(10.0, p.GetX(), 0D);
            CustomAssert.EqualWithDelta(20.0, p.GetY(), 0D);
			Assert.False(s.HasArea());

            // BBOX
            s = ctx.ReadShape("-10 -20 10 20");
			Assert.Equal(s, WriteThenRead(s));
            Rectangle b = (Rectangle)s;
            CustomAssert.EqualWithDelta(-10.0, b.GetMinX(), 0D);
            CustomAssert.EqualWithDelta(-20.0, b.GetMinY(), 0D);
            CustomAssert.EqualWithDelta(10.0, b.GetMaxX(), 0D);
            CustomAssert.EqualWithDelta(20.0, b.GetMaxY(), 0D);
			Assert.True(s.HasArea());

			// Circle
            s = ctx.ReadShape("Circle( 1.23 4.56 distance=7.89)");
			Assert.Equal(s, WriteThenRead(s));
            CircleImpl circle = (CircleImpl)s;
            CustomAssert.EqualWithDelta(1.23, circle.GetCenter().GetX(), 0D);
            CustomAssert.EqualWithDelta(4.56, circle.GetCenter().GetY(), 0D);
            CustomAssert.EqualWithDelta(7.89, circle.GetRadius(), 0D);
			Assert.True(s.HasArea());

            Shape s2 = ctx.ReadShape("CIRCLE( 4.56,1.23 d=7.89 )"); // use lat,lon and use 'd' abbreviation
            Assert.Equal(s, s2);
        }
    }
}
