/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes.Impl;
using Xunit;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Io.Nts;
using Spatial4n.Core;

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
            Assert.Equal(ctx.IsGeo, ctx2.IsGeo);
            Assert.Equal(ctx.DistCalc, ctx2.DistCalc);
            Assert.Equal(ctx.WorldBounds, ctx2.WorldBounds);
        }

        [Fact]
        public void TestCustom()
        {
            SpatialContext ctx = Call("geo", "false");
            Assert.True(!ctx.IsGeo);
            Assert.Equal(new CartesianDistCalc(), ctx.DistCalc);

            ctx = Call("geo", "false",
                      "distCalculator", "cartesian^2",
                      "worldBounds", "ENVELOPE(-100, 75, 200, 0)");//xMin, xMax, yMax, yMin
            Assert.Equal(new CartesianDistCalc(true), ctx.DistCalc);
            Assert.Equal(new Rectangle(-100, 75, 0, 200, ctx), ctx.WorldBounds);

            ctx = Call("geo", "true",
                      "distCalculator", "lawOfCosines");
            Assert.True(ctx.IsGeo);
            var test = new GeodesicSphereDistCalc.LawOfCosines();
            Assert.Equal(test, ctx.DistCalc);
        }

        [Fact]
        public void TestNtsContextFactory()
        {
            NtsSpatialContext ctx = (NtsSpatialContext)Call(
                "spatialContextFactory", typeof(NtsSpatialContextFactory).AssemblyQualifiedName,
                "geo", "true",
                "normWrapLongitude", "true",
                "precisionScale", "2.0",
                "wktShapeParserClass", typeof(CustomWktShapeParser).AssemblyQualifiedName,
                "datelineRule", "ccwRect",
                "validationRule", "repairConvexHull",
                "autoIndex", "true");
            Assert.True(ctx.IsNormWrapLongitude);
            CustomAssert.EqualWithDelta(2.0, ctx.GeometryFactory.PrecisionModel.Scale, 0.0);
            Assert.True(CustomWktShapeParser.once);//cheap way to test it was created
            Assert.Equal(DatelineRule.CcwRect,
                ((NtsWktShapeParser)ctx.WktShapeParser).DatelineRule);
            Assert.Equal(ValidationRule.RepairConvexHull,
                ((NtsWktShapeParser)ctx.WktShapeParser).ValidationRule);

            //ensure geo=false with worldbounds works -- fixes #72
            ctx = (NtsSpatialContext)Call(
                "spatialContextFactory", typeof(NtsSpatialContextFactory).AssemblyQualifiedName,
                "geo", "false",//set to false
                "worldBounds", "ENVELOPE(-500,500,300,-300)",
                "normWrapLongitude", "true",
                "precisionScale", "2.0",
                "wktShapeParserClass", typeof(CustomWktShapeParser).AssemblyQualifiedName,
                "datelineRule", "ccwRect",
                "validationRule", "repairConvexHull",
                "autoIndex", "true");
            CustomAssert.EqualWithDelta(300, ctx.WorldBounds.MaxY, 0.0);
        }

        [Fact]
        public void TestSystemPropertyLookup()
        {
            var customInstance = Call("spatialContextFactory", typeof(DSCF).AssemblyQualifiedName);
            Assert.True(!customInstance.IsGeo);//DSCF returns this
        }

        public class DSCF : SpatialContextFactory
        {
            protected internal override SpatialContext NewSpatialContext()
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
