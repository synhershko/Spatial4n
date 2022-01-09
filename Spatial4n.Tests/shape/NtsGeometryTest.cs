#if FEATURE_NTS
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

using GeoAPI.Geometries;
using Spatial4n.Core.Context;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.IO.Nts;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using Spatial4n.Core.Shapes.Nts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Extensions;

namespace Spatial4n.Core.Shape
{
    /// <summary>
    /// Tests {@link com.spatial4j.core.shape.jts.JtsGeometry} and some other code related
    /// to {@link com.spatial4j.core.context.jts.JtsSpatialContext}.
    /// </summary>
    public class NtsGeometryTest : AbstractTestShapes
    {
        public static IEnumerable<object[]> Contexts
        {
            get
            {
                yield return new object[] { SpatialContext.GEO };
                yield return new object[] { NtsSpatialContext.GEO };
            }
        }


        private readonly string POLY_STR = "Polygon((-10 30, -40 40, -10 -20, 40 20, 0 0, -10 30))";
        private NtsGeometry POLY_SHAPE;
        private readonly int DL_SHIFT = 180;//since POLY_SHAPE contains 0 0, I know a shift of 180 will make it cross the DL.
        private NtsGeometry POLY_SHAPE_DL;//POLY_SHAPE shifted by DL_SHIFT to cross the dateline

        public NtsGeometryTest()
            : base(NtsSpatialContext.GEO)
        {
            POLY_SHAPE = (NtsGeometry)ctx.ReadShapeFromWkt(POLY_STR);

            if (ctx.IsGeo)
            {
                POLY_SHAPE_DL = ShiftPoly(POLY_SHAPE, DL_SHIFT);
                Assert.True(POLY_SHAPE_DL.BoundingBox.CrossesDateLine);
            }
        }

        private class CoordinateFilterAnonymousHelper : ICoordinateFilter
        {
            private readonly NtsGeometryTest outerInstance;
            private readonly int lon_shift;

            public CoordinateFilterAnonymousHelper(NtsGeometryTest outerInstance, int lon_shift)
            {
                this.outerInstance = outerInstance;
                this.lon_shift = lon_shift;
            }

            public void Filter(Coordinate coord)
            {
                coord.X = outerInstance.NormX(coord.X + lon_shift);
                if (outerInstance.ctx.IsGeo && Math.Abs(coord.X) == 180 && outerInstance.random.nextBoolean())
                    coord.X = -coord.X;//invert sign of dateline boundary some of the time
            }
        }

        private NtsGeometry ShiftPoly(NtsGeometry poly, int lon_shift)
        {
            IGeometry pGeom = poly.Geometry;
            Assert.True(pGeom.IsValid);
            //shift 180 to the right
            pGeom = (IGeometry)pGeom.Clone();
            pGeom.Apply(new CoordinateFilterAnonymousHelper(this, lon_shift));
            pGeom.GeometryChanged();
            Assert.False(pGeom.IsValid);
            return (NtsGeometry)ctx.ReadShapeFromWkt(pGeom.AsText());
        }

