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

using Spatial4n.Core.Context;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.IO;
using Spatial4n.Core.Shapes;
using Xunit;

namespace Spatial4n.Core.IO
{
    public class WktShapeParserTest
    {
        internal readonly SpatialContext ctx;

        protected WktShapeParserTest(SpatialContext ctx)
        {
            this.ctx = ctx;
        }

        public WktShapeParserTest()
            : this(SpatialContext.GEO)
        {

        }

        protected virtual void AssertParses(string wkt, IShape expected)
        {
            Assert.Equal(ctx.ReadShapeFromWkt(wkt), expected);
        }

        protected virtual void AssertFails(string wkt)
        {
            try
            {
                ctx.ReadShapeFromWkt(wkt);
                Assert.True(false, "ParseException expected");
            }
#pragma warning disable 168
            catch (ParseException e)
#pragma warning restore 168
            {//expected
            }
        }

        [Fact]
        public virtual void TestNoOp()
        {
            WktShapeParser wktShapeParser = ctx.WktShapeParser;
            Assert.Null(wktShapeParser.ParseIfSupported(""));
            Assert.Null(wktShapeParser.ParseIfSupported("  "));
            Assert.Null(wktShapeParser.ParseIfSupported("BogusShape()"));
            Assert.Null(wktShapeParser.ParseIfSupported("BogusShape"));
        }

        [Fact]
        public virtual void TestParsePoint()
        {
            AssertParses("POINT (100 90)", ctx.MakePoint(100, 90));//typical
            AssertParses(" POINT (100 90) ", ctx.MakePoint(100, 90));//trimmed
            AssertParses("point (100 90)", ctx.MakePoint(100, 90));//case indifferent
            AssertParses("POINT ( 100 90 )", ctx.MakePoint(100, 90));//inner spaces
            AssertParses("POINT(100 90)", ctx.MakePoint(100, 90));
            AssertParses("POINT (-45 90 )", ctx.MakePoint(-45, 90));
            IPoint expected = ctx.MakePoint(-45.3, 80.4);
            AssertParses("POINT (-45.3 80.4 )", expected);
            AssertParses("POINT (-45.3 +80.4 )", expected);
            AssertParses("POINT (-45.3 8.04e1 )", expected);

            AssertParses("POINT EMPTY", ctx.MakePoint(double.NaN, double.NaN));

            //other dimensions are skipped
            AssertParses("POINT (100 90 2)", ctx.MakePoint(100, 90));
            AssertParses("POINT (100 90 2 3)", ctx.MakePoint(100, 90));
            AssertParses("POINT ZM ( 100 90 )", ctx.MakePoint(100, 90));//ignore dimension
            AssertParses("POINT ZM ( 100 90 -3 -4)", ctx.MakePoint(100, 90));//ignore dimension
        }

        [Fact]
        public virtual void TestParsePoint_invalidDefinitions()
        {
            AssertFails("POINT 100 90");
            AssertFails("POINT (100 90");
            AssertFails("POINT (100, 90)");
            AssertFails("POINT 100 90)");
            AssertFails("POINT (100)");
            AssertFails("POINT (10f0 90)");
            AssertFails("POINT (EMPTY)");

            AssertFails("POINT (1 2), POINT (2 3)");
            AssertFails("POINT EMPTY (1 2)");
            AssertFails("POINT ZM EMPTY (1 2)");
            AssertFails("POINT ZM EMPTY 1");
        }

        [Fact]
        public virtual void TestParseMultiPoint()
        {
            IShape s1 = ctx.MakeCollection(new IShape[] { ctx.MakePoint(10, 40) });
            AssertParses("MULTIPOINT (10 40)", s1);

            IShape s4 = ctx.MakeCollection(new IShape[] {
                ctx.MakePoint(10, 40), ctx.MakePoint(40, 30),
                ctx.MakePoint(20, 20), ctx.MakePoint(30, 10) });
            AssertParses("MULTIPOINT ((10 40), (40 30), (20 20), (30 10))", s4);
            AssertParses("MULTIPOINT (10 40, 40 30, 20 20, 30 10)", s4);

            AssertParses("MULTIPOINT Z EMPTY", ctx.MakeCollection(new IShape[0]));
        }

        [Fact]
        public virtual void TestParseEnvelope()
        {
            IRectangle r = ctx.MakeRectangle(ctx.MakePoint(10, 25), ctx.MakePoint(30, 45));
            AssertParses(" ENVELOPE ( 10 , 30 , 45 , 25 ) ", r);
            AssertParses("ENVELOPE(10,30,45,25) ", r);
            AssertFails("ENVELOPE (10 30 45 25)");
        }

        [Fact]
        public virtual void TestLineStringShape()
        {
            IPoint p1 = ctx.MakePoint(1, 10);
            IPoint p2 = ctx.MakePoint(2, 20);
            IPoint p3 = ctx.MakePoint(3, 30);
            IShape ls = ctx.MakeLineString(new IPoint[] { p1, p2, p3 });
            AssertParses("LINESTRING (1 10, 2 20, 3 30)", ls);

            AssertParses("LINESTRING EMPTY", ctx.MakeLineString(new IPoint[0]));
        }

        [Fact]
        public virtual void TestMultiLineStringShape()
        {
            IShape s = ctx.MakeCollection(new IShape[] {
                ctx.MakeLineString(new IPoint[] {
                    ctx.MakePoint(10, 10), ctx.MakePoint(20, 20), ctx.MakePoint(10, 40) }),
                ctx.MakeLineString(new IPoint[] {
                    ctx.MakePoint(40, 40), ctx.MakePoint(30, 30), ctx.MakePoint(40, 20), ctx.MakePoint(30, 10) }) }
            );
            AssertParses("MULTILINESTRING ((10 10, 20 20, 10 40),\n" +
                "(40 40, 30 30, 40 20, 30 10))", s);

            AssertParses("MULTILINESTRING M EMPTY", ctx.MakeCollection(new IShape[0]));
        }

        [Fact]
        public virtual void TestGeomCollection()
        {
            IShape s1 = ctx.MakeCollection(new IShape[] { ctx.MakePoint(1, 2) });
            IShape s2 = ctx.MakeCollection(new IShape[] {
                ctx.MakeRectangle(1, 2, 3, 4),
                ctx.MakePoint(-1, -2) });
            AssertParses("GEOMETRYCOLLECTION (POINT (1 2) )", s1);
            AssertParses("GEOMETRYCOLLECTION ( ENVELOPE(1,2,4,3), POINT(-1 -2)) ", s2);

            AssertParses("GEOMETRYCOLLECTION EMPTY", ctx.MakeCollection(new IShape[0]));

            AssertParses("GEOMETRYCOLLECTION ( POINT EMPTY )",
            ctx.MakeCollection(new IShape[] { ctx.MakePoint(double.NaN, double.NaN) }));
        }

        [Fact]
        public virtual void TestBuffer()
        {
            AssertParses("BUFFER(POINT(1 2), 3)", ctx.MakePoint(1, 2).GetBuffered(3, ctx));
        }
    }
}
