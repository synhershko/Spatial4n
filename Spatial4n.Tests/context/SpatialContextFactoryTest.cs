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
        private SpatialContext Call(params String [] argsStr) 
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
            SpatialContext s = SpatialContext.GEO_KM;
            SpatialContext t = Call();//default
            Assert.Equal(s.GetType(), t.GetType());
            Assert.Equal(s.GetUnits(), t.GetUnits());
            Assert.Equal(s.GetDistCalc(), t.GetDistCalc());
            Assert.Equal(s.GetWorldBounds(),t.GetWorldBounds());
        }
  
        [Fact]
        public void TestCustom()
        {
            SpatialContext sc = Call("units", "u");
            Assert.Equal(DistanceUnits.CARTESIAN, sc.GetUnits());
            Assert.Equal(new CartesianDistCalc(), sc.GetDistCalc());

            sc = Call("units", "u",
                      "distCalculator", "cartesian^2",
                      "worldBounds", "-100 0 75 200");//West South East North
            Assert.Equal(new CartesianDistCalc(true), sc.GetDistCalc());
            Assert.Equal(new RectangleImpl(-100, 75, 0, 200), sc.GetWorldBounds());

            sc = Call("units", "miles",
                      "distCalculator", "lawOfCosines");
            Assert.Equal(DistanceUnits.MILES, sc.GetUnits());
            var test = new GeodesicSphereDistCalc.LawOfCosines(sc.GetUnits().EarthRadius());
            Assert.Equal(test, sc.GetDistCalc());
        }
  
        [Fact]
        public void TestSystemPropertyLookup() 
        {
            var customInstance = Call("spatialContextFactory", typeof (DSCF).AssemblyQualifiedName);
            Assert.Equal(DistanceUnits.CARTESIAN, customInstance.GetUnits());//DSCF returns this
        }

        public class DSCF : SpatialContextFactory 
        {
            new protected SpatialContext NewSpatialContext()
            {
                return new SpatialContext(DistanceUnits.CARTESIAN);
            }
        }
    }
}
