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

#if FEATURE_NTS

using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.IO.Nts;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Nts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Spatial4n.Core.IO
{
    public class NtsWktShapeParserTest : WktShapeParserTest
    {
        //By extending WktShapeParserTest we inherit its test too

        internal readonly NtsSpatialContext ctx;//note: masks superclass

        public NtsWktShapeParserTest()
                  : base(NtsSpatialContext.GEO)
        {
            this.ctx = (NtsSpatialContext)base.ctx;
        }

        [Fact]
        public virtual void TestParsePolygon()
        {


            IShape polygonNoHoles = new PolygonBuilder(ctx)
        .Point(100, 0)
        .Point(101, 0)
        .Point(101, 1)
        .Point(100, 2)
        .Point(100, 0)
        .Build();
            string polygonNoHolesSTR = "POLYGON ((100 0, 101 0, 101 1, 100 2, 100 0))";
            AssertParses(polygonNoHolesSTR, polygonNoHoles);
            AssertParses("POLYGON((100 0,101 0,101 1,100 2,100 0))", polygonNoHoles);

            AssertParses("GEOMETRYCOLLECTION ( " + polygonNoHolesSTR + ")",
                ctx.MakeCollection(new List<IShape>(new IShape[] { polygonNoHoles })));

            IShape polygonWithHoles = new PolygonBuilder(ctx)
                .Point(100, 0)
                .Point(101, 0)
                .Point(101, 1)
                .Point(100, 1)
                .Point(100, 0)
                .NewHole()
                .Point(100.2, 0.2)
                .Point(100.8, 0.2)
                .Point(100.8, 0.8)
                .Point(100.2, 0.8)
                .Point(100.2, 0.2)
                .EndHole()
                .Build();
            AssertParses("POLYGON ((100 0, 101 0, 101 1, 100 1, 100 0), (100.2 0.2, 100.8 0.2, 100.8 0.8, 100.2 0.8, 100.2 0.2))", polygonWithHoles);

            GeometryFactory gf = ctx.GeometryFactory;
            AssertParses("POLYGON EMPTY", ctx.MakeShape(
                gf.CreatePolygon(gf.CreateLinearRing(new Coordinate[] { }), null)
            ));
        }

        [Fact]
        public virtual void TestPolyToRect()
        {
            //poly is a rect (no dateline issue)
            AssertParses("POLYGON((0 5, 10 5, 10 20, 0 20, 0 5))", ctx.MakeRectangle(0, 10, 5, 20));
        }

        [Fact]
        public virtual void PolyToRect180Rule()
        {
            //crosses dateline
            IRectangle expected = ctx.MakeRectangle(160, -170, 0, 10);
            //counter-clockwise
            AssertParses("POLYGON((160 0, -170 0, -170 10, 160 10, 160 0))", expected);
            //clockwise
            AssertParses("POLYGON((160 10, -170 10, -170 0, 160 0, 160 10))", expected);
        }

        [Fact]
        public virtual void PolyToRectCcwRule()
        {
            NtsSpatialContext ctx = (NtsSpatialContext)new NtsSpatialContextFactory() { datelineRule = DatelineRule.CcwRect }.NewSpatialContext();
            //counter-clockwise
            Assert.Equal(ctx.ReadShapeFromWkt("POLYGON((160 0, -170 0, -170 10, 160 10, 160 0))"),
        ctx.MakeRectangle(160, -170, 0, 10));
            //clockwise
            Assert.Equal(ctx.ReadShapeFromWkt("POLYGON((160 10, -170 10, -170 0, 160 0, 160 10))"),
                ctx.MakeRectangle(-170, 160, 0, 10));
        }

        [Fact]
        public virtual void TestParseMultiPolygon()
        {
            IShape p1 = new PolygonBuilder(ctx)
                .Point(100, 0)
                .Point(101, 0)//101
                .Point(101, 2)//101
                .Point(100, 1)
                .Point(100, 0)
                .Build();
            IShape p2 = new PolygonBuilder(ctx)
                .Point(100, 0)
                .Point(102, 0)//102
                .Point(102, 2)//102
                .Point(100, 1)
                .Point(100, 0)
                .Build();
            IShape s = ctx.MakeCollection(
                (new IShape[] { p1, p2 }).ToList()
            );
            AssertParses("MULTIPOLYGON(" +
                "((100 0, 101 0, 101 2, 100 1, 100 0))" + ',' +
                "((100 0, 102 0, 102 2, 100 1, 100 0))" +
                ")", s);

            AssertParses("MULTIPOLYGON EMPTY", ctx.MakeCollection(new List<IShape>()));
        }

        [Fact]
        public virtual void TestLineStringDateline()
        {
            //works because we use NTS (NtsGeometry); BufferedLineString doesn't yet do DL wrap.
            IShape s = ctx.ReadShapeFromWkt("LINESTRING(160 10, -170 15)");
            CustomAssert.EqualWithDelta(30, s.BoundingBox.Width, 0.0);
        }

        [Fact]
        public virtual void TestWrapTopologyException()
        {
            //test that we can catch ParseException without having to detect TopologyException too
            Debug.Assert(((NtsWktShapeParser)ctx.WktShapeParser).IsAutoValidate);
            try
            {
                ctx.ReadShapeFromWkt("POLYGON((0 0, 10 0, 10 20))");//doesn't connect around
                Assert.True(false);
            }
            catch (ParseException e)
            {
                //expected
            }

            try
            {
                ctx.ReadShapeFromWkt("POLYGON((0 0, 10 0, 10 20, 5 -5, 0 20, 0 0))");//Topology self-intersect
                Assert.True(false);
            }
            catch (ParseException e)
            {
                //expected
            }
        }

        [Fact]
        public virtual void TestPolygonRepair()
        {
            //because we're going to test validation
            //System.setProperty(NtsGeometry.SYSPROP_ASSERT_VALIDATE, "false"); // TODO: Figure this out...
            Environment.SetEnvironmentVariable(NtsGeometry.SYSPROP_ASSERT_VALIDATE, bool.FalseString);

            //note: doesn't repair all cases; this case isn't:
            //ctx.readShapeFromWkt("POLYGON((0 0, 10 0, 10 20))");//doesn't connect around
            string wkt = "POLYGON((0 0, 10 0, 10 20, 5 -5, 0 20, 0 0))";//Topology self-intersect

            NtsSpatialContextFactory factory = new NtsSpatialContextFactory();
            factory.validationRule = ValidationRule.RepairBuffer0;
            NtsSpatialContext ctx = (NtsSpatialContext)factory.NewSpatialContext(); // TODO: Can we remove this cast?
            IShape buffer0 = ctx.ReadShapeFromWkt(wkt);
            Assert.True(buffer0.GetArea(ctx) > 0);

            factory = new NtsSpatialContextFactory();
            factory.validationRule = ValidationRule.RepairConvexHull;
            ctx = (NtsSpatialContext)factory.NewSpatialContext();
            IShape cvxHull = ctx.ReadShapeFromWkt(wkt);
            Assert.True(cvxHull.GetArea(ctx) > 0);

            Assert.Equal(SpatialRelation.CONTAINS, cvxHull.Relate(buffer0));

            factory = new NtsSpatialContextFactory();
            factory.validationRule = ValidationRule.None;
            ctx = (NtsSpatialContext)factory.NewSpatialContext();
            ctx.ReadShapeFromWkt(wkt);//doesn't throw
        }
    }
}

#endif