        [Fact]
        public virtual void TestRelations()
        {
            TestRelationsImpl(false);
            TestRelationsImpl(true);
        }
#pragma warning disable xUnit1013
        public virtual void TestRelationsImpl(bool prepare)
#pragma warning restore xUnit1013
        {
            Debug.Assert(!((NtsWktShapeParser)ctx.WktShapeParser).IsAutoIndex);
            //base polygon
            NtsGeometry @base = (NtsGeometry)ctx.ReadShapeFromWkt("POLYGON((0 0, 10 0, 5 5, 0 0))");
            //shares only "10 0" with base
            NtsGeometry polyI = (NtsGeometry)ctx.ReadShapeFromWkt("POLYGON((10 0, 20 0, 15 5, 10 0))");
            //within base: differs from base by one point is within
            NtsGeometry polyW = (NtsGeometry)ctx.ReadShapeFromWkt("POLYGON((0 0, 9 0, 5 5, 0 0))");
            //a boundary point of base
            Core.Shapes.IPoint pointB = ctx.MakePoint(0, 0);
            //a shared boundary line of base
            NtsGeometry lineB = (NtsGeometry)ctx.ReadShapeFromWkt("LINESTRING(0 0, 10 0)");
            //a line sharing only one point with base
            NtsGeometry lineI = (NtsGeometry)ctx.ReadShapeFromWkt("LINESTRING(10 0, 20 0)");

            if (prepare) @base.Index();
            AssertRelation(SpatialRelation.CONTAINS, @base, @base);//preferred result as there is no EQUALS
            AssertRelation(SpatialRelation.INTERSECTS, @base, polyI);
            AssertRelation(SpatialRelation.CONTAINS, @base, polyW);
            AssertRelation(SpatialRelation.CONTAINS, @base, pointB);
            AssertRelation(SpatialRelation.CONTAINS, @base, lineB);
            AssertRelation(SpatialRelation.INTERSECTS, @base, lineI);
            if (prepare) lineB.Index();
            AssertRelation(SpatialRelation.CONTAINS, lineB, lineB);//line contains itself
            AssertRelation(SpatialRelation.CONTAINS, lineB, pointB);
        }

        [Fact]
        public virtual void TestEmpty()
        {
            IShape emptyGeom = ctx.ReadShapeFromWkt("POLYGON EMPTY");
            TestEmptiness(emptyGeom);
            AssertRelation("EMPTY", SpatialRelation.DISJOINT, emptyGeom, POLY_SHAPE);
        }

        [Fact]
        public virtual void TestArea()
        {
            //simple bbox
            IRectangle r = RandomRectangle(20);
            NtsSpatialContext ctxNts = (NtsSpatialContext)ctx;
            NtsGeometry rPoly = ctxNts.MakeShape(ctxNts.GetGeometryFrom(r), false, false);
            CustomAssert.EqualWithDelta(r.GetArea(null), rPoly.GetArea(null), 0.0);
            CustomAssert.EqualWithDelta(r.GetArea(ctx), rPoly.GetArea(ctx), 0.000001);//same since fills 100%

            CustomAssert.EqualWithDelta(1300, POLY_SHAPE.GetArea(null), 0.0);

            //fills 27%
            CustomAssert.EqualWithDelta(0.27, POLY_SHAPE.GetArea(ctx) / POLY_SHAPE.BoundingBox.GetArea(ctx), 0.009);
            Assert.True(POLY_SHAPE.BoundingBox.GetArea(ctx) > POLY_SHAPE.GetArea(ctx));
        }

#if FEATURE_XUNIT_1X
        [RepeatFact(100)]
        public virtual void TestPointAndRectIntersect()
#else
        [Repeat(100)]
        [Theory]
        public virtual void TestPointAndRectIntersect(int iterationNumber)
#endif
        {
            IRectangle r = RandomRectangle(5);

            AssertNtsConsistentRelate(r);
            AssertNtsConsistentRelate(r.Center);
        }

        [Fact]
        public virtual void TestRegressions()
        {
            AssertNtsConsistentRelate(new Point(-10, 4, ctx));//PointImpl not NtsPoint, and CONTAINS
            AssertNtsConsistentRelate(new Point(-15, -10, ctx));//point on boundary
            AssertNtsConsistentRelate(ctx.MakeRectangle(135, 180, -10, 10));//180 edge-case
        }

        [Fact]
        public virtual void TestWidthGreaterThan180()
        {
            //does NOT cross the dateline but is a wide shape >180
            NtsGeometry ntsGeo = (NtsGeometry)ctx.ReadShapeFromWkt("POLYGON((-161 49, 0 49, 20 49, 20 89.1, 0 89.1, -161 89.2, -161 49))");
            CustomAssert.EqualWithDelta(161 + 20, ntsGeo.BoundingBox.Width, 0.001);

            //shift it to cross the dateline and check that it's still good
            ntsGeo = ShiftPoly(ntsGeo, 180);
            CustomAssert.EqualWithDelta(161 + 20, ntsGeo.BoundingBox.Width, 0.001);
        }

