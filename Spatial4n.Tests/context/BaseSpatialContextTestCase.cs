using System;
using Spatial4n.Core.Context;
using Spatial4n.Core.Query;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using Xunit;

namespace Spatial4n.Tests.context
{
    //@SuppressWarnings("unchecked")
    public abstract class BaseSpatialContextTestCase
    {
        protected abstract SpatialContext GetSpatialContext();

        [Fact]
        public void TestArgsParser()
        {
            CheckArgParser(GetSpatialContext());
        }

        [Fact]
        public void TestImplementsEqualsAndHash()
        {
            CheckShapesImplementEquals(new[]
                                    {
                                        typeof(PointImpl),
                                        typeof(CircleImpl),
                                        typeof(RectangleImpl),
                                        typeof(MultiShape),
                                    });
        }

        [Fact]
        public void TestSimpleShapeIO()
        {
            SpatialContext io = GetSpatialContext();
            CheckBasicShapeIO(io, new WriteReader(io));
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

        public static void CheckArgParser(SpatialContext ctx)
        {
            SpatialArgsParser parser = new SpatialArgsParser();

            String arg = SpatialOperation.IsWithin + "(-10 -20 10 20)";
            SpatialArgs outValue = parser.Parse(arg, ctx);
            Assert.Equal(SpatialOperation.IsWithin, outValue.Operation);
            Rectangle bounds = (Rectangle)outValue.GetShape();
            CustomAssert.EqualWithDelta(-10.0, bounds.GetMinX(), 0D);
            CustomAssert.EqualWithDelta(10.0, bounds.GetMaxX(), 0D);

            // Disjoint should not be scored
            arg = SpatialOperation.IsDisjointTo + " (-10 10 -20 20)";
            outValue = parser.Parse(arg, ctx);
            Assert.Equal(SpatialOperation.IsDisjointTo, outValue.Operation);

            try
            {
                parser.Parse(SpatialOperation.IsDisjointTo + "[ ]", ctx);
                Assert.True(false, "spatial operations need args");
            }
            catch (Exception)
            {
                //expected
            }

            try
            {
                parser.Parse("XXXX(-10 10 -20 20)", ctx);
                Assert.True(false, "unknown operation!");
            }
            catch (Exception)
            {
                //expected
            }
        }

        public static void CheckShapesImplementEquals(Type[] classes)
        {
            foreach (var clazz in classes)
            {
                try
                {
                    //getDeclaredMethod( "equals", Object.class );
                    var method = clazz.GetMethod("Equals", new[] { typeof(Object) });
                }
                catch (Exception)
                {
                    //We want the equivalent of Assert.Fail(msg)
                    Assert.True(false, "Shape needs to define 'equals' : " + clazz.Name);
                }
                try
                {
                    //clazz.getDeclaredMethod( "hashCode" );
                    var method = clazz.GetMethod("GetHashCode");
                }
                catch (Exception)
                {
                    //We want the equivalent of Assert.Fail(msg)
                    Assert.True(false, "Shape needs to define 'hashCode' : " + clazz.Name);
                }
            }
        }

        public static void CheckBasicShapeIO(SpatialContext ctx, IWriteReader help )
        {
            // Simple Point
            Shape s = ctx.ReadShape("10 20");
            Assert.Equal(s, ctx.ReadShape("20,10"));//check comma for y,x format
            Assert.Equal(s, ctx.ReadShape("20, 10"));//test space
            Point p = (Point)s;
            CustomAssert.EqualWithDelta(10.0, p.GetX(), 0D);
            CustomAssert.EqualWithDelta(20.0, p.GetY(), 0D);
            p = (Point)help.WriteThenRead(s);
            CustomAssert.EqualWithDelta(10.0, p.GetX(), 0D);
            CustomAssert.EqualWithDelta(20.0, p.GetY(), 0D);
            Assert.False(s.HasArea());

            // BBOX
            s = ctx.ReadShape("-10 -20 10 20");
            Rectangle b = (Rectangle)s;
            CustomAssert.EqualWithDelta(-10.0, b.GetMinX(), 0D);
            CustomAssert.EqualWithDelta(-20.0, b.GetMinY(), 0D);
            CustomAssert.EqualWithDelta(10.0, b.GetMaxX(), 0D);
            CustomAssert.EqualWithDelta(20.0, b.GetMaxY(), 0D);
            b = (Rectangle)help.WriteThenRead(s);
            CustomAssert.EqualWithDelta(-10.0, b.GetMinX(), 0D);
            CustomAssert.EqualWithDelta(-20.0, b.GetMinY(), 0D);
            CustomAssert.EqualWithDelta(10.0, b.GetMaxX(), 0D);
            CustomAssert.EqualWithDelta(20.0, b.GetMaxY(), 0D);
            Assert.True(s.HasArea());

            // Point/Distance
            s = ctx.ReadShape("Circle( 1.23 4.56 distance=7.89)");
            CircleImpl circle = (CircleImpl)s;
            CustomAssert.EqualWithDelta(1.23, circle.GetCenter().GetX(), 0D);
            CustomAssert.EqualWithDelta(4.56, circle.GetCenter().GetY(), 0D);
            CustomAssert.EqualWithDelta(7.89, circle.GetDistance(), 0D);
            Assert.True(s.HasArea());

            Shape s2 = ctx.ReadShape("Circle( 4.56,1.23 d=7.89 )"); // use lat,lon and use 'd' abbreviation
            Assert.Equal(s, s2);
        }
    }
}
