using System;
using System.Reflection;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using Xunit;

namespace Spatial4n.Tests.shape
{
	public class TestShapes2D : AbstractTestShapes
	{
		public TestShapes2D()
		{
			BeforeClass();
		}

		protected override SpatialContext GetContext()
		{
			return new SpatialContext(DistanceUnits.CARTESIAN);
		}

		[Fact]
		public void TestSimplePoint()
		{
			Point pt = ctx.MakePoint(0, 0);
			String msg = pt.ToString();

			//test equals & hashcode
			Point pt2 = ctx.MakePoint(0, 0);
			Assert.Equal(/*msg,*/ pt, pt2);
			Assert.Equal(/*msg,*/ pt.GetHashCode(), pt2.GetHashCode());

			Assert.False(pt.HasArea(), msg);
			Assert.Equal(/*msg,*/ pt.GetCenter(), pt);
			Rectangle bbox = pt.GetBoundingBox();
			Assert.False(bbox.HasArea(), msg);
			Assert.Equal(/*msg,*/ pt, bbox.GetCenter());

			AssertRelation(msg, SpatialRelation.CONTAINS, pt, pt2);
			AssertRelation(msg, SpatialRelation.DISJOINT, pt, ctx.MakePoint(0, 1));
			AssertRelation(msg, SpatialRelation.DISJOINT, pt, ctx.MakePoint(1, 0));
			AssertRelation(msg, SpatialRelation.DISJOINT, pt, ctx.MakePoint(1, 1));
		}

		[Fact]
		public void TestSimpleRectangle()
		{
			double[] minXs = new double[] { -1000, -360, -180, -20, 0, 20, 180, 1000 };
			foreach (double minX in minXs)
			{
				double[] widths = new double[] { 0, 10, 180, 360, 400 };
				foreach (double width in widths)
				{
					TestRectangle(minX, width, 0, 0);
					TestRectangle(minX, width, -10, 10);
					TestRectangle(minX, width, 5, 10);
				}
			}

			TestRectIntersect();
		}

		[Fact]
		public void TestSimpleCircle()
		{
			double[] theXs = new double[] { -10, 0, 10 };
			foreach (double x in theXs)
			{
				double[] theYs = new double[] { -20, 0, 20 };
				foreach (double y in theYs)
				{
					TestCircle(x, y, 0);
					TestCircle(x, y, 5);
				}
			}
			//INTERSECTION:
			//Start with some static tests that have shown to cause failures at some point:
			Assert.Equal( /*"getX not getY",*/
				SpatialRelation.INTERSECTS,
				ctx.MakeCircle(107, -81, 147).Relate(ctx.MakeRect(92, 121, -89, 74), ctx));

			TestCircleIntersect();
		}

		public static void CheckShapesImplementEquals(Type[] classes)
		{
			foreach (var clazz in classes)
			{
				try
				{
					//getDeclaredMethod( "equals", Object.class );
					var method = clazz.GetMethod("Equals", new[] { typeof(Object) });
				}
				catch (Exception)
				{
					//We want the equivalent of Assert.Fail(msg)
					Assert.True(false, "Shape needs to define 'equals' : " + clazz.Name);
				}
				try
				{
					//clazz.getDeclaredMethod( "hashCode" );
					var method = clazz.GetMethod("GetHashCode", BindingFlags.Public | BindingFlags.Instance);
				}
				catch (Exception)
				{
					//We want the equivalent of Assert.Fail(msg)
					Assert.True(false, "Shape needs to define 'hashCode' : " + clazz.Name);
				}
			}
		}

		[Fact]
		public void TestImplementsEqualsAndHash()
		{
			TestShapes2D.CheckShapesImplementEquals(new[]
                                    {
                                        typeof(PointImpl),
                                        typeof(CircleImpl),
										//GeoCircle.class  no: its fields are caches, not part of its identity
                                        typeof(RectangleImpl),
                                        typeof(MultiShape),
                                    });
		}
	}
}
