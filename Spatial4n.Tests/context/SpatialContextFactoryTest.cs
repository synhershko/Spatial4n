﻿/*
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

#if FEATURE_NTS
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.IO.Nts;
#endif

using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes.Impl;
using System.Collections.Generic;
using Xunit;

namespace Spatial4n.Core.Context
{
    public class SpatialContextFactoryTest
    {
        public static string PROP = "SpatialContextFactory";

        private SpatialContext Call(params string[] argsStr)
        {
            var args = new Dictionary<string, string>();
            for (int i = 0; i < argsStr.Length; i += 2)
            {
                string key = argsStr[i];
                string val = argsStr[i + 1];
                args.Add(key, val);
            }
            return SpatialContextFactory.MakeSpatialContext(args, GetType().Assembly);
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

#if FEATURE_NTS

        [Fact]
        public void TestNtsContextFactory()
        {
            NtsSpatialContext ctx = (NtsSpatialContext)Call(
                "spatialContextFactory", typeof(NtsSpatialContextFactory).AssemblyQualifiedName,
                "geo", "true",
                "normWrapLongitude", "true",
                "precisionScale", "2.0",
                "wktShapeParserClass", typeof(CustomWktShapeParser).FullName, // spatial4n: This is in the current assembly, so we pass non-qualified name
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
                "wktShapeParserClass", typeof(CustomWktShapeParser).FullName, // spatial4n: This is in the current assembly, so we pass non-qualified name
                "datelineRule", "ccwRect",
                "validationRule", "repairConvexHull",
                "autoIndex", "true");
            CustomAssert.EqualWithDelta(300, ctx.WorldBounds.MaxY, 0.0);
        }
#endif

        [Fact]
        public void TestSystemPropertyLookup()
        {
            var customInstance = Call("spatialContextFactory", typeof(DSCF).FullName); // spatial4n: This is in the current assembly, so we pass non-qualified name
            Assert.True(!customInstance.IsGeo);//DSCF returns this
        }

        public class DSCF : SpatialContextFactory
        {
            public override SpatialContext CreateSpatialContext()
            {
                geo = false;
#pragma warning disable 612, 618
                return new SpatialContext(false);
#pragma warning restore 612, 618
            }
        }

#if FEATURE_NTS
        public class CustomWktShapeParser : NtsWktShapeParser
        {
            internal static bool once = false;//cheap way to test it was created
            public CustomWktShapeParser(NtsSpatialContext ctx, NtsSpatialContextFactory factory)
                : base(ctx, factory)
            {
                once = true;
            }
        }
#endif
    }
}
