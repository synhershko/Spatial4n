using System;
using System.Collections.Generic;
using System.IO;
using GeoAPI.Geometries;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using Spatial4n.Core.Shapes.Nts;
using Xunit;

namespace Spatial4n.Tests.shape
{
	public class NtsPolygonTest : AbstractTestShapes
	{
		private String POLY_STR = "Polygon((-10 30, -40 40, -10 -20, 40 20, 0 0, -10 30))";
		private NtsGeometry POLY_SHAPE;
		public static int DL_SHIFT = 180; //since POLY_SHAPE contains 0 0, I know a shift of 180 will make it cross the DL.
		private NtsGeometry POLY_SHAPE_DL; //POLY_SHAPE shifted by DL_SHIFT to cross the dateline

		private bool TEST_DL_POLY = true;
		//TODO poly.relate(circle) doesn't work when other crosses the dateline
		private bool TEST_DL_OTHER = true;

		public static IEnumerable<object[]> Contexts
		{
			get
			{
				yield return new object[] { NtsSpatialContext.GEO };
			}
		}

		public NtsPolygonTest()
			: base(NtsSpatialContext.GEO)
		{
			POLY_SHAPE = (NtsGeometry) ctx.ReadShape(POLY_STR);

			if (TEST_DL_POLY && ctx.IsGeo())
			{
				var pGeom = POLY_SHAPE.GetGeom();
				Assert.True(pGeom.IsValid);
				//shift 180 to the right
				pGeom = (IGeometry) pGeom.Clone();
				pGeom.Apply(new NtsPolygonTestCoordinateFilter(this));
				pGeom.GeometryChanged();
				Assert.False(pGeom.IsValid);
				POLY_SHAPE_DL = (NtsGeometry) ctx.ReadShape(pGeom.AsText());
				Assert.True(
					POLY_SHAPE_DL.GetBoundingBox().GetCrossesDateLine() ||
					360 == POLY_SHAPE_DL.GetBoundingBox().GetWidth());
			}
		}

		[Fact]
		public void testArea()
		{
			//simple bbox
			Rectangle r = RandomRectangle(20);
			var ctxJts = (NtsSpatialContext) ctx;
			var rPoly = new NtsGeometry(ctxJts.GetGeometryFrom(r), ctxJts, false);
			CustomAssert.EqualWithDelta(r.GetArea(null), rPoly.GetArea(null), 0.0);
			CustomAssert.EqualWithDelta(r.GetArea(ctx), rPoly.GetArea(ctx), 0.000001); //same since fills 100%

			CustomAssert.EqualWithDelta(1300, POLY_SHAPE.GetArea(null), 0.0);

			//fills 27%
			CustomAssert.EqualWithDelta(0.27, POLY_SHAPE.GetArea(ctx)/POLY_SHAPE.GetBoundingBox().GetArea(ctx), 0.009);
			Assert.True(POLY_SHAPE.GetBoundingBox().GetArea(ctx) > POLY_SHAPE.GetArea(ctx));
		}


		[RepeatTest(100)]
		public void testPointAndRectIntersect()
		{
			Rectangle r = null;
			do
			{
				r = RandomRectangle(2);
			} while (!TEST_DL_OTHER && r.GetCrossesDateLine());

			assertJtsConsistentRelate(r);
			assertJtsConsistentRelate(r.GetCenter());
		}

		[Fact]
		public void testRegressions()
		{
			assertJtsConsistentRelate(new PointImpl(-10, 4, ctx)); //PointImpl not JtsPoint, and CONTAINS
			assertJtsConsistentRelate(new PointImpl(-15, -10, ctx)); //point on boundary
			assertJtsConsistentRelate(ctx.MakeRectangle(135, 180, -10, 10)); //180 edge-case
		}

