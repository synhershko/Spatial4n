using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Spatial4n.Tests.io
{
    public class NtsBinaryCodecTest : BinaryCodecTest
    {

        // TODO: Wtf is this?
        //      @ParametersFactory
        //public static Iterable<Object[]> parameters()
        //      {
        //          //try floats
        //          NtsSpatialContextFactory factory = new NtsSpatialContextFactory();
        //          factory.precisionModel = new PrecisionModel(PrecisionModel.FLOATING_SINGLE);

        //          return Arrays.asList($$(
        //              $(NtsSpatialContext.GEO),//doubles
        //      $(factory.newSpatialContext())//floats
        //          ));
        //      }

        public NtsBinaryCodecTest()
            : base(NtsSpatialContext.GEO)
        {
        }

        public NtsBinaryCodecTest(NtsSpatialContext ctx)
            : base(ctx)
        {
        }

        [Fact]
        public virtual void TestPoly()
        {
            NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
            ctx.MakeShape(RandomGeometry(random.Next(3, 20)), false, false);
        }

        protected override IShape RandomShape()
        {
            if (random.Next(3) == 0)
            {
                NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
                return ctx.MakeShape(RandomGeometry(random.Next(3, 20)), false, false);
            }
            else
            {
                return base.RandomShape();
            }
        }

        private IGeometry RandomGeometry(int points)
        {
            //a circle
            NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
            GeometricShapeFactory gsf = new GeometricShapeFactory(ctx.GetGeometryFactory());
            gsf.Centre = (new Coordinate(0, 0));
            gsf.Size = (180);//diameter
            gsf.NumPoints = (points);
            return gsf.CreateCircle();
        }
    }
}