        private void AssertNtsConsistentRelate(IShape shape)
        {
            IntersectionMatrix expectedM = POLY_SHAPE.Geometry.Relate(((NtsSpatialContext)ctx).GetGeometryFrom(shape));
            SpatialRelation expectedSR = NtsGeometry.IntersectionMatrixToSpatialRelation(expectedM);
            //NTS considers a point on a boundary INTERSECTS, not CONTAINS
            if (expectedSR == SpatialRelation.INTERSECTS && shape is Core.Shapes.IPoint)
                expectedSR = SpatialRelation.CONTAINS;
            AssertRelation(null, expectedSR, POLY_SHAPE, shape);

            if (ctx.IsGeo)
            {
                //shift shape, set to shape2
                IShape shape2;
                if (shape is IRectangle)
                {
                    IRectangle r = (IRectangle)shape;
                    shape2 = MakeNormRect(r.MinX + DL_SHIFT, r.MaxX + DL_SHIFT, r.MinY, r.MaxY);
                }
                else if (shape is Core.Shapes.IPoint)
                {
                    Core.Shapes.IPoint p = (Core.Shapes.IPoint)shape;
                    shape2 = ctx.MakePoint(base.NormX(p.X + DL_SHIFT), p.Y);
                }
                else
                {
                    throw new Exception("" + shape);
                }

                AssertRelation(null, expectedSR, POLY_SHAPE_DL, shape2);
            }
        }

        [Fact]
        public virtual void TestRussia()
        {
            string wktStr = ReadFirstLineFromRsrc("russia.wkt.txt");
            //Russia exercises NtsGeometry fairly well because of these characteristics:
            // * a MultiPolygon
            // * crosses the dateline
            // * has coordinates needing normalization (longitude +180.000xxx)

            //TODO THE RUSSIA TEST DATA SET APPEARS CORRUPT
            // But this test "works" anyhow, and exercises a ton.
            //Unexplained holes revealed via KML export:
            // TODO Test contains: 64°12'44.82"N    61°29'5.20"E
            //  64.21245  61.48475
            // FAILS
            //AssertRelation(null,SpatialRelation.CONTAINS, shape, ctx.makePoint(61.48, 64.21));

            NtsSpatialContextFactory factory = new NtsSpatialContextFactory();
            factory.normWrapLongitude = true;

            NtsSpatialContext ctx = (NtsSpatialContext)factory.NewSpatialContext();

            IShape shape = ctx.ReadShapeFromWkt(wktStr);
            //System.out.println("Russia Area: "+shape.getArea(ctx));
        }

        [Fact]
        public virtual void TestFiji()
        {
            //Fiji is a group of islands crossing the dateline.
            string wktStr = ReadFirstLineFromRsrc("fiji.wkt.txt");

            NtsSpatialContextFactory factory = new NtsSpatialContextFactory();
            factory.normWrapLongitude = true;
            NtsSpatialContext ctx = (NtsSpatialContext)factory.NewSpatialContext();

            IShape shape = ctx.ReadShapeFromWkt(wktStr);

            AssertRelation(null, SpatialRelation.CONTAINS, shape,
                    ctx.MakePoint(-179.99, -16.9));
            AssertRelation(null, SpatialRelation.CONTAINS, shape,
                    ctx.MakePoint(+179.99, -16.9));
            Assert.True(shape.BoundingBox.Width < 5);//smart bbox
            Console.WriteLine("Fiji Area: " + shape.GetArea(ctx));
        }

        private string ReadFirstLineFromRsrc(string wktRsrcPath)
        {

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var projectPath = baseDirectory.Substring(0,
                baseDirectory.LastIndexOf("Spatial4n.Tests", StringComparison.OrdinalIgnoreCase));

            var fullPath = Path.Combine(projectPath, "Spatial4n.Tests");
            fullPath = Path.Combine(fullPath, "resources");
            fullPath = Path.Combine(fullPath, wktRsrcPath);

            using (var stream = File.OpenText(fullPath))
            {
                return stream.ReadLine();
            }
        }
    }
}

#endif