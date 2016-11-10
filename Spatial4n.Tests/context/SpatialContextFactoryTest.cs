using System;
using System.Collections.Generic;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes.Impl;
using Xunit;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Io.Nts;

namespace Spatial4n.Tests.context
{
    public class SpatialContextFactoryTest
    {
        public static string PROP = "SpatialContextFactory";

        private static SpatialContext Call(params string[] argsStr)
        {
            var args = new Dictionary<string, string>();
            for (int i = 0; i < argsStr.Length; i += 2)
            {
                string key = argsStr[i];
                string val = argsStr[i + 1];
                args.Add(key, val);
            }
            return SpatialContextFactory.MakeSpatialContext(args);
        }

        [Fact]
        public void TestDefault()
        {
            SpatialContext ctx = SpatialContext.GEO;
            SpatialContext ctx2 = Call();//default
            Assert.Equal(ctx.GetType(), ctx2.GetType());
            Assert.Equal(ctx.IsGeo(), ctx2.IsGeo());
            Assert.Equal(ctx.GetDistCalc(), ctx2.GetDistCalc());
            Assert.Equal(ctx.GetWorldBounds(), ctx2.GetWorldBounds());
        }

        [Fact]
        public void TestCustom()
        {
            SpatialContext ctx = Call("geo", "false");
            Assert.True(!ctx.IsGeo());
            Assert.Equal(new CartesianDistCalc(), ctx.GetDistCalc());

            ctx = Call("geo", "false",
                      "distCalculator", "cartesian^2",
                      "worldBounds", "ENVELOPE(-100, 75, 200, 0)");//xMin, xMax, yMax, yMin
            Assert.Equal(new CartesianDistCalc(true), ctx.GetDistCalc());
            Assert.Equal(new RectangleImpl(-100, 75, 0, 200, ctx), ctx.GetWorldBounds());

            ctx = Call("geo", "true",
                      "distCalculator", "lawOfCosines");
            Assert.True(ctx.IsGeo());
            var test = new GeodesicSphereDistCalc.LawOfCosines();
            Assert.Equal(test, ctx.GetDistCalc());
        }

        [Fact]
        public void TestJtsContextFactory()
        {
            NtsSpatialContext ctx = (NtsSpatialContext)Call(
                "spatialContextFactory", typeof(NtsSpatialContextFactory).Name,
                "geo", "true",
                "normWrapLongitude", "true",
                "precisionScale", "2.0",
                "wktShapeParserClass", typeof(CustomWktShapeParser).Name,
                "datelineRule", "ccwRect",
                "validationRule", "repairConvexHull",
                "autoIndex", "true");
            Assert.True(ctx.IsNormWrapLongitude());
            Assert.Equal(2.0, ctx.GetGeometryFactory().PrecisionModel.Scale, (int)0.0);
            Assert.True(CustomWktShapeParser.once);//cheap way to test it was created
            Assert.Equal(NtsWktShapeParser.DatelineRule.ccwRect,
                ((NtsWktShapeParser)ctx.GetWktShapeParser()).GetDatelineRule());
            Assert.Equal(NtsWktShapeParser.ValidationRule.repairConvexHull,
                ((NtsWktShapeParser)ctx.GetWktShapeParser()).GetValidationRule());

            //ensure geo=false with worldbounds works -- fixes #72
            ctx = (NtsSpatialContext)Call(
                "spatialContextFactory", typeof(NtsSpatialContextFactory).Name,
                "geo", "false",//set to false
                "worldBounds", "ENVELOPE(-500,500,300,-300)",
                "normWrapLongitude", "true",
                "precisionScale", "2.0",
                "wktShapeParserClass", typeof(CustomWktShapeParser).Name,
                "datelineRule", "ccwRect",
                "validationRule", "repairConvexHull",
                "autoIndex", "true");
            Assert.Equal(300, ctx.GetWorldBounds().GetMaxY(), (int)0.0);
        }

        [Fact]
        public void TestSystemPropertyLookup()
        {
            var customInstance = Call("spatialContextFactory", typeof(DSCF).AssemblyQualifiedName);
            Assert.True(!customInstance.IsGeo());//DSCF returns this
        }

        public class DSCF : SpatialContextFactory
        {
            new public SpatialContext NewSpatialContext()
            {
                geo = false;
                return new SpatialContext(false);
            }
        }

        public class CustomWktShapeParser : NtsWktShapeParser
        {
            internal static bool once = false;//cheap way to test it was created
            public CustomWktShapeParser(NtsSpatialContext ctx, NtsSpatialContextFactory factory)
                : base(ctx, factory)
            {
                once = true;
            }
        }
    }
}
