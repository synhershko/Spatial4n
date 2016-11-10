using GeoAPI.IO;
using Spatial4n.Core.Context;
using Spatial4n.Core.Io;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Spatial4n.Tests.io
{
    public class BinaryCodecTest
    {
        protected readonly Random random = new Random(RandomSeed.Seed());

        internal readonly SpatialContext ctx;
        private BinaryCodec binaryCodec;

        protected BinaryCodecTest(SpatialContext ctx)
        {
            this.ctx = ctx;
            binaryCodec = ctx.BinaryCodec;//stateless
        }

        public BinaryCodecTest()
            : this(SpatialContext.GEO)
        {
        }

        //This test uses WKT to specify the shapes because the Jts based subclass tests will test
        // using floats instead of doubles, and WKT is normalized whereas ctx.makeXXX is not.

        [Fact]
        public virtual void TestPoint()
        {
            AssertRoundTrip(Wkt("POINT(-10 80.3)"));
        }

        [Fact]
        public virtual void TestRect()
        {
            AssertRoundTrip(Wkt("ENVELOPE(-10, 180, 42.3, 0)"));
        }

        [Fact]
        public virtual void TestCircle()
        {
            AssertRoundTrip(Wkt("BUFFER(POINT(-10 30), 5.2)"));
        }

        [Fact]
        public virtual void TestCollection()
        {
            ShapeCollection s = ctx.MakeCollection(
                (new Shape[]
                {
                RandomShape(),
                RandomShape(),
                RandomShape()
                }).ToList()
            );
            AssertRoundTrip(s);
        }

        protected virtual Shape Wkt(string wkt)
        {
            try
            {
                return ctx.ReadShapeFromWkt(wkt);
            }
            catch (ParseException e)
            {
                throw new Exception(e.Message, e);
            }
        }

        protected virtual Shape RandomShape()
        {
            switch (random.Next(2))
            {//inclusive
                case 0: return Wkt("POINT(-10 80.3)");
                case 1: return Wkt("ENVELOPE(-10, 180, 42.3, 0)");
                case 2: return Wkt("BUFFER(POINT(-10 30), 5.2)");
                default: throw new Exception();
            }
        }

        protected virtual void AssertRoundTrip(Shape shape)
        {
            try
            {
                MemoryStream baos = new MemoryStream();
                binaryCodec.WriteShape(new BinaryWriter(baos), shape);
                MemoryStream bais = new MemoryStream(baos.ToArray());
                Assert.Equal(shape, binaryCodec.ReadShape(new BinaryReader(bais)));
            }
            catch (IOException e)
            {
                throw new Exception(e.Message, e);
            }
        }
    }
}
