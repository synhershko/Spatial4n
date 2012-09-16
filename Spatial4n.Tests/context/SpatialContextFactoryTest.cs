using System;
using System.Collections.Generic;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes.Impl;
using Xunit;

namespace Spatial4n.Tests.context
{
    public class SpatialContextFactoryTest
    {
		public static String PROP = "SpatialContextFactory";

        private static SpatialContext Call(params String [] argsStr) 
        {			
            var args = new Dictionary<String,String>();
            for (int i = 0; i < argsStr.Length; i += 2)
            {
                String key = argsStr[i];
                String val = argsStr[i + 1];
                args.Add(key, val);
            }
            return SpatialContextFactory.MakeSpatialContext(args);
        }
  
        [Fact]
        public void TestDefault() 
        {
            SpatialContext s = SpatialContext.GEO;
            SpatialContext t = Call();//default
            Assert.Equal(s.GetType(), t.GetType());
            Assert.Equal(s.IsGeo(), t.IsGeo());
            Assert.Equal(s.GetDistCalc(), t.GetDistCalc());
            Assert.Equal(s.GetWorldBounds(),t.GetWorldBounds());
        }
  
        [Fact]
        public void TestCustom()
        {
            SpatialContext sc = Call("geo", "false");
			Assert.True(!sc.IsGeo());
            Assert.Equal(new CartesianDistCalc(), sc.GetDistCalc());

            sc = Call("geo", "false",
                      "distCalculator", "cartesian^2",
                      "worldBounds", "-100 0 75 200");//West South East North
            Assert.Equal(new CartesianDistCalc(true), sc.GetDistCalc());
            Assert.Equal(new RectangleImpl(-100, 75, 0, 200, sc), sc.GetWorldBounds());

            sc = Call("geo", "true",
                      "distCalculator", "lawOfCosines");
			Assert.True(sc.IsGeo());
            var test = new GeodesicSphereDistCalc.LawOfCosines();
            Assert.Equal(test, sc.GetDistCalc());
        }
  
        [Fact]
        public void TestSystemPropertyLookup() 
        {
            var customInstance = Call("spatialContextFactory", typeof (DSCF).AssemblyQualifiedName);
            Assert.True(!customInstance.IsGeo());//DSCF returns this
        }

        public class DSCF : SpatialContextFactory 
        {
            override protected SpatialContext NewSpatialContext()
            {
                return new SpatialContext(false);
            }
        }
    }
}