		private void assertJtsConsistentRelate(Shape shape)
		{
			IntersectionMatrix expectedM = POLY_SHAPE.GetGeom().Relate(((NtsSpatialContext) ctx).GetGeometryFrom(shape));
			SpatialRelation expectedSR = NtsGeometry.IntersectionMatrixToSpatialRelation(expectedM);
			//JTS considers a point on a boundary INTERSECTS, not CONTAINS
			if (expectedSR == SpatialRelation.INTERSECTS && shape is Point)
				expectedSR = SpatialRelation.CONTAINS;
			assertRelation(null, expectedSR, POLY_SHAPE, shape);

			if (TEST_DL_POLY && ctx.IsGeo())
			{
				//shift shape, set to shape2
				Shape shape2;
				if (shape is Rectangle)
				{
					Rectangle r = (Rectangle) shape;
                    shape2 = makeNormRect(r.GetMinX() + DL_SHIFT, r.GetMaxX() + DL_SHIFT, r.GetMinY(), r.GetMaxY());
					if (!TEST_DL_OTHER && shape2.GetBoundingBox().GetCrossesDateLine())
						return;
				}
				else if (shape is Point)
				{
					Point p = (Point) shape;
                    shape2 = ctx.MakePoint(normX(p.GetX() + DL_SHIFT), p.GetY());
				}
				else
				{
					throw new Exception("" + shape);
				}

				assertRelation(null, expectedSR, POLY_SHAPE_DL, shape2);
			}
		}


		[Fact]
		public void testRussia()
		{
			//TODO THE RUSSIA TEST DATA SET APPEARS CORRUPT
			// But this test "works" anyhow, and exercises a ton.

			//Russia exercises JtsGeometry fairly well because of these characteristics:
			// * a MultiPolygon
			// * crosses the dateline
			// * has coordinates needing normalization (longitude +180.000xxx)
			// * some geometries might(?) not be "valid" (requires union to overcome)
			String wktStr = readFirstLineFromRsrc("russia.wkt.txt");

			NtsGeometry jtsGeom = (NtsGeometry) ctx.ReadShape(wktStr);

			//Unexplained holes revealed via KML export:
			// TODO Test contains: 64°12'44.82"N    61°29'5.20"E
			//  64.21245  61.48475
			// FAILS
			//assertRelation(null,SpatialRelation.CONTAINS, jtsGeom, ctx.makePoint(61.48, 64.21));
		}

		[Fact]
		public void testFiji()
		{
			//Fiji is a group of islands crossing the dateline.
			String wktStr = readFirstLineFromRsrc("fiji.wkt.txt");

			var jtsGeom = (NtsGeometry) ctx.ReadShape(wktStr);

			assertRelation(null, SpatialRelation.CONTAINS, jtsGeom,
			               ctx.MakePoint(-179.99, -16.9));
			assertRelation(null, SpatialRelation.CONTAINS, jtsGeom,
			               ctx.MakePoint(+179.99, -16.9));
		}

		private static String readFirstLineFromRsrc(String wktRsrcPath)
		{
			var projectPath = AppDomain.CurrentDomain.BaseDirectory.Substring(0,
				AppDomain.CurrentDomain.BaseDirectory.LastIndexOf("Spatial4n.Tests", StringComparison.InvariantCultureIgnoreCase));

			var fullPath = Path.Combine(projectPath, "Spatial4n.Tests");
			fullPath = Path.Combine(fullPath, "resources");
			fullPath = Path.Combine(fullPath, wktRsrcPath);

			using (var stream = File.OpenText(fullPath))
			{
				return stream.ReadLine();
			}
		}

        public class NtsPolygonTestCoordinateFilter : ICoordinateFilter
        {
            private readonly NtsPolygonTest _enclosingInstance;

            public NtsPolygonTestCoordinateFilter(NtsPolygonTest enclosingInstance)
            {
                _enclosingInstance = enclosingInstance;
            }

            public void Filter(Coordinate coord)
            {
                coord.X = _enclosingInstance.normX(coord.X + NtsPolygonTest.DL_SHIFT);
            }
        }
	}
}
